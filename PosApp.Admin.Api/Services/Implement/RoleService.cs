using System.Data;
using URF.Core.EF.Trackable.Models;
using URF.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using URF.Core.Helper.Extensions;
using URF.Core.Helper;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Data.Models;
using URF.Core.EF.Trackable.Enums;
using PosApp.Admin.Api.Data.Enums;
using System.Security.Claims;
using System.Reflection.Metadata;

namespace PosApp.Admin.Api.Services.Implement
{
    public class RoleService : IRoleService
    {
        private readonly int UserId;
        private readonly AdminUserModel User;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotifyService _notifyService;
        private readonly IRepositoryX<Role> _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepositoryX<UserRole> _userRoleRepository;
        private readonly IRepositoryX<RolePermission> _rolePermissionRepository;

        public RoleService(
            IUnitOfWork unitOfWork,
            INotifyService notifyService,
            IRepositoryX<Role> repository,
            IHttpContextAccessor httpContextAccessor,
            IRepositoryX<UserRole> userRoleRepository,
            IRepositoryX<RolePermission> rolePermissionRepository)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _notifyService = notifyService;
            _userRoleRepository = userRoleRepository;
            _httpContextAccessor = httpContextAccessor;
            _rolePermissionRepository = rolePermissionRepository;
            if (httpContextAccessor != null && httpContextAccessor.HttpContext != null)
            {
                var identity = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (identity != null)
                    UserId = identity.Value.ToInt32();

                var identityUser = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.UserData);
                if (identityUser != null)
                    User = identityUser.Value.ToObject<AdminUserModel>();
            }
        }

        public ResultApi AllRoles(int? userId)
        {
            var roleIds = userId.IsNumberNull()
                ? new List<int>()
                : _userRoleRepository.Queryable().AsNoTracking()
                        .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                        .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                        .Where(c => c.UserId == userId)
                        .Select(c => c.RoleId)
                        .Distinct()
                        .ToList() ?? new List<int>();

            var allRoleIds = new List<int>();
            switch (User.UserType)
            {
                case (int)AccountType.Operation:
                    {
                        var otherRoles = new List<string>();
                        otherRoles.AddRange(Data.Constants.Constant.ROLE_SHOPS);
                        otherRoles.AddRange(Data.Constants.Constant.ROLE_AGENCIES);
                        otherRoles.AddRange(Data.Constants.Constant.ROLE_COLLABORATORS);
                        allRoleIds = _repository.Queryable().AsNoTracking()
                            .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                            .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                            .Where(c => !otherRoles.Contains(c.Code))
                            .Select(c => c.Id)
                            .Distinct()
                            .ToList();
                    } 
                    break;
                case (int)AccountType.Shop:
                    {
                        var otherRoles = new List<string>();
                        otherRoles.AddRange(Data.Constants.Constant.ROLE_AGENCIES);
                        otherRoles.AddRange(Data.Constants.Constant.ROLE_COLLABORATORS);
                        allRoleIds = _repository.Queryable().AsNoTracking()
                            .Where(c => !otherRoles.Contains(c.Code))
                            .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                            .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                            .Where(c => Data.Constants.Constant.ROLE_SHOPS.Contains(c.Code) || c.CreatedBy == UserId)
                            .Select(c => c.Id)
                            .Distinct()
                            .ToList();
                    }
                    break;
                case (int)AccountType.Agency:
                    {
                        var otherRoles = new List<string>();
                        otherRoles.AddRange(Data.Constants.Constant.ROLE_SHOPS);
                        otherRoles.AddRange(Data.Constants.Constant.ROLE_COLLABORATORS);
                        allRoleIds = _repository.Queryable().AsNoTracking()
                            .Where(c => !otherRoles.Contains(c.Code))
                            .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                            .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                            .Where(c => Data.Constants.Constant.ROLE_AGENCIES.Contains(c.Code) || c.CreatedBy == UserId)
                            .Select(c => c.Id)
                            .Distinct()
                            .ToList();
                    }
                    break;
                case (int)AccountType.Collaborator:
                    {
                        allRoleIds = _repository.Queryable().AsNoTracking()
                            .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                            .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                            .Where(c => Data.Constants.Constant.ROLE_COLLABORATORS.Contains(c.Code) || c.CreatedBy == UserId)
                            .Select(c => c.Id)
                            .Distinct()
                            .ToList();
                    }
                    break;
            }
            var query = _repository.Queryable().AsNoTracking()
                        .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                        .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                        .Where(c => allRoleIds.Contains(c.Id))
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Code,
                            c.Description,
                            Allow = roleIds.Contains(c.Id),
                        });
            return ResultApi.ToEntity(query.ToList());
        }

        public async Task<ResultApi> Trash(int id)
        {
            var entity = await _repository.FindAsync(id);
            if (entity == null)
                return ResultApi.ToError(ErrorResult.Role.NotExists);

            if (!entity.IsDelete.HasValue || !entity.IsDelete.Value)
            {
                var count = _userRoleRepository.Queryable()
                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                    .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                    .Where(c => c.RoleId == id)
                    .Count();
                if (count > 0)
                    return ResultApi.ToError("Can't delete because have " + count + " employees");
            }

            entity.IsDelete = !entity.IsDelete;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return ResultApi.ToEntity(id);
        }

        public async Task<ResultApi> AddUsers(int id, List<int> items)
        {
            var userRoles = _userRoleRepository.Queryable().Where(c => c.RoleId == id).ToList() ?? new List<UserRole>();
            if (!items.IsNullOrEmpty())
            {
                foreach (var userId in items)
                {
                    var itemDb = userRoles
                            .Where(c => c.RoleId == id)
                            .FirstOrDefault(c => c.UserId == userId);
                    if (itemDb == null)
                    {
                        itemDb = new UserRole
                        {
                            RoleId = id,
                            UserId = userId
                        };
                        _userRoleRepository.Insert(itemDb);
                    }
                    else
                    {
                        itemDb.IsActive = true;
                        _userRoleRepository.Update(itemDb);
                    }
                }
                await _unitOfWork.SaveChangesAsync();

                // notify
                await _notifyService.AddNotifyAsync(new Notify
                {
                    IsRead = false,
                    DateTime = DateTime.Now,
                    Title = "Admin update role",
                    Type = (int)NotifyType.UpdateRole,
                }, items);
            }
            return ResultApi.ToEntity(true);
        }

        public async Task<ResultApi> UpdateUsers(int id, List<int> items)
        {
            var userRoles = _userRoleRepository.Queryable().Where(c => c.RoleId == id).ToList() ?? new List<UserRole>();
            var nextIds = items.IsNullOrEmpty() ? new List<int>() : items.Distinct().OrderBy(c => c).ToList();
            var currentIds = userRoles.Select(c => c.UserId).Distinct().OrderBy(c => c).ToList();
            var needNotify = nextIds.ToJson() != currentIds.ToJson();
            foreach (var item in userRoles) 
                item.IsActive = false;

            // update permission
            if (!items.IsNullOrEmpty())
            {
                foreach (var userId in items)
                {
                    var itemDb = userRoles
                            .Where(c => c.RoleId == id)
                            .FirstOrDefault(c => c.UserId == userId);
                    if (itemDb == null)
                    {
                        itemDb = new UserRole
                        {
                            RoleId = id,
                            UserId = userId
                        };
                        _userRoleRepository.Insert(itemDb);
                    }
                    else
                    {
                        itemDb.IsActive = true;
                        _userRoleRepository.Update(itemDb);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // notify
            if (needNotify)
            {
                var notEffectUsers = nextIds.Intersect(currentIds).Distinct().ToList();
                var unionUserIds = nextIds.Union(currentIds).Distinct().ToList();
                var userIds = unionUserIds
                    .Where(c => !notEffectUsers.Contains(c))
                    .Distinct()
                    .ToList();
                if (!userIds.IsNullOrEmpty())
                {
                    await _notifyService.AddNotifyAsync(new Notify
                    {
                        IsRead = false,
                        DateTime = DateTime.Now,
                        Title = "Admin update role",
                        Type = (int)NotifyType.UpdateRole,
                    }, userIds);
                }
            }
            return ResultApi.ToEntity(true);
        }

        public async Task<ResultApi> AddOrUpdateRoleAsync(RoleModel model)
        {
            // check empty
            if (model == null ||
                model.Code.IsStringNullOrEmpty() ||
                model.Name.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // save role
            var entity = Mapper.Map<Role>(model);
            if (entity.Id.IsNumberNull())
            {
                _repository.Insert(entity);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var entityDb = await _repository.FindAsync(entity.Id);
                if (entityDb == null)
                    return ResultApi.ToError(ErrorResult.DataInvalid);

                entity.Id = entityDb.Id;
                entityDb = Mapper.MapTo<Role>(entity, entityDb);
                _repository.Update(entityDb);
                await _unitOfWork.SaveChangesAsync();
            }

            // update users
            await UpdateUsers(entity.Id, model.UserIds);

            // update permission
            return await UpdatePermissions(entity.Id, model.Permissions);
        }

        public async Task<ResultApi> UpdatePermissions(int id, List<RolePermissionModel> items)
        {
            // update permission
            var rolePermissions = _rolePermissionRepository.Queryable().Where(c => c.RoleId == id).ToList() ?? new List<RolePermission>();
            var nextIds = items.IsNullOrEmpty() ? string.Empty : items.Select(c => c.Id).Distinct().ToJson();
            var currentIds = rolePermissions.Select(c => c.PermissionId).Distinct().ToJson();
            var needNotify = currentIds != nextIds;
            foreach (var item in rolePermissions)
                item.Allow = false;
            if (!items.IsNullOrEmpty())
            {
                foreach (var permission in items)
                {
                    var itemDb = rolePermissions
                            .Where(c => c.RoleId == id)
                            .FirstOrDefault(c => c.PermissionId == permission.Id);
                    if (itemDb == null)
                    {
                        itemDb = new RolePermission
                        {
                            RoleId = id,
                            Allow = true,
                            Type = permission.Type,
                            PermissionId = permission.Id,
                        };
                        _rolePermissionRepository.Insert(itemDb);
                    }
                    else
                    {
                        itemDb.Allow = true;
                        itemDb.Type = permission.Type;
                        _rolePermissionRepository.Update(itemDb);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // notify
            if (needNotify)
            {
                var userIds = _userRoleRepository.Queryable().Where(c => c.RoleId == id).Select(c => c.UserId).Distinct().ToList();
                if (!userIds.IsNullOrEmpty())
                {
                    await _notifyService.AddNotifyAsync(new Notify
                    {
                        IsRead = false,
                        DateTime = DateTime.Now,
                        Title = "Admin update role",
                        Type = (int)NotifyType.UpdateRole,
                    }, userIds);
                }
            }
            return ResultApi.ToEntity(true);
        }
    }
}
