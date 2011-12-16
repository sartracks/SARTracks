using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SarTracks.Website
{
    public interface IIdObject
    {
        Guid Id { get; }
    }
}