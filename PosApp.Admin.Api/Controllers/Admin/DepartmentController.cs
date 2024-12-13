using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Controllers.Admin;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Services.Contract;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class DepartmentController : AdminBaseController<Department>
    {
        private readonly IDepartmentService _service;
        public DepartmentController(
            IDepartmentService service,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _service = service;
        }

        [HttpGet("AllItems")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAllItemsAsync()
        {
            try
            {
                var items = await Repository.Queryable().FilterQueryNoTraking()
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Code,
                        c.ParentId,
                        Parent = c.Parent != null ? new
                        {
                            c.Parent.Id,
                            c.Parent.Code,
                            c.Parent.Name
                        } : null
                    })
                    .ToListAsync();
                return Ok(ResultApi.ToEntity(items));
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
                var result = await Repository.Queryable().AsNoTracking()
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Code,
                        c.IsActive,
                        c.IsDelete,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.Description,
                        Amount = c.Users.Count(),
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
                        c.Code,
                        c.IsActive,
                        c.IsDelete,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.Description,
                        Amount = c.Users.Count(),
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

        [HttpGet("LookupDepartment/{parentId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> LookupDepartmentAsync([FromRoute] int parentId)
        {
            try
            {
                var items = await Repository.Queryable().AsNoTracking()
                    .Where(c => c.ParentId.HasValue)
                    .Where(c => c.ParentId.Value == parentId)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Code,
                    })
                    .ToListAsync();
                return Ok(ResultApi.ToEntity(items));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut("AddOrUpdate")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AddOrUpdateAsync([FromBody] DepartmentModel entity)
        {
            try
            {
                var result = await _service.AddOrUpdateAsync(entity);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut("AddUsers/{departmentId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AddUsersAsync([FromRoute] int departmentId, [FromBody] List<int> items)
        {
            try
            {
                var result = await _service.AddUsers(departmentId, items);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut("UpdateUsers/{departmentId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdateUsersAsync([FromRoute] int departmentId, [FromBody] List<int> items)
        {
            try
            {
                var result = await _service.UpdateUsers(departmentId, items);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }
    }
}
