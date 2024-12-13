using System.Reflection;
using URF.Core.EF.Trackable.Enums;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Helpers
{
    public class UtilityHelper
    {
        public static List<string> FindControllers()
        {
            var items = FindAssemblies().SelectMany(c => c.DefinedTypes)
                .Where(c => c.Name.EndsWithEx("Controller"))
                .Where(c => !c.Name.EqualsEx("UploadController"))
                .Where(c => !c.Name.EqualsEx("UtilityController"))
                .Where(c => !c.Name.EqualsEx("SecurityController"))
                .Where(c => !c.Name.EqualsEx("AdminBaseController"))
                .Where(c => !c.Name.EqualsEx("AdminApiBaseController"))
                .Where(c => !c.Name.EqualsEx("RequestFilterController"))
                .Where(c => c.Namespace.ContainsEx("Controllers.Admin"))
                .Where(c => !c.Name.EqualsEx("LanguageDetailController"))
                .Select(c => c.Name.Replace("Controller", string.Empty))
                .Distinct()
                .ToList();
            items.Add("Dashboard");
            return items;
        }
        public static List<Assembly> FindAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(c => c.FullName.ContainsEx("PosApp.Admin.Api")).ToList();
        }
        public static Type FindEntity(string entityName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.DefinedTypes)
                .Where(c => c.FullName.ContainsEx("PosApp.Admin.Api.Data.Models"))
                .Where(c => c.Name.EqualsEx(entityName))
                .FirstOrDefault();
            return type != null ? type.DeclaringType : null;
        }
        public static string CorrectAction(string action)
        {
            switch (action)
            {
                case "View": return "View";
                case "Insert": return "Insert";
                case "Update": return "Update";
                case "Delete": return "Delete";
                case "Active": return "Active";
                default: return action;
            }
        }
        public static void CorrectExportData(TableData obj)
        {
            obj ??= new TableData();
            obj.Paging ??= new PagingData
            {
                Index = 1,
                Size = obj.Export != null ? obj.Export.Limit : 1000
            };
            obj.Filters ??= new List<FilterData>();
            var filters = obj.Filters ?? new List<FilterData>();
            var createdDateFilter = filters.Where(c => c.Name == "CreatedDate").FirstOrDefault();
            if (createdDateFilter == null)
            {
                if (obj.Export != null)
                {
                    createdDateFilter = new FilterData
                    {
                        Name = "CreatedDate",
                        Compare = CompareType.D_Between,
                        Value = obj.Export.DateRange[0].ToString("dd/MM/yyyy"),
                        Value2 = obj.Export.DateRange[1].ToString("dd/MM/yyyy"),
                    };
                    obj.Filters.Add(createdDateFilter);
                }
            }
        }
        public static List<string> FindActions(string controller)
        {
            if (controller.IsStringNullOrEmpty()) controller = string.Empty;
            switch (controller.ToLower())
            {
                case "dashboard":
                    return new List<string> { "View" };
                case "job":
                    return new List<string> { "View", "Bidding", "Reject", "Edit" };
                case "agreement":
                    return new List<string> { "View", "AddNew", "Edit", "Delete", "Assign" };
                case "role":
                    return new List<string> { "View", "AddNew", "Edit", "Delete", "Add Users", "Permision" };
                case "user":
                    return new List<string> { "View", "AddNew", "Edit", "Delete", "Reset Password", "Lock Account", "Unlock Account" };
                case "recruitment":
                    return new List<string> { "View", "AddNew", "Edit", "Delete", "Verify" };
                case "profile":
                    return new List<string> { "View", "AddNew", "Edit", "Delete", "ViewDetail", "Profile" };
                case "documentout":
                case "documentarrive":
                    return new List<string> { "View", "AddNew", "Edit", "Delete", "ViewDetail", "Process", "Transfer", "Approve", "End", "Publish", "Confirm", "History", "Signature", "Rollback", "Config" };
                default:
                    return new List<string> { "View", "AddNew", "Edit", "Delete", "ViewDetail" };
            }
        }
    }
}
