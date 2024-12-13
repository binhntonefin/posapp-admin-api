using URF.Core.EF.Trackable.Entities.Message;
using URF.Core.EF.Trackable.Entities;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface IRefreshDataService
    {
        public Task RefreshLoadData(string key);
        public Task Notify(int userId, Notify notify);
        public Task Notify(string email, Notify notify);
        public Task Notify(List<int> userIds, Notify notify);
        public Task Notify(List<string> emails, Notify notify);

        public Task SendMessage(Message model);
        public Task SendTeamMessage(Message model);
        public Task SendGroupMessage(Message model);
    }
}
