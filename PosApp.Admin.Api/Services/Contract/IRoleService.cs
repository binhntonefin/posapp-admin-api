using URF.Core.EF.Trackable.Models;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface IRoleService
    {
        public Task<ResultApi> Trash(int id);
        public ResultApi AllRoles(int? userId);
        public Task<ResultApi> AddUsers(int id, List<int> items);
        public Task<ResultApi> UpdateUsers(int id, List<int> items);
        public Task<ResultApi> AddOrUpdateRoleAsync(RoleModel model);
        public Task<ResultApi> UpdatePermissions(int id, List<RolePermissionModel> items);
    }
}
