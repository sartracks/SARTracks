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
    using System.Runtime.Serialization;

    public enum TimelineStatus
    {
        Activated,
        Enroute,
        OnScene,
        SignedOut,
        Cleared
    }

    public enum MissionRole
    {
        Responder,
        Base,
        InTown,
        Field,
        OL
    }

    public interface IEventAttendance
    {
        ISarEvent Event { get; }

        SarMember Member { get; set; }
        string TempMemberName { get; set; }
        string TempMemberNumber { get; set; }

        string MemberName { get; }
        string MemberNumber { get; }        
        Guid EffectiveMemberId { get; }

        double? TotalHours { get; set; }
        double? OnSceneHours { get; set; }

        int? Miles { get; set; }
    }

    [DataContract]
    public abstract class EventAttendance<EventType, R> : SarObject, IEventAttendance
        where EventType : SarEvent<R>
        where R : IEventAttendance
    {
        [IgnoreDataMember]
        public EventType Event { get; set; }
        [IgnoreDataMember]
        ISarEvent IEventAttendance.Event { get { return this.Event; } }

        
        public SarMember Member { get; set; }
        [ForeignKey("Member")]
        public Guid? MemberId { get; set; }

        public string TempMemberName { get; set; }
        public string TempMemberNumber { get; set; }
        public Guid? TempMemberId { get; set; }

        [NotMapped]
        [DataMember]
        public string MemberName 
        {
            get
            {
                return this.TempMemberName ?? ((this.Member == null) ? "unknown" : this.Member.ReverseName);
            }
            protected set
            {
                this.TempMemberName = value;
            }
        }
        
        [NotMapped]
        [DataMember]
        public string MemberNumber
        {
            get
            {
                return this.TempMemberNumber ?? ((this.Member == null) ? null : "$TODO: Designator");
            }
            protected set
            {
            }
        }

        [NotMapped]
        [DataMember]
        public Guid EffectiveMemberId
        {
            get
            {
                return (this.MemberId.HasValue) ? this.MemberId.Value : (TempMemberId.HasValue ? TempMemberId.Value : Guid.Empty);
            }
            protected set
            {
            }
        }

        [NotMapped]
        [DataMember]
        public bool IsTemporary
        {
            get
            {
                return (this.Member == null) ? true : false;
            }
            protected set
            {
            }
        }

        [DataMember]
        public double? TotalHours { get; set; }
        [DataMember]
        public double? OnSceneHours { get; set; }
        [DataMember]
        public int? Miles { get; set; }
    }

    [DataContract]
    public class MissionAttendance : EventAttendance<Mission, MissionAttendance>
    {
        [DataMember]
        public MissionRole Role { get; set; }

        [IgnoreDataMember]
        public SarUnit Unit { get; set; }
        public string TempUnitName { get; set; }

        [DataMember]
        [NotMapped]
        public string UnitName
        {
            get
            {
                return this.TempMemberName ?? ((this.Unit == null) ? null : this.Unit.Name);
            }
            set
            {
            }
        }

        [NotMapped]
        [DataMember]
        public Guid UnitId
        {
            get
            {
                return (this.Unit == null) ? this.Id : this.Unit.Id;
            }
            protected set
            {
            }
        }
    }

    public class TrainingAttendance : EventAttendance<Training, TrainingAttendance>
    {
        public ICollection<TrainingRecord> Records { get; set; }
    }

    public abstract class TimelineEntry<AttendanceType, T> : SarObject
        where AttendanceType : EventAttendance<T, AttendanceType>
        where T : SarEvent<AttendanceType>
    {
        // public Something Responder { get; set; }
        public DateTime Time { get; set; }
        public AttendanceType Event { get; set; }
        public TimelineStatus State { get; set; }
    }

    public class MissionTimelineEntry : TimelineEntry<MissionAttendance, Mission>
    {

    }

    public class TrainingTimelineEntry : TimelineEntry<TrainingAttendance, Training>
    {

    }
}
