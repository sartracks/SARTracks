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
namespace SarTracks.Website.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Data.Spatial;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    [DataContract]
    [KnownType(typeof(SarUnit))]
    public class Organization : SarObject
    {
        public Organization() : base()
        {
            this.MailingAddress = new Address();
            this.JoinKey = Guid.NewGuid();
            this.UnitStatusTypes = new List<UnitStatusType>();
            this.Memberships = new List<UnitMembership>();
        }

        [Required]
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string LongName { get; set; }

        [DataMember]
        public bool IsApproved { get; set; }

        [DataMember]
        public Guid JoinKey { get; set; }

        [DataMember]
        public string AdminAccount { get; set; }

        [DataMember]
        public Address MailingAddress { get; set; }

        public WellKnownPlace PrimaryCoverage { get; set; }
        public string PrimaryCoverageName { get; set; }
        public DbGeography PrimaryCoverageGeo { get; set; }

        public WellKnownPlace MouCoverage { get; set; }
        public string MouCoverageName { get; set; }
        public DbGeography MouCoverageGeo { get; set; }

        [Required]
        public string TimeZone { get; set; }
        
        public ICollection<UnitStatusType> UnitStatusTypes { get; set; }

        public ICollection<UnitMembership> Memberships { get; set; }

        [ForeignKey("DesignatorsFrom")]
        public Guid? DesignatorsFromId { get; set; }
        public Organization DesignatorsFrom { get; set; }
    }
}