using URF.Core.EF.Trackable.Entities.Message;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using URF.Core.EF.Trackable;
using URF.Core.EF.Trackable.Entities;
using URF.Core.Helper.Extensions;
using URF.Core.Services.Contract;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using URF.Core.Helper.Helpers;
using static URF.Core.Helper.Helpers.AuditTrailHelper;
using System.Security.Claims;

namespace PosApp.Admin.Api.Data
{
    public partial class PosAppAdminContext : IdentityDbContext<User, Role, int, UserClaim, UserRole, IdentityUserLogin<int>, RoleClaim, IdentityUserToken<int>>
    {
        public int UserId { get; set; }
        public string TenantId { get; set; }
        private readonly ITenantService _tenantService;

        // audit
        private AuditTrailHelper auditFactory;
        private readonly List<Audit> auditList = new();
        private readonly List<EntityEntry> objectList = new();

        public PosAppAdminContext(
            DbContextOptions options,
            ITenantService tenantService,
            IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _tenantService = tenantService;
            auditFactory = new AuditTrailHelper();
            TenantId = _tenantService.GetTenant()?.TID;
            if (httpContextAccessor != null && httpContextAccessor.HttpContext != null)
            {
                var identity = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (identity != null)
                    UserId = Convert.ToInt32(identity.Value);
            }
        }

        public virtual DbSet<Audit> Audit { get; set; }
        public virtual DbSet<LogException> LogExceptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Locked);
                entity.HasIndex(e => e.UserName);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsDelete);
                entity.HasIndex(e => e.FullName);
                entity.HasIndex(e => e.PhoneNumber);
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.UserName).HasMaxLength(50);
                entity.Property(e => e.Address).HasMaxLength(550);
                entity.Property(e => e.FullName).HasMaxLength(160);
                entity.Property(e => e.VerifyCode).HasMaxLength(10);
                entity.Property(e => e.PhoneNumber).HasMaxLength(15);
                entity.Property(e => e.ReasonLock).HasMaxLength(500);
                entity.Property(e => e.ExtPhoneNumber).HasMaxLength(10);
                entity.HasOne(d => d.Department)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.DepartmentId)
                    .HasConstraintName("FK_Users_DepartmentId");
                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.ChildUsers)
                    .HasForeignKey(d => d.ParentId)
                    .HasConstraintName("FK_ChildUsers_ParentId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<Team>(entity =>
            {
                entity.ToTable("Team");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.Property(e => e.Code).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<Audit>(entity =>
            {
                entity.ToTable("Audit");
                entity.HasIndex(c => c.UserId);
                entity.HasIndex(c => c.Action);
                entity.HasIndex(c => c.EndTime);
                entity.HasIndex(c => c.TableName);
                entity.HasIndex(c => c.StartTime);
                entity.HasIndex(c => c.CreatedDate);
                entity.HasIndex(c => c.TableIdValue);
                entity.Property(e => e.Action).HasMaxLength(10);
                entity.Property(e => e.OldData).HasMaxLength(4000);
                entity.Property(e => e.NewData).HasMaxLength(4000);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.TableName).HasMaxLength(150);
                entity.Property(e => e.MachineName).HasMaxLength(250);
                entity.HasOne(d => d.User)
                    .WithMany(p => p.Audits)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Audits_UserId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRole");
                entity.HasIndex(c => c.RoleId);
                entity.HasIndex(c => c.UserId);
                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_UserRole_UserId");
                entity.HasOne(d => d.Role)
                    .WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_UserRole_RoleId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<UserTeam>(entity =>
            {
                entity.HasIndex(c => c.UserId);
                entity.HasIndex(c => c.TeamId);
                entity.ToTable("UserTeam");
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserTeams)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_UserTeams_UserId");
                entity.HasOne(d => d.Team)
                    .WithMany(p => p.UserTeams)
                    .HasForeignKey(d => d.TeamId)
                    .HasConstraintName("FK_UserTeams_TeamId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<RoleClaim>(entity =>
            {
                entity.ToTable("RoleClaim");
            });
            modelBuilder.Entity<UserClaim>(entity =>
            {
                entity.ToTable("UserClaim");
            });
            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("Department");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(250);
                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.Childs)
                    .HasForeignKey(d => d.ParentId)
                    .HasConstraintName("FK_Childs_ParentId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permission");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.Controller);
                entity.Property(e => e.Title).HasMaxLength(250);
                entity.Property(e => e.Group).HasMaxLength(250);
                entity.Property(e => e.Types).HasMaxLength(250);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(250);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(250);
                entity.Property(e => e.Controller).IsRequired().HasMaxLength(250);
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<LogActivity>(entity =>
            {
                entity.ToTable("LogActivity");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Url);
                entity.HasIndex(e => e.ObjectId);
                entity.HasIndex(e => e.Controller);
                entity.Property(e => e.Ip).HasMaxLength(20);
                entity.Property(e => e.Url).HasMaxLength(250);
                entity.Property(e => e.Method).HasMaxLength(20);
                entity.Property(e => e.Action).HasMaxLength(150);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                entity.Property(e => e.ObjectId).HasMaxLength(50);
                entity.Property(e => e.Controller).HasMaxLength(150);
                entity.HasOne(d => d.User)
                    .WithMany(p => p.LogActivities)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_LogActivities_UserId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<UserActivity>(entity =>
            {
                entity.ToTable("UserActivity");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Ip).HasMaxLength(50);
                entity.Property(e => e.Country).HasMaxLength(150);
                entity.Property(e => e.Os).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Browser).IsRequired().HasMaxLength(150);
                entity.HasOne(d => d.User)
                    .WithMany(p => p.Activities)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_UserActivity_UserId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<LogException>(entity =>
            {
                entity.ToTable("LogException");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Exception).HasMaxLength(4000);
                entity.Property(e => e.StackTrace).HasMaxLength(4000);
                entity.Property(e => e.InnerException).HasMaxLength(4000);
                entity.HasOne(d => d.User)
                    .WithMany(p => p.LogExceptions)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_LogExceptions_UserId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.ToTable("UserPermission");
                entity.HasIndex(c => c.UserId);
                entity.HasIndex(c => c.PermissionId);
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserPermissions)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_UserPermissions_UserId");
                entity.HasOne(d => d.Permission)
                    .WithMany(p => p.UserPermissions)
                    .HasForeignKey(d => d.PermissionId)
                    .HasConstraintName("FK_UserPermissions_PermissionId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("RolePermission");
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.Role)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_RolePermissions_RoleId");
                entity.HasOne(d => d.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(d => d.PermissionId)
                    .HasConstraintName("FK_RolePermissions_PermissionId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<LinkPermission>(entity =>
            {
                entity.ToTable("LinkPermission");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Order);
                entity.Property(e => e.Group).HasMaxLength(50);
                entity.Property(e => e.CssIcon).HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Link).IsRequired().HasMaxLength(150);
                entity.HasOne(d => d.Permission)
                    .WithMany(p => p.LinkPermissions)
                    .HasForeignKey(d => d.PermissionId)
                    .HasConstraintName("FK_LinkPermissions_PermissionId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<IdentityUserLogin<int>>(entity =>
            {
                entity.Property(e => e.ProviderKey).HasMaxLength(250);
                entity.Property(e => e.LoginProvider).HasMaxLength(250);
            });
            modelBuilder.Entity<IdentityUserToken<int>>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(250);
                entity.Property(e => e.LoginProvider).HasMaxLength(250);
            });

            // common
            modelBuilder.Entity<Notify>(entity =>
            {
                entity.ToTable("Notify");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsDelete);
                entity.Property(e => e.Content).HasMaxLength(550);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
                entity.HasOne(d => d.User)
                   .WithMany(p => p.Notifies)
                   .HasForeignKey(d => d.UserId)
                   .HasConstraintName("FK_Notify_UserId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<Language>(entity =>
            {
                entity.ToTable("Language");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsDelete);
                entity.Property(e => e.Icon).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(250);
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<SmtpAccount>(entity =>
            {
                entity.ToTable("SmtpAccount");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Host).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.EmailFrom).IsRequired().HasMaxLength(250);
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.ToTable("EmailTemplate");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Title).IsRequired().HasMaxLength(550);
                entity.HasOne(d => d.SmtpAccount)
                    .WithMany(p => p.EmailTemplates)
                    .HasForeignKey(d => d.SmtpAccountId)
                    .HasConstraintName("FK_EmailTemplates_SmtpAccountId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<RequestFilter>(entity =>
            {
                entity.ToTable("RequestFilter");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Controller);
                entity.Property(e => e.FilterData).IsRequired();
                entity.Property(e => e.Controller).HasMaxLength(150);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Controller).IsRequired().HasMaxLength(150);
                entity.HasOne(d => d.User)
                    .WithMany(p => p.RequestFilters)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_RequestFilters_UserId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<LanguageDetail>(entity =>
            {
                entity.ToTable("LanguageDetail");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Table);
                entity.HasIndex(e => e.ObjectId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsDelete);
                entity.HasIndex(e => e.Property);
                entity.HasIndex(e => e.LanguageId);
                entity.Property(e => e.Table).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Property).IsRequired().HasMaxLength(250);
                entity.HasOne(d => d.Language)
                   .WithMany(p => p.LanguageDetails)
                   .HasForeignKey(d => d.LanguageId)
                   .HasConstraintName("FK_LanguageDetails_LanguageId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });

            // Chat
            modelBuilder.Entity<Group>(entity =>
            {
                entity.ToTable("Group");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(250);
                entity.HasOne(d => d.User)
                  .WithMany(p => p.Groups)
                  .HasForeignKey(d => d.UserId)
                  .HasConstraintName("FK_Groups_UserId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Message");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Files).HasMaxLength(4000);
                entity.Property(e => e.Content).HasMaxLength(2000);
                entity.HasOne(d => d.Team)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.TeamId)
                    .HasConstraintName("FK_Messages_TeamId");
                entity.HasOne(d => d.Group)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.GroupId)
                    .HasConstraintName("FK_Messages_GroupId");
                entity.HasOne(d => d.Send)
                    .WithMany(p => p.SendMessages)
                    .HasForeignKey(d => d.SendId)
                    .HasConstraintName("FK_SendMessages_SendId");
                entity.HasOne(d => d.Receive)
                    .WithMany(p => p.ReceiveMessages)
                    .HasForeignKey(d => d.ReceiveId)
                    .HasConstraintName("FK_ReceiveMessages_ReceiveId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.ToTable("UserGroup");
                entity.HasKey(e => e.Id);
                entity.HasOne(d => d.User)
                  .WithMany(p => p.UserGroups)
                  .HasForeignKey(d => d.UserId)
                  .HasConstraintName("FK_UserGroups_UserId");
                entity.HasOne(d => d.Group)
                  .WithMany(p => p.UserGroups)
                  .HasForeignKey(d => d.GroupId)
                  .HasConstraintName("FK_UserGroups_GroupId");
                entity.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedBy);
                entity.HasOne(d => d.UpdatedByUser).WithMany().HasForeignKey(c => c.UpdatedBy);
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var tenantConnectionString = _tenantService.GetConnectionString();
            if (!string.IsNullOrEmpty(tenantConnectionString))
            {
                var DBProvider = _tenantService.GetDatabaseProvider();
                if (DBProvider.ToLower() == "mysql")
                {
                    optionsBuilder.UseMySQL(tenantConnectionString);
                }
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<ISqlTenantEntity>().ToList())
            {
                if (entry.Entity.TenantId.IsStringNullOrEmpty())
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                        case EntityState.Modified:
                            entry.Entity.TenantId = TenantId;
                            break;
                    }
                }
            }

            OnBeforeSaveChanges();
            var result = await base.SaveChangesAsync(cancellationToken);
            try
            {
                if (auditList.Count > 0)
                {
                    OnAfterSaveChanges();
                    base.SaveChanges();
                }
            }
            catch
            {
            }
            return result;
        }

        private void OnAfterSaveChanges()
        {
            int i = 0;
            foreach (Audit audit in auditList)
            {
                if (audit.Action == AuditActions.I.ToString())
                    audit.TableIdValue = GetKeyValue(objectList[i]);
                if (!audit.TableIdValue.HasValue)
                    continue;

                audit.TenantId = TenantId;
                audit.EndTime = DateTime.Now;
                Audit.Add(audit);
                i += 1;
            }
        }
        private void OnBeforeSaveChanges()
        {
            if (!UserId.IsNumberNull())
            {
                auditList.Clear();
                objectList.Clear();
                auditFactory ??= new AuditTrailHelper();
                var entityList = ChangeTracker.Entries().Where(p => p.State == EntityState.Added || p.State == EntityState.Deleted || p.State == EntityState.Modified);
                foreach (EntityEntry entry in entityList)
                {
                    var audit = auditFactory.GetAudit(entry, UserId);
                    if (audit != null)
                    {
                        if (audit.UserId.HasValue && audit.UserId.Value > 0)
                            auditList.Add(audit);
                        objectList.Add(entry);
                    }
                }
            }
        }
    }
}
