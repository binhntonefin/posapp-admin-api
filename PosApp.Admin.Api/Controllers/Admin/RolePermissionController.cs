using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Services.Contract;
using PosApp.Admin.Api.Helpers;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class RolePermissionController : AdminBaseController<RolePermission>
    {
        private readonly ISecurityService _securityService;

        public RolePermissionController(
            ISecurityService securityService,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _securityService = securityService;
        }

        [HttpGet("Permissions/{roleId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Permissions([FromRoute] int? roleId)
        {
            try
            {
                var result = _securityService.RolePermissions(roleId);
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
                        c.Allow,
                        c.RoleId,
                        c.IsActive,
                        c.IsDelete,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.PermissionId,
                        Role = c.Role != null ? c.Role.Name : string.Empty,
                        Permission = c.Permission != null ? c.Permission.Name : string.Empty,
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
    }
}
