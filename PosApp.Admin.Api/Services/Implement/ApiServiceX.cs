using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using PosApp.Admin.Api.Data.Enums;
using PosApp.Admin.Api.Helpers;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.EF.Trackable.Enums;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.Services;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PosApp.Admin.Api.Services.Implement
{
    public class ApiServiceX : ServiceX
    {
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly HttpClientEx httpClientEx = new HttpClientEx();

        public ApiServiceX(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> CallApi(string urlSystemName, object obj = null, string id = default, MethodType type = MethodType.Get, string token = default)
        {
            var url = urlSystemName;
            if (!id.IsStringNullOrEmpty())
            {
                if (url.Contains("{id}")) url = url.Replace("{id}", id);
                else url += url.EndsWith("/") ? id : "/" + id;
            }

            var headers = new Dictionary<string, string>();
            if (!token.IsStringNullOrEmpty())
            {
                headers.Add("Authorization", token);
            }
            return await httpClientEx.CallApi(url, obj, headers, type);
        }

        public async Task<byte[]> CallApiByte(string urlSystemName, object obj = null, string id = default, MethodType type = MethodType.Get, string token = default)
        {
            if (urlSystemName != null)
            {
                var url = urlSystemName;
                if (!id.IsStringNullOrEmpty())
                {
                    if (url.Contains("{id}")) url = url.Replace("{id}", id);
                    else url += url.EndsWith("/") ? id : "/" + id;
                }

                var headers = new Dictionary<string, string>();
                if (!token.IsStringNullOrEmpty())
                {
                    headers.Add("Authorization", token);
                }

                return await httpClientEx.CallApiByte(url, obj, headers, type);
            }
            return null;
        }

        protected void AddPageSize(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null)
            {
                if (obj.Paging == null) obj.Paging = new PagingData { Index = 1, Size = 20 };
                keyValues.Add("pageIndex", obj.Paging.Index);
                keyValues.Add("pageSize", obj.Paging.Size);
            }
        }
        protected void AddPageLimit(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null)
            {
                if (obj.Paging == null) obj.Paging = new PagingData { Index = 1, Size = 20 };
                keyValues.Add("page", obj.Paging.Index);
                keyValues.Add("limit", obj.Paging.Size);
            }
        }
        protected void AddSkipLimit(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null)
            {
                if (obj.Paging == null) obj.Paging = new PagingData { Index = 1, Size = 20 };
                keyValues.Add("skip", (obj.Paging.Index - 1) * obj.Paging.Size);
                keyValues.Add("limit", obj.Paging.Size);
            }
        }
        protected void AddPagePerPage(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null)
            {
                if (obj.Paging == null)
                    obj.Paging = new PagingData { Index = 1, Size = 20 };
                if (obj.Paging.Index.IsNumberNull())
                    obj.Paging.Index = 1;
                keyValues.Add("Page", obj.Paging.Index);
                keyValues.Add("PerPage", obj.Paging.Size);
            }
        }
        protected void AddSortOneField(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null && !obj.Orders.IsNullOrEmpty())
            {
                var order = string.Empty;
                foreach (var item in obj.Orders)
                {
                    if (!order.IsStringNullOrEmpty()) order += ",";
                    order += item.Name.ToLower() + (item.Type == OrderType.Asc ? ":asc" : ":desc");
                }
                keyValues.Add("sort", order);
            }
        }
        protected void AddSortTwoField(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null && !obj.Orders.IsNullOrEmpty())
            {
                var item = obj.Orders.FirstOrDefault();
                keyValues.Add("sortDesc", item.Type == OrderType.Desc);
                keyValues.Add("sortBy", item.Name.Substring(0, 1).ToLower() + item.Name.Substring(1));
            }
        }
        protected void AddPageSizeZero(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null)
            {
                if (obj.Paging == null) obj.Paging = new PagingData { Index = 1, Size = 20 };
                keyValues.Add("pageIndex", obj.Paging.Index - 1);
                keyValues.Add("pageSize", obj.Paging.Size);
            }
        }
        protected void AddPageLimitZero(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null)
            {
                if (obj.Paging == null) obj.Paging = new PagingData { Index = 1, Size = 20 };
                keyValues.Add("page", obj.Paging.Index - 1);
                keyValues.Add("limit", obj.Paging.Size);
            }
        }
        protected void AddPageSizeShort(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null)
            {
                if (obj.Paging == null) obj.Paging = new PagingData { Index = 1, Size = 20 };
                keyValues.Add("page", obj.Paging.Index);
                keyValues.Add("size", obj.Paging.Size);
            }
        }
        protected void AddOrderTwoField(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null && !obj.Orders.IsNullOrEmpty())
            {
                var item = obj.Orders.FirstOrDefault();
                keyValues.Add("orderDir", item.Type == OrderType.Desc ? -1 : 1);
                keyValues.Add("orderColumn", item.Name.Substring(0, 1).ToLower() + item.Name.Substring(1));
            }
        }
        protected void AddPagePerPageLower(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null)
            {
                if (obj.Paging == null)
                    obj.Paging = new PagingData { Index = 1, Size = 20 };
                if (obj.Paging.Index.IsNumberNull())
                    obj.Paging.Index = 1;
                keyValues.Add("page", obj.Paging.Index);
                keyValues.Add("perPage", obj.Paging.Size);
            }
        }
        protected void AddPageSizeShortZero(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null)
            {
                if (obj.Paging == null) obj.Paging = new PagingData { Index = 1, Size = 20 };
                keyValues.Add("page", obj.Paging.Index - 1);
                keyValues.Add("size", obj.Paging.Size);
            }
        }
        protected void AddSortTwoFieldObject(TableData obj, Dictionary<string, object> keyValues)
        {
            if (obj != null && !obj.Orders.IsNullOrEmpty())
            {
                var item = obj.Orders.FirstOrDefault();
                keyValues.Add("sort", new { order = item.Type == OrderType.Asc ? "asc" : "desc" });
                keyValues.Add("field", item.Name.Substring(0, 1).ToLower() + item.Name.Substring(1));
            }
        }

        protected void CorrectPageSize(TableData model)
        {
            if (model != null)
            {
                if (model.Paging != null)
                {
                    if (model.Paging.Size <= 0) model.Paging.Size = 20;
                    if (model.Paging.Index <= 0) model.Paging.Index = 1;
                    if (model.Paging.Size > 100) model.Paging.Size = 100;
                }
                else
                {
                    model.Paging = new PagingData
                    {
                        Index = 1,
                        Size = 20,
                    };
                }
                if (model.Export != null) model.Paging.Size = model.Export.Limit;
            }
        }
        protected void CorrectPaging(TableData model, JObject jsonObj, int total = default)
        {
            if (jsonObj != null)
            {
                if (model.Paging == null) model.Paging = new PagingData();
                var apiTotal = jsonObj.HasValue("data", "total") ? jsonObj["data"]["total"].ToInt32() : 0;
                model.Paging.Total = total.IsNumberNull() ? apiTotal : Math.Max(total, apiTotal);
                model.Paging.Pages = model.Paging.Size.IsNumberNull()
                    ? 0
                    : model.Paging.Total % model.Paging.Size == 0
                        ? model.Paging.Total / model.Paging.Size
                        : model.Paging.Total / model.Paging.Size + 1;
            }
        }
        protected void CorrectPagingMeta(TableData model, JObject jsonObj, int total = default)
        {
            if (jsonObj != null && (jsonObj.HasValue() || jsonObj.HasValue("meta")))
            {
                if (model.Paging == null) model.Paging = new PagingData();
                var apiTotal = jsonObj.HasValue("meta", "total") ? jsonObj["meta"]["total"].ToInt32() : 0;
                if (apiTotal.IsNumberNull())
                {
                    if (jsonObj.ContainsKey("data"))
                    {
                        var data = jsonObj["data"];
                        if (data != null && data.GetType().Name == "JObject" && ((JObject)data).ContainsKey("meta"))
                        {
                            apiTotal = data["meta"] != null && data["meta"]["total"] != null ? data["meta"]["total"].ToInt32() : 0;
                        }
                    }
                }
                model.Paging.Total = total.IsNumberNull() ? apiTotal : Math.Max(total, apiTotal);
                model.Paging.Pages = model.Paging.Size.IsNumberNull()
                    ? 0
                    : model.Paging.Total % model.Paging.Size == 0
                        ? model.Paging.Total / model.Paging.Size
                        : model.Paging.Total / model.Paging.Size + 1;
            }
        }
        protected void CorrectPagingTotal(TableData model, JObject jsonObj, int total = default)
        {
            if (jsonObj != null)
            {
                if (model.Paging == null) model.Paging = new PagingData();
                var apiTotal = jsonObj.HasValue("total") ? jsonObj["total"].ToInt32() : 0;
                if (apiTotal.IsNumberNull())
                    apiTotal = jsonObj.HasValue("Total") ? jsonObj["Total"].ToInt32() : 0;
                if (apiTotal.IsNumberNull())
                    apiTotal = jsonObj.HasValue("totalDocs") ? jsonObj["totalDocs"].ToInt32() : 0;
                if (apiTotal.IsNumberNull())
                    apiTotal = jsonObj.HasValue("totalCount") ? jsonObj["totalCount"].ToInt32() : 0;
                if (apiTotal.IsNumberNull())
                    apiTotal = jsonObj.HasValue("totalResults") ? jsonObj["totalResults"].ToInt32() : 0;
                if (apiTotal.IsNumberNull())
                    apiTotal = jsonObj.HasValue("totalElements") ? jsonObj["totalElements"].ToInt32() : 0;
                if (apiTotal.IsNumberNull())
                    apiTotal = jsonObj.HasValue("data", "totalDocs") ? jsonObj["data"]["totalDocs"].ToInt32() : 0;

                model.Paging.Total = total.IsNumberNull() ? apiTotal : Math.Max(total, apiTotal);
                model.Paging.Pages = model.Paging.Size.IsNumberNull()
                    ? 0
                    : model.Paging.Total % model.Paging.Size == 0
                        ? model.Paging.Total / model.Paging.Size
                        : model.Paging.Total / model.Paging.Size + 1;
            }
        }
        protected void CorrectPagingCount(TableData model, JObject jsonObj, int total = default)
        {
            if (jsonObj != null)
            {
                if (model.Paging == null) model.Paging = new PagingData();
                var apiTotal = jsonObj.HasValue("count") ? jsonObj["count"].ToInt32() : 0;
                if (apiTotal.IsNumberNull())
                    apiTotal = jsonObj.HasValue("data", "count") ? jsonObj["data"]["count"].ToInt32() : 0;
                model.Paging.Total = total.IsNumberNull() ? apiTotal : Math.Max(total, apiTotal);
                model.Paging.Pages = model.Paging.Size.IsNumberNull()
                    ? 0
                    : model.Paging.Total % model.Paging.Size == 0
                        ? model.Paging.Total / model.Paging.Size
                        : model.Paging.Total / model.Paging.Size + 1;
            }
        }
        protected void AddKeyValue(Dictionary<string, object> keyValues, string key, object value)
        {
            if (keyValues.IsNullOrEmpty())
                keyValues = new Dictionary<string, object>();
            keyValues.Add(key, value);
        }
        protected void AddFilterValue(Dictionary<string, object> keyValues, string key, object value)
        {
            if (keyValues.ContainsKey(key))
                return;
            keyValues.Add(key, value);
        }
        protected void AddFilterSearch(TableData obj, Dictionary<string, object> keyValues, string key = "keyword")
        {
            if (obj != null && !obj.Search.IsStringNullOrEmpty())
                keyValues.Add(key, obj.Search);
        }
        protected void AddFilter(TableData obj, Dictionary<string, object> keyValues, string key, string property = default, int index = 1, string format = "yyyy-dd-MM")
        {
            if (keyValues.ContainsKey(key))
                return;

            if (obj != null && !obj.Filters.IsNullOrEmpty())
            {
                if (property.IsStringNullOrEmpty()) property = key;
                var filter = obj.Filters.FirstOrDefault(c => c.Name.EqualsEx(property));
                if (filter != null)
                {
                    if (index.IsNumberNull() || index == 1)
                    {
                        if (filter.Value != null)
                        {
                            if (filter.Value.GetType() == typeof(DateTime) || filter.Value.GetType() == typeof(DateTime?))
                            {
                                var value = filter.Value.ToDateTime().AddHours(7).ToString(format);
                                keyValues.Add(key, value);
                            }
                            else
                            {
                                if (format == "bool")
                                {
                                    keyValues.Add(key, filter.Value.ToBoolean());
                                }
                                else keyValues.Add(key, filter.Value);
                            }
                        }
                    }
                    else
                    {
                        if (filter.Value2 != null)
                        {
                            if (filter.Value2.GetType() == typeof(DateTime) || filter.Value2.GetType() == typeof(DateTime?))
                            {
                                var value = filter.Value2.ToDateTime().AddHours(7).ToString(format);
                                keyValues.Add(key, value);
                            }
                            else
                            {
                                if (format == "bool")
                                {
                                    keyValues.Add(key, filter.Value2.ToBoolean());
                                }
                                else keyValues.Add(key, filter.Value2);
                            }
                        }
                    }
                }
            }
        }

        protected T GetHeaderValueAs<T>(string headerName)
        {
            StringValues values;
            if (_httpContextAccessor.HttpContext?.Request?.Headers?.TryGetValue(headerName, out values) ?? false)
            {
                string rawValues = values.ToString();   // writes out as Csv when there are multiple.

                if (!rawValues.IsStringNullOrEmpty())
                    return (T)Convert.ChangeType(values.ToString(), typeof(T));
            }
            return default;
        }
        protected string GetIP(bool tryUseXForwardHeader = true)
        {
            string ip = null;
            if (tryUseXForwardHeader)
                ip = SplitCsv(GetHeaderValueAs<string>("X-Forwarded-For")).FirstOrDefault();
            if (ip.IsStringNullOrEmpty() && _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress != null)
                ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            if (ip.IsStringNullOrEmpty()) ip = GetHeaderValueAs<string>("REMOTE_ADDR");
            return ip;
        }
        protected string GetFilter(TableData obj, string filter)
        {
            // filter Sale
            var filterItem = obj.Filters?.FirstOrDefault(c => c.Name == filter);
            return filterItem != null ? filterItem.Value.ToString() : string.Empty;
        }
        protected List<string> SplitCsv(string csvList, bool nullOrWhitespaceInputReturnsNull = false)
        {
            if (string.IsNullOrWhiteSpace(csvList))
                return nullOrWhitespaceInputReturnsNull ? null : new List<string>();

            return csvList
                .TrimEnd(',')
                .Split(',')
                .AsEnumerable<string>()
                .Select(s => s.Trim())
                .ToList();
        }

    }
}
