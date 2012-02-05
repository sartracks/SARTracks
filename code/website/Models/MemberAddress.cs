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
    public class MemberAddress : SarObject
    {
        public MemberAddress()
            : base()
        {
            this.Address = new Models.Address();
        }

        [Required]
        [ForeignKey("MemberId")]
        public SarMember Member { get; set; }
        [DataMember]
        public Guid MemberId { get; set; }

        [DataMember]
        public MemberAddressType Type { get; set; }

        [DataMember]
        public Address Address { get; set; }

        public void CopyFrom(MemberAddress other)
        {
            this.Member = other.Member;
            this.MemberId = (other.Member == null) ? other.MemberId : other.Member.Id;
            this.Type = other.Type;
            this.Address.CopyFrom(other.Address);
        }
    }
}