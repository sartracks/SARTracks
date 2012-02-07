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
namespace SarTracks.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using SarTracks.Website.Models;
    using SarTracks.Website.ViewModels;
    using SarTracks.Website.Services;
    using System.Runtime.Serialization;

    public abstract class SarEventController<E,R,T> : ControllerBase where E : SarEvent<R, T> where R : IEventAttendance where T : ITimelineEntry
    {
        [HttpGet]
        public ActionResult List(string units)
        {
            EventFilter filter = new EventFilter();
            if (!string.IsNullOrWhiteSpace(units))
            {
                filter.Units = units.Split(',').Select(f => Guid.Parse(f.Trim())).ToArray();
            }

            ViewData["filter"] = new JsonDataContractResult(filter).GetJsonString();
            ViewData["canAdd"] = UserCanAdd(filter);
            return View("EventList", typeof(E));
        }

        protected abstract IQueryable<E> GetEventSummaries(IDataStoreService context);

        protected abstract bool UserCanAdd(EventFilter currentFilter);

        protected virtual IQueryable<E> ApplyFilter(IQueryable<E> list, EventFilter filter)
        {
            if (filter.Begin.HasValue)
            {
                list = list.Where(f => f.Start >= filter.Begin.Value);
            }
            if (filter.End.HasValue)
            {
                list = list.Where(f => f.Start < filter.End.Value);
            }
            return list;
        }

        [HttpPost]
        public DataActionResult GetList(EventFilter filter)
        {
            EventSummaryView[] model;
            using (var ctx = GetRepository())
            {
                var query = GetEventSummaries(ctx).IncludePaths("Roster");

                query = ApplyFilter(query, filter);

                model = query.OrderByDescending(f => f.Start).ToArray().Select(f =>
                    new EventSummaryView
                    {
                        Id = f.Id,
                        Number = f.StateNumber,
                        Title = f.Title,
                        StartTime = f.Start,
                        Persons = f.Roster.Select(g => g.EffectiveMemberId).Distinct().Count(),
                        Hours = f.Roster.Sum(g => g.TotalHours),
                        Miles = f.Roster.Sum(g => g.Miles)
                    }).ToArray();
            }

            return Data(model);
        }

        [DataContract]
        public class TimeRangeFilter
        {
            [DataMember(EmitDefaultValue = false)]
            public DateTime? Begin { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public DateTime? End { get; set; }
        }

        public class EventFilter : TimeRangeFilter
        {
            [DataMember]
            public Guid[] Units { get; set; }
        }
    }
}
