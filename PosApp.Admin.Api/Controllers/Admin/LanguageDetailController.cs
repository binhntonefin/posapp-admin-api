using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.EF.Trackable.Entities;
using PosApp.Admin.Api.Helpers;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class LanguageDetailController : AdminBaseController<LanguageDetail>
    {
        public LanguageDetailController(IServiceProvider serviceProvider) : base(serviceProvider)
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
                        c.Table,
                        c.Value,
                        c.ObjectId,
                        c.Property,
                        c.IsActive,
                        c.IsDelete,
                        c.LanguageId,
                        c.CreatedDate,
                        c.UpdatedDate,
                        Language = c.Language.Name,
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
                        c.Table,
                        c.Value,
                        c.ObjectId,
                        c.Property,
                        c.IsActive,
                        c.IsDelete,
                        c.LanguageId,
                        c.CreatedDate,
                        c.UpdatedDate,
                        Language = c.Language.Name,
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

        [HttpPost("Delete")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> DeleteAsync([FromBody] LanguageDetailModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var entities = await Repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.LanguageId == model.LanguageId)
                    .Where(c => c.ObjectId == model.ObjectId)
                    .Where(c => c.Table == model.Table)
                    .ToListAsync();
                if (entities.IsNullOrEmpty())
                    return NotFound();

                foreach (var entity in entities)
                    Repository.Delete(entity);
                await UnitOfWork.SaveChangesAsync();
                return Ok(ResultApi.ToEntity(true));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("ItemByObjectId")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetItemByObjectIdAsync([FromBody] LanguageDetailModel model)
        {
            try
            {
                var entities = await Repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.LanguageId == model.LanguageId)
                    .Where(c => c.ObjectId == model.ObjectId)
                    .Where(c => c.Table == model.Table)
                    .ToListAsync();
                if (entities.IsNullOrEmpty())
                    return Ok(ResultApi.ToEntity(null));

                var result = new Dictionary<string, string>();
                foreach (var entity in entities)
                    result.Add(entity.Property, entity.Value);
                return Ok(ResultApi.ToEntity(result));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("AddOrUpdate")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AddOrUpdateAsync([FromBody] LanguageDetailUpdateModel model)
        {
            try
            {
                var entities = await Repository.Queryable()
                    .Where(c => c.LanguageId == model.LanguageId)
                    .Where(c => c.ObjectId == model.ObjectId)
                    .Where(c => c.Table == model.Table)
                    .ToListAsync() ?? new List<LanguageDetail>();
                foreach (var property in model.Properties)
                {
                    var entity = entities.Where(c => c.Property == property.Key).FirstOrDefault();
                    if (entity == null)
                    {
                        entity = new LanguageDetail
                        {
                            Table = model.Table,
                            Value = property.Value,
                            Property = property.Key,
                            ObjectId = model.ObjectId,
                            LanguageId = model.LanguageId,
                        };
                        Repository.Insert(entity);
                    }
                    else
                    {
                        entity.IsActive = true;
                        entity.IsDelete = false;
                        entity.Value = property.Value;
                        Repository.Update(entity);
                    }
                    await UnitOfWork.SaveChangesAsync();
                }
                return Ok(ResultApi.ToEntity(true));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("Items/{table}/{objectId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult GetAll([FromRoute] string table, [FromRoute] int objectId, [FromBody] TableData obj)
        {
            try
            {
                var entities = Repository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.ObjectId == objectId)
                    .Where(c => c.Table == table)
                    .Select(c => new
                    {
                        c.Table,
                        c.Value,
                        c.Property,
                        c.ObjectId,
                        c.LanguageId,
                        Language = c.Language.Name,
                    })
                    .ToList();
                var languages = entities.Select(c => c.LanguageId).Distinct().ToList();
                var resultObj = new List<Dictionary<string, object>>();
                foreach (var languageId in languages)
                {
                    var languageName = entities.FirstOrDefault(c => c.LanguageId == languageId)?.Language;
                    var dic = new Dictionary<string, object>();
                    var keyValues = entities.Where(c => c.LanguageId == languageId).ToList();
                    foreach (var key in keyValues)
                    {
                        if (!dic.ContainsKey("Id"))
                            dic.Add("Id", key.ObjectId);
                        if (!dic.ContainsKey("Table"))
                            dic.Add("Table", key.Table);
                        dic.Add(key.Property, key.Value);
                    }
                    dic.Add("Language", languageName);
                    dic.Add("LanguageId", languageId);
                    resultObj.Add(dic);
                }

                if (obj == null) obj = new TableData();
                obj.Paging = new PagingData
                {
                    Index = 1,
                    Size = 1000,
                    Total = resultObj.Count,
                };
                return Ok(ResultApi.ToEntity(resultObj, obj));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }
    }
}
