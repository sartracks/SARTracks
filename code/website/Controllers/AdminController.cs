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
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Text;
using SarTracks.Website.Models;
    using System.Data.Entity;

    public class AdminController : ControllerBase
    {
        public const string VIEWDATA_ADMINPASSWORD = "adminPassword";
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

            using (var ctx = GetRepository())
            {
                ViewData[AdminController.VIEWDATA_ADMINPASSWORD] = ctx.InitializeSystemSecurity();
            }

            return View("Setup");
        }

        [HttpGet]
        public ActionResult ResetAdminPassword()
        {
            if (!Permissions.IsLocal(Request)) return GetLoginRedirect();

            using (var ctx = GetRepository())
            {
                var user = ctx.Users.Single(f => f.Username == "admin");
                string password = AuthIdentityService.ResetPassword(user);
                ctx.SaveChanges();
                return new ContentResult { Content = string.Format("New password is \"{0}\". <a href=\"{1}\">Change Password</a>", password, Url.Action("ChangePassword", "Account")), ContentType = "text/html" };
            }
        }

        [HttpGet]
        public ActionResult DisplaySecurity()
        {
            if (!Permissions.IsAdmin) return GetLoginRedirect();

            return View();
        }

        [HttpPost]
        public DataActionResult GetRoles()
        {
            if (!Permissions.IsAdmin) return GetLoginError();

            using (var ctx = this.StoreFactory.Create(Permissions.UserLogin))
            {
                return Data(ctx.Roles.Include("Organization").OrderBy(f => f.Organization.Name).ThenBy(f => f.Name).ToArray());
            }
        }

        [HttpPost]
        public DataActionResult GetRoleSecurity(Guid q)
        {
            if (!Permissions.IsAdmin) return GetLoginError();

            using (var ctx = this.StoreFactory.Create(Permissions.UserLogin))
            {
                var role = ctx.Roles.IncludePaths("Users.User", "MemberRoles.Child.Organization").Single(f => f.Id == q);
                
                var model = new RoleSecurityViewModel {
                    Users = role.Users.Select(f => (f.User == null) ? "" : f.User.Username).ToArray(),
                    Children = role.MemberRoles.Select(f => f.Child).ToArray(),
                    Authorizations = ctx.Authorization.Where(f => f.RoleId == role.Id).ToArray()
                };
                
                return Data(model);
            }
        }
    }
}
