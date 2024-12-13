using URF.Core.Abstractions;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Helpers;
using URF.Core.Helper;
using URF.Core.Helper.Extensions;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using URF.Core.EF.Trackable.Enums;
using PosApp.Admin.Api.Data.Models;
using PosApp.Admin.Api.Services.Contract;

namespace PosApp.Admin.Api.Services.Implement
{
    public class EmailService : ServiceX, IEmailService
    {
        private readonly IRepositoryX<SmtpAccount> _smtpAccountRepository;
        private readonly IRepositoryX<EmailTemplate> _emailTemplateRepository;

        public EmailService(
            IHttpContextAccessor httpContextAccessor,
            IRepositoryX<SmtpAccount> smtpAccountRepository,
            IRepositoryX<EmailTemplate> emailTemplateRepository) : base(httpContextAccessor)
        {
            _smtpAccountRepository = smtpAccountRepository;
            _emailTemplateRepository = emailTemplateRepository;
        }

        public ResultApi SendMail(EmailEntity entity)
        {
            // send mail
            var result = EmailHelper.SendEmail(entity);
            return ResultApi.ToEntity(result);
        }

        public ResultApi SendMail(string email, EmailTemplateType type, Dictionary<string, string> keyValues)
        {
            // check data
            if (email.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // get email-template
            var emailTemplate = _emailTemplateRepository.Queryable().FilterQueryNoTraking().FirstOrDefault(c => c.Type == type);
            if (emailTemplate == null || !emailTemplate.SmtpAccountId.HasValue)
                return ResultApi.ToError(ErrorResult.EmailTemplate.NotExists);

            // get smtp-account
            var smtpAccount = _smtpAccountRepository.Queryable().FilterQueryNoTraking().FirstOrDefault(c => c.Id == emailTemplate.SmtpAccountId.Value);
            if (smtpAccount == null)
                return ResultApi.ToError(ErrorResult.SmtpAccount.NotExists);

            // replace content
            var content = emailTemplate.Content;
            foreach (var item in keyValues)
                content = content.Replace("{{" + item.Key + "}}", item.Value);

            // send mail
            var result = EmailHelper.SendEmail(new EmailEntity
            {
                Content = content,
                Subject = emailTemplate.Title,
                Contacts = new List<string> { email },
                SmtpAccount = Mapper.Map<SmtpAccountEntity>(smtpAccount),
            });
            return ResultApi.ToEntity(result);
        }

        public ResultApi SendMail(List<string> emails, EmailTemplateType type, Dictionary<string, string> keyValues)
        {
            // check data
            if (emails.IsNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // get email-template
            var emailTemplate = _emailTemplateRepository.Queryable().FilterQueryNoTraking().FirstOrDefault(c => c.Type == type);
            if (emailTemplate == null || !emailTemplate.SmtpAccountId.HasValue)
                return ResultApi.ToError(ErrorResult.EmailTemplate.NotExists);

            // get smtp-account
            var smtpAccount = _smtpAccountRepository.Queryable().FilterQueryNoTraking().FirstOrDefault(c => c.Id == emailTemplate.SmtpAccountId.Value);
            if (smtpAccount == null)
                return ResultApi.ToError(ErrorResult.SmtpAccount.NotExists);

            // replace content
            var content = emailTemplate.Content;
            foreach (var item in keyValues)
                content = content.Replace("{{" + item.Key + "}}", item.Value);

            // send mail
            var result = EmailHelper.SendEmail(new EmailEntity
            {
                Content = content,
                Subject = emailTemplate.Title,
                Contacts = emails,
                SmtpAccount = Mapper.Map<SmtpAccountEntity>(smtpAccount),
            });
            return ResultApi.ToEntity(result);
        }
    }
}
