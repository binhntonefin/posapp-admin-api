using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.Abstractions.Trackable;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class PermissionController : AdminBaseController<Permission>
    {
        private readonly IPermissionService _permissionService;
        private readonly IRepositoryX<LinkPermission> _linkPermissionRepository;

        public PermissionController(
            IServiceProvider serviceProvider,
            IPermissionService permissionService,
            IRepositoryX<LinkPermission> linkPermissionRepository) : base(serviceProvider)
        {
            _permissionService = permissionService;
            _linkPermissionRepository = linkPermissionRepository;
        }

        [HttpGet("AllPermissionByRole/{roleId?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult AllPermissionByRole([FromRoute] int? roleId)
        {
            try
            {
                var result = _permissionService.AllPermissions(roleId);
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
                var result = await Repository.Queryable().AsNoTracking()
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Title,
                        c.Types,
                        c.Group,
                        c.Action,
                        c.IsActive,
                        c.IsDelete,
                        c.Controller,
                        c.CreatedDate,
                        c.UpdatedDate,
                        CreatedBy = c.CreatedByUser != null ? c.CreatedByUser.UserName : string.Empty,
                        UpdatedBy = c.UpdatedByUser != null ? c.UpdatedByUser.UserName : string.Empty,
                    })
                    .ToQueryAsync(obj);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("Export")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ExportAsync([FromBody] TableData obj)
        {
            try
            {
                CorrectExportData(obj);
                var table = await Repository.Queryable().AsNoTracking()
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Title,
                        c.Types,
                        c.Group,
                        c.Action,
                        c.IsActive,
                        c.IsDelete,
                        c.Controller,
                        c.CreatedDate,
                        c.UpdatedDate,
                        CreatedBy = c.CreatedByUser != null ? c.CreatedByUser.UserName : string.Empty,
                        UpdatedBy = c.UpdatedByUser != null ? c.UpdatedByUser.UserName : string.Empty,
                    })
                    .ToDataTableAsync(obj);
                return Export(obj, table);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("CreatePermission")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAllAsync([FromBody] PermissionAutoModel model)
        {
            try
            {
                var permissions = Repository.Queryable().AsNoTracking()
                    .Where(c => c.Controller == model.Controller)
                    .ToList();
                foreach (var action in model.Actions)
                {
                    var entity = permissions
                        .Where(c => c.Action == action)
                        .FirstOrDefault();
                    if (entity == null)
                    {
                        var name = string.Empty;
                        switch (action)
                        {
                            case "View": name = "Xem"; break;
                            case "Edit": name = "Sửa"; break;
                            case "Delete": name = "Xóa"; break;
                            case "AddNew": name = "Thêm mới"; break;
                            case "ViewDetail": name = "Xem chi tiết"; break;
                        }
                        if (!name.IsStringNullOrEmpty())
                        {
                            entity = new Permission
                            {
                                Name = name,
                                Types = "[1]",
                                Action = action,
                                Title = model.Title,
                                Group = model.Group,
                                Controller = model.Controller,
                            };
                            Repository.Insert(entity);
                            await UnitOfWork.SaveChangesAsync();
                        }
                    }
                }

                permissions = Repository.Queryable().AsNoTracking()
                    .Where(c => c.Controller == model.Controller)
                    .Where(c => c.Action == "View")
                    .ToList();
                var permissionIds = permissions.Select(c => c.Id).ToList();
                var linkPermissions = _linkPermissionRepository.Queryable().AsNoTracking()
                    .Where(c => c.PermissionId.HasValue)
                    .Where(c => permissionIds.Contains(c.PermissionId.Value))
                    .ToList();
                var permission = permissions.FirstOrDefault();
                if (permission != null)
                {
                    var index = 1;
                    var linkPermission = linkPermissions.Where(c => c.PermissionId == permission.Id).FirstOrDefault();
                    if (linkPermission == null)
                    {
                        index += 1;
                        linkPermission = new LinkPermission
                        {
                            Order = index + 1,
                            Group = model.Group,
                            Name = permission.Title,
                            CssIcon = "la la-minus",
                            PermissionId = permission.Id,
                            Link = "/admin/allcategory/" + permission.Controller.ToLower()
                        };
                        _linkPermissionRepository.Insert(linkPermission);
                        await UnitOfWork.SaveChangesAsync();
                    }
                }
                return Ok(ResultApi.ToSuccess());
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("AllPermissions/{userId?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult AllPermissions([FromRoute] int? userId, [FromQuery] string roleIds)
        {
            try
            {
                var result = _permissionService.AllPermissions(userId, roleIds.ToListId());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }
    }
}
