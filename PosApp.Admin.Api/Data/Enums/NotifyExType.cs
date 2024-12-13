namespace PosApp.Admin.Api.Data.Enums
{
    public enum NotifyExType
    {
        Message = 1,
        Logout,
        LockUser = 10,
        UpdateRole,
        ChangePassword,
        Answer,        
        DocumentOut = 20,
        DocumentArrive,
    }
}
