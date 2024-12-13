using URF.Core.EF.Trackable.Enums;
using URF.Core.EF.Trackable.Models;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface IPermissionService
    {
        public ResultApi AllPermissions(int? roleId);
        public ResultApi AllPermissions(int? userId, List<int> roleIds);
        bool Allow(string controller, string action, int? userId = null);
        public PermissionType GetPermissionType(string controller, string action = "View");
    }
}
