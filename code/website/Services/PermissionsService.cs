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
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Security;
    using System.Security.Principal;
    using System.Runtime.Serialization.Json;
    using System.Runtime.Serialization;
    using System.IO;
    using System.Globalization;
    using System.Text;

    [DataContract]
    public class UserMetadata
    {
        [DataMember(EmitDefaultValue=false)]
        public Guid VerifyKey { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Guid LinkedMember { get; set; }
    }

    public interface IPermissionsService
    {
        string Username { get; }
        bool IsAuthenticated { get; }
        bool IsUser { get; }

        //IPrincipal User { get; }
        bool IsLocal(HttpRequestBase request);
        bool IsUserInRole(string username, string role);
        void AddUserToRole(string username, string role);
        bool IsInRole(string role);
        //RedirectResult LoginRedirect(string message);
        bool CanViewOrganization(Guid orgId);
        bool CanAdminOrganization(Guid orgId);
        bool CanViewMember(Guid id);

        string[] GetRolesForUser(string name, bool recursive);

        //bool UserHasAccount { get; }
        //MembershipUser CurrentAccount { get; }

        //UserMetadata GetUserMetadata();
        //void SetUserMetadata(UserMetadata value);
    }

    public static class PermissionExtensions
    {
        public static UserMetadata GetMetadata(this MembershipUser user)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(UserMetadata));
            using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(user.Comment ?? "null")))
            {
                return ser.ReadObject(ms) as UserMetadata ?? new UserMetadata();
            }
        }

        public static void SetMetadata(this MembershipUser user, UserMetadata value)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(UserMetadata));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, value);
                user.Comment = Encoding.ASCII.GetString(ms.ToArray());
                Membership.UpdateUser(user);
            }
        }

        public static void VerifyAccount(this MembershipUser user)
        {
            user.IsApproved = true;
            UserMetadata data = user.GetMetadata();
            data.VerifyKey = Guid.Empty;
            user.SetMetadata(data);
        }
    }

    public class PermissionsServiceCache
    {
        public static IPermissionsService GetInstance(string username)
        {
            if (!PermissionsServiceCache.cache.ContainsKey(username))
            {
                cache[username] = new PermissionsService(username);
            }
            return cache[username];
        }

        protected static Dictionary<string, IPermissionsService> cache = new Dictionary<string, IPermissionsService>();
    }

    public class PermissionsService : IPermissionsService
    {
        private MembershipUser currentUser = null;

        public PermissionsService(string username)
        {
            this.currentUser = Membership.GetUser(username);
            this.Username = username;
            this.IsAuthenticated = !string.IsNullOrWhiteSpace(username);
            this.IsUser = this.currentUser != null;
        }

 //       private HttpRequestBase request;
        //private bool checkedUser = false;

        public string Username { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public bool IsUser { get; private set; }

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

        //public static IPermissionsService TestService { get; set; }

        //public static IPermissionsService Create()
        //{
        //    return PermissionsService.TestService ?? new PermissionsService();
        //}

        //public virtual IPrincipal User
        //{
        //    get { return request.RequestContext.HttpContext.User; }
        //}

        //public bool UserHasAccount
        //{
        //    get
        //    {
        //        return this.GetCurrentUser() != null;
        //    }
        //}

        //public MembershipUser CurrentAccount
        //{
        //    get { return this.GetCurrentUser(); }
        //}

        //public PermissionsService()
        //{
        //}

        //private UserMetadata userMeta = null;
        //public UserMetadata GetUserMetadata()
        //{
        //        if (this.userMeta == null)
        //        {
        //            this.userMeta = GetCurrentUser().GetMetadata();
        //        }
        //        return this.userMeta;
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

        public virtual bool IsUserInRole(string username, string role)
        {
            return Roles.Provider.IsUserInRole(username, role);
        }

        public void AddUserToRole(string username, string role)
        {
            Roles.Provider.AddUsersToRoles(new[] { username }, new[] { role });
        }

        public bool IsInRole(string role)
        {
            return this.IsUserInRole(this.Username, role);
        }

        public string[] GetRolesForUser(string name, bool recursive)
        {
            NestedRoleProvider nested = Roles.Provider as NestedRoleProvider;
            if (nested != null)
            {
                return nested.GetRolesForUser(name, recursive);
            }
            return Roles.Provider.GetRolesForUser(name);
        }

        //public RedirectResult LoginRedirect(string message)
        //{
        //    return new RedirectResult(FormsAuthentication.LoginUrl);
        //}

        public bool CanViewOrganization(Guid orgId)
        {
            // $TODO: CanViewOrganization from orgId
            return this.IsUser && true;
        }

        public bool CanAdminOrganization(Guid orgId)
        {
            // $TODO: CanAdminOrganization from orgId
            return this.IsUser && true;
        }

        public bool CanViewMember(Guid id)
        {
            //$TODO: CanViewMember
            return this.IsUser && true;
        }
    }
}