namespace PosApp.Admin.Api.Data.Models
{
    public static class ErrorResult
    {
        public static string DataInvalid = "DataInvalid";
        public static string TokenInvalid = "TokenInvalid";
        public static string DataNotExists = "DataNotExists";

        public static class User
        {
            public static string Locked = "User.Locked";
            public static string Logout = "User.Logout";
            public static string Exists = "User.Exists";
            public static string NotActive = "User.NotActive";
            public static string NotExists = "User.NotExists";
            public static string UserExists = "User.UserExists";
            public static string UserInvalid = "User.UserInvalid";
            public static string ReasonLogout = "User.ReasonLogout";
            public static string TokenInvalid = "User.TokenInvalid";
            public static string LoginInvalid = "User.LoginInvalid";
            public static string ChangeAvatar = "User.ChangeAvatar";
            public static string ChangePassword = "User.ChangePassword";
            public static string PasswordInvalid = "User.PasswordInvalid";
            public static string ChangeBackground = "User.ChangeBackground";
            public static string ReasonChangePassword = "User.ReasonChangePassword";
        }

        public static class Role
        {
            public static string Update = "Role.Update";
            public static string NotExists = "Role.NotExists";
            public static string CantDelete = "Role.CantDelete";
        }

        public static class Team
        {
            public static string Update = "Team.Update";
            public static string NotExists = "Team.NotExists";
            public static string CantDelete = "Team.CantDelete";
        }

        public static class Notify
        {
            public static string NotExists = "Notify.NotExists";
        }

        public static class Department
        {
            public static string NotExists = "Department.NotExists";
            public static string CantDelete = "Department.CantDelete";
        }

        public static class SmtpAccount
        {
            public static string NotExists = "SmtpAccount.NotExists";
        }

        public static class EmailTemplate
        {
            public static string NotExists = "EmailTemplate.NotExists";
        }
    }
}
