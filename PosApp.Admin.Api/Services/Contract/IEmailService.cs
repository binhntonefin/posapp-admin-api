using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Helpers;
using URF.Core.EF.Trackable.Enums;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface IEmailService
    {
        ResultApi SendMail(EmailEntity entity);
        ResultApi SendMail(string email, EmailTemplateType type, Dictionary<string, string> keyValues);
        ResultApi SendMail(List<string> emails, EmailTemplateType type, Dictionary<string, string> keyValues);
    }
}
