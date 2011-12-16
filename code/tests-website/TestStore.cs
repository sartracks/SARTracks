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
    using System.Linq;
    using System.Text;
    using SarTracks.Website.Models;
    using SarTracks.Website.Services;
    using System.Data.Entity;

    public class TestStore : IDataStoreService
    {
        public TestStore()
        {
            this.Members = new InMemoryDbSet<SarMember>();
            this.Organizations = new InMemoryDbSet<Organization>();
            this.Missions = new InMemoryDbSet<Mission>();
            this.TrainingCourses = new InMemoryDbSet<TrainingCourse>();
            this.Trainings = new InMemoryDbSet<Training>();
            this.Log = new InMemoryDbSet<LogEntry>();
            this.ExternalReferences = new InMemoryDbSet<ExternalReference>();
        }

        public IDbSet<SarMember> Members { get; set; }
        public IDbSet<Organization> Organizations { get; set; }
        public IDbSet<Mission> Missions { get; set; }
        public IDbSet<Training> Trainings { get; set; }
        public IDbSet<TrainingCourse> TrainingCourses { get; set; }
        public IDbSet<LogEntry> Log { get; set; }
        public IDbSet<ExternalReference> ExternalReferences { get; set; }

        void IDisposable.Dispose()
        {
        }

        int IDataStoreService.SaveChanges()
        {
            return 0;
        }

        public TestStore FixupReferences()
        {
            foreach (var o in this.Organizations)
            {
                o.DesignatorsFromId = (o.DesignatorsFrom == null) ? (Guid?)null : o.DesignatorsFrom.Id;
                foreach (var m in o.Memberships)
                {
                    m.OrganizationId = o.Id;
                    m.Organization = o;
                }

                foreach (var u in o.UnitStatusTypes)
                {
                    u.Organization = o;
                }
            }

            foreach (var m in this.Members)
            {
                foreach (var um in m.Memberships)
                {
                    um.Member = m;
                    um.OrganizationId = um.Organization.Id;
                }
                foreach (var c in m.ContactInfo)
                {
                    c.MemberId = m.Id;
                }
                foreach (var a in m.Addresses)
                {
                    a.MemberId = m.Id;
                }
            }

            return this;
        }


        public IEnumerable<System.Data.Entity.Validation.DbEntityValidationResult> GetValidationErrors()
        {
            throw new NotImplementedException();
        }
    }
}
