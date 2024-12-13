using Microsoft.AspNetCore.Identity;
using URF.Core.Abstractions;
using URF.Core.EF.Trackable.Models;
using URF.Core.EF.Trackable.Constants;
using URF.Core.Helper.Helpers;
using URF.Core.Helper;
using URF.Core.Helper.Extensions;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.EF.Trackable.Entities;
using URF.Core.Abstractions.Trackable;
using PosApp.Admin.Api.Data.Models;
using URF.Core.EF.Trackable.Enums;

namespace PosApp.Admin.Api.Services.Implement
{
    public class NotifyService : ServiceX, INotifyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly IRepositoryX<Notify> _repository;
        private readonly IRefreshDataService _refreshDataService;

        public NotifyService(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            IRepositoryX<Notify> repository,
            IRefreshDataService refreshDataService,
            IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _userManager = userManager;
            _refreshDataService = refreshDataService;
        }

        public async Task<ResultApi> ReadNotifyAsync(int id)
        {
            var notify = _repository.Queryable().FirstOrDefault(c => c.Id == id);
            if (notify == null)
                return ResultApi.ToError(ErrorResult.Notify.NotExists);
            if (notify.UserId != UserId)
                return ResultApi.ToError(ErrorResult.DataInvalid);

            notify.IsRead = true;
            _repository.Update(notify);
            await _unitOfWork.SaveChangesAsync();

            var model = Mapper.Map<NotifyModel>(notify);
            model.RelativeTime = UtilityHelper.ToRelativeTime(notify.DateTime);
            return ResultApi.ToEntity(model);
        }

        public ResultApi MyNotifies(int pageIndex = 1, int pageSize = Constant.PAGESIZE)
        {
            var skip = (pageIndex - 1) * pageSize;
            var items = _repository.Queryable().FilterQueryNoTraking()
                .Where(c => c.UserId == UserId)
                .OrderByDescending(c => c.Id)
                .Skip(skip).Take(pageSize).ToList()
                .Select(c =>
                {
                    var model = Mapper.Map<NotifyModel>(c);
                    model.RelativeTime = UtilityHelper.ToRelativeTime(c.DateTime);
                    return model;
                })
                .ToList();

            var query = _repository.Queryable().FilterQueryNoTraking()
                .Where(c => c.UserId == UserId)
                .Where(c => !c.IsRead);
            var count = query.Count();
            return ResultApi.ToEntity(items, count);
        }

        public async Task<ResultApi> AddNotifyAsync(Notify entity, List<int> userIds = null)
        {
            if (entity != null)
            {
                if (userIds.IsNullOrEmpty()) userIds = new List<int> { entity.UserId ?? 0 };
                var ignoreTypes = new List<int> { (int)NotifyType.Answer };
                if (!ignoreTypes.Contains(entity.Type))
                {
                    foreach (var userId in userIds.Where(c => !c.IsNumberNull()))
                    {
                        var notify = new Notify
                        {
                            IsRead = false,
                            IsActive = true,
                            UserId = userId,
                            IsDelete = false,
                            Type = entity.Type,
                            Title = entity.Title,
                            DateTime = DateTime.Now,
                            Content = entity.Content,
                            CreatedDate = DateTime.Now,
                            JsonObject = entity.JsonObject,
                            CreatedBy = UserId.IsNumberNull() ? null : (int?)UserId,
                        };
                        _repository.Insert(notify);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                var notifyModel = Mapper.Map<NotifyModel>(entity);
                notifyModel.RelativeTime = UtilityHelper.ToRelativeTime(entity.DateTime);
                await _refreshDataService.Notify(userIds, entity);
                return ResultApi.ToEntity(notifyModel);
            }
            return null;
        }

        public ResultApi MyNotifiesUnRead(int pageIndex = 1, int pageSize = Constant.PAGESIZE)
        {
            var skip = (pageIndex - 1) * pageSize;
            var query = _repository.Queryable()
                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                .Where(c => c.IsActive.HasValue && c.IsActive.Value)
                .Where(c => c.UserId == UserId)
                .Where(c => !c.IsRead);

            var count = query.Count();
            var items = query.OrderByDescending(c => c.Id)
                .Skip(skip).Take(pageSize).ToList()
                .Select(c =>
                {
                    var model = Mapper.Map<NotifyModel>(c);
                    model.RelativeTime = UtilityHelper.ToRelativeTime(c.DateTime);
                    return model;
                })
                .ToList();
            return ResultApi.ToEntity(items, count);
        }

        public async Task<ResultApi> AddNotifyByEmailAsync(Notify entity, List<string> emails)
        {
            if (entity != null)
            {
                var ignoreTypes = new List<int> { (int)NotifyType.Answer };
                if (!ignoreTypes.Contains(entity.Type))
                {
                    foreach (var email in emails.Where(c => !c.IsStringNullOrEmpty()))
                    {
                        var user = await _userManager.FindByEmailAsync(email);
                        if (user == null) continue;
                        var notify = new Notify
                        {
                            IsRead = false,
                            IsActive = true,
                            UserId = user.Id,
                            IsDelete = false,
                            CreatedBy = UserId,
                            Type = entity.Type,
                            Title = entity.Title,
                            DateTime = DateTime.Now,
                            Content = entity.Content,
                            CreatedDate = DateTime.Now,
                            JsonObject = entity.JsonObject,
                        };
                        _repository.Insert(notify);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                var notifyModel = Mapper.Map<NotifyModel>(entity);
                notifyModel.RelativeTime = UtilityHelper.ToRelativeTime(entity.DateTime);
                await _refreshDataService.Notify(emails, entity);
                return ResultApi.ToEntity(notifyModel);
            }
            return null;
        }
    }
}
