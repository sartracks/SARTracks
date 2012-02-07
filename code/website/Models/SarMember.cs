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
    using System.Drawing;
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    [DataContract]
    public class SarMember : SarObject
    {
        public SarMember()
            : base()
        {
            this.OldMemberships = new List<UnitMembership>();
            this.Memberships = new List<UnitMembership>();
            this.Addresses = new List<MemberAddress>();
            this.ContactInfo = new List<MemberContact>();
        }

        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public DateTime? BirthDate { get; set; }

        [DataMember]
        public Gender Gender { get; set; }

        public byte[] PhotoData { get; set; }
        
        [NotMapped]
        public Image Photo
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ICollection<UnitMembership> Memberships { get; set; }
        public ICollection<UnitMembership> OldMemberships { get; set; }
        public ICollection<MemberAddress> Addresses { get; set; }
        public ICollection<MemberContact> ContactInfo { get; set; }

        public ICollection<User> Accounts { get; set; }

        public string ReverseName { get { return this.LastName + ", " + this.FirstName; } }
        public string FullName { get { return this.FirstName + " " + this.LastName; } }

        public override void CopyFrom(SarObject right)
        {
            base.CopyFrom(right);
            SarMember r = (SarMember)right;

            this.FirstName = r.FirstName;
            this.LastName = r.LastName;
            this.BirthDate = r.BirthDate;
            this.Gender = r.Gender;
            this.PhotoData = r.PhotoData;
            //this.Memberships = r.Memberships;
            //this.OldMemberships = r.OldMemberships;
            //this.Addresses = r.Addresses;
            //this.ContactInfo = r.ContactInfo;
        }
   }
}