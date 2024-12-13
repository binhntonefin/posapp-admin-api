using System.Data;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Helpers;
using URF.Core.EF.Trackable.Enums;
using URF.Core.Helper;
using PosApp.Admin.Api.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace PosApp.Admin.Api.Services.Implement
{
    public class PermissionService : ServiceX, IPermissionService
    {
        private readonly IRepositoryX<Permission> _repository;
        private readonly IRepositoryX<UserRole> _userRoleRepository;
        private readonly IRepositoryX<Permission> _permissionRepository;
        private readonly IRepositoryX<RolePermission> _rolePermissionRepository;
        private readonly IRepositoryX<UserPermission> _userPermissionRepository;

        public PermissionService(
            IRepositoryX<Permission> repository,
            IHttpContextAccessor httpContextAccessor,
            IRepositoryX<UserRole> userRoleRepository,
            IRepositoryX<Permission> permissionRepository,
            IRepositoryX<RolePermission> rolePermissionRepository,
            IRepositoryX<UserPermission> userPermissionrepository) : base(httpContextAccessor)
        {
            _repository = repository;
            _userRoleRepository = userRoleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _userPermissionRepository = userPermissionrepository;
        }

        public ResultApi AllPermissions(int? roleId)
        {
            if (!roleId.HasValue) roleId = 0;
            var rolePermissionIds = _rolePermissionRepository.Queryable().FilterQueryNoTraking()
                .Where(c => c.Allow.HasValue && c.Allow.Value)
                .Where(c => c.RoleId == roleId)
                .Select(c => new
                {
                    c.Type,
                    c.PermissionId,
                })
                .Distinct()
                .ToList();
            var permissionIds = rolePermissionIds;

            var query = _repository.Queryable().FilterQueryNoTraking()
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Title,
                            c.Group,
                            c.Types,
                            c.Action,
                            c.Controller,
                        });
            switch (User.UserType)
            {
                case (int)AccountType.Shop:
                case (int)AccountType.Agency:
                case (int)AccountType.Collaborator:
                    {
                        var roleIds = _userRoleRepository.Queryable().FilterQueryNoTraking()
                            .Where(c => c.UserId == UserId)
                            .Select(c => c.RoleId)
                            .Distinct()
                            .ToList();
                        var allowRolePermissionIds = _rolePermissionRepository.Queryable().FilterQueryNoTraking()
                            .Where(c => roleIds.Contains(c.RoleId))
                            .Where(c => c.Allow.HasValue && c.Allow.Value)
                            .Select(c => c.PermissionId)
                            .Distinct()
                            .ToList();
                        query = query.Where(c => allowRolePermissionIds.Contains(c.Id));
                    }
                    break;
            }

            var items = query.ToList()
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Title,
                    c.Group,
                    c.Action,
                    c.Controller,
                    Allow = permissionIds.Any(p => p.PermissionId == c.Id),
                    permissionIds.FirstOrDefault(p => p.PermissionId == c.Id)?.Type,
                    Types = c.Types.IsStringNullOrEmpty() ? null : c.Types.ToObject<List<int>>(),
                }).OrderBy(c => c.Group).ToList();
            return ResultApi.ToEntity(items);
        }
        public ResultApi AllPermissions(int? userId, List<int> roleIds)
        {
            if (!userId.HasValue) userId = 0;
            var rolePermissionIds = _rolePermissionRepository.Queryable().FilterQueryNoTraking()
                        .Where(c => c.Allow.HasValue && c.Allow.Value)
                        .Where(c => roleIds.Contains(c.RoleId))
                        .Select(c => new
                        {
                            c.Type,
                            c.PermissionId,
                        })
                        .Distinct()
                        .ToList();
            var userPermissionIds = _userPermissionRepository.Queryable().FilterQueryNoTraking()
                        .Where(c => c.Allow.HasValue && c.Allow.Value)
                        .Where(c => c.UserId == userId)
                        .Select(c => new
                        {
                            c.Type,
                            c.PermissionId,
                        })
                        .Distinct()
                        .ToList();

            var permissionIds = rolePermissionIds.Union(userPermissionIds).Distinct().ToList();
            permissionIds = permissionIds.GroupBy(c => c.PermissionId)
                .Select(c => new
                {
                    Type = c.Last().Type,
                    PermissionId = c.Key,
                })
                .ToList();

            var query = _repository.Queryable().FilterQueryNoTraking()
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Title,
                            c.Group,
                            c.Types,
                            c.Action,
                            c.Controller,
                        });
            var items = query.ToList()
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Title,
                    c.Group,
                    c.Action,
                    c.Controller,
                    Allow = permissionIds.Any(p => p.PermissionId == c.Id),
                    ReadOnly = rolePermissionIds.Any(p => p.PermissionId == c.Id),
                    permissionIds.FirstOrDefault(p => p.PermissionId == c.Id)?.Type,
                    Types = c.Types.IsStringNullOrEmpty() ? null : c.Types.ToObject<List<int>>(),
                }).OrderBy(c => c.Group).ToList();
            return ResultApi.ToEntity(items);
        }
        public bool Allow(string controller, string action, int? userId = null)
        {
            if (IsAdmin) return true;
            if (action.StartsWithEx("lookup")) return true;
            if (controller.IsStringNullOrEmpty()) return false;

            action = CorrectAction(action);
            var permissions = Permissions(userId);
            controller = CorrectController(controller);
            return permissions
                .Where(c => c.Controller.EqualsEx(controller))
                .Where(c => c.Action.EqualsEx(action))
                .Any();
        }
        public PermissionType GetPermissionType(string controller, string action = "View")
        {
            var roleIds = _userRoleRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.UserId == UserId)
                    .Select(c => c.RoleId)
                    .Distinct()
                    .ToList();
            var permissionIds = _repository.Queryable().FilterQueryNoTraking()
                .Where(c => c.Controller.Equals(controller))
                .Where(c => c.Action.Equals(action))
                .Select(c => c.Id)
                .Distinct()
                .ToList();

            var permissionIdOfRoles = _rolePermissionRepository.Queryable().FilterQueryNoTraking()
                .Where(c => permissionIds.Contains(c.PermissionId))
                .Where(c => c.Allow.HasValue && c.Allow.Value)
                .Where(c => roleIds.Contains(c.RoleId))
                .Select(c => c.Type)
                .Distinct()
                .ToList();
            var permissionIdOfUsers = _userPermissionRepository.Queryable().FilterQueryNoTraking()
                .Where(c => permissionIds.Contains(c.PermissionId))
                .Where(c => c.Allow.HasValue && c.Allow.Value)
                .Where(c => c.UserId == UserId)
                .Select(c => c.Type)
                .Distinct()
                .ToList();
            var type = permissionIdOfUsers.IsNullOrEmpty()
                ? permissionIdOfRoles.OrderBy(c => c).FirstOrDefault()
                : permissionIdOfUsers.OrderBy(c => c).FirstOrDefault();
            return type;
        }

        private static string CorrectAction(string action)
        {
            switch (action)
            {
                case "":
                case "items":
                case "getall":
                    action = "view";
                    break;
                case "addnew":
                case "insert":
                    action = "add";
                    break;
                case "save":
                case "update":
                    action = "edit";
                    break;
                case "delete":
                    action = "delete";
                    break;
                case "get":
                case "item":
                    action = "viewdetail";
                    break;
            }
            return action.ToLower();
        }
        private List<Permission> Permissions(int? userId = null)
        {
            userId = userId.IsNumberNull() ? UserId : userId.Value;
            var roleIds = _userRoleRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.UserId == userId)
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
                .Where(c => c.UserId == userId)
                .Select(c => c.Permission.Id)
                .Distinct()
                .ToList();

            var permissionIds = permissionIdOfRoles.Union(permissionIdOfUsers).Distinct().ToList();
            var items = _permissionRepository.Queryable().FilterQueryNoTraking().ToList()
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
                .ToList()
                .Select(c => Mapper.Map<Permission>(c))
                .ToList();
            return items;
        }
        private static string CorrectController(string controller)
        {
            return controller.ToLower();
        }
        private Dictionary<string, PermissionType> GetPermissionTypes(string controller, List<string> actions)
        {
            var roleIds = _userRoleRepository.Queryable().FilterQueryNoTraking()
                    .OrderByDescending(c => c.RoleId)
                    .Where(c => c.UserId == UserId)
                    .Select(c => c.RoleId)
                    .Distinct()
                    .ToList();
            var allPermissions = StoreHelper.Permissions.IsNullOrEmpty()
                ? _repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.Controller.Equals(controller))
                    .Where(c => actions.Contains(c.Action))
                    .Select(c => new
                    {
                        c.Id,
                        c.Action,
                    })
                    .Distinct()
                    .ToList()
                : StoreHelper.Permissions.FilterQueryNoTraking()
                    .Where(c => c.Controller.Equals(controller))
                    .Where(c => actions.Contains(c.Action))
                    .Select(c => new
                    {
                        c.Id,
                        c.Action,
                    })
                    .Distinct()
                    .ToList();
            var allPermissionIds = allPermissions.Select(c => c.Id).Distinct().ToList();
            var allPermissionOfRoles = _rolePermissionRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => allPermissionIds.Contains(c.PermissionId))
                    .Where(c => c.Allow.HasValue && c.Allow.Value)
                    .Where(c => roleIds.Contains(c.RoleId))
                    .Select(c => new
                    {
                        c.Type,
                        c.PermissionId,
                    })
                    .Distinct()
                    .ToList();
            var allPermissionOfUsers = _userPermissionRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => allPermissionIds.Contains(c.PermissionId))
                    .Where(c => c.Allow.HasValue && c.Allow.Value)
                    .Where(c => c.UserId == UserId)
                    .Select(c => new
                    {
                        c.Type,
                        c.PermissionId,
                    })
                    .Distinct()
                    .ToList();
            var result = new Dictionary<string, PermissionType>();
            foreach (var action in actions)
            {

                var permissionIds = allPermissions.Where(c => c.Action.Equals(action))
                    .Select(c => c.Id)
                    .Distinct()
                    .ToList();

                var permissionIdOfRoles = allPermissionOfRoles.Where(c => permissionIds.Contains(c.PermissionId))
                    .Select(c => c.Type)
                    .Distinct()
                    .ToList();
                var permissionIdOfUsers = allPermissionOfUsers.Where(c => permissionIds.Contains(c.PermissionId))
                    .Select(c => c.Type)
                    .Distinct()
                    .ToList();
                var type = permissionIdOfUsers.IsNullOrEmpty()
                    ? permissionIdOfRoles.OrderBy(c => c).FirstOrDefault()
                    : permissionIdOfUsers.OrderBy(c => c).FirstOrDefault();
                if (!result.ContainsKey(action)) result.Add(action, type);
            }
            return result;
        }
    }
}
