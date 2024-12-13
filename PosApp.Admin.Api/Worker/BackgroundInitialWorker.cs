using PosApp.Admin.Api.Helpers;
using Microsoft.Extensions.Options;
using URF.Core.EF.Trackable.Enums;
using URF.Core.EF.Trackable;
using URF.Core.Helper.Helpers;
using Microsoft.AspNetCore.Identity;
using URF.Core.EF.Trackable.Entities;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Worker
{
    public class BackgroundInitialWorker : BackgroundService
    {
        private readonly AppSettings _appSettings;
        private readonly IServiceProvider _serviceProvider;

        public BackgroundInitialWorker(
            IServiceProvider serviceProvider,
            IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            if (_appSettings.InitDb.HasValue())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                // Insert admin
                var userAdmin = await userManager.FindByNameAsync(StoreHelper.UserAdmin);
                if (userAdmin == null)
                {
                    var password = SecurityHelper.CreateHash256("fWP7o1HXpWhwm8v7uGqN+ng+InV2Psld4s7FZRCPa1M=", _appSettings.Secret);
                    userAdmin = new User
                    {
                        Locked = false,
                        IsAdmin = true,
                        IsActive = true,
                        IsDelete = false,
                        FullName = "Admin",
                        EmailConfirmed = true,
                        Birthday = DateTime.Now,
                        PhoneNumber = "888888888",
                        CreatedDate = DateTime.Now,
                        Email = "admin@hrm.lazy.vn",
                        UserName = StoreHelper.UserAdmin,
                        UserType = (int)UserType.Management,
                    };
                    await userManager.CreateAsync(userAdmin, password);
                }
            }
            return;
        }
    }
}
