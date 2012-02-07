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
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Security;
    using System.Security.Principal;
    using System.Runtime.Serialization.Json;
    using System.Runtime.Serialization;
    using System.IO;
    using System.Text;
    using SarTracks.Website.Models;
    using System.Security.Cryptography;
    using System.Web.Configuration;
    using System.Globalization;
    using System.Reflection;

    //[DataContract]
    //public class UserMetadata
    //{
    //    [DataMember(EmitDefaultValue = false)]
    //    public Guid VerifyKey { get; set; }

    //    [DataMember(EmitDefaultValue = false)]
    //    public Guid LinkedMember { get; set; }
    //}

    //public interface IAuthIdentityService
    //{
    //    string UserLogin { get; }
    //    string UserName { get; }
    //    bool IsAuthenticated { get; }
    //    bool IsUser { get; }

    //    //        ////IPrincipal User { get; }
    //    //        //bool IsLocal(HttpRequestBase request);
    //    //        //bool IsUserInRole(string username, string role);
    //    //        //void AddUserToRole(string username, string role);
    //    //        //bool IsInRole(string role);
    //    string[] GetRolesForUser(string name, bool recursive);

    //    //        ////RedirectResult LoginRedirect(string message);
    //    //        ////bool CanViewOrganization(Guid orgId);
    //    //        ////bool CanAdminOrganization(Guid orgId);
    //    //        ////bool CanViewMember(Guid id);


    //    bool HasPermission(PermissionType permission, Guid? scope);

    //    //        ////bool UserHasAccount { get; }
    //    //        ////MembershipUser CurrentAccount { get; }
    //    LogonResult ValidateUser(string username, string password);

        
    //}

    public class AuthIdentityService// : IAuthIdentityService
    {
        private Dictionary<RoleKey, List<Role>> flattenedRoles = new Dictionary<RoleKey, List<Role>>();

        private List<Role> myRoles = null;

        private List<Authorization> myAuthzs = null;

        public const string ADMIN_ROLE = "Administrators";
        public const string EVERYONE_ROLE = "Everyone";
        public const string USERS_ROLE = "Users";
        public const string MISSION_VIEWERS_ROLE = "Mission Reports";

        private User currentUser = null;
        private DataStoreFactory storeFactory = null;

        /// <summary>
        /// Test constructor. Leaves many fields uninitialized; counts on methods being mocked away.
        /// </summary>
        protected AuthIdentityService()
        {
            this.IsAdmin = false;
        }

        public AuthIdentityService(string username, DataStoreFactory storeFactory)
        {
            this.storeFactory = storeFactory;
            using (var ctx = storeFactory.Create(username))
            {
                this.currentUser = ctx.Users.IncludePaths("Member").SingleOrDefault(f => f.Username == username);
                
            }

            this.IsUser = this.currentUser != null;

            this.UserLogin = username;
            this.UserName = (this.currentUser == null) ? username
                            : (this.currentUser.Member == null) ? this.currentUser.Name
                            : this.currentUser.Member.FullName;

            this.IsAuthenticated = !string.IsNullOrWhiteSpace(username);
            Init();
        }

        private void FillMyData()
        {
            if (this.IsUser)
            {
                if (this.myRoles == null || this.myAuthzs == null)
                {
                    using (var ctx = storeFactory.Create(this.UserLogin))
                    {
                        if (this.myRoles == null)
                        {
                            this.myRoles = GetUsersRolesRecursive(ctx, this.UserLogin, flattenedRoles);                            
                        }
                        if (this.myAuthzs == null)
                        {
                            this.myAuthzs = GetUsersAuthentications(ctx, this.UserLogin, myRoles);
                        }
                    }
                }

            }
            else
            {
                this.myRoles = new List<Role>();
                this.myAuthzs = new List<Authorization>();
            }

        }

        protected List<Role> GetUsersRolesRecursive(IDataStoreService ctx, string username, Dictionary<RoleKey, List<Role>> roleMemberLookup)
        {
            // get a list of roles the member is directly a member of
            var directRoles = ctx.Roles.Where(f => f.Users.Any(g => g.User.Username == username)).AsEnumerable().Select(f => new RoleKey(f.Name, f.OrganizationId));

            return roleMemberLookup.Where(f => directRoles.Any(g => g.Name == f.Key.Name && g.OrgId == f.Key.OrgId)).SelectMany(f => f.Value).Distinct().ToList();
        }

        protected Dictionary<RoleKey, List<Role>> GetRoleMemberLookup(IDataStoreService store)
        {
            var roleMap = store.Roles.IncludePaths("MemberOfRoles", "Organization").ToList();
            return roleMap.ToDictionary(f => new RoleKey(f.Name, f.OrganizationId), f => f.Flatten().Distinct().ToList()); 
        }

        protected List<Authorization> GetUsersAuthentications(IDataStoreService store, string username, List<Role> usersRoles)
        {
            var roleAuths = store.Authorization.Where(f => f.RoleId != null);

            var authList = (from have in usersRoles join granted in roleAuths on have.Id equals granted.RoleId select granted).ToList();

            authList.AddRange(store.Authorization.Where(f => f.UserName == username));

            return authList;
        }
    //            if (myRoles == null)
    //            {
    //                    var me = ctx.Users.IncludePaths("Roles.Role.Organization").Single(
    //f => f.Username == this.UserLogin
    //);
    //                myRoles = me.Roles.Select(f => f.Role).ToList();

    //            }
    //            if (myAuthzs == null)
    //            {
    //                using (var ctx = storeFactory.Create(this.UserLogin))
    //                {
    //                    foreach (var authz in ctx.Authorization.Where(f => f.Role != null))
    //                    {

    //                        //                if (
    //                    }


    //                }
    //            }
    //        }
    //    }

        private void Init()
        {
            if (flattenedRoles.Count == 0)
            {
                using (var ctx = storeFactory.Create(this.UserLogin))
                {
                    flattenedRoles = GetRoleMemberLookup(ctx);
                }
            }

            if (this.IsUser)
            {
                //using (var ctx = storeFactory.Create(this.UserLogin))
                //{
                //    var me = 
                //    ctx.Users.IncludePaths("Roles.Role.Organization").Single(
                //        f => f.Username == this.UserLogin
                //        );
                //    myRoles = me.Roles.Select(f => f.Role).ToList();
                //}
                FillMyData();
                RoleKey adminKey = new RoleKey(ADMIN_ROLE, null);
                this.IsAdmin = HasPermission(PermissionType.SiteAdmin, null); // flattenedRoles.ContainsKey(adminKey) && (from have in myRoles join need in flattenedRoles[adminKey] on have.Id equals need.Id select have).Any();
            }
        }

    //    private HttpRequestBase request;
   //     private bool checkedUser = false;

        public virtual string UserLogin { get; private set; }
        public virtual string UserName { get; private set; }
        public virtual bool IsAuthenticated { get; private set; }
        public virtual bool IsUser { get; private set; }

        public virtual bool HasPermission(PermissionType permission, Guid? scope)
        {
            if (this.IsAdmin)
            {
                return true;
            }

            if (this.IsUser)
            {
                if (myAuthzs.Any(f => f.Permission == permission && (scope == null || f.Scope == scope)))
                {
                    return true;
                }

                using (var ctx = storeFactory.Create(this.UserLogin))
                {
                    //// Authorization can be assigned directly to the user.
                    //if (ctx.Authorization.Any(f => f.Permission == permission && f.Scope == scope && f.UserName == this.UserLogin))
                    //{
                    //    return true;
                    //}

                    FieldInfo fi = permission.GetType().GetField(permission.ToString());
                    PermissionScopeAttribute[] attributes = (PermissionScopeAttribute[])fi.GetCustomAttributes(typeof(PermissionScopeAttribute), false);
                    foreach (var attrib in attributes)
                    {
                        if (attrib.ScopeType == PermissionScopeType.Organization)
                        {


                            var authzdRoles = ctx.Authorization.IncludePaths("Role").Where(f => f.Permission == permission && f.Scope == scope && f.Role != null).ToList();

                            var me = ctx.Users.IncludePaths("Roles.Role").Single(f => f.Username == this.UserLogin);
                            var myDirectRoles = me.Roles.Select(f => f.Role).ToList();

                            // If my direct membership in a role means that I end up being in a role that's got the authorization, then I get the authorization.
                            if (authzdRoles.Any(f =>
                            {
                                RoleKey key = new RoleKey { Name = f.Role.Name, OrgId = f.Scope };
                                return flattenedRoles.ContainsKey(key) && (from have in myDirectRoles join need in flattenedRoles[key] on have.Id equals need.Id select have).Any();
                            }))
                            {
                                return true;
                            }

                        }
                        else if (attrib.ScopeType == PermissionScopeType.MemberOfOrganization)
                        {
                            // Figure out if the user has permissions on a member due to being able to manage an organization to which the user belongs
                            // Get the member's current org id's
                            IEnumerable<Guid> orgIds = ctx.Members.Where(f => f.Id == scope.Value).SelectMany(
                                f => f.Memberships.Select(g => g.OrganizationId)).AsEnumerable();
                            
                            // If any of the users authorizations are of the given permission on a scope of one of the user's orgs,
                            // then grant the operation.
                            if (myAuthzs.Any(f => f.Permission == permission && orgIds.Any(g => g == f.Scope)))
                            {
                                return true;
                            }
                        }
                        else if (attrib.ScopeType == PermissionScopeType.Member)
                        {
                            if (myAuthzs.Any(f => f.Permission == permission && f.Scope == scope))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            throw new NotImplementedException("Don't know PermissionScope " + attrib.ScopeType.ToString());
                        }
                        
                    }

                    //Type t = PermissionScopeTypeAttribute.GetScopeType(permission);



                    //if (t == typeof(Organization))
                    //{
                    //    var me = ctx.Users.IncludePaths("Roles.Role").Single(f => f.Username == this.UserLogin);
                    //    var myDirectRoles = me.Roles.Select(f => f.Role).ToList();

                    //    // If my direct membership in a role means that I end up being in a role that's got the authorization, then I get the authorization.
                    //    if (authzdRoles.Any(f =>
                    //    {
                    //        RoleKey key = new RoleKey { Name = f.Role.Name, OrgId = f.Scope };
                    //        return flattenedRoles.ContainsKey(key) && (from have in myDirectRoles join need in flattenedRoles[key] on have.Id equals need.Id select have).Any();
                    //    }))
                    //    {
                    //        return true;
                    //    }
                    //}
                }
            }
            return false;
        }

        public virtual bool IsAdmin { get; private set; }

        //private MembershipUser GetCurrentUser()
        //{
        //    if (this.checkedUser == false)
        //    {
        //        this.currentUser = this.User.Identity.IsAuthenticated ? Membership.GetUser(this.User.Identity.Name) : null;
        //        checkedUser = true;
        //    }
        //    return this.currentUser;
        //}

        //public void SetRequest(HttpRequestBase request)
        //{
        //    this.request = request;
        //}

        //        //public static IPermissionsService TestService { get; set; }

        //        //public static IPermissionsService Create()
        //        //{
        //        //    return PermissionsService.TestService ?? new PermissionsService();
        //        //}

        public virtual User User { get { return this.currentUser; } }

        //public virtual IPrincipal User
        //{
        //    get { return request.RequestContext.HttpContext.User; }
        //}

        //        //public bool UserHasAccount
        //        //{
        //        //    get
        //        //    {
        //        //        return this.GetCurrentUser() != null;
        //        //    }
        //        //}

        //        //public MembershipUser CurrentAccount
        //        //{
        //        //    get { return this.GetCurrentUser(); }
        //        //}

        //        //public PermissionsService()
        //        //{
        //        //}

        //private UserMetadata userMeta = null;
        //public UserMetadata GetUserMetadata()
        //{
        //    if (this.userMeta == null)
        //    {
        //        this.userMeta = GetCurrentUser().GetMetadata();
        //    }
        //    return this.userMeta;
        //}
        //public void SetUserMetadata(UserMetadata value)
        //{
        //    GetCurrentUser().SetMetadata(value);
        //    userMeta = value;
        //}

        public virtual bool IsLocal(HttpRequestBase request)
        {
            var ips = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().SelectMany(f =>
                            f.GetIPProperties().UnicastAddresses.Select(g =>
                                    g.Address.ToString()));

            return ips.Contains(request.UserHostAddress);
        }

        //        public virtual bool IsUserInRole(string username, string role)
        //        {
        //            return Roles.Provider.IsUserInRole(username, role);
        //        }

        //        public void AddUserToRole(string username, string role)
        //        {
        //            Roles.Provider.AddUsersToRoles(new[] { username }, new[] { role });
        //        }

        //        public bool IsInRole(string role)
        //        {
        //            return this.IsUserInRole(this.UserLogin, role);
        //        }

        public virtual string[] GetRolesForUser(string name, bool recursive)
        {
            List<Role> roleList = this.myRoles;
            if (name != this.UserLogin)
            {
                using (var ctx = storeFactory.Create(this.UserLogin))
                {
                    roleList = GetUsersRolesRecursive(ctx, name, flattenedRoles);
                }
            }
            return roleList.Select(f => string.Format("{0}{1}", (f.Organization == null) ? "" : "[" + f.Organization.Name + "] ", f.Name)).OrderBy(f => f).ToArray();
        }

        public virtual void CreateRole(RoleKey role, bool system, RoleKey? parent)
        {
            if (string.IsNullOrWhiteSpace(role.Name))
            {
                throw new ArgumentException("role", "Role name cannot be null or empty");
            }

            using (var ctx = storeFactory.Create(this.UserLogin))
            {
                Role newRole = ctx.Roles.SingleOrDefault(f => f.Name == role.Name && f.OrganizationId == role.OrgId);
                if (newRole != null) throw new InvalidOperationException("Role " + role.Name + " already exists");

                newRole = new Role
                {
                    Name = role.Name,
                    OrganizationId = role.OrgId,
                    SystemRole = system,
                };

                if (parent != null)
                {
                    if (string.IsNullOrWhiteSpace(parent.Value.Name))
                    {
                        throw new ArgumentException("parent", "Parent role name cannot be null or empty");
                    }


                    RoleKey p = parent.Value;

                    var query = ctx.Roles.Where(f => f.Name == p.Name && f.OrganizationId == p.OrgId);
                    //if (parent.Value.OrgId.HasValue)
                    //{
                    //    Guid workaround = parent.Value.OrgId.Value;
                    //    query = query.Where(f => f.Organization.Id == workaround);
                    //}
                    //else
                    //{
                    //    query = query.Where(f => f.Organization == null);
                    //}

                    Role parentRole = query.SingleOrDefault();
                    if (parentRole == null)
                    {
                        throw new InvalidOperationException("Parent role does not exist");
                    }
                    else
                    {
                        newRole.MemberOfRoles.Add(new RoleRoleMembership { Parent = parentRole, Child = newRole, IsSystem = system });
                    }
                }

                ctx.Roles.Add(newRole);
                ctx.SaveChanges();
            }
        }

        //        //public RedirectResult LoginRedirect(string message)
        //        //{
        //        //    return new RedirectResult(FormsAuthentication.LoginUrl);
        //        //}

        //        public bool CanViewOrganization(Guid orgId)
        //        {
        //            // $TODO: CanViewOrganization from orgId
        //            return this.IsUser && true;
        //        }

        //        public bool CanAdminOrganization(Guid orgId)
        //        {
        //            // $TODO: CanAdminOrganization from orgId
        //            return this.IsUser && true;
        //        }

        //        public bool CanViewMember(Guid id)
        //        {
        //            //$TODO: CanViewMember
        //            return this.IsUser && true;
        //        }
        public LogonResult ValidateUser(string username, string password)
        {
            using (var ctx = this.storeFactory.Create(""))
            {
                User u = ctx.Users.SingleOrDefault(f => f.Username == username);
                if (u == null) return LogonResult.UserNotFound;

                if (u.Password != AuthIdentityService.CreatePasswordHash(password, u.PasswordSalt)) return LogonResult.BadPassword;

                return LogonResult.Okay;
            }
        }

        public virtual void SetAuthCookie(string username)
        {
            FormsAuthentication.SetAuthCookie(username, false /* create persistent cookie */);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="store"></param>
        /// <returns>The unencrypted password of the new 'admin' user</returns>
        public static User CreateAdminUser(string password)
        {
            string salt = GenerateSalt();
            User admin = new User {
                Username = "admin",
                Email = ConfigurationManager.AppSettings["adminEmail"] ?? "admin@example.local",
                Name = "Admin User",
                PasswordSalt = salt,
                Password = CreatePasswordHash(password, salt),
                State = UserState.Okay                
            };
            return admin;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="u"></param>
        /// <returns>New password in clear text</returns>
        public static string ResetPassword(User u)
        {
            u.PasswordSalt = GenerateSalt();
            string password = Membership.GeneratePassword(8, 2);
            u.Password = CreatePasswordHash(password, u.PasswordSalt);
            return password;
        }

        public static bool ChangePassword(User u, string oldPassword, string newPassword)
        {
            if (CreatePasswordHash(oldPassword, u.PasswordSalt) != u.Password)
            {
                return false;
            }
            SetPassword(u, newPassword);
            return true;
        }

        /// <summary>
        /// Overwrites .Password and .PasswordSalt of the specified user
        /// with the encrypted values for the password in .Password
        /// </summary>
        /// <param name="u"></param>
        /// <param name="newPassword"></param>
        private static void SetPassword(User u, string newPassword)
        {
            u.PasswordSalt = GenerateSalt();
            u.Password = CreatePasswordHash(newPassword, u.PasswordSalt);
        }

        internal static Models.User SetupUser(User u)
        {
            u.State = UserState.Verification;
            u.ValidationKey = Guid.NewGuid();
            SetPassword(u, u.Password);
            return u;
        }

        private static string GenerateSalt()
        {
            byte[] data = new byte[0x10];
            new RNGCryptoServiceProvider().GetBytes(data);
            return Convert.ToBase64String(data);
        }

        private static string CreatePasswordHash(string pwd, string salt)
        {
            string saltAndPwd = String.Concat(pwd, salt);
            string hashedPwd = FormsAuthentication.HashPasswordForStoringInConfigFile(saltAndPwd, "sha1");
            return hashedPwd;
        } 
    }

    public class AuthIdentityServiceCache
    {
        public static AuthIdentityService GetInstance(IPrincipal principal, DataStoreFactory factory)
        {
            string username = (principal == null) ? "" : principal.Identity.Name;

            //if (!AuthIdentityServiceCache.cache.ContainsKey(username))
            //{
            //    cache[username] = new AuthIdentityService(username, factory);
            //}
            //return cache[username];
            return new AuthIdentityService(username, factory);
        }

        protected static Dictionary<string, AuthIdentityService> cache = new Dictionary<string, AuthIdentityService>();
    }

    public enum LogonResult
    {
        Okay,
        BadPassword,
        Locked,
        UserNotFound
    }
    

    //public class PermissionScopeTypeAttribute : Attribute
    //{
    //    public Type Type { get; private set; }
    //    public PermissionScopeTypeAttribute(Type scopeType)
    //    {
    //        this.Type = scopeType;
    //    }

    //    public static Type GetScopeType(Enum value)
    //    {
    //        // Get the Description attribute value for the enum value
    //        FieldInfo fi = value.GetType().GetField(value.ToString());
    //        PermissionScopeTypeAttribute[] attributes = (PermissionScopeTypeAttribute[])fi.GetCustomAttributes(typeof(PermissionScopeTypeAttribute), false);
    //        if (attributes.Length > 0)
    //        {
    //            return attributes[0].Type;
    //        }
    //        else
    //        {
    //            return typeof(object);
    //        }
    //    }
    //}

    //public abstract class PermissionValidator
    //{
    //    public abstract bool HasPermission(PermissionType permission, Guid? scope, IDataStoreService store, string username, Dictionary<RoleKey, List<Role>> flattenedRoles);
    //}

    //public class OrgLevelPermissionValidator : PermissionValidator
    //{
    //    public override bool HasPermission(PermissionType permission, Guid? scope, IDataStoreService store, string username, Dictionary<RoleKey, List<Role>> flattenedRoles)
    //    {
    //        var authzdRoles = store.Authorization.IncludePaths("Role").Where(f => f.Permission == permission && f.Scope == scope && f.Role != null).ToList();

    //        var me = store.Users.IncludePaths("Roles.Role").Single(f => f.Username == username);
    //        var myDirectRoles = me.Roles.Select(f => f.Role).ToList();

    //        // If my direct membership in a role means that I end up being in a role that's got the authorization, then I get the authorization.
    //        if (authzdRoles.Any(f =>
    //        {
    //            RoleKey key = new RoleKey { Name = f.Role.Name, OrgId = f.Scope };
    //            return flattenedRoles.ContainsKey(key) && (from have in myDirectRoles join need in flattenedRoles[key] on have.Id equals need.Id select have).Any();
    //        }))
    //        {
    //            return true;
    //        }

    //        return false;
    //    }
    //}

    //public class MemberOfUnitPermissionValidator : PermissionValidator
    //{
    //    public override bool HasPermission(PermissionType permission, Guid? scope, IDataStoreService store, string username, Dictionary<RoleKey, List<Role>> flattenedRoles)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class MemberPermissionValidator : PermissionValidator
    //{
    //    public override bool HasPermission(PermissionType permission, Guid? scope, IDataStoreService store, string username, Dictionary<RoleKey, List<Role>> flattenedRoles)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class PermissionValidatorAttribute : Attribute
    //{
    //    public Type ValidatorType { get; private set; }
    //    public PermissionValidatorAttribute(Type validatorType)
    //    {
    //        if (!typeof(PermissionValidator).IsAssignableFrom(validatorType))
    //        {
    //            throw new ArgumentException("Must derive from PermissionValidator", "validatorType");
    //        }

    //        this.ValidatorType = validatorType;
    //    }
    //}

    public enum PermissionScopeType
    {
        Organization,
        MemberOfOrganization,
        Member
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class PermissionScopeAttribute : Attribute
    {
        public PermissionScopeType ScopeType { get; private set; }
        public PermissionScopeAttribute(PermissionScopeType scopeType)
        {
            this.ScopeType = scopeType;
        }
    }

    public enum PermissionType
    {
//        [PermissionValidator(typeof(OrgLevelPermissionValidator))]
        [PermissionScope(PermissionScopeType.Organization)]
        ViewOrganizationBasic = 1,

//        [PermissionValidator(typeof(OrgLevelPermissionValidator))]
        [PermissionScope(PermissionScopeType.Organization)]
        ViewOrganizationDetail = 2,

//        [PermissionValidator(typeof(OrgLevelPermissionValidator))]
        [PermissionScope(PermissionScopeType.Organization)]
        AddOrganizationMembers = 3,

//        [PermissionValidator(typeof(OrgLevelPermissionValidator))]
        [PermissionScope(PermissionScopeType.Organization)]
        AdminOrganization = 4,

//        [PermissionValidator(typeof(OrgLevelPermissionValidator))]
        [PermissionScope(PermissionScopeType.MemberOfOrganization)]
        [PermissionScope(PermissionScopeType.Member)]
        ViewMemberBasic = 5,

        //[PermissionValidator(typeof(MemberOfUnitPermissionValidator))]
        //[PermissionValidator(typeof(MemberPermissionValidator))]
        [PermissionScope(PermissionScopeType.MemberOfOrganization)]
        [PermissionScope(PermissionScopeType.Member)]
        ViewMemberStandard = 6,

        //[PermissionValidator(typeof(MemberPermissionValidator))]
        //[PermissionValidator(typeof(MemberOfUnitPermissionValidator))]
        [PermissionScope(PermissionScopeType.MemberOfOrganization)]
        [PermissionScope(PermissionScopeType.Member)]
        ViewMemberDetail = 7,

        //[PermissionValidator(typeof(MemberPermissionValidator))]
        //[PermissionValidator(typeof(MemberOfUnitPermissionValidator))]
        [PermissionScope(PermissionScopeType.MemberOfOrganization)]
        [PermissionScope(PermissionScopeType.Member)]
        EditMemberContacts = 8,

        //[PermissionValidator(typeof(MemberPermissionValidator))]
        //[PermissionValidator(typeof(MemberOfUnitPermissionValidator))]
        [PermissionScope(PermissionScopeType.MemberOfOrganization)]
        [PermissionScope(PermissionScopeType.Member)]
        EditMember = 13,

        //[PermissionValidator(typeof(MemberPermissionValidator))]
        //[PermissionValidator(typeof(OrgLevelPermissionValidator))]
        [PermissionScope(PermissionScopeType.Organization)]
        AddUnitMission = 9,

        //[PermissionValidator(typeof(MemberPermissionValidator))]
        //[PermissionValidator(typeof(OrgLevelPermissionValidator))]
        [PermissionScope(PermissionScopeType.Organization)]
        AddTraining = 10,

        //[PermissionValidator(typeof(MemberPermissionValidator))]
        //[PermissionValidator(typeof(OrgLevelPermissionValidator))]
        [PermissionScope(PermissionScopeType.Organization)]
        ListOrganization = 11,

        SiteAdmin = 12,
    }
}