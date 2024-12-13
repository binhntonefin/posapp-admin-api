using URF.Core.Helper.Extensions;
using Microsoft.AspNetCore.SignalR;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.Services.Hubs;
using URF.Core.EF.Trackable.Entities;
using URF.Core.EF.Trackable.Entities.Message;
using URF.Core.EF.Trackable.Entities.Message.Models;
using PosApp.Admin.Api.Helpers;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using URF.Core.Abstractions.Trackable;
using URF.Core.Abstractions;

namespace PosApp.Admin.Api.Services.Implement
{
    public class RefreshDataService : ServiceX, IRefreshDataService
    {
        private readonly INotifyHub _notifyHub;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepositoryX<User> _userRepository;
        private readonly IHubContext<NotifyHub> _hubContext;
        private readonly IRepositoryX<LogActivity> _logActivityRepository;

        public RefreshDataService(
            INotifyHub notifyHub,
            IUnitOfWork unitOfWork,
            IRepositoryX<User> userRepository,
            IHubContext<NotifyHub> hubContext,
            IHttpContextAccessor httpContextAccessor,
            IRepositoryX<LogActivity> logActivityRepository) : base(httpContextAccessor)
        {
            _notifyHub = notifyHub;
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _userRepository = userRepository;
            _logActivityRepository = logActivityRepository;
        }

        public async Task RefreshLoadData(string key)
        {
            // send
            await _hubContext.Clients.All.SendAsync("refreshData", new
            {
                Key = key,
            });
        }

        public async Task Notify(int userId, Notify notify)
        {
            var ids = _notifyHub.GetConnectionIdById(userId);
            if (!ids.IsNullOrEmpty())
            {
                await _hubContext.Clients.Clients(ids).SendAsync("notify", notify);
            }
        }

        public async Task Notify(string email, Notify notify)
        {
            var ids = _notifyHub.GetConnectionId(email);
            if (!ids.IsNullOrEmpty())
            {
                await _hubContext.Clients.Clients(ids).SendAsync("notify", notify);
            }
        }

        public async Task Notify(List<int> userIds, Notify notify)
        {
            var connectionIds = _notifyHub.GetConnectionIdByIds(userIds);
            if (!connectionIds.IsNullOrEmpty())
            {
                await _hubContext.Clients.Clients(connectionIds).SendAsync("notify", notify);
            }
        }

        public async Task Notify(List<string> emails, Notify notify)
        {
            var connectionIds = _notifyHub.GetConnectionIds(emails);
            if (!connectionIds.IsNullOrEmpty())
            {
                await _hubContext.Clients.Clients(connectionIds).SendAsync("notify", notify);
            }
        }

        public async Task SendMessage(Message model)
        {
            var receiveId = model.ReceiveId ?? 0;
            var ids = _notifyHub.GetConnectionIdById(receiveId);
            if (!ids.IsNullOrEmpty())
            {
                await _hubContext.Clients.Clients(ids).SendAsync("chat", new
                {
                    model.Status,
                    model.SendId,
                    model.IsRead,
                    model.Content,
                    Right = false,
                    model.DateTime,
                    model.ReceiveId,
                    SendName = User.FullName,
                    SendAvatar = User.Avatar,
                    Files = model.Files.ToObject<List<MessageFileData>>()
                });
            }
        }

        public async Task SendTeamMessage(Message model)
        {
            if (model != null && model.TeamId.HasValue)
            {
                // get Ids
                var ids = _notifyHub.GetConnectionIdsByTeam(model.TeamId.Value);
                var curentIds = _notifyHub.GetConnectionIdById(UserId);
                ids.RemoveAll(c => curentIds.Contains(c));

                // send
                await _hubContext.Clients.Clients(ids).SendAsync("chat", new
                {
                    model.Status,
                    model.SendId,
                    model.IsRead,
                    model.TeamId,
                    model.Content,
                    Right = false,
                    model.DateTime,
                    SendName = User.FullName,
                    SendAvatar = User.Avatar,
                    Files = model.Files.ToObject<List<MessageFileData>>()
                });
            }
        }

        public async Task SendGroupMessage(Message model)
        {
            if (model != null && model.GroupId.HasValue)
            {
                // get Ids
                var ids = _notifyHub.GetConnectionIdsByGroup(model.GroupId.Value);
                var curentIds = _notifyHub.GetConnectionIdById(UserId);
                ids.RemoveAll(c => curentIds.Contains(c));

                // send
                await _hubContext.Clients.Clients(ids).SendAsync("chat", new
                {
                    model.Status,
                    model.SendId,
                    model.IsRead,
                    model.GroupId,
                    model.Content,
                    Right = false,
                    model.DateTime,
                    SendName = User.FullName,
                    SendAvatar = User.Avatar,
                    Files = model.Files.ToObject<List<MessageFileData>>()
                });
            }
        }

        private async Task<string> SendFirebaseNotifyAsync(Notify notify, string data, string token)
        {
            try
            {
                // add log
                var log = new LogActivity
                {
                    Body = notify.ToJson(),
                    DateTime = DateTime.Now,
                    Notes = data + "_" + token,
                    Controller = "NotifyFirebase",
                    Action = "SendFirebaseNotifyAsync",
                };
                _logActivityRepository.Insert(log);
                await _unitOfWork.SaveChangesAsync();

                if (StoreHelper.FirebaseApp == null)
                {
                    StoreHelper.FirebaseApp = FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile("firebase.json"),
                    });
                }

                var obj = new FirebaseAdmin.Messaging.Message
                {
                    Token = token,
                    Apns = new FirebaseAdmin.Messaging.ApnsConfig
                    {
                        Aps = new FirebaseAdmin.Messaging.Aps
                        {
                            Sound = "alert.caf",
                            ThreadId = "LazyPos",
                            Category = "LazyPos",
                            ContentAvailable = true,
                            AlertString = "alert.caf",
                            CustomData = new Dictionary<string, object>
                            {
                                { "action", data },
                                { "data", notify.ToJson() }
                            },
                        },
                        CustomData = new Dictionary<string, object>
                        {
                            { "action", data },
                            { "data", notify.ToJson() }
                        },
                    },
                    Android = new FirebaseAdmin.Messaging.AndroidConfig
                    {
                        TimeToLive = TimeSpan.FromSeconds(900),
                        Priority = FirebaseAdmin.Messaging.Priority.High,
                        Notification = new FirebaseAdmin.Messaging.AndroidNotification
                        {
                            Sound = "alert.caf",
                            Title = notify.Title,
                            Body = notify.Content,
                            ChannelId = "LazyPos",
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                        },
                        Data = new Dictionary<string, string>
                        {
                            { "action", data },
                            { "data", notify.ToJson() }
                        },
                    },
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = notify.Title,
                        Body = notify.Content,
                    },
                    Webpush = new FirebaseAdmin.Messaging.WebpushConfig
                    {
                        Data = new Dictionary<string, string>
                        {
                            { "action", data },
                            { "data", notify.ToJson() }
                        },
                        Notification = new FirebaseAdmin.Messaging.WebpushNotification
                        {
                            Title = notify.Title,
                            Body = notify.Content,
                        }
                    },
                    Data = new Dictionary<string, string>
                    {
                        { "action", data },
                        { "data", notify.ToJson() }
                    },
                };

                // send message
                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
                var result = await messaging.SendAsync(obj);
                return result;
            }
            catch
            {
                return string.Empty;
            }
        }

    }
}
