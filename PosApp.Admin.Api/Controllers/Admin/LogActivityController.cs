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
    public class LogActivityController : AdminBaseController<LogActivity>
    {
        public LogActivityController(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        [HttpPost("Items")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAllAsync([FromBody] TableData obj)
        {
            try
            {
                var result = await Repository.Queryable().FilterQueryNoTraking()
                    .Select(c => new
                    {
                        c.Id,
                        c.Ip,
                        c.Url,
                        c.Body,
                        c.Notes,
                        c.Method,
                        c.Action,
                        c.ObjectId,
                        c.DateTime,
                        c.IsActive,
                        c.IsDelete,
                        c.Controller,
                        c.User.Email,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.User.FullName,
                        Phone = c.User.PhoneNumber,
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
                var table = await Repository.Queryable().FilterQueryNoTraking()
                    .Select(c => new
                    {
                        c.Id,
                        c.Ip,
                        c.Url,
                        c.Body,
                        c.Notes,
                        c.Method,
                        c.Action,
                        c.ObjectId,
                        c.DateTime,
                        c.IsActive,
                        c.IsDelete,
                        c.Controller,
                        c.User.Email,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.User.FullName,
                        Phone = c.User.PhoneNumber,
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
    }
}
