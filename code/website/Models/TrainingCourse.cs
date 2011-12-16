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
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text;

    public class TrainingCourse : SarObject
    {
        public string DisplayName { get; set; }

        public ICollection<Training> Trainings { get; set; }
    }

    public class TrainingRecord : SarObject
    {
        public Guid CourseId { get; set; }
        [ForeignKey("CourseId")]
        public TrainingCourse Course { get; set; }

        public TrainingAttendance Attended { get; set; }

        public Guid PersonId { get; set; }
        [ForeignKey("PersonId")]
        public SarMember Member { get; set; }

        public double? CourseHours { get; set; }
    }
}
