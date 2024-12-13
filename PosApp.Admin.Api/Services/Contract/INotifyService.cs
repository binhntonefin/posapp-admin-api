using URF.Core.EF.Trackable.Models;
using URF.Core.EF.Trackable.Constants;
using URF.Core.EF.Trackable.Entities;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface INotifyService
    {
        Task<ResultApi> ReadNotifyAsync(int id);
        Task<ResultApi> AddNotifyAsync(Notify entity, List<int> userIds = null);
        ResultApi MyNotifies(int pageIndex = 1, int pageSize = Constant.PAGESIZE);
        Task<ResultApi> AddNotifyByEmailAsync(Notify entity, List<string> emails);
        ResultApi MyNotifiesUnRead(int pageIndex = 1, int pageSize = Constant.PAGESIZE);
    }
}
