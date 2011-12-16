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


    public class UnitsController : ControllerBase
    {
        //
        // GET: /Units/

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public ActionResult Roster(Guid q)
        {
            if (!Permissions.CanViewOrganization(q)) return GetLoginRedirect();
            //var orgs = GetUsersDatabaseOrgs(this.AccountMetadata.LinkedMember, this.Account.UserName);
            //if (!orgs.Any(f => f.Key == q)) return GetLoginRedirect();

            Organization model;
            using (var ctx = GetRepository())
            {
                model = ctx.Organizations.Single(f => f.Id == q);
            }
            ViewData["canAddUser"] = ((NestedRoleProvider)Roles.Provider).IsUserInRole(this.Account.UserName, string.Format("org{0}.admins", q), true);

            return View(model);
        }

        [HttpGet]
        public ActionResult RosterManagement(Guid q)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginError();

            //NewUnitMemberViewModel model = new NewUnitMemberViewModel
            //{
            //    Member = new SarMember(),
            //    Membership = new UnitMembership { Status = new UnitStatusType() }
            //};

            UnitMembership model = new UnitMembership
            {
                Member = new SarMember(),
            };

            using (var ctx = GetRepository())
            {
                var org = ctx.Organizations.IncludePaths("UnitStatusTypes").Single(f => f.Id == q);
                model.Organization = org;
                model.OrganizationId = org.Id;
                model.Status = org.UnitStatusTypes.First();
                //model.Membership.Member = model.Member;
            }

            return PartialView(model);
        }

        public enum RosterImportColumn
        {
            MemberId,
            LastName,
            FirstName,
            Designator,
            Gender,
            BirthDate,
            Street,
            City,
            State,
            ZIP,
            HomePhone,
            CellPhone,
            WorkPhone,
            Pager,
            Email,
            DateJoined,
            Status
        }

        [HttpGet]
        public ActionResult ImportRoster(Guid q)
        {
            if (!Permissions.CanAdminOrganization(q)) return GetLoginRedirect();

            Organization org;
            using (var ctx = GetRepository())
            {
                org = ctx.Organizations.Single(f => f.Id == q);
            }
            ViewData["columnNames"] = Enum.GetNames(typeof(RosterImportColumn));

            return View(org);
        }

        [HttpPost]
        public ActionResult ImportRoster(Guid q, FormCollection fields)
        {
            // Provide a common way to abort this method while providing useful messages to the user
            Func<string, ActionResult> fail = x => { ViewData["error"] = x; return ImportRoster(q); };

            // Permission check
            if (!Permissions.CanAdminOrganization(q)) return GetLoginRedirect();

            // Basic validation of file
            if (Request.Files.Count != 1)
            {
                throw new InvalidOperationException("Can only submit one roster");
            }
            
            Organization org;
            UnitStatusType defaultStatus;
            using (var ctx = this.GetRepository())
            {
                org = ctx.Organizations.IncludePaths("UnitStatusTypes").Single(f => f.Id == q);
                defaultStatus = org.UnitStatusTypes.First(f => f.IsActive && f.IsMissionQualified);
            }

            var postedFile = Request.Files[0];

            byte[] fileData = new byte[postedFile.ContentLength];

            // Read file as Excel spreadsheet
            ExcelFileType fileType;
            if (!Enum.TryParse<ExcelFileType>(System.IO.Path.GetExtension(postedFile.FileName).TrimStart('.'), true, out fileType))
            {
                return fail("Filename does not end in one of: ." + string.Join(", .", Enum.GetNames(typeof(ExcelFileType))));
            }

            ExcelFile file;
            try
            {
                file = ExcelService.Read(postedFile.InputStream, fileType);                
            }
            catch
            {
                return fail(string.Format("Error reading {0} file \"{1}\"", fileType, postedFile.FileName));
            }

            ExcelSheet sheet = file.GetSheet(0);

            // Figure out the order of the columns, and which ones were included...
            Dictionary<RosterImportColumn, int> columnLookup = new Dictionary<RosterImportColumn, int>();

            // Figure out which columns the user included in the spreadsheet
            int col = -1;
            do
            {
                col++;               
                string header = sheet.CellAt(0, col).StringValue;
                if (string.IsNullOrWhiteSpace(header))
                {
                    break;
                }

                RosterImportColumn column;
                if (Enum.TryParse<RosterImportColumn>(header, true, out column))
                {
                    if (columnLookup.ContainsKey(column))
                    {
                        return fail(string.Format("Found multiple '{0}' columns", column));
                    }
                    columnLookup.Add(column, col);
                }
            } while (col < 100);

            // Make sure the required columns are included
            if (!columnLookup.ContainsKey(RosterImportColumn.LastName))
            {
                return fail(string.Format("File does not contain required column '{0}'", RosterImportColumn.LastName));
            }

            int row = 0;
            string externalKeySource = string.Format("roster{0}", q);
            List<string> errors = new List<string>();

            // ### Start Processing Rows
            do
            {
                row++;

                // Get the lastname. Lastname is required - a column without indicates the end of the data
                string lastname = sheet.CellAt(row, columnLookup[RosterImportColumn.LastName]).StringValue;
                if (string.IsNullOrWhiteSpace(lastname))
                {
                    break;
                }
                
                using (var ctx = GetRepository())
                {
                    ctx.Organizations.Attach(org);

                    // If the spreadsheet supplies an external key, look the member up in the database.
                    string key = columnLookup.ContainsKey(RosterImportColumn.MemberId) ?
                        sheet.CellAt(row, columnLookup[RosterImportColumn.MemberId]).StringValue :
                        null;
                    
                    SarMember member = null;
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        var query = from x in
                                        (from f in ctx.ExternalReferences where f.Source == externalKeySource && f.ExternalKey == key select f)
                                    join m in ctx.Members.IncludePaths("Addresses", "ContactInfo") on x.InternalKey equals m.Id into mbrs
                                    select mbrs;

                        member = query.SelectMany(f => f).FirstOrDefault();
                    }
                    
                    // If the member was not found by external key, add it to this database.
                    if (member == null)
                    {
                        member = new SarMember();
                        ctx.Members.Add(member);

                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            ctx.ExternalReferences.Add(new ExternalReference {
                                Source = externalKeySource,
                                ExternalKey = key,
                                InternalKey = member.Id
                            });
                        }                        
                    }

                    // Some data is updated all the time.
                    // First and Last names are.
                    member.LastName = lastname;
                    
                    member.FirstName = columnLookup.ContainsKey(RosterImportColumn.FirstName) ?
                        sheet.CellAt(row, columnLookup[RosterImportColumn.FirstName]).StringValue :
                        null;
                
                    // Birthdate is updated if the column exists.
                    // I think this is formatted differently than the FirstName because it was using more
                    // than one statement. Should be able to change it back.
                    if (columnLookup.ContainsKey(RosterImportColumn.BirthDate))
                    {
                        member.BirthDate = sheet.CellAt(row, columnLookup[RosterImportColumn.BirthDate]).DateValue;
                    }

                    // Gender if the column exists
                    if (columnLookup.ContainsKey(RosterImportColumn.Gender))
                    {
                        Gender gender;
                        string attempt = sheet.CellAt(row, columnLookup[RosterImportColumn.Gender]).StringValue;
                        if (!Enum.TryParse<Gender>(attempt, true, out gender))
                        {
                            errors.Add(string.Format("Row {0}: Can't understand gender '{1}'", row + 1, attempt));
                            continue;
                        }
                        member.Gender = gender;
                    }

                    DateTimeOffset joined = DataStoreService.DATE_UNKNOWN;
                    if (columnLookup.ContainsKey(RosterImportColumn.DateJoined))
                    {
                        string dateString = sheet.CellAt(row, columnLookup[RosterImportColumn.DateJoined]).StringValue;
                        DateTime rowDateTime;
                        if (DateTime.TryParse(dateString, out rowDateTime))
                        {
                            joined = TimeZoneInfo.ConvertTimeToUtc(rowDateTime, TimeZoneInfo.FindSystemTimeZoneById(org.TimeZone));
                        }
                    }

                    UnitStatusType status = defaultStatus;
                    if (columnLookup.ContainsKey(RosterImportColumn.Status))
                    {
                        status = org.UnitStatusTypes.Single(
                            f => f.Name.Equals(sheet.CellAt(row, columnLookup[RosterImportColumn.Status]).StringValue, StringComparison.OrdinalIgnoreCase));                                                
                    }

                    string cardNumber = null;
                    if (columnLookup.ContainsKey(RosterImportColumn.Designator))
                    {
                        cardNumber = sheet.CellAt(row, columnLookup[RosterImportColumn.Designator]).StringValue;
                    }

                    UnitMembership um = new UnitMembership
                    {
                        Member = member,
                        OrganizationId = org.Id,
                        Start = joined.UtcDateTime,
                        Designator = cardNumber,
                        Status = status,
                        Organization = org
                    };

                    member.Memberships.Add(um);
                    member.OldMemberships.Add(um);

                    if (columnLookup.ContainsKey(RosterImportColumn.Street) && columnLookup.ContainsKey(RosterImportColumn.City)
                        && columnLookup.ContainsKey(RosterImportColumn.State) && columnLookup.ContainsKey(RosterImportColumn.ZIP))
                    {
                        Address address = new Address
                        {
                            Street = sheet.CellAt(row, columnLookup[RosterImportColumn.Street]).StringValue,
                            City = sheet.CellAt(row, columnLookup[RosterImportColumn.City]).StringValue,
                            State = sheet.CellAt(row, columnLookup[RosterImportColumn.State]).StringValue,
                            Zip = sheet.CellAt(row, columnLookup[RosterImportColumn.ZIP]).StringValue
                        };

                        if (!(string.IsNullOrWhiteSpace(address.Street) || string.IsNullOrWhiteSpace(address.City) ||
                            string.IsNullOrWhiteSpace(address.State) || string.IsNullOrWhiteSpace(address.Zip))
                            &&
                            !member.Addresses.Any(f => f.Address.Street == address.Street && f.Address.City == address.City &&
                            f.Address.State == address.State && f.Address.Zip == address.Zip))
                        {
                            member.Addresses.Add(new MemberAddress
                            {
                                Member = member,
                                Type = MemberAddressType.Mailing,
                                Address = address
                            });                            
                        }
                    }

                    foreach (var column in new[] { RosterImportColumn.HomePhone, RosterImportColumn.WorkPhone, RosterImportColumn.CellPhone, RosterImportColumn.Pager })
                    {
                        if (columnLookup.ContainsKey(column))
                        {
                            string phone = sheet.CellAt(row, columnLookup[column]).StringValue;
                            if (!string.IsNullOrWhiteSpace(phone) &&
                                !member.ContactInfo.Any(f => f.Value == phone))
                            {
                                member.ContactInfo.Add(new MemberContact
                                {
                                    Member = member,
                                    Type = ContactType.Phone,
                                    SubType = column.ToString().ToLowerInvariant().Replace("phone", ""),
                                    Value = phone
                                });
                            }
                        }
                    }

                    if (columnLookup.ContainsKey(RosterImportColumn.Email))
                    {
                        string email = sheet.CellAt(row, columnLookup[RosterImportColumn.Email]).StringValue;
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            member.ContactInfo.Add(new MemberContact
                            {
                                Member = member,
                                Type = ContactType.Email,
                                Value = email
                            });
                        }
                    }

                    ctx.SaveChanges();
                } 
            } while (row < 1002); // Only allow 1000 rows per file

            if (errors.Count > 0)
            {
                throw new NotImplementedException("Should send email with errors: " + string.Join("::", errors.ToArray()));
            }

            file.Dispose();
            return RedirectToAction("ImportRosterResults", new { q = q, success = row - errors.Count, errors = errors.Count });
        }

        [HttpGet]
        public ActionResult ImportRosterResults(Guid q, int success, int errors)
        {            
            ViewData["rows"] = success;
            ViewData["errors"] = errors;
            return View();
        }


        [HttpPost]
        public DataActionResult SubmitUnitMembership(UnitMembership model)
        {
            // Values that must be set on entering:
            //  model.OrganizationId
            //  model.Member.Id
            //  model.Status.Id

            if (model.Member == null)
            {
                throw new ArgumentNullException("Member");
            }

            if (model.Status == null)
            {
                throw new ArgumentNullException("Status");
            }

            List<SubmitError> errors = new List<SubmitError>();
            if (!Permissions.IsUser) return GetLoginError();
            var orgs = GetUsersDatabaseOrgs(this.AccountMetadata.LinkedMember, this.Account.UserName);
            if (!orgs.Any(f => f.Key == model.OrganizationId)) return GetLoginError();

            SarMember m = model.Member;

            ModelState.Remove("Status.Organization");
            ModelState.Remove("Status.Name");
            ModelState.Remove("Organization");

            using (var ctx = GetRepository())
            {
                var org = ctx.Organizations.Where(f => f.Id == model.OrganizationId).FirstOrDefault();
                if (org == null)
                {
                    throw new NotImplementedException();
                }
                model.Organization = org;

                var member = ctx.Members.IncludePaths("OldMemberships", "Memberships").FirstOrDefault(f => f.Id == model.Member.Id);
                if (member == null)
                {
                    ctx.Members.Add(model.Member);
                    //member = model.Member;
                }
                else
                {
                    member.CopyFrom(model.Member);
                    model.Member = member;
                }


                var status = ctx.Organizations.Where(f => f.Id == org.Id).SelectMany(f => f.UnitStatusTypes).SingleOrDefault(f => f.Id == model.Status.Id);
                if (status == null)
                {
                    throw new NotImplementedException();
                }
                status.Organization = org;
                model.Status = status;

                var working = ctx.Organizations.Where(f => f.Id == model.OrganizationId).SelectMany(f => f.Memberships).SingleOrDefault(f => f.Id == model.Id);
                if (working == null)
                {
                    org.Memberships.Add(model);
                    working = model;
                }
                else
                {
                    working.CopyFrom(model);
                }

                //working.Member.Memberships.Add(working);
                bool addMembership = true;
                //foreach (var oldMembership in working.Member.OldMemberships)
                //{
                //    if (oldMembership.OrganizationId == org.Id && oldMembership.Finish == null && oldMembership.Start < working.Start)
                //    {
                //        oldMembership.Finish = working.Start;
                //        System.Diagnostics.Debug.Assert(oldMembership == working.Member.Memberships.Single(f => f.Id == oldMembership.Id));
                //        working.Member.Memberships.Remove(oldMembership);
                //    }
                //}
                if (addMembership) working.Member.Memberships.Add(working);
                working.Member.OldMemberships.Add(working);

                ValidationErrorsToModelState(ctx.GetValidationErrors());

                if (ModelState.IsValid)
                {
                    ctx.SaveChanges();
                }
                else
                {
                    ModelStateToSubmitErrors(errors);
                }
            }

            return Data(new SubmitResult<UnitMembership> { Errors = errors.ToArray(), Result = model });
        }

        [HttpPost]
        public DataActionResult GetRosterList(Guid q)
        {
            if (!Permissions.CanViewOrganization(q)) return GetLoginError();

            RosterViewModel[] model;
            using (var ctx = GetRepository())
            {
                DateTime now = DateTime.UtcNow;
                model = ctx.Members.SelectMany(f => f.Memberships)
                    .Where(g => g.OrganizationId == q && g.Start < now && (g.Finish == null || g.Finish > now))
                    .OrderBy(f => f.Member.LastName).ThenBy(f => f.Member.FirstName).Select(f =>
                new RosterViewModel
                {
                    Id = f.Member.Id,
                    FirstName = f.Member.FirstName,
                    LastName = f.Member.LastName,
                    Designator = f.Designator,
                    Status = f.Status.Name,
                    Email = f.Member.ContactInfo.Where(g => g.Type == ContactType.Email).Select(g => g.Value).FirstOrDefault(),
                    Phone = f.Member.ContactInfo.Where(g => g.Type == ContactType.Phone).Select(g => g.Value).FirstOrDefault()
                }).ToArray();
            }
            return Data(model);
        }
    }
}
