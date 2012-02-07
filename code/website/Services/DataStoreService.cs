/* Copyright 2011 Matt Cosand and others (see AUTHORS.TXT)
 *
 * This file is part of SARTracks.
 *
 *  SARTracks is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  SARTracks is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with SARTracks.  If not, see <http://www.gnu.org/licenses/>.
 */
namespace SarTracks.Website.Services
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Entity;
    using SarTracks.Website.Models;
    using System.Linq;
    using System.Data.Entity.Validation;
    using System.Data.Entity.ModelConfiguration.Conventions;
    using System.Web.Security;

    public interface IDataStoreService : IDisposable
    {
        IDbSet<SarMember> Members { get; set; }
        IDbSet<Organization> Organizations { get; set; }
        IDbSet<Mission> Missions { get; set; }
        IDbSet<Training> Trainings { get; set; }
        IDbSet<TrainingCourse> TrainingCourses { get; set; }
        IDbSet<LogEntry> Log { get; set; }
        IDbSet<ExternalReference> ExternalReferences { get; set; }
        IDbSet<ChangeRequest> PendingChanges { get; set; }
        IDbSet<User> Users { get; set; }
        IDbSet<Role> Roles { get; set; }
        IDbSet<Authorization> Authorization { get; set; }

        int SaveChanges();
        IEnumerable<DbEntityValidationResult> GetValidationErrors();
    }

    public static class DataStoreActions
    {
        public static string InitializeSystemSecurity(this IDataStoreService ctx)
        {
            string adminGroup = AuthIdentityService.ADMIN_ROLE;
            string password = null;
            Role adminRole = ctx.Roles.IncludePaths("Users").SingleOrDefault(f => f.Name == adminGroup && f.Organization == null);
            if (adminRole == null)
            {
                adminRole = new Role { Name = adminGroup, SystemRole = true };
                ctx.Roles.Add(adminRole);
            }

            User admin = ctx.Users.SingleOrDefault(f => f.Username == "admin");
            if (admin == null)
            {
                password = Membership.GeneratePassword(8, 2);
                admin = AuthIdentityService.CreateAdminUser(password);
                ctx.Users.Add(admin);
            }
            else
            {
                password = AuthIdentityService.ResetPassword(admin);
            }

            if (!adminRole.Users.Any(f => f.User.Username == admin.Username))
            {
                adminRole.Users.Add(new RoleUserMembership { User = admin, Role = adminRole, IsSystem = true });
            }            

            foreach (string name in new[] { AuthIdentityService.EVERYONE_ROLE, AuthIdentityService.USERS_ROLE, AuthIdentityService.MISSION_VIEWERS_ROLE })
            {
                Role role = ctx.Roles.SingleOrDefault(f => f.Name == name);
                if (role == null)
                {
                    role = new Role { Name = name, SystemRole = true };
                    ctx.Roles.Add(role);
                }
            }

            ctx.Authorization.Add(new Authorization { Permission = PermissionType.SiteAdmin, Role = adminRole });

            ctx.SaveChanges();
            return password;
        }

        public static void InitializeOrganizationSecurity(this IDataStoreService ctx, Organization org, User admin)
        {
            org.AdminAccount = (admin == null) ? "setup" : admin.Username;

            var usersRole = new Role { Name = "Members", OrganizationId = org.Id, SystemRole = true };
            var adminRole = new Role { Name = "Administrators", OrganizationId = org.Id, SystemRole = true };
            adminRole.MemberOfRoles.Add(new RoleRoleMembership { Parent = usersRole, Child = adminRole, IsSystem = true });

            var siteAdmin = ctx.Roles.Single(f => f.Name == AuthIdentityService.ADMIN_ROLE && f.OrganizationId == null);
            siteAdmin.MemberOfRoles.Add(new RoleRoleMembership { Parent = adminRole, Child = siteAdmin, IsSystem = true });

            if (admin != null)
            {
                adminRole.Users.Add(new RoleUserMembership { Role = adminRole, User = admin });
            }

            ctx.Roles.Add(adminRole);
            ctx.Roles.Add(usersRole);

            ctx.Authorization.Add(new Authorization { Role = adminRole, Scope = org.Id, Permission = PermissionType.AdminOrganization, IsSystem = true });
            ctx.Authorization.Add(new Authorization { Role = adminRole, Scope = org.Id, Permission = PermissionType.AddOrganizationMembers, IsSystem = true });
            ctx.Authorization.Add(new Authorization { Role = adminRole, Scope = org.Id, Permission = PermissionType.EditMember, IsSystem = true });
            ctx.Authorization.Add(new Authorization { Role = adminRole, Scope = org.Id, Permission = PermissionType.EditMemberContacts, IsSystem = true });
            ctx.Authorization.Add(new Authorization { Role = adminRole, Scope = org.Id, Permission = PermissionType.ViewMemberDetail, IsSystem = true });
            ctx.Authorization.Add(new Authorization { Role = adminRole, Scope = org.Id, Permission = PermissionType.ViewMemberStandard, IsSystem = true });

            ctx.Authorization.Add(new Authorization { Role = usersRole, Scope = org.Id, Permission = PermissionType.ViewOrganizationBasic, IsSystem = true });
            ctx.Authorization.Add(new Authorization { Role = usersRole, Scope = org.Id, Permission = PermissionType.ViewOrganizationDetail, IsSystem = true });
            ctx.Authorization.Add(new Authorization { Role = usersRole, Scope = org.Id, Permission = PermissionType.ListOrganization, IsSystem = true });
            ctx.Authorization.Add(new Authorization { Role = usersRole, Scope = org.Id, Permission = PermissionType.ViewMemberStandard, IsSystem = false });
            ctx.Authorization.Add(new Authorization { Role = usersRole, Scope = org.Id, Permission = PermissionType.ViewMemberBasic, IsSystem = false });
        }
    }

    public class DataStoreFactory
    {
        private IDataStoreService sharedInstance = null;
        public DataStoreFactory(IDataStoreService service)
        {
            this.sharedInstance = service;
        }

        public DataStoreFactory()
        {
        }

        public IDataStoreService Create(string username)
        {
            if (this.sharedInstance != null)
            {                
                return this.sharedInstance;
            }
            return new DataStoreService(username);
        }
    }

    public class DataStoreService : DbContext, IDataStoreService
    {
        public static DateTime DATE_UNKNOWN
        {
            get { return new DateTime(1800, 1, 1, 0, 0, 0, DateTimeKind.Utc); }
        }

        ///// <summary>Allows for test cases to pass in a test context</summary>
        //public static IDataStoreService TestStore { get; set; }
        
        ///// <summary>
        ///// Generates a real or test context
        ///// </summary>
        ///// <returns></returns>
        //public static IDataStoreService Create(string username)
        //{
        //    if (DataStoreService.TestStore == null)
        //    {
        //        return new DataStoreService(username);
        //    }
        //    return DataStoreService.TestStore;
        //}

        private string username;

        public IDbSet<SarMember> Members { get; set; }
        public IDbSet<Organization> Organizations { get; set; }
        public IDbSet<Mission> Missions { get; set; }
        public IDbSet<Training> Trainings { get; set; }
        public IDbSet<TrainingCourse> TrainingCourses { get; set; }
        public IDbSet<LogEntry> Log { get; set; }
        public IDbSet<ExternalReference> ExternalReferences { get; set; }
        public IDbSet<ChangeRequest> PendingChanges { get; set; }
        public IDbSet<User> Users { get; set; }
        public IDbSet<Role> Roles { get; set; }
        public IDbSet<Authorization> Authorization { get; set; }

        public DataStoreService(string username)
            : base("Data")
        {
            this.username = username;
            string initType = ConfigurationManager.AppSettings["testDataInitializerType"];
            IDatabaseInitializer<DataStoreService> initializer;
            if (string.IsNullOrWhiteSpace(initType))
            {
                initializer = new CreateDatabaseIfNotExists<DataStoreService>();
            }
            else
            {
                Type type = Type.GetType(initType);
                initializer = (IDatabaseInitializer<DataStoreService>)Activator.CreateInstance(type);
            }
            Database.SetInitializer<DataStoreService>(initializer);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Entity<Role>().HasMany<RoleRoleMembership>(f => f.MemberOfRoles).WithRequired(f => f.Child);
            modelBuilder.Entity<Role>().HasMany<RoleRoleMembership>(f => f.MemberRoles).WithRequired(f => f.Parent);
            modelBuilder.Entity<Role>().HasOptional<Organization>(f => f.Organization).WithMany().WillCascadeOnDelete();
            modelBuilder.Entity<Role>().HasMany<RoleRoleMembership>(f => f.MemberRoles).WithRequired(f => f.Parent).WillCascadeOnDelete();
            modelBuilder.Entity<Role>().HasMany<RoleUserMembership>(f => f.Users).WithRequired(f => f.Role).WillCascadeOnDelete();

            modelBuilder.Entity<Authorization>().HasOptional<Role>(f => f.Role).WithMany().WillCascadeOnDelete();

            modelBuilder.Entity<Organization>().HasMany<UnitStatusType>(f => f.UnitStatusTypes).WithRequired(f => f.Organization).WillCascadeOnDelete();

            modelBuilder.Entity<SarMember>().HasMany<MemberAddress>(f => f.Addresses).WithRequired(f => f.Member).WillCascadeOnDelete();
            modelBuilder.Entity<SarMember>().HasMany<UnitMembership>(f => f.Memberships).WithRequired(f => f.Member).WillCascadeOnDelete();
            //modelBuilder.Entity<SarMember>().HasMany<UnitMembership>(f => f.OldMemberships).WithRequired(f => f.Member).WillCascadeOnDelete();
            
            //modelBuilder.Entity<Role>().HasOptional<RoleRoleMembership>(f => f.Parent).WithRequired(f => f.Child);
            //modelBuilder.Entity<Role>().HasMany<RoleRoleMembership>(f => f.Children).WithRequired(f => f.Parent);
//            modelBuilder.Entity<RoleRoleMembership>().HasRequired<Role>(f => f.Parent).WithMany(f => f.Children);
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            List<LogEntry> logs = new List<LogEntry>();
            foreach (var entry in this.ChangeTracker.Entries().Where(f => f.State != System.Data.EntityState.Unchanged))
            {                
                SarObject obj = entry.Entity as SarObject;
                if (obj == null)
                    continue;

                obj.ChangedBy = this.username;
                logs.Add(new LogEntry
                {
                    ReferenceId = obj.Id,
                    ChangedBy = this.username,
                    LastChanged = DateTime.UtcNow,
                    Action = entry.State.ToString(),
                    Type = obj.GetType().ToString()
                });
            }

            logs.ForEach(f => this.Log.Add(f));

            try
            {
                return base.SaveChanges();
            }
            catch (Exception ex)
            {
                // Set breakpoint
                throw;
            }
        }
    }   
}