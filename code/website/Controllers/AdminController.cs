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
namespace SarTracks.Website.Controllers
{
    using System;
    using System.Linq;
    using System.Web.Mvc;
    using System.Web.Security;
    using SarTracks.Website.Services;
    using SarTracks.Website.ViewModels;

    public class AdminController : ControllerBase
    {
        //
        // GET: /Admin/        
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Setup()
        {
            if (!Permissions.IsLocal(Request)) return GetLoginRedirect();

            string adminGroup = "administrators";

            if (!Roles.RoleExists(adminGroup))
            {
                Roles.CreateRole(adminGroup);
                ViewData["adminGroupCreated"] = true;
            }

            MembershipUser user = Membership.FindUsersByName("admin").Cast<MembershipUser>().SingleOrDefault();
            if (user == null)
            {
                string password = Membership.GeneratePassword(10, 5);
                user = Membership.CreateUser("admin", password);
                ViewData["adminPassword"] = password;
            }

            if (!Permissions.IsUserInRole(user.UserName, adminGroup))
            {
                Permissions.AddUserToRole(user.UserName, adminGroup);
                ViewData["adminInGroup"] = true;
            }

            using (var ctx = GetRepository())
            {
                foreach (var org in ctx.Organizations.Select(f => f.Id))
                {
                    string prefix = "org" + org.ToString() + '.';
                    if (!Roles.RoleExists(prefix + "admins"))
                    {
                        Roles.CreateRole(prefix + "admins");
                        ((NestedRoleProvider)Roles.Provider).AddRoleToRole("administrators", prefix + "admins");
                    }
                    if (!Roles.RoleExists(prefix + "users"))
                    {
                        Roles.CreateRole(prefix + "users");
                        ((NestedRoleProvider)Roles.Provider).AddRoleToRole(prefix + "admins", prefix + "users");
                    }
                }
            }

            return View();
        }

        [HttpGet]
        public ActionResult ResetAdminPassword()
        {
            if (!Permissions.IsLocal(Request)) return GetLoginRedirect();

            MembershipUser user = Membership.FindUsersByName("admin").Cast<MembershipUser>().SingleOrDefault();
            string password = user.ResetPassword();

            return new ContentResult { Content = string.Format("New password is \"{0}\". <a href=\"{1}\">Change Password</a>", password, Url.Action("ChangePassword", "Account")), ContentType = "text/html" };
        }

        [HttpGet]
        [Authorize(Roles="Administrators")]
        public ActionResult Users()
        {
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">Organization ID</param>
        [HttpPost]
        public DataActionResult GetUserList()
        {
            if (!Permissions.IsInRole("Administrators")) return GetLoginError();


            var model = Membership.GetAllUsers().Cast<MembershipUser>().Select(f =>
            {
                //var profile = ProfileBase.Create(f.UserName);
                var metadata = f.GetMetadata();

                return new AccountAdminInfo
                {
                    UserName = f.UserName,
                    Email = f.Email,
                    IsLocked = f.IsLockedOut,
                    IsApproved = f.IsApproved,
                    LastActive = f.LastActivityDate,
                    Created = f.CreationDate,
                    LinkedMember = (metadata.LinkedMember  == Guid.Empty) ? (Guid?)null : metadata.LinkedMember
                };
            }).ToArray();

            return Data(model);
        }

        [HttpPost]
        public DataActionResult DoRemoveUser(string q)
        {
            if (!Permissions.IsInRole("Administrators")) return GetLoginError();

            SubmitResult<bool> result = new SubmitResult<bool>();
            
            result.Result = Membership.DeleteUser(q);

            return Data(result);
        }

        [HttpPost]
        public DataActionResult SetUserApproved(string q, bool approved)
        {
            if (!Permissions.IsInRole("Administrators")) return GetLoginError();

            SubmitResult<bool> result = new SubmitResult<bool>();

            MembershipUser user = Membership.GetUser(q);
            user.IsApproved = approved;
            Membership.UpdateUser(user);
            result.Result = true;

            return Data(result);
        }
    }
}
