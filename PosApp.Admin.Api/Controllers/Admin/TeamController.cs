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
    public class TeamController : AdminBaseController<Team>
    {
        private readonly ITeamService _service;
        public TeamController(
            ITeamService service,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _service = service;
        }

        [HttpGet("Detail/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Detail([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var item = Repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.Id == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Code,
                        c.Description,
                        UserIds = c.UserTeams.Select(p => p.UserId)
                    })
                    .FirstOrDefault();
                return Ok(ResultApi.ToEntity(item));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("AllTeams/{userId?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult AllTeams([FromRoute] int? userId)
        {
            try
            {
                var result = _service.AllTeams(userId);
                return Ok(result);
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
                        Amount = c.UserTeams.Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                            .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                            .Count(),
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
                        Amount = c.UserTeams.Count(),
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

        [HttpPut("AddOrUpdate")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AddOrUpdateAsync([FromBody] TeamModel entity)
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

        [HttpPut("AddUsers/{teamId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AddUsersAsync([FromRoute] int teamId, [FromBody] List<int> items)
        {
            try
            {
                var result = await _service.AddUsers(teamId, items);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut("UpdateUsers/{teamId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdateUsersAsync([FromRoute] int teamId, [FromBody] List<int> items)
        {
            try
            {
                var result = await _service.UpdateUsers(teamId, items);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }
    }
}
