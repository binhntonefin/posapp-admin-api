using URF.Core.EF.Trackable.Models;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface IDepartmentService
    {
        public Task<ResultApi> Trash(int id);
        public Task<ResultApi> AddUsers(int id, List<int> items);
        public Task<ResultApi> UpdateUsers(int id, List<int> items);
        public Task<ResultApi> AddOrUpdateAsync(DepartmentModel model);
    }
}
