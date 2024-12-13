using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Services.Contract;
using PosApp.Admin.Api.Data.Enums;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class LinkPermissionController : AdminBaseController<LinkPermission>
    {
        private readonly IUtilityService _utilityService;
        public LinkPermissionController(IServiceProvider serviceProvider, IUtilityService utilityService) : base(serviceProvider)
        {
            _utilityService = utilityService;
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
                        c.Link,
                        c.Group,
                        c.CssIcon,
                        c.IsActive,
                        c.IsDelete,
                        c.ParentId,
                        c.GroupOrder,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.PermissionId,
                        Order = c.Order ?? 0,
                        Parent = c.Parent.Name,
                        CreatedBy = c.CreatedByUser != null ? c.CreatedByUser.UserName : string.Empty,
                        UpdatedBy = c.UpdatedByUser != null ? c.UpdatedByUser.UserName : string.Empty,
                        Permission = string.Format("{0} - {1}", c.Permission.Title, c.Permission.Name),
                    })
                    .ToQueryAsync(obj);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("ResetCache")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult ResetCache()
        {
            try
            {
                var result = _utilityService.ResetCache(CachedType.LinkPermission);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }
    }
}
