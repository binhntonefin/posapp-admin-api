using PosApp.Admin.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Entities;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class RequestFilterController : AdminBaseController<RequestFilter>
    {
        public RequestFilterController(IServiceProvider serviceProvider) : base(serviceProvider)
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
                        c.Name,
                        c.UserId,
                        c.IsActive,
                        c.IsDelete,
                        c.Controller,
                        c.CreatedDate,
                        c.UpdatedDate,
                        User = c.User.FullName,
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
                        c.UserId,
                        c.IsActive,
                        c.IsDelete,
                        c.Controller,
                        c.CreatedDate,
                        c.UpdatedDate,
                        User = c.User.FullName,
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

        [HttpGet("MyRequestFilters")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult MyRequestFilters([FromQuery] string controller)
        {
            try
            {
                var result = Repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.Controller == controller)
                    .Where(c => c.UserId == UserId)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.UserId,
                        c.IsActive,
                        c.IsDelete,
                        c.FilterData,
                        c.Controller,
                        c.CreatedDate,
                        c.UpdatedDate,
                        User = c.User.FullName,
                        CreatedBy = c.CreatedByUser != null ? c.CreatedByUser.UserName : string.Empty,
                        UpdatedBy = c.UpdatedByUser != null ? c.UpdatedByUser.UserName : string.Empty,
                    })
                    .Distinct()
                    .ToList();
                return Ok(ResultApi.ToEntity(result));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut("AddOrUpdate")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AddOrUpdateAsync([FromBody] RequestFilter entity)
        {
            try
            {
                entity.UserId = UserId;
                var entityDb = Repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.UserId == entity.UserId)
                    .Where(c => c.Name == entity.Name)
                    .FirstOrDefault();
                entity.Id = entityDb != null ? entityDb.Id : 0;
                var result = entity.Id.IsNumberNull()
                    ? await InsertAsync(entity)
                    : await UpdateAsync(entity);
                return result;
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }
    }
}
