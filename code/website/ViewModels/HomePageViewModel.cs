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
namespace SarTracks.Website.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public class HomePageViewModel
    {
        public bool LoggedIn { get; set; }
        public bool HasAccount { get; set; }
        public Guid LinkedMember { get; set; }

        public bool MyDetails { get; set; }
        public bool MyTraining { get; set; }
        public bool MyMissions { get; set; }
        public bool UnitRoster { get; set; }
        public bool UnitTraining { get; set; }
        public bool UnitMissions { get; set; }
        public bool PublicMapping { get; set; }
        public bool PublicReports { get; set; }

        public NameIdPair[] MyUnits { get; set; }
    }
}