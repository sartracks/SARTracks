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
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Security;
    using SarTracks.Website.Models;
    using SarTracks.Website.Services;
    using SarTracks.Website.ViewModels;

    public class OrganizationsController : ControllerBase
    {
        public const string VIEWDATA_LIST_TIMEZONES = "listTimezones";
        public const string VIEWDATA_LIST_VISIBILITY = "listVisibility";

        public OrganizationsController() : base() { }
        public OrganizationsController(AuthIdentityService perms, DataStoreFactory factory) : base(perms, factory) { }

        [HttpGet]
        [Authorize]
        public ActionResult Create()
        {
            string selectedRole = AuthIdentityService.USERS_ROLE;
            var arguments = new NewOrganizationViewModel
            {
                Org = new SarUnit { AdminAccount = Permissions.UserLogin },
                Visibility = selectedRole
            };
            ViewData[OrganizationsController.VIEWDATA_LIST_TIMEZONES] = new SelectList(TimeZoneInfo.GetSystemTimeZones().Select(f => new { Id = f.Id, Name = string.Format("[{0}{1:hh\\:mm}] {2}", (f.BaseUtcOffset.TotalHours < 0) ? '-' : '+', f.BaseUtcOffset, f.StandardName) }), "Id", "Name");
            ViewData[OrganizationsController.VIEWDATA_LIST_VISIBILITY] = new SelectList(new[] { AuthIdentityService.EVERYONE_ROLE, AuthIdentityService.USERS_ROLE, AuthIdentityService.MISSION_VIEWERS_ROLE, "Restricted" }, selectedRole);
            return View(arguments);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Create(NewOrganizationViewModel model)
        {
            var org = model.Org;
            if (ModelState.IsValid)
            {
                using (var ctx = GetRepository())
                {
                    ctx.Users.Attach(Permissions.User);

                    // Default status types
                    org.UnitStatusTypes.Add(new UnitStatusType { Name = "Active", IsActive = true, IsMissionQualified = true, Organization = org });
                    org.UnitStatusTypes.Add(new UnitStatusType { Name = "Inactive", IsActive = false, IsMissionQualified = false, Organization = org });

                    ctx.Organizations.Add(org);

                    ctx.InitializeOrganizationSecurity(org, Permissions.User);

                    ctx.SaveChanges();

                    return RedirectToAction("Home", new { q = org.Id });
                }
            }

            // If we got this far, something failed - redisplay form
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public DataActionResult DoDeleteOrganization(Guid q)
        {
            List<SubmitError> errors = new List<SubmitError>();
            bool result = false;
            Organization model = null;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations.SingleOrDefault(f => f.Id == q);
                if (model != null)
                {
                    ctx.Organizations.Remove(model);
                    ctx.SaveChanges();
                    result = true;
                }
                else
                {
                    errors.Add(new SubmitError { Error = "Organization not found", Id = new [] { q } });
                }
            }
            return Data(new SubmitResult<bool> { Result = result, Errors = errors.ToArray() });
        }

        [Authorize]
        [HttpGet]
        public ActionResult Home(Guid q)
        {
            Organization model = null;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations.SingleOrDefault(f => f.Id == q);

                if (!model.IsApproved)
                {
                    ViewData["warning"] = "Your organization has not been approved by the site administrator, and will not be displayed in site reports.";
                }
            }


            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Settings(Guid q)
        {
            Organization model = null;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations.SingleOrDefault(f => f.Id == q);

                if (!model.IsApproved)
                {
                    ViewData["warning"] = "Your organization has not been approved by the site administrator, and will not be displayed in site reports.";
                }
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult StatusManagement(Guid q)
        {
            if (!Permissions.HasPermission(PermissionType.ViewOrganizationDetail, q)) return GetLoginError();

            UnitStatusType model = new UnitStatusType();
            using (var ctx = GetRepository())
            {
                var org = ctx.Organizations.Single(f => f.Id == q);
                model.Organization = org;
                model.OrganizationId = org.Id;
            }

            return PartialView(model);
        }

        [HttpPost]
        public DataActionResult SetOrganizationApproved(Guid q, bool approved)
        {
            List<SubmitError> errors = new List<SubmitError>();
            Organization org = null;
            using (var ctx = GetRepository())
            {
                org = ctx.Organizations.SingleOrDefault(f => f.Id == q);
                if (org == null)
                {
                    errors.Add(new SubmitError { Property = q.ToString(), Error = "Organization Not Found" });
                }
                else
                {
                    org.IsApproved = approved;
                    ctx.SaveChanges();
                }
            }

            return Data(new SubmitResult<Organization> { Errors = errors.ToArray(), Result = org });
        }

        [HttpGet]
        public ActionResult List()
        {
            if (!Permissions.HasPermission(PermissionType.SiteAdmin, null)) return GetLoginRedirect();
            return View();
        }

        [HttpPost]
        public DataActionResult GetList()
        {
            //if (!Permissions.IsInRole(AuthIdentityService.ADMIN_ROLE)) return GetLoginError();

            Organization[] model;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations
                    .OrderBy(f => f.LongName).AsEnumerable()
                    .Where(f => Permissions.HasPermission(PermissionType.ListOrganization, f.Id)).ToArray();
            }
            return Data(model);
        }

        [HttpGet]
        [Authorize]
        public ActionResult StatusTypes(Guid q)
        {
            if (!Permissions.HasPermission(PermissionType.ViewOrganizationDetail, q)) return GetLoginRedirect();

            Organization unit;
            using (var ctx = GetRepository())
            {
                unit = (Organization)ctx.Organizations.SingleOrDefault(f => f.Id == q);
            }
            return View(unit);
        }

        [HttpGet]
        [Authorize]
        public ActionResult CreateStatus(Guid q)
        {
            if (!Permissions.HasPermission(PermissionType.AdminOrganization, q)) return GetLoginRedirect();

            UnitStatusType model;
            using (var ctx = GetRepository())
            {
                model = new UnitStatusType
                {
                    //Organization = (SarUnit)ctx.Organizations.Single(f => f.Id == q)
                };
            }
            ViewBag.OrgId = q;
            return PartialView("StatusForm", model);
        }

        [HttpGet]
        [Authorize]
        public ActionResult EditStatus(Guid q, Guid status)
        {
            if (!Permissions.HasPermission(PermissionType.AdminOrganization, q)) return GetLoginRedirect();
            UnitStatusType model;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations.Where(f => f.Id == q).SelectMany(f => f.UnitStatusTypes).Where(f => f.Id == status).Single();
            }
            ViewBag.OrgId = q;
            return PartialView("StatusForm", model);
        }

        [HttpPost]
        [Authorize]
        public DataActionResult SaveStatus(Guid q, UnitStatusType model)
        {
            if (!Permissions.HasPermission(PermissionType.AdminOrganization, q)) return GetLoginError();

            SubmitResult<UnitStatusType> result = new SubmitResult<UnitStatusType>();
            ModelState.Remove("Organization");
            if (ModelState.IsValid)
            {
                using (var ctx = GetRepository())
                {
                    var oldModel = ctx.Organizations.Where(f => f.Id == q).SelectMany(f => f.UnitStatusTypes).SingleOrDefault(f => f.Id == model.Id);
                    if (oldModel == null)
                    {
                        ctx.Organizations.Single(f => f.Id == q).UnitStatusTypes.Add(model);
                    }
                    else
                    {
                        oldModel.CopyFrom(model);
                    }

                    ctx.SaveChanges();
                }
            }
            result.Result = model;
            return Data(result);
        }


        [HttpPost]
        [Authorize]
        public DataActionResult GetStatusTypes(Guid q)
        {
            if (!Permissions.HasPermission(PermissionType.ViewOrganizationBasic, q)) return GetLoginError();

            UnitStatusType[] model;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations.Where(f => f.Id == q).SelectMany(f => f.UnitStatusTypes).ToArray();
            }

            return Data(model);
        }
    }
}