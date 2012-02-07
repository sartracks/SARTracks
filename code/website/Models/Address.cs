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
    using System.Runtime.Serialization;
    using System.Data.Spatial;
    using System.ComponentModel.DataAnnotations;

    [DataContract]
    public class Address
    {
        [DataMember]
        public string Street { get; set; }

        [DataMember]
        public string City { get; set; }
        
        [DataMember]
        public string State { get; set; }

        [DataMember]
        public string Zip { get; set; }

        public DbGeography Location { get; set; }

        [DataMember]
        public double? Latitude { get { return (this.Location != null) ? this.Location.Latitude : null; } protected set { } }

        [DataMember]
        public double? Longitude { get { return (this.Location != null) ? this.Location.Longitude : null; } protected set { } }

        public void CopyFrom(Address other)
        {
            this.Street = other.Street;
            this.City = other.City;
            this.State = other.State;
            this.Zip = other.Zip;
            this.Location = other.Location;
        }
    }
}