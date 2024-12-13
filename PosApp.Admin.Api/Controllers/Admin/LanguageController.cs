using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Services.Contract;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class LanguageController : AdminBaseController<Language>
    {
        public LanguageController(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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
                        c.Code,
                        c.Icon,
                        c.IsActive,
                        c.IsDelete,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.Description,
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
                        c.Code,
                        c.Icon,
                        c.IsActive,
                        c.IsDelete,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.Description,
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
