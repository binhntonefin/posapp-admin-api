using PosApp.Admin.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URF.Core.EF.Trackable.Entities;
using URF.Core.EF.Trackable.Models;
using URF.Core.EF;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class AuditController : AdminBaseController<Audit>
    {
        public AuditController(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        [HttpPost("Export")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ExportAsync([FromBody] TableData obj)
        {
            try
            {
                CorrectExportData(obj);
                var table = await Repository.Queryable()
                    .Select(c => new
                    {
                        c.Id,
                        c.UserId,
                        c.Action,
                        c.EndTime,
                        c.NewData,
                        c.OldData,
                        c.IsActive,
                        c.IsDelete,
                        c.TableName,
                        c.StartTime,
                        c.IpAddress,
                        c.User.Email,
                        c.MachineName,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.TableIdValue,
                        c.User.FullName,
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

        [HttpPost("Items")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAllAsync([FromQuery] string controller, [FromQuery] long id, [FromBody] TableData obj)
        {
            try
            {
                var result = await Repository.Queryable()
                    .Where(c => c.TableName == controller)
                    .Where(c => c.TableIdValue == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.UserId,
                        c.Action,
                        c.EndTime,
                        c.NewData,
                        c.OldData,
                        c.IsActive,
                        c.IsDelete,
                        c.TableName,
                        c.StartTime,
                        c.IpAddress,
                        c.User.Email,
                        c.MachineName,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.TableIdValue,
                        c.User.FullName,
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
