using URF.Core.EF.Trackable.Models;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface IUserService
    {
        List<int> AllUserInTeam();
        List<int> AllUserInDepartment();
        ResultApi AllUsers(int? roleId = null);
        ResultApi AllUsersByRoleId(int roleId);
        ResultApi AllUsersByTeamId(int teamId);
        Task<ResultApi> UnLockAsync(int userId);
        Task<ResultApi> ResetPasswordAsync(string email);
        List<int> AllUsersByManagerId(string controller);
        ResultApi AllUsersByDepartmentId(int departmentId);
        ResultApi GetProfileByVerifyCode(string verifyCode);
        Task<ResultApi> UpdateProfile(AdminUserUpdateModel model);
        ResultApi AllUsersByDepartmentIds(List<int> departmentIds);
        Task<ResultApi> LockAsync(int userId, AdminUserLockModel model);
    }
}
