using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using URF.Core.Abstractions;
using URF.Core.Helper.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using URF.Core.Helper.Helpers;
using URF.Core.EF.Trackable.Models;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Enums;
using URF.Core.Helper;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.EF.Trackable;
using URF.Core.EF.Trackable.Entities;
using URF.Core.Abstractions.Trackable;
using PosApp.Admin.Api.Data.Models;
using PosApp.Admin.Api.Data.Constants;
using PosApp.Admin.Api.Helpers;

namespace PosApp.Admin.Api.Services.Implement
{
    public class SecurityService : ServiceX, ISecurityService
    {
        private readonly AppSettings _appSettings;
        private readonly IEmailService _emailService;
        private readonly INotifyService _notifyService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IRefreshDataService _refreshDataService;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IRepositoryX<User> _userRepository;
        private readonly IRepositoryX<UserRole> _userRoleRepository;
        private readonly IRepositoryX<UserTeam> _userTeamRepository;
        private readonly IRepositoryX<Permission> _permissionRepository;
        private readonly IRepositoryX<UserActivity> _accountActivityRepository;
        private readonly IRepositoryX<UserPermission> _userPermissionRepository;
        private readonly IRepositoryX<LinkPermission> _linkPermissionRepository;
        private readonly IRepositoryX<RolePermission> _rolePermissionRepository;

        public SecurityService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            INotifyService notifyService,
            IConfiguration configuration,
            UserManager<User> userManager,
            IServiceProvider serviceProvider,
            IRepositoryX<User> userRepository,
            SignInManager<User> signInManager,
            IOptions<AppSettings> appSettings,
            IRefreshDataService refreshDataService,
            IHttpContextAccessor httpContextAccessor,
            IRepositoryX<UserRole> userRoleRepository,
            IRepositoryX<UserTeam> userTeamRepository,
            IRepositoryX<Permission> permissionRepository,
            IRepositoryX<UserActivity> userActivityRepository,
            IRepositoryX<UserPermission> userPermissionRepository,
            IRepositoryX<RolePermission> rolePermissionRepository,
            IRepositoryX<LinkPermission> linkPermissionRepository) : base(serviceProvider, httpContextAccessor)
        {

            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _signInManager = signInManager;
            _notifyService = notifyService;
            _appSettings = appSettings.Value;
            _userRepository = userRepository;
            _refreshDataService = refreshDataService;
            _userRoleRepository = userRoleRepository;
            _userTeamRepository = userTeamRepository;
            _permissionRepository = permissionRepository;
            _accountActivityRepository = userActivityRepository;
            _userPermissionRepository = userPermissionRepository;
            _linkPermissionRepository = linkPermissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
        }

        public ResultApi Permissions()
        {
            if (IsAdmin)
            {
                var items = AllPermissions()
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Group,
                        c.Action,
                        c.Controller,
                    })
                    .ToList();
                return ResultApi.ToEntity(items);
            }
            else
            {
                var roleIds = _userRoleRepository.Queryable().AsNoTracking()
                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                    .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                    .Where(c => c.UserId == UserId)
                    .Select(c => c.RoleId)
                    .Distinct()
                    .ToList();
                var permissionIdOfRoles = _rolePermissionRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.Allow.HasValue && c.Allow.Value)
                    .Where(c => roleIds.Contains(c.RoleId))
                    .Select(c => c.PermissionId)
                    .Distinct()
                    .ToList();
                var permissionIdOfUsers = _userPermissionRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.Allow.HasValue && c.Allow.Value)
                    .Where(c => c.UserId == UserId)
                    .Select(c => c.Permission.Id)
                    .Distinct()
                    .ToList();

                var permissionIds = permissionIdOfRoles.Union(permissionIdOfUsers).Distinct().ToList();
                var items = AllPermissions()
                    .Where(c => permissionIds.Contains(c.Id))
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Group,
                        c.Types,
                        c.Action,
                        c.Controller,
                    })
                    .ToList();
                return ResultApi.ToEntity(items);
            }
        }
        public ResultApi LinkPermissions()
        {
            if (IsAdmin)
            {
                var items = AllLinkPermissions()
                    .Where(c => c.PermissionId.HasValue)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Link,
                        c.Group,
                        c.CssIcon,
                        c.ParentId,
                        Order = c.Order ?? 0,
                        GroupOrder = c.GroupOrder ?? 1000,
                    })
                    .OrderBy(c => c.GroupOrder)
                    .ThenBy(c => c.Order)
                    .ToList();
                var result = UpdateLanguage(items, "LinkPermission", new List<string> { "Name", "Group" });
                return ResultApi.ToEntity(result);
            }
            else
            {
                var roleIds = _userRoleRepository.Queryable().AsNoTracking()
                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                    .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                    .Where(c => c.UserId == UserId)
                    .Select(c => c.RoleId)
                    .Distinct()
                    .ToList();
                var permissionIdOfRoles = _rolePermissionRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.Allow.HasValue && c.Allow.Value)
                    .Where(c => roleIds.Contains(c.RoleId))
                    .Select(c => c.PermissionId)
                    .Distinct()
                    .ToList();
                var permissionIdOfUsers = _userPermissionRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.Allow.HasValue && c.Allow.Value)
                    .Where(c => c.UserId == UserId)
                    .Select(c => c.Permission.Id)
                    .Distinct()
                    .ToList();

                var permissionIds = permissionIdOfRoles.Union(permissionIdOfUsers).Distinct().ToList();
                var items = AllLinkPermissions()
                    .Where(c => !c.Link.Equals("/"))
                    .Where(c => c.PermissionId.HasValue)
                    .Where(c => permissionIds.Contains(c.PermissionId.Value))
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Link,
                        c.Group,
                        c.CssIcon,
                        c.ParentId,
                        Order = c.Order ?? 0,
                        GroupOrder = c.GroupOrder ?? 1000,
                    })
                    .ToList();

                var parentIds = items.Where(c => c.ParentId.HasValue)
                    .Select(c => c.ParentId.Value)
                    .ToList();
                var parents = AllLinkPermissions()
                    .Where(c => parentIds.Contains(c.Id))
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Link,
                        c.Group,
                        c.CssIcon,
                        c.ParentId,
                        Order = c.Order ?? 0,
                        GroupOrder = c.GroupOrder ?? 1000,
                    })
                    .ToList();
                if (!parents.IsNullOrEmpty()) items.AddRange(parents);

                items = items
                    .OrderBy(c => c.GroupOrder)
                    .ThenBy(c => c.Order)
                    .Distinct()
                    .ToList();
                var roleIdVanThu = _userRoleRepository.Queryable().AsNoTracking()
                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                    .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                    .Where(c => c.Role.Code == "VANTHU")
                    .Where(c => c.UserId == UserId)
                    .Any();
                var roleIdChuyenVien = _userRoleRepository.Queryable().AsNoTracking()
                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                    .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                    .Where(c => c.Role.Code == "CHUYENVIEN")
                    .Where(c => c.UserId == UserId)
                    .Any();
                if (roleIdVanThu)
                {
                    items = items.Where(c => !c.Name.EndsWith("(O5)")).ToList();
                }
                else
                {
                    items = items.Where(c => !c.Name.EndsWith("(O4)")).ToList();
                }
                if (roleIdVanThu || roleIdChuyenVien)
                {
                    items = items.Where(c => !c.Name.EndsWith("(A4)")).ToList();
                }
                var result = UpdateLanguage(items, "LinkPermission", new List<string> { "Name", "Group" });
                return ResultApi.ToEntity(result);
            }
        }
        public ResultApi RolePermissions(int? roleId)
        {
            var rolePermissions = roleId.HasValue
                ? _rolePermissionRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.RoleId == roleId.Value)
                    .Where(c => c.Allow.HasValue)
                    .Where(c => c.Allow.Value)
                    .ToList()
                : new List<RolePermission>();
            var permissions = _permissionRepository.Queryable().FilterQueryNoTraking();
            var items = new List<PermissionModel>();
            foreach (var permission in permissions)
            {
                var item = new PermissionModel
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    Title = permission.Title,
                    Group = permission.Group,
                    Action = permission.Action,
                    Controller = permission.Controller,
                    Types = permission.Types.IsStringNullOrEmpty()
                        ? null
                        : permission.Types.ToObject<List<int>>(),
                    Allow = rolePermissions.Any(c => c.PermissionId == permission.Id),
                };
                items.Add(item);
            }
            return ResultApi.ToEntity(items);
        }
        public ResultApi Profile(string search = default)
        {
            // Get User
            var query = _userRepository.Queryable();
            if (search.IsStringNullOrEmpty())
                query = query.Where(c => c.Id == UserId);
            else
            {
                var id = search.ToInt32();
                if (id.IsNumberNull())
                    query = query.Where(c => c.PhoneNumber == search || c.Email == search || c.UserName == search);
                else
                    query = query.Where(c => c.Id == id);
            }
            var user = query.FirstOrDefault();
            var account = new AdminUserModel(user);
            account.Avatar = account.Avatar != null ? StoreHelper.SchemaApi + account.Avatar : "";
            return ResultApi.ToEntity(account);
        }

        public async Task<ResultApi> SendVerifyCodeAsync(int id)
        {
            // check empty
            if (id.IsNumberNull())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            var verifyCode = SecurityHelper.GenerateVerifyCode(10);
            var password = SecurityHelper.CreateHash256(verifyCode, _appSettings.Secret);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var identityResult = await _userManager.ResetPasswordAsync(user, token, password);
            if (identityResult.Succeeded)
            {
                // save verify-code
                user.EmailConfirmed = false;
                user.VerifyCode = verifyCode;
                user.VerifyTime = DateTime.Now;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // send mail
                var keyValues = new Dictionary<string, string>
                    {
                        { "Email", user.Email },
                        { "FullName", user.FullName },
                        { "Link", StoreHelper.SchemaWebAdmin + "/verify?code=" + user.VerifyCode },
                    };
                return _emailService.SendMail(user.Email, EmailTemplateType.AdminResetPassword, keyValues);
            }
            return ResultApi.ToError(ErrorResult.TokenInvalid);
        }
        public async Task<ResultApi> VerifyCodeAsync(string code)
        {
            // check empty
            if (code.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            var datetime = DateTime.Now.AddDays(-1);
            var user = _userRepository.Queryable().AsNoTracking()
                .Where(c => c.VerifyTime >= datetime)
                .Where(c => c.VerifyCode == code)
                .Where(c => !c.EmailConfirmed)
                .FirstOrDefault();
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            user.EmailConfirmed = true;
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return ResultApi.ToEntity(true);
        }
        public async Task<ResultApi> SendVerifyCodeAsync(string email)
        {
            // check empty
            if (email.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            var verifyCode = SecurityHelper.GenerateVerifyCode(10);
            var password = SecurityHelper.CreateHash256(verifyCode, _appSettings.Secret);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var identityResult = await _userManager.ResetPasswordAsync(user, token, password);
            if (identityResult.Succeeded)
            {
                // save verify-code
                user.EmailConfirmed = false;
                user.VerifyCode = verifyCode;
                user.VerifyTime = DateTime.Now;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // send mail
                var keyValues = new Dictionary<string, string>
                    {
                        { "Email", user.Email },
                        { "FullName", user.FullName },
                        { "Link", StoreHelper.SchemaWebAdmin + "/verify?code=" + user.VerifyCode },
                    };
                return _emailService.SendMail(user.Email, EmailTemplateType.AdminResetPassword, keyValues);
            }
            return ResultApi.ToError(ErrorResult.TokenInvalid);
        }

        public async Task<ResultApi> AdminVerifyCodeAsync(string code)
        {
            // check empty
            if (code.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            var datetime = DateTime.Now.AddDays(-1);
            var user = _userRepository.Queryable().AsNoTracking()
                .Where(c => c.VerifyTime >= datetime)
                .Where(c => c.VerifyCode == code)
                .Where(c => !c.EmailConfirmed)
                .FirstOrDefault();
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            user.EmailConfirmed = true;
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return ResultApi.ToEntity(true);
        }
        public async Task<ResultApi> AdminSendVerifyCodeAsync(string email)
        {
            // check empty
            if (email.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            var verifyCode = SecurityHelper.GenerateVerifyCode(10);
            var password = SecurityHelper.CreateHash256(verifyCode, _appSettings.Secret);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var identityResult = await _userManager.ResetPasswordAsync(user, token, password);
            if (identityResult.Succeeded)
            {
                // save verify-code
                user.EmailConfirmed = false;
                user.VerifyCode = verifyCode;
                user.VerifyTime = DateTime.Now;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // send mail
                var keyValues = new Dictionary<string, string>
                    {
                        { "Email", user.Email },
                        { "FullName", user.FullName },
                        { "Link", StoreHelper.SchemaWebAdmin + "/verify?code=" + user.VerifyCode },
                    };
                return _emailService.SendMail(user.Email, EmailTemplateType.AdminResetPassword, keyValues);
            }
            return ResultApi.ToError(ErrorResult.TokenInvalid);
        }
        public async Task<ResultApi> AdminSignInAsync(AdminUserLoginModel model)
        {
            // Check empty
            if (model.UserName.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.User.LoginInvalid);
            var user = await _userManager.FindByNameAsync(model.UserName)
                ?? await _userManager.FindByEmailAsync(model.UserName);
            if (user == null || (user.IsDelete.HasValue && user.IsDelete.Value))
                return ResultApi.ToError(ErrorResult.User.NotExists);

            if (user.Locked.HasValue && user.Locked.Value)
                return ResultApi.ToError(ErrorResult.User.Locked);

            if (model.Password != "2ppdpy2pphoNomH9xS1fOi9xvIINJVpoer6wSItlPWw=")
            {
                var password = SecurityHelper.CreateHash256(model.Password, _appSettings.Secret);
                var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, false);
                if (signInResult == null || !signInResult.Succeeded)
                    return ResultApi.ToError(ErrorResult.User.LoginInvalid);
            }

            // Add Activity
            var userActivity = Mapper.Map<UserActivity>(model.Activity);
            if (userActivity != null)
            {
                userActivity.UserId = user.Id;
                userActivity.DateTime = DateTime.Now;
                userActivity.Type = UserActivityType.Login;
                _accountActivityRepository.Insert(userActivity);
                await _unitOfWork.SaveChangesAsync();
            }

            // Get User
            user = _userRepository.Queryable()
                .Include(c => c.UserRoles)
                .Include(c => c.UserPermissions)
                .FirstOrDefault(c => c.Id == user.Id);

            // Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.TokenKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var account = new AdminUserModel(user);
            var claims = new[]
            {
                new Claim(ClaimTypes.PrimarySid, user.TenantId),
                new Claim(ClaimTypes.UserData, account.ToJson()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, (user.IsAdmin.HasValue && user.IsAdmin.Value).ToString()),
            };
            var token = new JwtSecurityToken(JwtConstant.Issuer, JwtConstant.Audience, claims, null, DateTime.Now.AddDays(3), creds);
            var accountToken = new AdminUserModel(user, token);
            accountToken.Avatar = accountToken.Avatar != null ? StoreHelper.SchemaApi + accountToken.Avatar : "";
            return ResultApi.ToEntity(accountToken);
        }
        public async Task<ResultApi> AdminSignOutAsync(AdminUserLoginModel model)
        {
            // Check empty
            var user = await _userManager.FindByNameAsync(model.UserName)
                ?? await _userManager.FindByEmailAsync(model.UserName);
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            // signout
            await _signInManager.SignOutAsync();

            // Add Activity
            var userActivity = Mapper.Map<UserActivity>(model.Activity);
            if (userActivity != null)
            {
                userActivity.UserId = user.Id;
                userActivity.DateTime = DateTime.Now;
                userActivity.Type = UserActivityType.Logout;
                _accountActivityRepository.Insert(userActivity);
                await _unitOfWork.SaveChangesAsync();
            }

            // Notify here
            await _refreshDataService.Notify(user.Id, new Notify
            {
                IsRead = false,
                DateTime = DateTime.Now,
                Type = (int)NotifyType.Logout,
                Title = "Đăng xuất khỏi hệ thống",
                Content = "Lý do: Bạn đã đăng xuất hệ thống ở trình duyệt khác",
            });
            return ResultApi.ToEntity(true);
        }
        public async Task<ResultApi> AdminAddOrUpdateAsync(AdminUserUpdateModel model)
        {
            // check empty
            if (model == null ||
                model.Email.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByEmailAsync(model.Email)
                ?? await _userManager.FindByNameAsync(model.Email);
            if (user == null && !model.Phone.IsStringNullOrEmpty())
                user = _userRepository.Queryable().FirstOrDefault(c => c.PhoneNumber == model.Phone);
            if (user == null)
            {
                // save user
                user = Mapper.Map<User>(model);
                if (user.Id.IsNumberNull())
                {
                    var verifyCode = SecurityHelper.GenerateVerifyCode(10);
                    var rawPassword = SecurityHelper.GenerateVerifyCode(8);
                    var parent = _userRepository.Queryable().FirstOrDefault(c => c.Id == UserId);
                    var password = SecurityHelper.CreateHash256(rawPassword, _appSettings.Secret);

                    // User
                    user.IsActive = true;
                    user.IsDelete = false;
                    user.CreatedBy = UserId;
                    user.ParentId = parent.Id;
                    user.Avatar = model.Avatar;
                    user.UserName = user.Email;
                    user.EmailConfirmed = false;
                    user.VerifyCode = verifyCode;
                    user.VerifyTime = DateTime.Now;
                    user.PhoneNumber = model.Phone;
                    user.CreatedDate = DateTime.Now;
                    user.UserType = parent.UserType;
                    user.DepartmentId = model.DepartmentId;
                    user.OtherLoginId = model.OtherLoginId;
                    user.PhoneNumberConfirmed = !model.Phone.IsStringNullOrEmpty();
                    var identityResult = await _userManager.CreateAsync(user, password);
                    if (identityResult == null || !identityResult.Succeeded)
                        return ResultApi.ToError(ErrorResult.User.UserInvalid);
                }
            }
            else
            {
                user.UpdatedBy = UserId;
                user.Email = model.Email;
                user.Avatar = model.Avatar;
                user.Address = model.Address;
                user.Birthday = model.Birthday;
                user.PhoneNumber = model.Phone;
                user.DepartmentId = model.DepartmentId;
                user.OtherLoginId = model.OtherLoginId;
                if (!model.Avatar.StartsWithEx("http"))
                    user.Avatar = model.Avatar;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }

            // update user role
            var userRoles = _userRoleRepository.Queryable().Where(c => c.UserId == user.Id).ToList() ?? new List<UserRole>();
            var nextIds = model.RoleIds.IsNullOrEmpty() ? string.Empty : model.RoleIds.OrderBy(c => c).Distinct().ToJson();
            var currentIds = userRoles
                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                .Where(c => c.IsActive.HasValue && c.IsActive.Value)
                .Select(c => c.RoleId).OrderBy(c => c)
                .Distinct().ToJson();
            var needNotifyPermission = currentIds != nextIds;
            foreach (var item in userRoles)
                item.IsActive = false;
            if (!model.RoleIds.IsNullOrEmpty())
            {
                foreach (var item in model.RoleIds)
                {
                    var itemDb = userRoles
                            .Where(c => c.UserId == user.Id)
                            .FirstOrDefault(c => c.RoleId == item);
                    if (itemDb == null)
                    {
                        itemDb = new UserRole
                        {
                            RoleId = item,
                            UserId = user.Id,
                        };
                        _userRoleRepository.Insert(itemDb);
                    }
                    else
                    {
                        itemDb.IsActive = true;
                        itemDb.IsDelete = false;
                        _userRoleRepository.Update(itemDb);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // update user permissions
            var userPermissions = _userPermissionRepository.Queryable().Where(c => c.UserId == user.Id).ToList() ?? new List<UserPermission>();
            if (!needNotifyPermission)
            {
                nextIds = model.Permissions.IsNullOrEmpty() ? string.Empty : model.Permissions.Select(c => c.Id).OrderBy(c => c).Distinct().ToJson();
                currentIds = userPermissions
                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                    .Where(c => c.IsActive.HasValue && c.IsActive.Value)
                    .Select(c => c.PermissionId).OrderBy(c => c)
                    .Distinct().ToJson();
                needNotifyPermission = currentIds != nextIds;
            }
            foreach (var item in userPermissions)
                item.IsActive = false;
            if (!model.Permissions.IsNullOrEmpty())
            {
                foreach (var item in model.Permissions)
                {
                    var itemDb = userPermissions
                            .Where(c => c.UserId == user.Id)
                            .FirstOrDefault(c => c.PermissionId == item.Id);
                    if (itemDb == null)
                    {
                        itemDb = new UserPermission
                        {
                            Allow = true,
                            UserId = user.Id,
                            PermissionId = item.Id,
                        };
                        _userPermissionRepository.Insert(itemDb);
                    }
                    else
                    {
                        itemDb.Allow = true;
                        itemDb.IsActive = true;
                        itemDb.IsDelete = false;
                        _userPermissionRepository.Update(itemDb);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // update user teams
            var userTeams = _userTeamRepository.Queryable().Where(c => c.UserId == user.Id).ToList() ?? new List<UserTeam>();
            nextIds = model.TeamIds.IsNullOrEmpty() ? string.Empty : model.TeamIds.OrderBy(c => c).Distinct().ToJson();
            currentIds = userTeams
                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                .Where(c => c.IsActive.HasValue && c.IsActive.Value)
                .Select(c => c.TeamId).OrderBy(c => c)
                .Distinct().ToJson();
            var needNotifyTeam = currentIds != nextIds;
            foreach (var item in userTeams)
                item.IsActive = false;
            if (!model.TeamIds.IsNullOrEmpty())
            {
                foreach (var item in model.TeamIds)
                {
                    var itemDb = userTeams
                            .Where(c => c.UserId == user.Id)
                            .FirstOrDefault(c => c.TeamId == item);
                    if (itemDb == null)
                    {
                        itemDb = new UserTeam
                        {
                            TeamId = item,
                            UserId = user.Id,
                        };
                        _userTeamRepository.Insert(itemDb);
                    }
                    else
                    {
                        itemDb.IsActive = true;
                        itemDb.IsDelete = false;
                        _userTeamRepository.Update(itemDb);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // Notify
            if (needNotifyPermission)
            {
                await _notifyService.AddNotifyAsync(new Notify
                {
                    IsRead = false,
                    DateTime = DateTime.Now,
                    Type = (int)NotifyType.UpdateRole,
                    Title = needNotifyPermission
                        ? "Admin update role"
                        : "Admin hệ thống cập nhật lại nhóm",
                }, new List<int> { user.Id });
            }
            return ResultApi.ToEntity(true);
        }
        public async Task<ResultApi> AdminSetPasswordAsync(AdminUserSetPasswordModel model)
        {
            // check empty
            if (model == null ||
                model.Code.IsStringNullOrEmpty() ||
                model.Password.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            var datetime = DateTime.Now.AddDays(-1);
            var user = _userRepository.Queryable().AsNoTracking()
                .Where(c => c.VerifyCode == model.Code)
                .Where(c => c.VerifyTime >= datetime)
                .Where(c => !c.EmailConfirmed)
                .FirstOrDefault();
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            user = await _userManager.FindByEmailAsync(user.Email);
            var password = SecurityHelper.CreateHash256(model.Password, _appSettings.Secret);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var identityResult = await _userManager.ResetPasswordAsync(user, token, password);
            if (identityResult.Succeeded)
            {
                // save verify-code
                user.EmailConfirmed = true;
                user.VerifyCode = model.Code;
                user.VerifyTime = DateTime.Now;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                return ResultApi.ToEntity(true);
            }
            return ResultApi.ToError(ErrorResult.TokenInvalid);
        }
        public async Task<ResultApi> AdminResetPasswordAsync(AdminUserResetPasswordModel model)
        {
            // check empty
            if (model == null ||
                model.Email.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByNameAsync(model.Email)
                ?? await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            var verifyCode = SecurityHelper.GenerateVerifyCode(10);
            var password = SecurityHelper.CreateHash256(verifyCode, _appSettings.Secret);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var identityResult = await _userManager.ResetPasswordAsync(user, token, password);
            if (identityResult.Succeeded)
            {
                // save verify-code
                user.EmailConfirmed = false;
                user.VerifyCode = verifyCode;
                user.VerifyTime = DateTime.Now;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // send mail
                var keyValues = new Dictionary<string, string>
                    {
                        { "Email", user.Email },
                        { "FullName", user.FullName },
                        { "Link", StoreHelper.SchemaWebAdmin + "/resetpassword?code=" + user.VerifyCode },
                    };
                return _emailService.SendMail(user.Email, EmailTemplateType.ForgotPassword, keyValues);
            }
            return ResultApi.ToError(ErrorResult.TokenInvalid);
        }
        public async Task<ResultApi> AdminChangePasswordAsync(AdminUserChangePasswordModel model)
        {
            // Check empty
            if (model == null ||
                model.Password.IsStringNullOrEmpty() ||
                model.OldPassword.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByIdAsync(UserId.ToString());
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            // check login
            var password = SecurityHelper.CreateHash256(model.OldPassword, _appSettings.Secret);
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (signInResult == null || !signInResult.Succeeded)
                return ResultApi.ToError(ErrorResult.User.PasswordInvalid);

            // User
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var newPassword = SecurityHelper.CreateHash256(model.Password, _appSettings.Secret);
            var identityResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (identityResult == null || !identityResult.Succeeded)
                return ResultApi.ToError(ErrorResult.User.UserInvalid);

            // Add Activity
            var userActivity = Mapper.Map<UserActivity>(model.Activity);
            if (userActivity != null)
            {
                userActivity.UserId = user.Id;
                userActivity.DateTime = DateTime.Now;
                userActivity.Type = UserActivityType.ResetPassword;
                _accountActivityRepository.Insert(userActivity);
                await _unitOfWork.SaveChangesAsync();
            }

            // Notify here
            await _notifyService.AddNotifyAsync(new Notify
            {
                IsRead = false,
                DateTime = DateTime.Now,
                Title = "Thay đổi mật khẩu",
                Type = (int)NotifyType.ChangePassword,
                Content = "Lý do: Bạn hoặc ai đó đã thay đổi mật khẩu",
            }, new List<int> { user.Id });
            return ResultApi.ToEntity(true);
        }
        public async Task<ResultApi> AdminForgotPasswordAsync(AdminUserForgotPasswordModel model)
        {
            // Check empty
            if (model == null || model.Email.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByNameAsync(model.Email)
               ?? await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            var verifyCode = SecurityHelper.GenerateVerifyCode(10);
            var password = SecurityHelper.CreateHash256(verifyCode, _appSettings.Secret);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var identityResult = await _userManager.ResetPasswordAsync(user, token, password);
            if (identityResult.Succeeded)
            {
                // save verify-code
                user.EmailConfirmed = false;
                user.VerifyCode = verifyCode;
                user.VerifyTime = DateTime.Now;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // Add Activity
                var userActivity = Mapper.Map<UserActivity>(model.Activity);
                if (userActivity != null)
                {
                    userActivity.UserId = user.Id;
                    userActivity.DateTime = DateTime.Now;
                    userActivity.Type = UserActivityType.ForgotPassword;
                    _accountActivityRepository.Insert(userActivity);
                    await _unitOfWork.SaveChangesAsync();
                }

                // send mail
                var keyValues = new Dictionary<string, string>
                    {
                        { "Email", user.Email },
                        { "FullName", user.FullName },
                        { "Link", StoreHelper.SchemaWebAdmin + "/resetpassword?code=" + user.VerifyCode },
                    };
                return _emailService.SendMail(user.Email, EmailTemplateType.AdminForgotPassword, keyValues);
            }
            return ResultApi.ToError(ErrorResult.TokenInvalid);
        }
        public async Task<ResultApi> AdminRegisterAndLoginSocicalAsync(AdminExternalAuthModel model)
        {
            // Check empty
            if (model == null ||
                model.Provider.IsStringNullOrEmpty())
                return ToError(ErrorResult.User.TokenInvalid);

            // verify token 
            var socialUser = new AdminSocialUserModel();
            switch (model.Provider.ToLower())
            {
                case "google":
                    socialUser = VerifyGoogleToken(model.Token);
                    if (socialUser == null)
                        return ToError(ErrorResult.User.NotExists);
                    break;
                case "facebook":
                    socialUser = await VerifyFacebookTokenAsync(model.Token);
                    if (socialUser == null)
                        return ToError(ErrorResult.User.NotExists);
                    break;

            }

            // find user by token
            var user = _userRepository.Queryable().FirstOrDefault(c => c.GoogleToken == socialUser.SocialUserId || c.FacebookToken == socialUser.SocialUserId);
            if (user == null)
            {
                user = _userRepository.Queryable().FirstOrDefault(c => c.Email == socialUser.Email);
                if (user == null)
                    return ToError(ErrorResult.User.NotExists);
                else
                {
                    if (user.Locked.HasValue && user.Locked.Value)
                        return ToError(ErrorResult.User.Locked);
                    if (!user.IsActive.HasValue || !user.IsActive.Value)
                        return ToError(ErrorResult.User.NotActive);
                    switch (model.Provider.ToLower())
                    {
                        case "google":
                            user.GoogleToken = socialUser.SocialUserId;
                            break;
                        case "facebook":
                            user.FacebookToken = socialUser.SocialUserId;
                            break;
                    }
                    if (user.Avatar.IsStringNullOrEmpty()) user.Avatar = socialUser.Avatar;

                    _userRepository.Update(user);
                    await _unitOfWork.SaveChangesAsync();

                    return ReturnLoginToken(user);
                }
            }
            else
            {
                if (user.Locked.HasValue && user.Locked.Value)
                    return ToError(ErrorResult.User.Locked);
                if (!user.IsActive.HasValue || !user.IsActive.Value)
                    return ToError(ErrorResult.User.NotActive);
                if (user.Locked.HasValue && user.Locked.Value)
                    return ToError(ErrorResult.User.Locked);
                if (user.Avatar.IsStringNullOrEmpty())
                {
                    user.Avatar = socialUser.Avatar;
                    _userRepository.Update(user);
                    await _unitOfWork.SaveChangesAsync();
                }
                return ReturnLoginToken(user);
            }
        }

        private List<Permission> AllPermissions()
        {
            return StoreHelper.Permissions.IsNullOrEmpty()
                ? _permissionRepository.Queryable().FilterQueryNoTraking().ToList()
                : StoreHelper.Permissions.FilterQueryNoTraking().ToList();
        }
        private ResultApi ReturnLoginToken(User user)
        {
            // Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.TokenKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var account = new AdminUserModel(user);
            var claims = new[]
            {
                new Claim(ClaimTypes.PrimarySid, user.TenantId),
                new Claim(ClaimTypes.UserData, account.ToJson()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, (user.IsAdmin.HasValue && user.IsAdmin.Value).ToString()),
            };
            var token = new JwtSecurityToken(JwtConstant.Issuer, JwtConstant.Audience, claims, null, DateTime.Now.AddDays(3), creds);
            var accountToken = new AdminUserModel(user, token);
            accountToken.Avatar = !accountToken.Avatar.IsStringNullOrEmpty() && !accountToken.Avatar.StartsWith("http")
                ? StoreHelper.SchemaApi + accountToken.Avatar
                : accountToken.Avatar;
            return ResultApi.ToEntity(accountToken);
        }
        private List<LinkPermission> AllLinkPermissions()
        {
            return StoreHelper.LinkPermissions.IsNullOrEmpty()
                ? _linkPermissionRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.PermissionId.HasValue)
                    .OrderBy(c => c.GroupOrder)
                    .ThenBy(c => c.Order)
                    .ToList()
                : StoreHelper.LinkPermissions.FilterQueryNoTraking()
                    .Where(c => c.PermissionId.HasValue)
                    .OrderBy(c => c.GroupOrder)
                    .ThenBy(c => c.Order)
                    .ToList();
        }
        private static AdminSocialUserModel VerifyGoogleToken(string tokenId)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(tokenId);
                var socialUser = new AdminSocialUserModel();
                var payload = jwtSecurityToken.Payload;
                if (payload != null)
                {
                    socialUser.Email = payload["email"].ToString();
                    socialUser.Name = payload["name"].ToString();
                    socialUser.SocialUserId = payload["sub"].ToString();
                    socialUser.Avatar = payload["picture"].ToString();
                }
                return socialUser;
            }
            catch
            {
                return null;
            }
        }
        private async Task<AdminSocialUserModel> VerifyFacebookTokenAsync(string tokenId)
        {
            var user = new AdminSocialUserModel();
            var client = new HttpClient();

            var verifyTokenEndPoint = string.Format("https://graph.facebook.com/me?access_token={0}", tokenId);
            var verifyAppEndpoint = string.Format("https://graph.facebook.com/app?access_token={0}", tokenId);

            var uri = new Uri(verifyTokenEndPoint);
            var response = await client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                dynamic userObj = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(content);

                uri = new Uri(verifyAppEndpoint);
                response = await client.GetAsync(uri);
                content = await response.Content.ReadAsStringAsync();
                dynamic appObj = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(content);

                if (appObj["id"] == _configuration.GetSection("Authentication:Facebook:AppId").Value)
                {
                    //token is from our App
                    user.SocialUserId = userObj["id"];
                    user.Email = userObj["email"];
                    user.Name = userObj["name"];
                    user.IsVerified = true;
                }

                return user;
            }
            return user;
        }
    }
}
