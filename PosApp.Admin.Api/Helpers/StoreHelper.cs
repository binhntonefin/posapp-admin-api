using FirebaseAdmin;
using PosApp.Admin.Api.Service.Caching;
using URF.Core.EF.Trackable.Entities;

namespace PosApp.Admin.Api.Helpers
{
    public class StoreHelper
    {
        public static string SchemaApi;
        public static string SchemaWebAdmin;
        public static FirebaseApp FirebaseApp;
        public static string UserAdmin = "admin";

        public static List<Role> Roles = new();
        public static List<User> Users = new();
        public static List<Team> Teams = new();
        public static List<UserTeam> UserTeams = new();
        public static List<Permission> Permissions = new();
        public static List<Department> Departments = new();
        public static Cache<string, object> Caches = new();
        public static List<LinkPermission> LinkPermissions = new();
        public static Dictionary<string, Dictionary<string, string>> KeyValues = new();
    }
}
