using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SarTracks.Website.Models
{
    public class LogEntry : SarObject
    {
        public string Type { get; set; }
        public string Action { get; set; }
        public string Message { get; set; }
        public Guid ReferenceId { get; set; }
    }
}