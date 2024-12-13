using PosApp.Admin.Api.Helpers;
using Microsoft.Extensions.Primitives;
using System.Collections;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using URF.Core.EF.Trackable.Enums;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Services
{
    public class ServiceX
    {
        public bool IsLocalhost
        {
            get
            {
                return Process.GetCurrentProcess().ProcessName.ToLower().Contains("iisexpress");
            }
        }
        protected readonly int UserId;
        protected readonly bool IsAdmin;
        private readonly string LanguageCode;
        private readonly LanguageType Language;
        protected readonly AdminUserModel User;
        protected string NotifyFullName
        {
            get
            {
                var name = User.FullName;
                if (name.IsStringNullOrEmpty()) name = User.Phone;
                return name;
            }
        }
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepositoryX<LanguageDetail> _languageDetailRepository;

        protected ServiceX(IHttpContextAccessor httpContextAccessor)
        {
            LoadLanguages();
            _httpContextAccessor = httpContextAccessor;
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

                var language = GetHeaderValueAs<string>("Accept-Language");
                if (language.IsStringNullOrEmpty())
                    language = "vi";
                LanguageCode = language;
                switch (language)
                {
                    case "vi": Language = LanguageType.VietNam; break;
                    case "en": Language = LanguageType.English; break;
                    case "jp": Language = LanguageType.Japan; break;
                    default: Language = LanguageType.VietNam; break;
                }
            }
        }
        protected ServiceX(
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            LoadLanguages();
            _httpContextAccessor = httpContextAccessor;
            _languageDetailRepository = (IRepositoryX<LanguageDetail>)serviceProvider.GetService(typeof(IRepositoryX<LanguageDetail>));
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

                var language = GetHeaderValueAs<string>("Accept-Language");
                if (language.IsStringNullOrEmpty())
                    language = "vi";
                LanguageCode = language;
                switch (language)
                {
                    case "vi": Language = LanguageType.VietNam; break;
                    case "en": Language = LanguageType.English; break;
                    case "jp": Language = LanguageType.Japan; break;
                    default: Language = LanguageType.VietNam; break;
                }
            }
        }

        protected ResultApi ToException(Exception ex)
        {
            return ResultApi.ToException(ex);
        }
        protected string GetIpAddress(bool tryUseXForwardHeader = true)
        {
            string ip = null;
            if (tryUseXForwardHeader)
                ip = SplitCsv(GetHeaderValueAs<string>("X-Forwarded-For")).FirstOrDefault();
            if (ip.IsStringNullOrEmpty() && _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress != null)
                ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            if (ip.IsStringNullOrEmpty()) ip = GetHeaderValueAs<string>("REMOTE_ADDR");
            return ip;
        }
        protected string GetDescription(string key, params string[] items)
        {
            if (key.IsStringNullOrEmpty())
                return string.Empty;
            var language = GetHeaderValueAs<string>("Accept-Language");
            if (language.IsStringNullOrEmpty())
                language = GetHeaderValueAs<string>("Language");
            if (language.IsStringNullOrEmpty())
                language = "vi";
            if (language.Contains("en")) language = "en";
            else language = "vi";
            var keyValues = StoreHelper.KeyValues[language];
            if (!keyValues.IsNullOrEmpty() && keyValues.ContainsKey(key))
            {
                var text = keyValues[key];
                if (!text.Contains("{0}"))
                    return text;

                items = items.Select(c => c.IsStringNullOrEmpty() ? string.Empty : c).ToArray();
                return text = string.Format(text, items);
            }
            return string.Empty;
        }
        protected ResultApi ToEntity(object item = null, object extra = null)
        {
            return ResultApi.ToEntity(item, extra);
        }
        protected ResultApi ToError(string description = default, params string[] items)
        {
            description = GetDescription(description, items);
            return ResultApi.ToError(description);
        }
        protected ResultApi ToSuccess(string description = default, object extra = null)
        {
            description = GetDescription(description);
            return ResultApi.ToSuccess(description, extra);
        }
        protected IDictionary<string, object> UpdateLanguage(object model, string table, List<string> properties)
        {
            var item = model.ToDictionary();
            if (_languageDetailRepository != null)
            {
                if (Language != LanguageType.VietNam && item != null)
                {
                    var id = item["Id"].ToInt32();
                    var languageDetails = _languageDetailRepository.Queryable().FilterQueryNoTraking()
                        .Where(c => c.Language.Code == LanguageCode)
                        .Where(c => c.ObjectId == id)
                        .Where(c => c.Table == table)
                        .ToList();
                    foreach (var property in properties)
                    {
                        if (item.ContainsKey(property))
                        {
                            var languageValue = languageDetails.FirstOrDefault(c => c.Property == property);
                            if (languageValue != null && !languageValue.Value.IsStringNullOrEmpty())
                                item[property] = languageValue.Value;
                        }
                    }
                }
            }
            return item;
        }
        protected List<IDictionary<string, object>> UpdateLanguage(IEnumerable models, string table, List<string> properties)
        {
            var items = models.ToListDictionary();
            if (_languageDetailRepository != null)
            {
                if (Language != LanguageType.VietNam && !items.IsNullOrEmpty())
                {
                    var ids = items.Select(c => c["Id"].ToInt32()).ToList();
                    var languageDetails = _languageDetailRepository.Queryable().FilterQueryNoTraking()
                        .Where(c => c.Language.Code == LanguageCode)
                        .Where(c => ids.Contains(c.ObjectId))
                        .Where(c => c.Table == table)
                        .ToList();
                    foreach (var item in items)
                    {
                        var itemId = item["Id"].ToInt32();
                        foreach (var property in properties)
                        {
                            var languageValue = languageDetails.Where(c => c.ObjectId == itemId).FirstOrDefault(c => c.Property == property);
                            if (languageValue != null && !languageValue.Value.IsStringNullOrEmpty())
                                item[property] = languageValue.Value;
                        }
                    }
                }
            }
            return items;
        }

        private void LoadLanguages()
        {
            if (StoreHelper.KeyValues.IsNullOrEmpty())
            {
                StoreHelper.KeyValues = new Dictionary<string, Dictionary<string, string>>();
                var files = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), @"resources"));
                if (!files.IsNullOrEmpty())
                {
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file).Replace(".json", string.Empty);
                        var sr = new StreamReader(file, encoding: Encoding.UTF8);
                        var keyValues = sr.ReadToEnd().ToObject<Dictionary<string, string>>();
                        sr.Close();
                        try
                        {
                            if (!StoreHelper.KeyValues.ContainsKey(fileName))
                                StoreHelper.KeyValues.Add(fileName, keyValues);
                        } catch { }
                    }
                }
            }
        }
        private T GetHeaderValueAs<T>(string headerName)
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
        private List<string> SplitCsv(string csvList, bool nullOrWhitespaceInputReturnsNull = false)
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
