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
namespace SarTracks.Tests.Website
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using SarTracks.Website.Models;
    using SarTracks.Website.Services;

    public class DevWebsiteDataInitializer : DropCreateDatabaseIfModelChanges<DataStoreService>
    {
        protected override void Seed(DataStoreService context)
        {
            DevWebsiteDataInitializer.FillDefaultDevSet(context);
        }

        public static IDataStoreService FillDefaultDevSet(IDataStoreService context)
        {
            try
            {
                var kcso = new Organization { LongName = "King County Sheriff", Name = "KCSO" };
                var kcsara = new Organization { LongName = "King County Search and Rescue Association", Name = "KCSARA" };
                var snoco = new Organization { LongName = "Snohomish County Search and Rescue", Name = "SnoSAR" };
                var smr = new SarUnit { LongName = "Seattle Mountain Rescue", Name = "SMR", DesignatorsFrom = kcso };
                var esar = new SarUnit { LongName = "King County Explorer Search and Rescue", Name = "ESAR", DesignatorsFrom = kcso };
                var emr = new SarUnit { LongName = "Everett Mountain Rescue", Name = "EMRU", DesignatorsFrom = snoco };

                new List<Organization> { 
                kcso,
                kcsara,
                snoco,
                smr,
                esar,
                emr,
                new Organization { LongName = "Umbrella Organization", Name = "Parent", TimeZone = "Pacific Standard Time" }
                }.ForEach(org =>
                {
                    org.IsApproved = true;
                    org.TimeZone = org.TimeZone ?? "Pacific Standard Time";
                    context.Organizations.Add(org);
                    var status = new List<UnitStatusType> {
                        new UnitStatusType { Organization = org, Name = "Active", IsActive = true, IsMissionQualified = true },
                        new UnitStatusType { Organization = org, Name = "Inactive", IsActive = false, IsMissionQualified = false }
                    };

                    if (org is SarUnit)
                    {
                        status.Add(new UnitStatusType { /*Organization = org,*/ Name = "Trainee", IsActive = true, IsMissionQualified = false });
                    }
                    status.ForEach(m => org.UnitStatusTypes.Add(m));
                });

                var robert = new SarMember { FirstName = "Robert", LastName = "Redford", BirthDate = new DateTime(1955, 5, 24) };
                var steve = new SarMember { FirstName = "Steve", LastName = "Marten" };
                var clint = new SarMember { FirstName = "Clint", LastName = "Eastwood" };
                var natalie = new SarMember { FirstName = "Natalie", LastName = "Portman" };

                var members = new List<SarMember> {
                    robert,
                    steve,
                    clint,
                    natalie
                };
                members.ForEach(m => context.Members.Add(m));

                new List<UnitMembership> {
                   new UnitMembership { Start = new DateTime(1995,9,1), Designator="1234", Member = robert, Organization = kcso, Status = kcso.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(2002,3,1), Designator="7234", Member = steve, Organization = kcso, Status = kcso.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(2010,09,23), Designator ="5234", Member = clint, Organization = kcso, Status = kcso.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(1996,9,1), Designator = "8234", Member = natalie, Organization = kcso, Status = kcso.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(2005,3,1), Designator="Sno 54-3", Member = clint, Organization = snoco, Status = snoco.UnitStatusTypes.Single(f => f.Name == "Active") },
                                    
                   new UnitMembership { Start = new DateTime(1996,3,1), Designator="1234", Member = robert, Organization = esar, Status = esar.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(1997,3,1), Designator="8234", Member = natalie, Organization = esar, Status = esar.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(2002,3,1), Designator="7234", Member = steve, Organization = esar, Status = esar.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(2010,3,1), Designator="5234", Member = clint, Organization = esar, Status = esar.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(2007,3,1), Designator="8234", Member = natalie, Organization = smr, Status = smr.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(2007,3,1), Designator="7234", Member = steve, Organization = smr, Status = smr.UnitStatusTypes.Single(f => f.Name == "Active") },
                   new UnitMembership { Start = new DateTime(2000,3,1), Designator="Sno 54-3", Member = clint, Organization = emr, Status = emr.UnitStatusTypes.Single(f => f.Name == "Active") },
                }.ForEach(m => members.Single(f => f.Id == m.Member.Id).Memberships.Add(m));

                new List<MemberAddress> {
                    new MemberAddress { Type = MemberAddressType.Residence, Address = new Address { Street = "7220 NE 181st St", City = "Kenmore", State = "WA", Zip = "98028" }, Member = robert },
                    new MemberAddress { Type = MemberAddressType.Mailing, Address = new Address { Street = "17301 133rd Avenue NE", City = "Woodinville", State = "WA", Zip = "98072" }, Member = robert }
                }.ForEach(m => robert.Addresses.Add(m));

                new List<MemberContact> {
                    new MemberContact { Type = ContactType.Phone, SubType = "Home", Value = "206-555-1234", Priority = 1, Member = robert },
                    new MemberContact { Type = ContactType.Phone, SubType = "Cell", Value = "206-555-7234", Priority = 0, Member = robert },
                    new MemberContact { Type = ContactType.Email, Value = "main@test.local", Priority = 0, Member = robert },
                    new MemberContact { Type = ContactType.Email, Value = "other@example.local", Priority = 1, Member = robert },
                    new MemberContact { Type = ContactType.Radio, SubType = "HAM", Value = "A7BCD", Priority = 0, Member = robert }
                }.ForEach(m => robert.ContactInfo.Add(m));

                var missions = new List<Mission> {
                    new Mission { StateNumber ="11-0542", Title = "Test Mission 1", Location = "Granite Mountain", Start = DateTime.Now.AddDays(-5), Finish = DateTime.Now.AddDays(-3), Type = MissionType.Search },
                    new Mission { StateNumber = "11-0849", Title = "Test Mission 2", Location = "Mailbox Peak", Start = DateTime.Now.AddDays(-3), Finish = DateTime.Now.AddDays(-3).AddHours(4), Type = MissionType.Rescue }
                };
                missions.ForEach(m => context.Missions.Add(m));

                var missionRosters = new List<MissionAttendance> {
                    new MissionAttendance { Event = missions[0], Member = robert, Role = MissionRole.Field, TotalHours = 24.3, Miles = 80, Unit = esar },
                    new MissionAttendance { Event = missions[0], Member = natalie, Role = MissionRole.Field, TotalHours = 22, Unit = smr },
                    new MissionAttendance { Event = missions[0], Member = steve, Role = MissionRole.Field, TotalHours = 22.75, Unit = smr },
                    new MissionAttendance { Event = missions[0], Member = clint, Role = MissionRole.Field, TotalHours = 18, Unit = esar },
                    new MissionAttendance { Event = missions[1], Member = robert, Role = MissionRole.OL, TotalHours = 3, Miles = 20, Unit = esar },
                };
                missionRosters.ForEach(m => m.Event.Roster.Add(m));

                context.SaveChanges();
                
                return context;
            }
            catch (Exception)
            {
                // set breakpoint
                throw;
            }
        }
    }
}
