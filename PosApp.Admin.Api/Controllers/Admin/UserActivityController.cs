using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Helpers;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]

    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class UserActivityController : AdminBaseController<UserActivity>
    {
        public UserActivityController(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        [HttpPost("Items")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAllAsync([FromBody] TableData obj)
        {
            try
            {
                var query = Repository.Queryable().FilterQueryNoTraking();
                if (!IsAdmin) query = query.Where(c => c.UserId == UserId);
                var result = await query
                    .Select(c => new
                    {
                        c.Id,
                        c.Ip,
                        c.Os,
                        c.Type,
                        c.UserId,
                        c.Browser,
                        c.Country,
                        c.IsActive,
                        c.IsDelete,
                        c.DateTime,
                        c.Incognito,
                        c.CreatedDate,
                        c.UpdatedDate,
                        User = c.User != null ? c.User.FullName : string.Empty,
                        Phone = c.User != null ? c.User.PhoneNumber : string.Empty,
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
