﻿/* Copyright 2011 Matt Cosand and others (see AUTHORS.TXT)
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
namespace SarTracks.Website.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Runtime.Serialization;
    using SarTracks.Website.Models;

    [DataContract]
    public class EventSummaryView
    {
        [DataMember]
        public Guid Id { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Number { get; set; }
        [DataMember]
        public DateTime StartTime { get; set; }


        // Optionally populated
        [DataMember(EmitDefaultValue=false)]
        public bool IsActive { get; set; }
        
        [DataMember(EmitDefaultValue = false)]
        public int Persons { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public double? Hours { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public int? Miles { get; set; }

        public EventSummaryView() { }
    }
}
