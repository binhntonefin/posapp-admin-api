﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using PosApp.Admin.Api.Helpers;
using URF.Core.EF.Trackable.Entities;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class LogExceptionController : AdminBaseController<LogException>
    {
        public LogExceptionController(IServiceProvider serviceProvider) : base(serviceProvider)
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
                        c.DateTime,
                        c.IsActive,
                        c.IsDelete,
                        c.Exception,
                        c.StackTrace,
                        c.User.Email,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.User.FullName,
                        c.InnerException,
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
                var table = await Repository.Queryable().AsNoTracking()
                    .Select(c => new
                    {
                        c.Id,
                        c.DateTime,
                        c.IsActive,
                        c.IsDelete,
                        c.Exception,
                        c.StackTrace,
                        c.User.Email,
                        c.CreatedDate,
                        c.UpdatedDate,
                        c.User.FullName,
                        c.InnerException,
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