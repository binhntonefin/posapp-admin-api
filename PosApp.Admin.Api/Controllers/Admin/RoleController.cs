using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.Abstractions;
using URF.Core.EF.Trackable.Enums;
using URF.Core.Helper;
using System.Linq.Dynamic.Core;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Services.Contract;
using PosApp.Admin.Api.Data.Enums;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class RoleController : AdminApiBaseController
    {
        private readonly IRoleService _service;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepositoryX<Role> _repository;
        private readonly IRepositoryX<UserRole> _userRoleRepository;

        public RoleController(
            IRoleService service,
            IUnitOfWork unitOfWork, 
            IRepositoryX<Role> repository,
            IRepositoryX<UserRole> userRoleRepository,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _service = service;
            _unitOfWork = unitOfWork;
            _repository = repository;
            _userRoleRepository = userRoleRepository;
        }

        [HttpGet("AllRoles/{userId?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult AllRoles([FromRoute] int? userId)
        {
            try
            {
                var result = _service.AllRoles(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAsync([FromRoute] int id)
        {
            try
            {
                var entity = await _repository.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }
                _repository.Detach(entity);

                var amount = _userRoleRepository.Queryable()
                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                    .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                    .Where(c => c.RoleId == id)
                    .Count();
                var model = Mapper.Map<RoleModel>(entity);
                return Ok(ResultApi.ToEntity(model));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpDelete("Trash/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> TrashAsync([FromRoute] int id)
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
        public async Task<IActionResult> DeleteAsync([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var entity = await _repository.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }

                _repository.Delete(entity);
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
        public async Task<IActionResult> ActiveAsync([FromRoute] int id)
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

        [HttpDelete("TrashVerify/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> TrashVerifyAsync([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.Trash(id);
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
                var allRoleIds = new List<int>();
                switch (User.UserType)
                {
                    case (int)AccountType.Operation:
                        {
                            if (IsAdmin)
                            {
                                allRoleIds = _repository.Queryable().AsNoTracking()
                                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                                    .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                                    .Select(c => c.Id)
                                    .Distinct()
                                    .ToList();
                            }
                            else
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

                var result = await _repository.Queryable().AsNoTracking()
                    .Where(c => allRoleIds.Contains(c.Id))
                    .Select(c => new
                    {
                        c.Id,
                        c.Code,
                        c.Name,
                        c.IsActive,
                        c.IsDelete,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.Description,
                        CreatedBy = c.CreatedByUser != null ? c.CreatedByUser.UserName : string.Empty,
                        UpdatedBy = c.UpdatedByUser != null ? c.UpdatedByUser.UserName : string.Empty,
                        Amount = c.UserRoles.Where(c => c.IsActive.HasValue && c.IsActive.Value).Count(),
                    })
                    .ToQueryAsync(obj);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdateAsync([FromBody] RoleModel entity)
        {
            try
            {
                var result = await _service.AddOrUpdateRoleAsync(entity);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut("AddUsers/{roleId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AddUsersAsync([FromRoute] int roleId, [FromBody] List<int> items)
        {
            try
            {
                var result = await _service.AddUsers(roleId, items);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut("UpdateUsers/{roleId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdateUsersAsync([FromRoute] int roleId, [FromBody] List<int> items)
        {
            try
            {
                var result = await _service.UpdateUsers(roleId, items);
                return Ok(result);
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
                var exists = _repository.Queryable()
                           .Where(property + ".Equals(@0)", value)
                           .Where("!Id.Equals(@0)", id)
                           .Any();
                return Ok(ResultApi.ToEntity(exists));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("Lookup")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Lookup([FromQuery] string properties = default, int pageIndex = 1, int pageSize = 2000)
        {
            try
            {
                if (properties.IsStringNullOrEmpty())
                {
                    var entities = _repository.Queryable().AsNoTracking()
                        .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                        .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                        .ToList();
                    return Ok(ResultApi.ToEntity(entities));
                }
                else
                {
                    var paging = new TableData
                    {
                        Paging = new PagingData
                        {
                            Size = pageSize,
                            Index = pageIndex,
                        }
                    };
                    if (properties != null && properties.Contains(";"))
                    {
                        var propertieNames = properties.Split(';').ToList();
                        var model = new TableData()
                        {
                            Orders = new List<OrderData> { new OrderData { Name = propertieNames.FirstOrDefault(), Type = OrderType.Asc } }
                        };
                        var entities = _repository.Queryable().AsNoTracking().Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                            .Where(c => !c.IsActive.HasValue || c.IsActive.Value).ToOrder(model)
                            .ToSelect(propertieNames).Cast<dynamic>()
                            .ToPaging(paging)
                            .Distinct()
                            .ToList();
                        return Ok(ResultApi.ToEntity(entities));
                    }
                    else
                    {
                        var model = new TableData()
                        {
                            Orders = new List<OrderData> { new OrderData { Name = properties, Type = OrderType.Asc } }
                        };
                        var entities = _repository.Queryable().AsNoTracking().Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                            .Where(c => !c.IsActive.HasValue || c.IsActive.Value).ToOrder(model)
                            .ToSelect(properties).Cast<dynamic>()
                            .ToPaging(paging)
                            .Distinct()
                            .ToList();
                        return Ok(ResultApi.ToEntity(entities));
                    }
                }
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut("UpdatePermissions/{roleId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdatePermissionsAsync([FromRoute] int roleId, [FromBody] List<RolePermissionModel> items)
        {
            try
            {
                var result = await _service.UpdatePermissions(roleId, items);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }
    }
}
