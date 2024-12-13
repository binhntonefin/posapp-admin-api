using PosApp.Admin.Api.Data.Enums;
using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Services.Contract;
using Microsoft.EntityFrameworkCore;
using System.Data;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Services.Implement
{
    public class UtilityService : IUtilityService
    {
        private readonly IRepositoryX<User> _userRepository;
        private readonly IRepositoryX<Team> _teamRepository;
        private readonly IRepositoryX<Role> _roleRepository;
        private readonly IRepositoryX<UserTeam> _userTeamRepository;
        private readonly IRepositoryX<Permission> _permissionRepository;
        private readonly IRepositoryX<Department> _departmentRepository;
        private readonly IRepositoryX<LinkPermission> _linkPermissionRepository;

        public UtilityService(
            IRepositoryX<User> userRepository,
            IRepositoryX<Team> teamRepository,
            IRepositoryX<Role> roleRepository,
            IRepositoryX<UserTeam> userTeamRepository,
            IRepositoryX<Permission> permissionRepository,
            IRepositoryX<Department> departmentRepository,
            IRepositoryX<LinkPermission> linkPermissionRepository)
        {
            _userRepository = userRepository;
            _teamRepository = teamRepository;
            _roleRepository = roleRepository;
            _userTeamRepository = userTeamRepository;
            _permissionRepository = permissionRepository;
            _departmentRepository = departmentRepository;
            _linkPermissionRepository = linkPermissionRepository;
        }

        public ResultApi Controllers()
        {
            var items = UtilityHelper.FindControllers();
            var options = items.Select(c => new
            {
                Id = c,
                Name = c
            }).OrderBy(c => c.Name).ToList();
            return ResultApi.ToEntity(options);
        }

        public ResultApi Actions(string controller = default)
        {
            var items = UtilityHelper.FindActions(controller);
            var options = items.Select(c => new { Id = c, Name = c }).ToList();
            return ResultApi.ToEntity(options);
        }

        public ResultApi ResetCache(CachedType? type = null)
        {
            var types = new List<CachedType>();
            if (type.HasValue)
                types.Add(type.Value);
            else
            {
                var items = Enum.GetValues(typeof(CachedType));
                foreach (var item in items)
                    types.Add((CachedType)item);
            }
            foreach (var item in types)
            {
                switch (item)
                {
                    case CachedType.User:
                        StoreHelper.Users = _userRepository.Queryable().FilterQueryNoTraking().ToList();
                        break;
                    case CachedType.Team:
                        StoreHelper.Teams = _teamRepository.Queryable().FilterQueryNoTraking().ToList();
                        StoreHelper.UserTeams = _userTeamRepository.Queryable().FilterQueryNoTraking().ToList();
                        break;
                    case CachedType.Role:
                        StoreHelper.Roles = _roleRepository.Queryable().AsNoTracking().ToList();
                        break;
                    case CachedType.Department:
                        StoreHelper.Departments = _departmentRepository.Queryable().FilterQueryNoTraking().ToList();
                        break;
                    case CachedType.Permission:
                        StoreHelper.Permissions = _permissionRepository.Queryable().FilterQueryNoTraking().ToList();
                        break;
                    case CachedType.LinkPermission:
                        StoreHelper.LinkPermissions = _linkPermissionRepository.Queryable().FilterQueryNoTraking().ToList();
                        break;
                }
            }
            return ResultApi.ToEntity(true);
        }
    }
}
