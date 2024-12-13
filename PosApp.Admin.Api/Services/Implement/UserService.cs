using Microsoft.AspNetCore.Identity;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.Abstractions;
using System.Web;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Data.Models;
using URF.Core.EF.Trackable.Enums;
using PosApp.Admin.Api.Helpers;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable;
using URF.Core.Helper.Helpers;
using Microsoft.Extensions.Options;
using PosApp.Admin.Api.Data.Enums;

namespace PosApp.Admin.Api.Services.Implement
{
    public class UserService : ServiceX, IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppSettings _appSettings;
        private readonly INotifyService _notifyService;
        private readonly IRepositoryX<User> _repository;
        private readonly UserManager<User> _userManager;
        private readonly IPermissionService _permissionService;
        private readonly IRepositoryX<UserTeam> _userTeamRepository;
        private readonly IRepositoryX<UserRole> _userRoleRepository;

        public UserService(
            IUnitOfWork unitOfWork,
            INotifyService notifyService,
            IRepositoryX<User> repository,
            UserManager<User> userManager,
            IOptions<AppSettings> appSettings,
            IPermissionService permissionService,
            IHttpContextAccessor httpContextAccessor,
            IRepositoryX<UserRole> userRoleRepository,
            IRepositoryX<UserTeam> userTeamRepository) : base(httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _userManager = userManager;
            _notifyService = notifyService;
            _appSettings = appSettings.Value;
            _permissionService = permissionService;
            _userRoleRepository = userRoleRepository;
            _userTeamRepository = userTeamRepository;
        }

        public List<int> AllUserInTeam()
        {
            if (StoreHelper.UserTeams.IsNullOrEmpty())
            {
                var teamIds = _userTeamRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.UserId == UserId)
                    .Select(c => c.TeamId)
                    .Distinct()
                    .ToList();
                var userIds = _userTeamRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => teamIds.Contains(c.TeamId))
                    .Select(c => c.UserId)
                    .Distinct()
                    .ToList() ?? new List<int>();
                if (!userIds.Contains(UserId)) userIds.Add(UserId);
                return userIds;
            }
            else
            {
                var teamIds = StoreHelper.UserTeams.FilterQueryNoTraking()
                    .Where(c => c.UserId == UserId)
                    .Select(c => c.TeamId)
                    .Distinct()
                    .ToList();
                var userIds = StoreHelper.UserTeams.FilterQueryNoTraking()
                    .Where(c => teamIds.Contains(c.TeamId))
                    .Select(c => c.UserId)
                    .Distinct()
                    .ToList() ?? new List<int>();
                if (!userIds.Contains(UserId)) userIds.Add(UserId);
                return userIds;
            }
        }
        public List<int> AllUserInDepartment()
        {
            if (StoreHelper.Users.IsNullOrEmpty())
            {
                StoreHelper.Users = _repository.Queryable().FilterQueryNoTraking().ToList();
                var departmentIds = _repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.DepartmentId.HasValue).Where(c => c.Id == UserId)
                    .Select(c => c.DepartmentId.Value)
                    .Distinct()
                    .ToList();
                var userIds = _repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.DepartmentId.HasValue)
                    .Where(c => departmentIds.Contains(c.DepartmentId.Value))
                    .Select(c => c.Id)
                    .Distinct()
                    .ToList() ?? new List<int>();
                if (!userIds.Contains(UserId)) userIds.Add(UserId);

                return userIds;
            }
            else
            {
                var departmentIds = StoreHelper.Users
                    .Where(c => c.DepartmentId.HasValue).Where(c => c.Id == UserId)
                    .Select(c => c.DepartmentId.Value)
                    .Distinct()
                    .ToList();
                var userIds = StoreHelper.Users
                    .Where(c => c.DepartmentId.HasValue)
                    .Where(c => departmentIds.Contains(c.DepartmentId.Value))
                    .Select(c => c.Id)
                    .Distinct()
                    .ToList() ?? new List<int>();
                if (!userIds.Contains(UserId)) userIds.Add(UserId);
                return userIds;
            }
        }
        public ResultApi AllUsers(int? roleId = null)
        {
            var query = _repository.Queryable()
                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                .Where(c => !c.Locked.HasValue || !c.Locked.Value);
            if (!roleId.IsNumberNull())
            {
                var ids = _userRoleRepository.Queryable()
                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                    .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                    .Where(c => c.RoleId == roleId)
                    .Select(c => c.UserId)
                    .Distinct()
                    .ToList();
                query = query.Where(c => ids.Contains(c.Id));
            }
            var items = query
                .Select(c => new
                {
                    c.Id,
                    c.Email,
                    c.Locked,
                    c.Avatar,
                    c.FullName,
                    c.IsActive,
                    c.IsDelete,
                    c.CreatedDate,
                    c.UpdatedDate,
                    Phone = c.PhoneNumber,
                    Role = string.Join(", ", c.UserRoles.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Role.Name)),
                }).ToList();
            return ResultApi.ToEntity(items);
        }
        public ResultApi AllUsersByRoleId(int roleId)
        {
            var ids = _userRoleRepository.Queryable().AsNoTracking()
                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                .Where(c => c.RoleId == roleId)
                .Select(c => c.UserId)
                .Distinct()
                .ToList();
            var items = _repository.Queryable().AsNoTracking()
                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                .Where(c => !c.Locked.HasValue || !c.Locked.Value)
                .Where(c => ids.Contains(c.Id))
                .Select(c => new
                {
                    c.Id,
                    c.Email,
                    c.Locked,
                    c.Avatar,
                    c.FullName,
                    c.IsActive,
                    c.IsDelete,
                    c.CreatedDate,
                    c.UpdatedDate,
                    Phone = c.PhoneNumber,
                    Department = c.Department.Name,
                    Team = string.Join(", ", c.UserTeams.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Team.Name)),
                    Role = string.Join(", ", c.UserRoles.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Role.Name)),
                }).ToList();
            return ResultApi.ToEntity(items);
        }
        public ResultApi AllUsersByTeamId(int teamId)
        {
            var ids = _userTeamRepository.Queryable().FilterQueryNoTraking()
                .Where(c => c.TeamId == teamId)
                .Select(c => c.UserId)
                .Distinct()
                .ToList();
            var items = _repository.Queryable().AsNoTracking()
                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                .Where(c => !c.Locked.HasValue || !c.Locked.Value)
                .Where(c => ids.Contains(c.Id))
                .Select(c => new
                {
                    c.Id,
                    c.Email,
                    c.Locked,
                    c.Avatar,
                    c.FullName,
                    c.IsActive,
                    c.IsDelete,
                    c.CreatedDate,
                    c.UpdatedDate,
                    Phone = c.PhoneNumber,
                    Department = c.Department.Name,
                    Team = string.Join(", ", c.UserTeams.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Team.Name)),
                    Role = string.Join(", ", c.UserRoles.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Role.Name)),
                }).ToList();
            return ResultApi.ToEntity(items);
        }
        public List<int> AllUsersByManagerId(string controller)
        {
            var userIds = new List<int>();
            var type = _permissionService.GetPermissionType(controller);
            switch (type)
            {
                case PermissionType.Ower:
                    {
                        userIds = new List<int> { UserId };
                    }
                    break;
                case PermissionType.Team:
                    {
                        userIds = AllUserInTeam();
                    }
                    break;
                case PermissionType.Department:
                    {
                        userIds = AllUserInDepartment();
                    }
                    break;
            }
            if (!userIds.Contains(UserId)) userIds.Add(UserId);
            return userIds;
        }
        public ResultApi AllUsersByDepartmentId(int departmentId)
        {
            var items = _repository.Queryable().AsNoTracking()
                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                .Where(c => !c.Locked.HasValue || !c.Locked.Value)
                .Where(c => c.DepartmentId == departmentId)
                .Select(c => new
                {
                    c.Id,
                    c.Email,
                    c.Locked,
                    c.Avatar,
                    c.FullName,
                    c.IsActive,
                    c.IsDelete,
                    c.CreatedDate,
                    c.UpdatedDate,
                    Phone = c.PhoneNumber,
                    Department = c.Department.Name,
                    Team = string.Join(", ", c.UserTeams.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Team.Name)),
                    Role = string.Join(", ", c.UserRoles.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Role.Name)),
                }).ToList();
            return ResultApi.ToEntity(items);
        }
        public ResultApi GetProfileByVerifyCode(string verifyCode)
        {
            // check empty
            if (verifyCode.IsStringNullOrEmpty())
                return ToError(ErrorResult.DataInvalid);

            var datetime = DateTime.Now.AddDays(-1);
            var user = _repository.Queryable().AsNoTracking()
                .Where(c => c.VerifyCode == verifyCode)
                .Where(c => c.VerifyTime >= datetime)
                .Where(c => !c.EmailConfirmed)
                .FirstOrDefault();
            if (user == null)
                return ToError(ErrorResult.User.NotExists);
            return ResultApi.ToEntity(user.Email);
        }
        public async Task<ResultApi> ResetPasswordAsync(string email)
        {
            // check empty
            if (email.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            var valid = false;
            var user = await _userManager.FindByEmailAsync(email);
            switch (User.UserType)
            {
                case (int)AccountType.Operation: valid = true; break;
            }
            if (!valid)
                return ResultApi.ToError("Bạn không có quyền thiết lập mật khẩu cho tài khoản này");
            var verifyCode = SecurityHelper.GenerateVerifyCode(10);
            var rawPassword = SecurityHelper.GenerateVerifyCode(8);
            var clientPassword = SecurityHelper.CreateHash256(rawPassword, _appSettings.Secret);
            var password = SecurityHelper.CreateHash256(clientPassword, _appSettings.Secret);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var identityResult = await _userManager.ResetPasswordAsync(user, token, password);
            if (identityResult.Succeeded)
            {
                // save verify-code
                user.EmailConfirmed = true;
                user.VerifyCode = verifyCode;
                user.VerifyTime = DateTime.Now;
                _repository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                return ResultApi.ToEntity(rawPassword);
            }
            return ResultApi.ToError(ErrorResult.TokenInvalid);
        }
        public ResultApi AllUsersByDepartmentIds(List<int> departmentIds)
        {
            var items = _repository.Queryable().FilterQueryNoTraking()
                .Where(c => c.DepartmentId.HasValue)
                .Where(c => departmentIds.Contains(c.DepartmentId.Value))
                .Select(c => new
                {
                    c.Id,
                    c.Email,
                    c.Avatar,
                    c.IsActive,
                    c.IsDelete,
                    c.FullName,
                    c.CreatedDate,
                    c.UpdatedDate,
                    Department = c.Department.Name,
                }).ToList();
            return ResultApi.ToEntity(items);
        }

        public async Task<ResultApi> UnLockAsync(int userId)
        {
            // Check empty
            if (userId.IsNumberNull()) return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            user.Locked = false;
            _repository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Notify here
            return ResultApi.ToEntity(true);
        }
        public async Task<ResultApi> UpdateProfile(AdminUserUpdateModel model)
        {
            // check empty
            if (model == null ||
                model.FullName.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByIdAsync(UserId.ToString());
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            user.Avatar = model.Avatar;
            user.Birthday = model.Birthday;
            user.FullName = model.FullName;
            user.PhoneNumber = model.Phone;
            _repository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return ResultApi.ToEntity(new
            {
                user.Email,
                user.Gender,
                user.Avatar,
                user.FullName,
                user.Birthday,
                Phone = user.PhoneNumber
            });
        }
        public async Task<ResultApi> LockAsync(int userId, AdminUserLockModel model)
        {
            // Check empty
            if (model == null ||
                userId.IsNumberNull() ||
                model.ReasonLock.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // Check exists
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResultApi.ToError(ErrorResult.User.NotExists);

            user.Locked = true;
            user.ReasonLock = model.ReasonLock;
            _repository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Notify here
            await _notifyService.AddNotifyAsync(new Notify
            {
                IsRead = false,
                DateTime = DateTime.Now,
                Type = (int)NotifyType.LockUser,
                Content = "Lý do: " + HttpUtility.HtmlEncode(user.ReasonLock),
                Title = "Tài khoản của bạn bị khóa. Hãy liên hệ với Admin hệ thống để biết thêm thông tin",
            }, new List<int> { userId });
            return ResultApi.ToEntity(true);
        }
    }
}
