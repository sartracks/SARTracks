
namespace SarTracks.Website.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.ComponentModel.DataAnnotations;

    public class MissionUnitResponse : SarObject
    {
        [ForeignKey("Mission")]
        public Guid MissionId { get; set; }
        public Mission Mission { get; set; }

        [ForeignKey("Unit")]
        public Guid UnitId { get; set; }
        public SarUnit Unit { get; set; }

        public DateTime? Notified { get; set; }
        public DateTime? Activated { get; set; }
        public DateTime? OnScene { get; set; }
        public DateTime? Demobilized { get; set; }
    }
}