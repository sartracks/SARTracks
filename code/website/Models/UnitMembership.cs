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
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    [DataContract]
    public class UnitMembership : SarObject
    {
        [Required]
        public SarMember Member { get; set; }
        
        [Required]
        [DataMember]
        public Organization Organization { get; set; }

        [ForeignKey("Organization")]
        public Guid OrganizationId { get; set; }

        [DataMember]
        public DateTime? Start { get; set; }       

        [DataMember]
        public DateTime? Finish { get; set; }
        
        [DataMember]
        public string WorkerNumber { get; set; }
        
        [Required]
        [DataMember]
        public UnitStatusType Status { get; set; }

        public override void CopyFrom(SarObject right)
        {
            UnitMembership r = (UnitMembership)right;
            base.CopyFrom(right);
            this.OrganizationId = r.OrganizationId;
            this.Organization = r.Organization;
            this.Start = r.Start;
            this.Finish = r.Finish;
            this.Member = r.Member;
            this.WorkerNumber = r.WorkerNumber;
            this.Status = r.Status;
        }
    }
}