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
    using SarTracks.Website.Models;
    using SarTracks.Website.ViewModels;
    using System.Data.Entity.Infrastructure;
    using SarTracks.Website.Services;

    public class MembersController : ControllerBase
    {
        public MembersController() : base() { }
        public MembersController(AuthIdentityService perms, DataStoreFactory store) : base(perms, store) { }
        
        [HttpGet]
        public ActionResult Detail(Guid q)
        {
            if (!Permissions.HasPermission(PermissionType.ViewMemberStandard, q)) return GetLoginRedirect();

            SarMember model;
            using (var ctx = GetRepository())
            {
                model = ctx.Members.Single(f => f.Id == q);
                if (!Permissions.HasPermission(PermissionType.ViewMemberDetail, q))
                {
                    MembersController.FilterPersonalInfo(new[] { model });
                }
            }

            ViewData["canEdit"] = Permissions.HasPermission(PermissionType.EditMemberContacts, q);
            return View(model);
        }

        [HttpGet]
        public ActionResult ContactManagement(Guid q)
        {
            SarMember member = null;
            using (var ctx = GetRepository())
            {
                member = ctx.Members.Single(f => f.Id == q);
            }
            return PartialView(member);
        }


        public static void FilterPersonalInfo(IEnumerable<SarMember> members)
        {
            foreach (var member in members)
            {
                member.BirthDate = null;
            }
        }

        [HttpPost]
        public DataActionResult SubmitAddress(MemberAddress model)
        {
            if (!Permissions.HasPermission(PermissionType.EditMemberContacts, model.MemberId)) return GetLoginError();
            List<SubmitError> errors = new List<SubmitError>();
            ModelState.Remove("Member");
            ModelState.Remove("Id");

            if (ModelState.IsValid)
            {
                using (var ctx = this.GetRepository())
                {
                    var oldModel = ctx.Members.IncludePaths("Address.Member").Where(f => f.Id == model.MemberId).SelectMany(f => f.Addresses).SingleOrDefault(f => f.Id == model.Id);
                    if (oldModel == null)
                    {
                        ctx.Members.Single(f => f.Id == model.MemberId).Addresses.Add(model);
                    }
                    else
                    {
                        oldModel.CopyFrom(model);
                        if (oldModel.Member == null) oldModel.Member = ctx.Members.Single(f => f.Id == oldModel.MemberId);
                    }
                    ctx.SaveChanges();
                }
            }
            else
            {
                ModelStateToSubmitErrors(errors);
            }

            return Data(new SubmitResult<MemberAddress> { Errors = errors.ToArray(), Result = model });
        }

        [HttpPost]
        public DataActionResult DeleteAddress(Guid q, Guid e)
        {
            if (!Permissions.HasPermission(PermissionType.EditMemberContacts, q)) return GetLoginError();

            using (var ctx = GetRepository())
            {
                var model = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.Addresses).Single(f => f.Id == e);

                ctx.Delete(model);

                ctx.SaveChanges();
            }
            return Data(new SubmitResult<bool> { Result = true });
        }


        [HttpPost]
        public DataActionResult GetAddresses(Guid q)
        {
            if (!Permissions.HasPermission(PermissionType.ViewMemberStandard, q)) return GetLoginError();

            MemberAddress[] model;
            using (var ctx = GetRepository())
            {
                model = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.Addresses)
                    .OrderBy(f => f.Type).ThenBy(f => f.Address.Street)
                    .ToArray();
            }

            return Data(model);
        }

        [HttpPost]
        public DataActionResult DeleteContact(Guid q)
        {
            using (var ctx = GetRepository())
            {
                var model = ctx.Members.SelectMany(f => f.ContactInfo).Single(f => f.Id == q);

                if (!Permissions.HasPermission(PermissionType.EditMemberContacts, model.MemberId)) return GetLoginError();

                ctx.Delete(model);

                ctx.SaveChanges();
            }
            return Data(new SubmitResult<bool> { Result = true });
        }


        [HttpPost]
        public DataActionResult GetContactInfo(Guid q)
        {
            if (!Permissions.HasPermission(PermissionType.ViewMemberStandard, q)) return GetLoginError();

            MemberContact[] model;
            using (var ctx = GetRepository())
            {
                model = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.ContactInfo)
                    .OrderBy(f => f.Type).ThenBy(f => f.Priority).ThenBy(f => f.SubType).ThenBy(f => f.Value)
                    .ToArray();
            }

            return Data(model);
        }

        [HttpPost]
        public DataActionResult SubmitContact(MemberContact model)
        {
            if (!Permissions.HasPermission(PermissionType.EditMemberContacts, model.MemberId)) return GetLoginError();
            List<SubmitError> errors = new List<SubmitError>();
            ModelState.Remove("Member");
            ModelState.Remove("Id");

            if (ModelState.IsValid)
            {
                using (var ctx = this.GetRepository())
                {
                    var oldModel = ctx.Members.IncludePaths("ContactInfo.Member").Where(f => f.Id == model.MemberId).SelectMany(f => f.ContactInfo).SingleOrDefault(f => f.Id == model.Id);
                    if (oldModel == null)
                    {
                        ctx.Members.Single(f => f.Id == model.MemberId).ContactInfo.Add(model);
                    }
                    else
                    {
                        oldModel.CopyFrom(model);
                        
                        if (oldModel.Member == null) oldModel.Member = ctx.Members.Single(f => f.Id == oldModel.MemberId);
                    }
                    ctx.SaveChanges();
                }
            }
            else
            {
                ModelStateToSubmitErrors(errors);
            }
            return Data(new SubmitResult<MemberContact> { Errors = errors.ToArray(), Result = model });
        }

        [HttpPost]
        public DataActionResult GetAllMemberships(Guid q)
        {
            if (!Permissions.HasPermission(PermissionType.ViewMemberBasic, q)) return GetLoginError();

            UnitMembership[] model;

            using (var ctx = GetRepository())
            {
                var orgs = ctx.Organizations.ToArray();

                model = ctx.Members.IncludePaths("Memberships.Unit", "Memberships.Status")
                    .Where(f => f.Id == q)
                    .SelectMany(f => f.Memberships)
                    .OrderBy(f => f.Organization.Name).ThenByDescending(f => f.Status).ToArray();

                foreach (var row in model)
                {

                }
            }

            return Data(model);
        }

        [HttpPost]
        [Authorize]
        public DataActionResult GetDesignators(Guid q, DateTime? when)
        {
            if (!Permissions.HasPermission(PermissionType.ViewMemberBasic, q)) return GetLoginError();

            when = when ?? DateTime.Now;

            Dictionary<string, List<Guid>> designators = new Dictionary<string, List<Guid>>();
            Dictionary<Guid, string> lookup = new Dictionary<Guid, string>();

            using (var ctx = GetRepository())
            {
                Organization[] orgs = ctx.Organizations.ToArray();

                List<UnitMembership> memberships = ctx.Members
                    .Where(f => f.Id == q)
                    .SelectMany(f => f.Memberships)
                    .Where(f => f.Status.IsActive && f.Start < when && (f.Finish == null || f.Finish < when)).ToList();

                int count;
                int newCount = 0;

                // Iterate through the current members.
                // If the worker number is not blank, keep track of it in the list/tables
                // If the number is blank, then retrieve it from the linked org and keep track of where it came from

                do
                {
                    count = newCount;
                    int i = 0;
                    for (i = 0; i < memberships.Count; )
                    {
                        var membership = memberships[i];

                        if (!string.IsNullOrWhiteSpace(membership.WorkerNumber))
                        {
                            if (!designators.ContainsKey(membership.WorkerNumber))
                            {
                                designators.Add(membership.WorkerNumber, new List<Guid>());
                            }

                            designators[membership.WorkerNumber].Add(membership.OrganizationId);
                            lookup.Add(membership.OrganizationId, membership.WorkerNumber);
                            memberships.RemoveAt(i);
                            continue;
                        }
                        else
                        {
                            // Inspect the links from other units and see if any are tagged as assigning this unit's worker numbers
                            Guid? designatingUnit = membership.Organization.LinksFromOrgs
                                .Where(f => (f.LinkType | OrgLinkType.WorkerNumbers) == OrgLinkType.WorkerNumbers)
                                .Select(f => f.FromOrganization.Id)
                                .SingleOrDefault();

                            // If there is an organization that gives worker numbers, see if we've retrieved this member's
                            // number for that organizaiton
                            if (designatingUnit.HasValue && lookup.ContainsKey(designatingUnit.Value))
                            {
                                string designator = lookup[designatingUnit.Value];
                                designators[designator].Add(membership.OrganizationId);
                                lookup.Add(membership.OrganizationId, designator);
                                memberships.RemoveAt(i);
                                continue;
                            }
                        }
                        i++;
                    }

                    newCount = designators.SelectMany(f => f.Value).Count();
                } while (newCount != count);

                return Data(designators.Select(f => new DesignatorsViewModel
                {
                    Designator = f.Key,
                    Issuer = orgs.Single(g => g.Id == f.Value.First()).Name,
                    ForUnits = f.Value.ToArray()
                }).OrderBy(f => f.Issuer).ToArray());
            }
        }
    }
}
