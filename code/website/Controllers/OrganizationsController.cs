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

    public class OrganizationsController : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public ActionResult Create()
        {
            SarUnit newOrg = new SarUnit { AdminAccount = Permissions.Username };
            ViewData["timezones"] = new SelectList(TimeZoneInfo.GetSystemTimeZones().Select(f => new { Id = f.Id, Name = string.Format("[{0}{1:hh\\:mm}] {2}", (f.BaseUtcOffset.TotalHours < 0) ? '-' : '+', f.BaseUtcOffset, f.StandardName) }), "Id", "Name");
            return View(newOrg);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Create(SarUnit model)
        {
            model.AdminAccount = Permissions.Username;
            if (ModelState.IsValid)
            {
                using (var ctx = GetRepository())
                {
                    // Default status types
                    model.UnitStatusTypes.Add(new UnitStatusType { Name = "Active", IsActive = true, IsMissionQualified = true, Organization = model });
                    model.UnitStatusTypes.Add(new UnitStatusType { Name = "Inactive", IsActive = false, IsMissionQualified = false, Organization = model });

                    ctx.Organizations.Add(model);
                    ctx.SaveChanges();
                    
                    // Security roles
                    string adminRoleName = string.Format("org{0}.admins", model.Id);
                    Roles.CreateRole(adminRoleName);
                    string userRole = string.Format("org{0}.users", model.Id); 
                    Roles.CreateRole(userRole);
                    ((NestedRoleProvider)Roles.Provider).AddRoleToRole(adminRoleName, userRole);
                    Permissions.AddUserToRole(Permissions.Username, adminRoleName);

                    return RedirectToAction("Home", new { q = model.Id });
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
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
        [Authorize(Roles="Administrators")]
        public ActionResult List()
        {
            return View();
        }

        [HttpPost]
        public DataActionResult GetList()
        {
            if (!Permissions.IsInRole("Administrators")) return GetLoginError();

            Organization[] model;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations.OrderBy(f => f.LongName).ToArray();
            }
            return Data(model);
        }

        [HttpGet]
        [Authorize]
        public ActionResult StatusTypes(Guid q)
        {
            if (!Permissions.CanViewOrganization(q)) return GetLoginRedirect();

            SarUnit unit;
            using (var ctx = GetRepository())
            {
                unit = (SarUnit)ctx.Organizations.SingleOrDefault(f => f.Id == q);
            }
            return View(unit);
        }

        [HttpGet]
        public ActionResult CreateStatus(Guid q)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginRedirect();

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
        public ActionResult EditStatus(Guid q, Guid status)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginRedirect();
            UnitStatusType model;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations.Where(f => f.Id == q).SelectMany(f => f.UnitStatusTypes).Where(f => f.Id == status).Single();
            }
            ViewBag.OrgId = q;
            return PartialView("StatusForm", model);
        }

        [HttpPost]
        public DataActionResult SaveStatus(Guid q, UnitStatusType model)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginError();

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
        public DataActionResult GetStatusTypes(Guid id)
        {
            if (!Permissions.CanViewOrganization(id)) return GetLoginError();

            UnitStatusType[] model;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations.Where(f => f.Id == id).SelectMany(f => f.UnitStatusTypes).ToArray();
            }

            return Data(model);
        }
    }
}