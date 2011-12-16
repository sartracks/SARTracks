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
    using System.Text;
    using System.ComponentModel.DataAnnotations;

    public interface ISarEvent
    {
        Guid Id { get; set; }
        string Title { get; set; }
        string StateNumber { get; set; }
        DateTime Start { get; set; }
        DateTime? Finish { get; set; }
        string Location { get; set; }

        IEnumerable<IEventAttendance> Roster { get; }
    }

    public abstract class SarEvent<R> : SarObject, IValidatableObject, ISarEvent where R : IEventAttendance
    {
        public SarEvent()
        {
            this.Roster = new List<R>();
        }

        /// <summary></summary>
        [Required]
        public string Title { get; set; }
        
        /// <summary></summary>
        [Column("state_num")]
        public string StateNumber { get; set; }

        /// <summary></summary>
        [Required]
        [Column("start_time")]
        public DateTime Start { get; set; }
        
        /// <summary></summary>
        [Column("stop_time")]
        public DateTime? Finish { get; set; }
        
        /// <summary></summary>
        [Required]
        public string Location { get; set; }
        
        public ICollection<R> Roster { get; set; }

        IEnumerable<IEventAttendance> ISarEvent.Roster { get { return this.Roster.Cast<IEventAttendance>(); } }
        
        /// <summary></summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Finish.HasValue && Finish <= Start)
            {
                yield return new ValidationResult("Finish must be after Start", new[] { "Start", "Finish" });
            }
        }
    }
}
