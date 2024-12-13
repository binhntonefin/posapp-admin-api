using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URF.Core.EF.Trackable.Models;
using Microsoft.EntityFrameworkCore;
using URF.Core.Abstractions;
using URF.Core.EF.Trackable.Enums;
using URF.Core.Helper.Extensions;
using PosApp.Admin.Api.Helpers;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Services.Implement;
using PosApp.Admin.Api.Data.Enums;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]

    [Route("api/admin/[controller]")]
    public class UserController : AdminApiBaseController
    {
        private IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        private ISecurityService _securityService;
        private IPermissionService _permissionService;
        private readonly IRepositoryX<User> _repository;
        private readonly IRepositoryX<Role> _roleRepository;
        private readonly IRepositoryX<Team> _teamRepository;
        private static IHttpContextAccessor _httpContextAccessor;
        private readonly IRepositoryX<UserRole> _userRoleRepository;
        private readonly IRepositoryX<UserTeam> _userTeamRepository;
        private readonly IRepositoryX<Department> _departmentRepository;
        private readonly IRepositoryX<UserPermission> _userPermissionRepository;

        public UserController(
            IUnitOfWork unitOfWork,
            IUserService userService,
            IRepositoryX<User> repository,
            ISecurityService securityService,
            IRepositoryX<Role> roleRepository,
            IRepositoryX<Team> teamRepository,
            IPermissionService permissionService,
            IHttpContextAccessor httpContextAccessor,
            IRepositoryX<UserRole> userRoleRepository,
            IRepositoryX<UserTeam> userTeamRepository,
            IRepositoryX<Department> departmentRepository,
            IRepositoryX<UserPermission> userPermissionRepository,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _userService = userService;
            _roleRepository = roleRepository;
            _teamRepository = teamRepository;
            _securityService = securityService;
            _permissionService = permissionService;
            _userTeamRepository = userTeamRepository;
            _userRoleRepository = userRoleRepository;
            _httpContextAccessor = httpContextAccessor;
            _departmentRepository = departmentRepository;
            _userPermissionRepository = userPermissionRepository;
        }

        [HttpGet("Lookup")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Lookup()
        {
            try
            {
                var entities = _repository.Queryable().FilterQueryNoLocked()
                    .Select(c => new
                    {
                        c.Id,
                        c.Email,
                        c.FullName,
                        Phone = c.PhoneNumber,
                    })
                    .ToList();
                return Ok(ResultApi.ToEntity(entities));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("AllUsers")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult AllUsers()
        {
            try
            {
                var result = _userService.AllUsers();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("LookupOtherLogin")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult LookupOtherLogin()
        {
            try
            {
                var query = _repository.Queryable().FilterQueryNoLocked();
                if (!IsAdmin)
                {
                    if (!User.OtherLoginId.HasValue) return Ok(ResultApi.ToEntity());
                    query = query.Where(c => c.Id == User.OtherLoginId);
                }
                var entities = query
                    .Select(c => new
                    {
                        c.Id,
                        c.Email,
                        c.FullName,
                        Phone = c.PhoneNumber,
                    })
                    .ToList();
                return Ok(ResultApi.ToEntity(entities));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Get([FromRoute] int id)
        {
            try
            {
                var user = _repository.Queryable().AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Email,
                        c.Avatar,
                        c.Gender,
                        c.Locked,
                        c.Address,
                        c.FullName,
                        c.Birthday,
                        c.DepartmentId,
                        c.OtherLoginId,
                        Phone = c.PhoneNumber,
                    })
                    .FirstOrDefault();
                if (user == null)
                {
                    return NotFound();
                }

                // TeamIds
                var teamIds = _userTeamRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.UserId == user.Id)
                    .Select(c => c.TeamId)
                    .Distinct()
                    .ToList();

                // RoleIds
                var roleIds = _userRoleRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.UserId == user.Id)
                    .Select(c => c.RoleId)
                    .Distinct()
                    .ToList();

                // PermissionIds
                var permissionIds = _userPermissionRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.UserId == user.Id)
                    .Select(c => c.PermissionId)
                    .Distinct()
                    .ToList();

                var entity = new
                {
                    user.Id,
                    user.Phone,
                    user.Email,
                    user.Avatar,
                    user.Gender,
                    user.Locked,
                    user.Address,
                    user.FullName,
                    user.Birthday,
                    user.DepartmentId,
                    user.OtherLoginId,
                    TeamIds = teamIds,
                    RoleIds = roleIds,
                    PermissionIds = permissionIds,
                };
                return Ok(ResultApi.ToEntity(entity));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("Profile/{search?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Profile([FromRoute] string search)
        {
            try
            {
                var result = _securityService.Profile(search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("Lookup/{userType}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult LookupType([FromRoute] int userType)
        {
            try
            {
                var entities = _repository.Queryable().FilterQueryNoLocked()
                    .Where(c => c.UserType == userType)
                    .Select(c => new
                    {
                        c.Id,
                        c.Email,
                        c.FullName,
                        Phone = c.PhoneNumber,
                    })
                    .ToList();
                return Ok(ResultApi.ToEntity(entities));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpDelete("Trash/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Trash([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var entity = await _repository.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }

                entity.IsDelete = !entity.IsDelete;
                _repository.Update(entity);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ResultApi.ToEntity(id));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var entity = await _repository.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }

                entity.IsDelete = !entity.IsDelete;
                _repository.Update(entity);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ResultApi.ToEntity(id));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("Active/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Active([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var entity = await _repository.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }

                entity.IsActive = !entity.IsActive;
                _repository.Update(entity);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ResultApi.ToEntity(id));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("AllUsersByRoleId/{roleId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult AllUsersByRoleId([FromRoute] int roleId)
        {
            try
            {
                var result = _userService.AllUsers(roleId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("AllUsersByTeamId/{teamId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult AllUsersByTeamId([FromRoute] int teamId)
        {
            try
            {
                var result = _userService.AllUsersByTeamId(teamId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("LookupLogin/{departmentId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult LookupLogin([FromRoute] int departmentId)
        {
            try
            {
                var departments = _departmentRepository.Queryable().FilterQueryNoTraking().ToList();
                var departmentIds = GetDepartmentAndChildren(departments, departmentId).Select(c => c.Id).ToList();
                var entities = _repository.Queryable().FilterQueryNoLocked()
                    .Where(c => !c.IsAdmin.HasValue || !c.IsAdmin.Value)
                    .Where(c => departmentIds.Contains(c.Id))
                    .Select(c => new
                    {
                        c.Id,
                        c.Email,
                        c.FullName,
                        Phone = c.PhoneNumber,
                    })
                    .ToList();
                return Ok(ResultApi.ToEntity(entities));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Insert([FromBody] User entity)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                _repository.Insert(entity);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ResultApi.ToEntity(entity));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("UnLock/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UnLockAsync([FromRoute] int id)
        {
            var obj = await _userService.UnLockAsync(id);
            return Ok(obj);
        }

        [HttpPost("SendVerifyCode/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> SendVerifyCode([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _securityService.SendVerifyCodeAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }

        }

        [HttpPost("Items")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAllAsync([FromBody] TableData obj)
        {
            try
            {
                var query = _repository.Queryable().AsNoTracking();
                switch (User.UserType)
                {
                    case (int)AccountType.Operation:
                        query = query.Where(c => c.UserType.HasValue).Where(c => c.UserType.Value == (int)AccountType.Operation);
                        break;
                    case (int)AccountType.Shop:
                        query = query.Where(c => c.UserType.HasValue)
                            .Where(c => c.Id == UserId || (c.UserType.Value == (int)AccountType.Shop && c.ParentId == UserId))
                            .Where(c => c.UserType.Value != (int)AccountType.Agency && c.UserType.Value != (int)AccountType.Collaborator);
                        break;
                    case (int)AccountType.Agency:
                        query = query.Where(c => c.UserType.HasValue)
                            .Where(c => c.UserType.Value == (int)AccountType.Agency || c.ParentId == UserId)
                            .Where(c => c.UserType.Value != (int)AccountType.Shop && c.UserType.Value != (int)AccountType.Collaborator);
                        break;
                    case (int)AccountType.Collaborator:
                        query = query.Where(c => c.UserType.HasValue)
                            .Where(c => c.UserType.Value == (int)AccountType.Collaborator || c.ParentId == UserId)
                            .Where(c => c.UserType.Value != (int)AccountType.Agency && c.UserType.Value != (int)AccountType.Shop);
                        break;
                }   

                // filter
                if (!obj.Filters.IsNullOrEmpty())
                {
                    var locked = obj.Filters.FirstOrDefault(c => c.Name.EqualsEx("LockedStatus"));
                    if (locked != null)
                    {
                        var value = locked.Value.ToBoolean();
                        query = value
                            ? query.Where(c => c.Locked.HasValue && c.Locked.Value == true)
                            : query.Where(c => !c.Locked.HasValue || (c.Locked.HasValue && c.Locked.Value == false));
                    }

                    var role = obj.Filters.FirstOrDefault(c => c.Name.EqualsEx("RoleIds"));
                    if (role != null)
                    {
                        var values = role.Value.ToString().ToListId();
                        query = query.Where(c => c.UserRoles.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Any(p => values.Contains(p.RoleId)));
                    }
                }
                var result = await query
                    .Select(c => new
                    {
                        c.Id,
                        c.Email,
                        c.Avatar,
                        c.Locked,
                        c.Gender,
                        c.UserType,
                        c.FullName,
                        c.IsActive,
                        c.IsDelete,
                        c.Birthday,
                        c.CreatedDate,
                        c.UpdatedDate,
                        Phone = c.PhoneNumber,
                        CreatedBy = c.CreatedByUser != null ? c.CreatedByUser.UserName : string.Empty,
                        UpdatedBy = c.UpdatedByUser != null ? c.UpdatedByUser.UserName : string.Empty,
                        LastLogin = c.Activities != null
                            ? c.Activities.Where(p => p.UserId == c.Id).Where(c => c.Type == UserActivityType.Login).Max(c => c.DateTime)
                            : (DateTime?)null,
                        Role = c.UserRoles != null
                            ? string.Join(", ", c.UserRoles.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Role.Name))
                            : string.Empty,
                        Team = string.Join(", ", c.UserTeams.Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                                                            .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                                                            .Select(p => p.Team.Name)),
                    })
                    .ToQueryAsync(obj);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("AllUsersByDepartmentId/{departmentId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult AllUsersByDepartmentId([FromRoute] int departmentId)
        {
            try
            {
                var result = _userService.AllUsersByDepartmentId(departmentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("GetProfileByVerifyCode/{verifyCode}")]
        public IActionResult GetProfileByVerifyCode([FromRoute] string verifyCode)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = _userService.GetProfileByVerifyCode(verifyCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }

        }

        [HttpGet("AllUsersByDepartmentIds")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult AllUsersByDepartmentId([FromQuery] string departmentIds)
        {
            try
            {
                var result = _userService.AllUsersByDepartmentIds(departmentIds.ToListId());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("UpdateProfile")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdateProfile([FromBody] AdminUserUpdateModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _userService.UpdateProfile(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }

        }

        [HttpPost("ResetPassword")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] AdminUserResetPasswordModel model)
        {
            var obj = await _userService.ResetPasswordAsync(model.Email);
            return Ok(obj);
        }

        [HttpPost("Lock/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> LockAsync([FromRoute] int id, [FromBody] AdminUserLockModel model)
        {
            var obj = await _userService.LockAsync(id, model);
            return Ok(obj);
        }

        [HttpGet("ProfileExists/{id?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult ProfileExists([FromRoute] int? id, string property = default, string value = default)
        {
            try
            {
                var identity = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                var userId = id != null ? id : (identity != null && identity.Value != null ? identity.Value.ToInt32() : 0);
                var exists = _repository.Queryable()
                           .Where(property + ".Equals(@0)", value)
                           .Where("!Id.Equals(@0)", userId)
                           .Any();
                return Ok(ResultApi.ToEntity(exists));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("Exists/{id?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Exists([FromRoute] int id = default, string property = default, string value = default)
        {
            try
            {
                var query = _repository.Queryable();
                if (!id.IsNumberNull())
                    query = query.Where(c => c.Id != id);
                var exists = query.Where(property + ".Equals(@0)", value).Any();
                return Ok(ResultApi.ToEntity(exists));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("IgnoreItems/{type}/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> IgnoreItemsAsync([FromRoute] string type, [FromRoute] int id, [FromBody] TableData obj)
        {
            try
            {
                var query = _repository.Queryable().AsNoTracking()
                    .Where(c => !c.Locked.HasValue || !c.Locked.Value);
                switch (type.ToLower())
                {
                    case "team":
                        {
                            var userIds = _userTeamRepository.Queryable().FilterQueryNoTraking()
                                .Where(c => c.TeamId == id)
                                .Select(c => c.UserId)
                                .Distinct()
                                .ToList();
                            query = query.Where(c => !userIds.Contains(c.Id));
                        }
                        break;
                    case "role":
                        {
                            var userIds = _userRoleRepository.Queryable().AsNoTracking()
                                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                                .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                                .Where(c => c.RoleId == id)
                                .Select(c => c.UserId)
                                .Distinct()
                                .ToList();
                            query = query.Where(c => !userIds.Contains(c.Id));
                        }
                        break;
                    case "department":
                        query = query.Where(c => !c.DepartmentId.HasValue || c.DepartmentId.Value == 0);
                        break;
                }
                var result = await query
                    .Select(c => new
                    {
                        c.Id,
                        c.Email,
                        c.Avatar,
                        c.Locked,
                        c.FullName,
                        c.IsActive,
                        c.IsDelete,
                        c.CreatedDate,
                        c.UpdatedDate,
                        Phone = c.PhoneNumber,
                        CreatedBy = c.CreatedByUser != null ? c.CreatedByUser.UserName : string.Empty,
                        UpdatedBy = c.UpdatedByUser != null ? c.UpdatedByUser.UserName : string.Empty,
                        LastLogin = c.Activities != null
                            ? c.Activities.Where(p => p.UserId == c.Id).Where(c => c.Type == UserActivityType.Login).Max(c => c.DateTime)
                            : (DateTime?)null,
                        Role = c.UserRoles != null
                            ? string.Join(", ", c.UserRoles.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Role.Name))
                            : string.Empty,
                    })
                    .ToQueryAsync(obj);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("ChoiceItems/{type}/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ChoiceItemsAsync([FromRoute] string type, [FromRoute] int id, [FromBody] TableData obj)
        {
            try
            {
                var query = _repository.Queryable().AsNoTracking()
                    .Where(c => !c.Locked.HasValue || !c.Locked.Value);
                switch (type.ToLower())
                {
                    case "team":
                        {
                            var ids = _userTeamRepository.Queryable().FilterQueryNoTraking()
                                .Where(c => c.TeamId == id)
                                .Select(c => c.UserId)
                                .Distinct()
                                .ToList();
                            query = query.Where(c => ids.Contains(c.Id));
                        }
                        break;
                    case "role":
                        {
                            var ids = _userRoleRepository.Queryable().AsNoTracking()
                                .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                                .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                                .Where(c => c.RoleId == id)
                                .Select(c => c.UserId)
                                .Distinct()
                                .ToList();
                            query = query.Where(c => ids.Contains(c.Id));
                        }
                        break;
                    case "department":
                        query = query.Where(c => c.DepartmentId.HasValue).Where(c => c.DepartmentId.Value == id);
                        break;
                }
                var result = await query
                    .Select(c => new
                    {
                        c.Id,
                        c.Email,
                        c.Avatar,
                        c.Locked,
                        c.FullName,
                        c.IsActive,
                        c.IsDelete,
                        c.CreatedDate,
                        c.UpdatedDate,
                        Phone = c.PhoneNumber,
                        CreatedBy = c.CreatedByUser != null ? c.CreatedByUser.UserName : string.Empty,
                        UpdatedBy = c.UpdatedByUser != null ? c.UpdatedByUser.UserName : string.Empty,
                        LastLogin = c.Activities != null
                            ? c.Activities.Where(p => p.UserId == c.Id).Where(c => c.Type == UserActivityType.Login).Max(c => c.DateTime)
                            : (DateTime?)null,
                        Role = c.UserRoles != null
                            ? string.Join(", ", c.UserRoles.Where(c => c.IsActive.HasValue).Where(c => c.IsActive.Value).Select(p => p.Role.Name))
                            : string.Empty,
                    })
                    .ToQueryAsync(obj);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        private List<Department> GetDepartmentAndChildren(List<Department> departments, int parentId)
        {
            var result = departments.Where(d => d.Id == parentId).ToList();
            result.AddRange(departments.Where(d => d.ParentId == parentId).SelectMany(d => GetDepartmentAndChildren(departments, d.Id)));
            return result;
        }
    }
}
