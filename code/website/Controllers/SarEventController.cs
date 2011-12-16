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

    public abstract class SarEventController<T,R> : ControllerBase where T : SarEvent<R> where R : IEventAttendance
    {
        //
        // GET: /SarEvent/

        [HttpGet]
        public ActionResult Index()
        {
            return List();
        }

        [HttpGet]
        public ActionResult List()
        {
            return View("EventList", typeof(T));
        }

        protected abstract IQueryable<T> GetEventSummaries(IDataStoreService context);

        [HttpPost]
        public DataActionResult GetList()
        {
            EventSummaryView[] model;
            using (var ctx = GetRepository())
            {
                model = GetEventSummaries(ctx).IncludePaths("Roster").OrderByDescending(f => f.Start).ToArray().Select(f =>
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
    }
}
