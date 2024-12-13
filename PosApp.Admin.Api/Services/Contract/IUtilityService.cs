using PosApp.Admin.Api.Data.Enums;
using URF.Core.EF.Trackable.Models;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface IUtilityService
    {
        ResultApi Controllers();
        ResultApi Actions(string controller = default);
        ResultApi ResetCache(CachedType? type = null);
    }
}
