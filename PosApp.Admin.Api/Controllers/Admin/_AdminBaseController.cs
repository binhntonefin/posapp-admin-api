using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections;
using System.Data;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable;
using URF.Core.EF.Trackable.Entities;
using URF.Core.EF.Trackable.Enums;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    public class AdminBaseController<T> : ControllerBase where T : SqlTenantEntity
    {
        protected readonly int UserId;
        protected readonly bool IsAdmin;
        private readonly IUnitOfWork _unitOfWork;
        protected readonly new AdminUserModel User;
        private readonly IRepositoryX<T> _repository;
        private readonly IRepositoryX<User> _userRepository;
        private readonly IRefreshDataService _refreshDataService;

        public IUnitOfWork UnitOfWork { get { return _unitOfWork; } }
        public IRepositoryX<T> Repository { get { return _repository; } }

        public AdminBaseController(IServiceProvider serviceProvider)
        {
            _unitOfWork = (IUnitOfWork)serviceProvider.GetService(typeof(IUnitOfWork));
            _repository = (IRepositoryX<T>)serviceProvider.GetService(typeof(IRepositoryX<T>));
            _userRepository = (IRepositoryX<User>)serviceProvider.GetService(typeof(IRepositoryX<User>));
            _refreshDataService = (IRefreshDataService)serviceProvider.GetService(typeof(IRefreshDataService));
            var httpContextAccessor = (IHttpContextAccessor)serviceProvider.GetService(typeof(IHttpContextAccessor));
            if (httpContextAccessor != null && httpContextAccessor.HttpContext != null)
            {
                var identity = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (identity != null)
                    UserId = identity.Value.ToInt32();

                var identityUser = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.UserData);
                if (identityUser != null)
                {
                    User = identityUser.Value.ToObject<AdminUserModel>();
                }    

                var identityAdmin = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role);
                if (identityAdmin != null)
                    IsAdmin = identityAdmin.Value.ToBoolean();
            }
        }

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAsync([FromRoute] int id)
        {
            try
            {
                var entity = await _repository.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }
                _repository.Detach(entity);
                return Ok(ResultApi.ToEntity(entity));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("Item/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GeItemAsync([FromRoute] int id)
        {
            try
            {
                var entity = await _repository.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }
                _repository.Detach(entity);
                return Ok(ResultApi.ToEntity(entity));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpDelete("Trash/{id?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TrashAsync([FromRoute] int? id = null, [FromQuery] string ids = null)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                if (!id.IsNumberNull())
                {
                    var entity = await _repository.FindAsync(id);
                    if (entity == null)
                        return NotFound();
                    entity.IsDelete = !entity.IsDelete;
                    _repository.Update(entity);
                    await _unitOfWork.SaveChangesAsync();
                }
                else if (!ids.IsStringNullOrEmpty())
                {
                    var arrayIds = ids.ToListId();
                    var entities = _repository.Queryable()
                        .Where(c => arrayIds.Contains(c.Id))
                        .ToList();
                    if (!entities.IsNullOrEmpty())
                    {
                        foreach (var entity in entities)
                        {
                            entity.IsDelete = !entity.IsDelete;
                            _repository.Update(entity);
                        }
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                // notify
                var name = typeof(T).Name;
                await _refreshDataService.RefreshLoadData(name);
                return Ok(ResultApi.ToEntity(id));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpDelete("{id?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAsync([FromRoute] int? id = null, [FromQuery] string ids = null)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                if (!id.IsNumberNull())
                {
                    var entity = await _repository.FindAsync(id);
                    if (entity == null)
                        return NotFound();

                    _repository.Delete(entity);
                    await _unitOfWork.SaveChangesAsync();
                }
                else if (!ids.IsStringNullOrEmpty())
                {
                    var arrayIds = ids.ToListId();
                    var entities = _repository.Queryable()
                        .Where(c => arrayIds.Contains(c.Id))
                        .ToList();
                    if (!entities.IsNullOrEmpty())
                    {
                        foreach (var entity in entities)
                            _repository.Delete(entity);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                // notify
                var name = typeof(T).Name;
                await _refreshDataService.RefreshLoadData(name);
                return Ok(ResultApi.ToEntity(id));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("Active/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActiveAsync([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var entity = await _repository.FindAsync(id);
                if (entity == null)
                {
                    return NotFound();
                }

                entity.IsActive = !entity.IsActive;
                _repository.Update(entity);
                await _unitOfWork.SaveChangesAsync();

                // notify
                var name = typeof(T).Name;
                await _refreshDataService.RefreshLoadData(name);
                return Ok(ResultApi.ToEntity(id));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InsertAsync([FromBody] T entity)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                _repository.Insert(entity);
                await _unitOfWork.SaveChangesAsync();

                // notify
                var name = typeof(T).Name;
                await _refreshDataService.RefreshLoadData(name);
                return Ok(ResultApi.ToEntity(entity));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPost("BatchInsert")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BatchInsertAsync([FromBody] List<T> entities)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                foreach (var entity in entities)
                    _repository.Insert(entity);
                await _unitOfWork.SaveChangesAsync();

                // notify
                var name = typeof(T).Name;
                await _refreshDataService.RefreshLoadData(name);
                return Ok(ResultApi.ToEntity(entities));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpPut]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAsync([FromBody] T entity, [FromQuery] string columns = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                if (columns.IsStringNullOrEmpty())
                {
                    var entityDb = await _repository.FindAsync(entity.Id);
                    if (entityDb == null)
                    {
                        return NotFound();
                    }
                    entityDb = Mapper.MapTo<T>(entity, entityDb);

                    _repository.Update(entityDb);
                    await _unitOfWork.SaveChangesAsync();

                    // notify
                    var name = typeof(T).Name;
                    await _refreshDataService.RefreshLoadData(name);
                    return Ok(ResultApi.ToEntity(entityDb));
                }
                else
                {
                    var arrayColumns = columns.ToListString();
                    _repository.Attach(entity);
                    _repository.Update(entity, arrayColumns);
                    await _unitOfWork.SaveChangesAsync();

                    // notify
                    var name = typeof(T).Name;
                    await _refreshDataService.RefreshLoadData(name);
                    return Ok(ResultApi.ToEntity(entity));
                }
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("Exists/{id?}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public IActionResult Exists([FromRoute] int id = default, string property = default, string value = default)
        {
            try
            {
                var query = _repository.Queryable()
                    .FilterQueryNoTraking()
                    .Where(property + ".Equals(@0)", value);
                if (!id.IsNumberNull())
                    query = query.Where("!Id.Equals(@0)", id);
                var exists = query.Any();
                if (!exists)
                {
                    var valueInt = value.ToInt32();
                    if (!valueInt.IsNumberNull())
                    {
                        query = _repository.Queryable()
                            .FilterQueryNoTraking()
                            .Where(property + ".Equals(@0)", valueInt);
                        if (!id.IsNumberNull())
                            query = query.Where("!Id.Equals(@0)", id);
                        exists = query.Any();
                    }
                }
                return Ok(ResultApi.ToEntity(exists));
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        [HttpGet("Lookup")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ContentResult), StatusCodes.Status500InternalServerError)]
        public IActionResult Lookup([FromQuery] string properties = default, string search = default, bool datetime = false, int pageIndex = 1, int pageSize = 2000, string value = default)
        {
            try
            {
                if (value.IsStringNullOrEmpty())
                {
                    if (properties.IsStringNullOrEmpty())
                    {
                        var entities = _repository.Queryable().FilterQueryNoTraking().ToList();
                        return Ok(ResultApi.ToEntity(entities));
                    }
                    else
                    {
                        var paging = new TableData
                        {
                            Paging = new PagingData
                            {
                                Size = pageSize,
                                Index = pageIndex,
                            }
                        };
                        if (properties != null && properties.Contains(";"))
                        {
                            var propertieNames = properties.Split(';').ToList();
                            var model = new TableData()
                            {
                                Orders = new List<OrderData> { new OrderData { Name = propertieNames.FirstOrDefault(), Type = OrderType.Asc } }
                            };
                            var entities = _repository.Queryable().FilterQueryNoTraking().ToOrder(model)
                                .ToSelect(propertieNames).Cast<dynamic>()
                                .ToPaging(paging)
                                .Distinct()
                                .ToList();
                            return Ok(ResultApi.ToEntity(entities));
                        }
                        else
                        {
                            var model = new TableData()
                            {
                                Orders = new List<OrderData> { new OrderData { Name = properties, Type = OrderType.Asc } }
                            };
                            if (datetime)
                            {
                                try
                                {
                                    var entities = _repository.Queryable().FilterQueryNoTraking().ToOrder(model)
                                        .ToSelect(properties + ".Date").Cast<dynamic>()
                                        .ToPaging(paging)
                                        .Distinct()
                                        .ToList();
                                    return Ok(ResultApi.ToEntity(entities));
                                }
                                catch
                                {
                                    var entities = _repository.Queryable().FilterQueryNoTraking().ToOrder(model)
                                        .Where(properties + ".HasValue")
                                        .ToSelect(properties + ".Value.Date").Cast<dynamic>()
                                        .ToPaging(paging)
                                        .Distinct()
                                        .ToList();
                                    return Ok(ResultApi.ToEntity(entities));
                                }
                            }
                            else
                            {
                                var entities = search.IsStringNullOrEmpty()
                                        ? _repository.Queryable().FilterQueryNoTraking().ToOrder(model)
                                            .ToSelect(properties).Cast<dynamic>()
                                            .Distinct().ToPaging(paging)
                                            .ToList()
                                        : _repository.Queryable().FilterQueryNoTraking().ToOrder(model).ToSelect(properties)
                                            .Where(properties + ".Contains(@0)", search).Cast<dynamic>()
                                            .Distinct().ToPaging(paging)
                                            .ToList();
                                return Ok(ResultApi.ToEntity(entities));
                            }
                        }
                    }
                }
                else
                {
                    var paging = new TableData
                    {
                        Paging = new PagingData
                        {
                            Size = pageSize,
                            Index = pageIndex,
                        }
                    };
                    var key = properties.IsStringNullOrEmpty() ? "Id" : properties.ToListString().FirstOrDefault();
                    if (key.IsStringNullOrEmpty()) key = "Id";
                    if (properties.IsStringNullOrEmpty()) properties = "Name";

                    var propertieNames = properties.ToListString();
                    if (key.EqualsEx("Id"))
                    {
                        var id = value.ToInt32();
                        var entities = _repository.Queryable().Where(c => c.Id == id)
                                    .ToSelect(propertieNames).Cast<dynamic>()
                                    .ToList();
                        var otherEntities = _repository.Queryable().FilterQueryNoTraking()
                                .ToSelect(propertieNames).Cast<dynamic>()
                                .ToPaging(paging)
                                .Distinct()
                                .ToList();
                        if (!otherEntities.IsNullOrEmpty()) entities.AddRange(otherEntities);
                        entities = entities.Distinct().ToList();
                        return Ok(ResultApi.ToEntity(entities));
                    }
                    else
                    {
                        var entities = _repository.Queryable()
                            .Where(key + ".Equals(@0)", value)
                            .ToSelect(propertieNames).Cast<dynamic>()
                            .ToList();
                        var otherEntities = _repository.Queryable().FilterQueryNoTraking()
                                .ToSelect(propertieNames).Cast<dynamic>()
                                .ToPaging(paging)
                                .Distinct()
                                .ToList();
                        if (!otherEntities.IsNullOrEmpty()) entities.AddRange(otherEntities);
                        entities = entities.Distinct().ToList();
                        return Ok(ResultApi.ToEntity(entities));
                    }
                }
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex, UserId);
            }
        }

        protected void CorrectExportData(TableData obj)
        {
            UtilityHelper.CorrectExportData(obj);
        }
        protected string GetFilter(TableData obj, string filter)
        {
            // filter Sale
            var filterItem = obj?.Filters?.FirstOrDefault(c => c.Name == filter);
            return filterItem != null ? filterItem.Value.ToString() : string.Empty;
        }
        protected FileContentResult Export(TableData obj, DataTable table)
        {
            var ignoreProperties = new string[] { "Id", "IsActive", "IsDelete", "CreatedBy", "UpdatedBy", "CreatedDate", "UpdatedDate" };
            if (table != null && table.Columns.Count > 0)
            {
                foreach (var property in ignoreProperties)
                {
                    var column = table.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName == property);
                    if (column != null)
                        table.Columns.Remove(column);
                }
            }
            switch (obj.Export.Type)
            {
                case ExportType.Csv:
                    return ExportHelper.ExportToCsv(obj.Name, table);
                case ExportType.Excel:
                    return ExportHelper.ExportToExcel(obj.Name, table);
            }
            return ExportHelper.ExportToCsv(obj.Name, table);
        }
        protected List<IDictionary<string, object>> ToListDictionaries(object obj)
        {
            if (obj == null) return null;
            var items = obj is IDictionary<string, object>
                ? new[] { obj }
                : obj is IEnumerable ? (IEnumerable)obj : new[] { obj };
            var dictionaries = new List<IDictionary<string, object>>();
            foreach (var item in items)
            {
                var dic = item is IDictionary<string, object>
                    ? (IDictionary<string, object>)item
                    : item.ToDictionary();
                dictionaries.Add(dic);
            }
            return dictionaries;
        }
    }

    [ApiController]
    public class AdminApiBaseController : ControllerBase
    {
        protected readonly int UserId;
        protected readonly bool IsAdmin;
        private readonly IUnitOfWork _unitOfWork;
        protected readonly new AdminUserModel User;
        public IUnitOfWork UnitOfWork { get { return _unitOfWork; } }

        public AdminApiBaseController(IServiceProvider serviceProvider)
        {
            _unitOfWork = (IUnitOfWork)serviceProvider.GetService(typeof(IUnitOfWork));
            var httpContextAccessor = (IHttpContextAccessor)serviceProvider.GetService(typeof(IHttpContextAccessor));
            if (httpContextAccessor != null && httpContextAccessor.HttpContext != null)
            {
                var identity = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (identity != null)
                    UserId = identity.Value.ToInt32();

                var identityUser = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.UserData);
                if (identityUser != null)
                    User = identityUser.Value.ToObject<AdminUserModel>();

                var identityAdmin = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role);
                if (identityAdmin != null)
                    IsAdmin = identityAdmin.Value.ToBoolean();
            }
        }
    }
}
