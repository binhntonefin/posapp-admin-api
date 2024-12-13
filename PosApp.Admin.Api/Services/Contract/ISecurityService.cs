using URF.Core.EF.Trackable.Models;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface ISecurityService
    {
        ResultApi Permissions();
        ResultApi LinkPermissions();
        ResultApi RolePermissions(int? roleId);
        ResultApi Profile(string search = default);

        Task<ResultApi> SendVerifyCodeAsync(int id);
        Task<ResultApi> VerifyCodeAsync(string code);
        Task<ResultApi> SendVerifyCodeAsync(string email);

        Task<ResultApi> AdminSignInAsync(AdminUserLoginModel model);
        Task<ResultApi> AdminSignOutAsync(AdminUserLoginModel model);
        Task<ResultApi> AdminAddOrUpdateAsync(AdminUserUpdateModel model);
        Task<ResultApi> AdminSetPasswordAsync(AdminUserSetPasswordModel model);
        Task<ResultApi> AdminResetPasswordAsync(AdminUserResetPasswordModel model);
        Task<ResultApi> AdminChangePasswordAsync(AdminUserChangePasswordModel model);
        Task<ResultApi> AdminForgotPasswordAsync(AdminUserForgotPasswordModel model);
        Task<ResultApi> AdminRegisterAndLoginSocicalAsync(AdminExternalAuthModel model);
    }
}
