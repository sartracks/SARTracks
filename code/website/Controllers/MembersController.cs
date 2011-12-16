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

    public class MembersController : ControllerBase
    {
        //
        // GET: /Members/

        public ActionResult Index()
        {
            return View();
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="q">Org ID</param>
        ///// <returns></returns>
        //[HttpGet]
        //public ActionResult AddUnitMember(Guid q)
        //{
        //    var orgs = GetUsersDatabaseOrgs(this.AccountMetadata.LinkedMember, this.Account.UserName);
        //    if (!orgs.Any(f => f.Key == q)) return GetLoginRedirect();

        //    NewUnitMemberViewModel model = new NewUnitMemberViewModel
        //    {
        //        Member = new SarMember(),
        //        Membership = new UnitMembership { Status = new UnitStatusType() }
        //    };

        //    using (var ctx = GetRepository())
        //    {
        //       var org = ctx.Organizations.IncludePaths("UnitStatusTypes").Single(f => f.Id == q);
        //       model.Unit = org;
        //       model.Membership.Organization = org;
        //       model.Membership.OrganizationId = org.Id;
        //       model.Membership.Member = model.Member;
        //    }

        //    return PartialView("NewUnitMemberForm", model);
        //}



        [HttpGet]
        public ActionResult Detail(Guid q)
        {
            if (!Permissions.CanViewMember(q)) return GetLoginRedirect();

            SarMember model;
            using (var ctx = GetRepository())
            {
                model = ctx.Members.Single(f => f.Id == q);
                if (!Permissions.IsInRole("Administrators"))
                {
                    MembersController.FilterPersonalInfo(new[] { model });
                }
            }

            return View(model);
        }

        public static void FilterPersonalInfo(IEnumerable<SarMember> members)
        {            
            foreach (var member in members)
            {
                member.BirthDate = null;
            }
        }

        #region Addresses
        [HttpGet]
        public ActionResult AddAddress(Guid q)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginRedirect();

            MemberAddress model = new MemberAddress();
            return PartialView("AddressForm", model);
        }


        [HttpGet]
        public ActionResult EditAddress(Guid q, Guid e)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginRedirect();
            MemberAddress model;
            using (var ctx = GetRepository())
            {
                model = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.Addresses).Single(f => f.Id == e);
            }
            return PartialView("AddressForm", model);
        }

        [HttpPost]
        public DataActionResult SaveAddress(Guid q, MemberAddress model)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginError();

            SubmitResult<MemberAddress> result = new SubmitResult<MemberAddress>();

            ModelState.Remove("Member");
            if (ModelState.IsValid)
            {
                using (var ctx = GetRepository())
                {
                    var oldModel = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.Addresses).SingleOrDefault(f => f.Id == model.Id);
                    SarMember member = ctx.Members.Single(f => f.Id == q);
                    model.Member = member;
                    model.MemberId = member.Id;

                    if (oldModel == null)
                    {
                        member.Addresses.Add(model);
                    }
                    else
                    {
                        oldModel.CopyFrom(model);
                    }

                    try
                    {
                        ctx.SaveChanges();
                    }
                    catch
                    {
                        // Set breakpoint
                        throw;
                    }
                }
            }
            result.Result = model;
            return Data(result);
        }

        [HttpPost]
        public DataActionResult DeleteAddress(Guid q, Guid e)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginError();

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
            if (!Permissions.CanViewOrganization(q)) return GetLoginError();

            MemberAddress[] model;
            using (var ctx = GetRepository())
            {
                model = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.Addresses)
                    .OrderBy(f => f.Type).ThenBy(f => f.Address.Street)
                    .ToArray();
            }

            return Data(model);
        }
        #endregion

        #region Contact Info
        [HttpGet]
        public ActionResult AddContactInfo(Guid q)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginRedirect();

            MemberContact model = new MemberContact();
            return PartialView("ContactForm", model);
        }


        [HttpGet]
        public ActionResult EditContactInfo(Guid q, Guid contact)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginRedirect();
            MemberContact model;
            using (var ctx = GetRepository())
            {
                model = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.ContactInfo).Single(f => f.Id == contact);
            }
            return PartialView("ContactForm", model);
        }

        [HttpPost]
        public DataActionResult SaveContactInfo(Guid q, MemberContact model)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginError();

            SubmitResult<MemberContact> result = new SubmitResult<MemberContact>();

            ModelState.Remove("Member");
            if (ModelState.IsValid)
            {
                using (var ctx = GetRepository())
                {
                    var oldModel = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.ContactInfo).SingleOrDefault(f => f.Id == model.Id);
                    SarMember member = ctx.Members.Single(f => f.Id == q);
                    model.Member = member;
                    model.MemberId = member.Id;

                    if (oldModel == null)
                    {
                        member.ContactInfo.Add(model);
                    }
                    else
                    {
                        oldModel.CopyFrom(model);
                    }

                    try
                    {
                        ctx.SaveChanges();
                    }
                    catch
                    {
                        // Set breakpoint
                        throw;
                    }
                }
            }
            result.Result = model;
            return Data(result);
        }

        [HttpPost]
        public DataActionResult DeleteContactInfo(Guid q, Guid contact)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginError();
            
            using (var ctx = GetRepository())
            {
                var model = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.ContactInfo).Single(f => f.Id == contact);

                ctx.Delete(model);
                
                ctx.SaveChanges();
            }
            return Data(new SubmitResult<bool> { Result = true });
        }


        [HttpPost]
        public DataActionResult GetContactInfo(Guid q)
        {
            if (!Permissions.CanViewOrganization(q)) return GetLoginError();

            MemberContact[] model;
            using (var ctx = GetRepository())
            {
                model = ctx.Members.Where(f => f.Id == q).SelectMany(f => f.ContactInfo)
                    .OrderBy(f => f.Type).ThenBy(f => f.Priority).ThenBy(f => f.SubType).ThenBy(f => f.Value)
                    .ToArray();
            }

            return Data(model);
        }
        #endregion

        [HttpPost]
        public DataActionResult GetAllMemberships(Guid q)
        {
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
            when = when ?? DateTime.Now;

            Dictionary<string, List<Guid>> designators = new Dictionary<string,List<Guid>>();
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

                do
                {
                    count = newCount;
                    int i = 0;
                    for (i = 0; i < memberships.Count; )
                    {
                        var membership = memberships[i];

                        if (!string.IsNullOrWhiteSpace(membership.Designator))
                        {
                            if (!designators.ContainsKey(membership.Designator))
                            {
                                designators.Add(membership.Designator, new List<Guid>());
                            }

                            designators[membership.Designator].Add(membership.OrganizationId);
                            lookup.Add(membership.OrganizationId, membership.Designator);
                            memberships.RemoveAt(i);
                            continue;
                        }
                        else
                        {
                            Guid? designatingUnit = orgs.Single(f => f.Id == membership.OrganizationId).DesignatorsFromId;
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
