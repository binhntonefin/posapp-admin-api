using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Services.Contract;
using PosApp.Admin.Api.Services.Implement;
using URF.Core.EF.Trackable.Enums;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class NotifyController : AdminBaseController<Notify>
    {
        private readonly INotifyService _service;

        public NotifyController(
            INotifyService service,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _service = service;
        }

        [HttpPost("ReadAllNotify")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ReadAllNotifyAsync()
        {
            try
            {
                var ids = Repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.UserId == UserId)
                    .Where(c => !c.IsRead)
                    .Select(c => c.Id)
                    .Distinct()
                    .ToList();
                if (!ids.IsNullOrEmpty())
                {
                    foreach (var id in ids)
                        await _service.ReadNotifyAsync(id);
                }                
                return Ok(ResultApi.ToSuccess());
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("CreateNotify")]
        public async Task<IActionResult> CreateNotifyAsync()
        {
            try
            {
                await _service.AddNotifyAsync(new Notify
                {
                    IsRead = false,
                    DateTime = DateTime.Now,
                    Type = (int)NotifyType.UpdateGroup,
                    Title = "Bạn đã được thêm vào nhóm: xxx",
                }, new List<int> { 8 });
                return Ok(ResultApi.ToSuccess());
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("ReadNotify/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ReadNotifyAsync([FromRoute] int id)
        {
            try
            {
                var result = await _service.ReadNotifyAsync(id);
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
                        c.Type,
                        c.Title,
                        c.UserId,
                        c.IsRead,
                        c.Content,
                        c.IsActive,
                        c.IsDelete,
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

        [HttpGet("MyNotifies")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult MyNotifies([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = _service.MyNotifies(pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("MyNotifiesUnRead")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult MyNotifiesUnRead([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 99)
        {
            try
            {
                var result = _service.MyNotifiesUnRead(pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }
    }
}
