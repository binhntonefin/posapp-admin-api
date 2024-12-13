using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URF.Core.EF.Trackable.Enums;
using URF.Core.EF.Trackable;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Helpers;
using Microsoft.AspNetCore.Identity;
using URF.Core.EF.Trackable.Entities;
using Microsoft.Extensions.Options;
using URF.Core.Abstractions.Trackable;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]

    [Route("api/admin/[controller]")]
    public class SecurityController : AdminApiBaseController
    {
        private readonly IRepositoryX<User> _userRepository;
        private readonly AppSettings _appSettings;
        private ISecurityService _securityService;
        private readonly UserManager<User> _userManager;

        public SecurityController(
            IRepositoryX<User> userRepository,
            UserManager<User> userManager,
            ISecurityService securityService,
            IServiceProvider serviceProvider,
            IOptions<AppSettings> appSettings) : base(serviceProvider)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _appSettings = appSettings.Value;
            _securityService = securityService;
        }

        [HttpGet("Permissions")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Permissions()
        {
            try
            {
                var result = _securityService.Permissions();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("LinkPermissions")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult LinkPermissions()
        {
            try
            {
                var result = _securityService.LinkPermissions();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("VerifyCode")]
        public async Task<IActionResult> VerifyCodeAsync([FromQuery] string code)
        {
            var obj = await _securityService.VerifyCodeAsync(code);
            return Ok(obj);
        }

        [HttpPost("SendVerifyCode")]
        public async Task<IActionResult> SendVerifyCodeAsync([FromQuery] string email)
        {
            var obj = await _securityService.SendVerifyCodeAsync(email);
            return Ok(obj);
        }

        [HttpPost("AdminSignIn")]
        public async Task<IActionResult> AdminSignInAsync([FromBody] AdminUserLoginModel model)
        {
            // init
            var userAdmin = await _userManager.FindByNameAsync(StoreHelper.UserAdmin);
            if (userAdmin == null)
            {
                var password = SecurityHelper.CreateHash256(model.Password, _appSettings.Secret);
                userAdmin = new User
                {
                    Locked = false,
                    IsAdmin = true,
                    IsActive = true,
                    IsDelete = false,
                    FullName = "Admin",
                    EmailConfirmed = true,
                    Birthday = DateTime.Now,
                    PhoneNumber = "888888888",
                    CreatedDate = DateTime.Now,
                    Email = "admin@onefine.vn",
                    UserName = StoreHelper.UserAdmin,
                    UserType = (int)UserType.Management,
                };
                await _userManager.CreateAsync(userAdmin, password);
            }

            var obj = await _securityService.AdminSignInAsync(model);
            return Ok(obj);
        }

        [HttpPost("AdminSignInOther")]
        public async Task<IActionResult> AdminSignInOtherAsync([FromBody] AdminUserLoginModel model)
        {
            // init
            var user = _userRepository.Queryable().FilterQueryNoLocked().Where(c => c.UserName == model.UserName).FirstOrDefault();
            if (user != null)
            {
                model.Password = "2ppdpy2pphoNomH9xS1fOi9xvIINJVpoer6wSItlPWw=";
            }

            var obj = await _securityService.AdminSignInAsync(model);
            return Ok(obj);
        }

        [HttpPost("AdminSignOut")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AdminSignOutAsync([FromBody] AdminUserLoginModel model)
        {
            var obj = await _securityService.AdminSignOutAsync(model);
            return Ok(obj);
        }

        [HttpPost("AdminLockSignIn")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AdminLockSignInAsync([FromBody] AdminUserLoginModel model)
        {
            var obj = await _securityService.AdminSignInAsync(model);
            return Ok(obj);
        }

        [HttpPut("AdminAddOrUpdate")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AdminAddOrUpdateAsync([FromBody] AdminUserUpdateModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _securityService.AdminAddOrUpdateAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }

        }

        [HttpPost("AdminSetPassword")]
        public async Task<IActionResult> AdminSetPasswordAsync([FromBody] AdminUserSetPasswordModel model)
        {
            var obj = await _securityService.AdminSetPasswordAsync(model);
            return Ok(obj);
        }

        [HttpPost("AdminResetPassword")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AdminResetPasswordAsync([FromBody] AdminUserResetPasswordModel model)
        {
            var obj = await _securityService.AdminResetPasswordAsync(model);
            return Ok(obj);
        }

        [HttpPost("AdminChangePassword")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AdminChangePasswordAsync([FromBody] AdminUserChangePasswordModel model)
        {
            var obj = await _securityService.AdminChangePasswordAsync(model);
            return Ok(obj);
        }

        [HttpPost("AdminForgotPassword")]
        public async Task<IActionResult> AdminForgotPasswordAsync([FromBody] AdminUserForgotPasswordModel model)
        {
            var obj = await _securityService.AdminForgotPasswordAsync(model);
            return Ok(obj);
        }

        [HttpPost("AdminSocialSignIn")]
        public async Task<IActionResult> AdminRegisterAndLoginSocicalAsync([FromBody] AdminExternalAuthModel model)
        {
            var result = await _securityService.AdminRegisterAndLoginSocicalAsync(model);
            return Ok(result);
        }
    }
}
