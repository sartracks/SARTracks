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
    using System.Linq;
    using System.Web.Mvc;
    using SarTracks.Website.Models;
    using SarTracks.Website.Services;
    using System.Collections.Generic;

    public class MissionsController : SarEventController<Mission, MissionAttendance, MissionTimelineEntry>
    {
//        //
//        // GET: /Missions/

        protected override IQueryable<Mission> GetEventSummaries(IDataStoreService context)
        {
            return context.Missions;
        }

        protected override IQueryable<Mission> ApplyFilter(IQueryable<Mission> list, EventFilter filter)
        {
            list = base.ApplyFilter(list, filter);
            if (filter.Units != null && filter.Units.Length > 0)
            {
                list = list.Where(f => f.RespondingUnits.Any(g => filter.Units.Contains(g.Id)));
            }
            return list;
        }

        protected override bool UserCanAdd(EventFilter currentFilter)
        {
            bool hasPermission = false;

            if (currentFilter != null && currentFilter.Units != null)
            {
                hasPermission = currentFilter.Units.Any(f => Permissions.HasPermission(PermissionType.AddUnitMission, f));
            }

            return hasPermission;
        }

        [HttpGet]
        public ActionResult Roster(Guid q)
        {
            Mission model = null;
            using (var context = GetRepository())
            {
                model = context.Missions.Single(f => f.Id == q);
            }
            return View(model);
        }

        [HttpPost]
        public DataActionResult GetRoster(Guid q)
        {
            IEnumerable<MissionAttendance> model;
            using (var context = GetRepository())
            {
                model = context.Missions.IncludePaths("Roster.Member", "Roster.Unit").Single(f => f.Id == q)
                    .Roster.OrderBy(f => f.UnitName).ThenBy(f => f.MemberName).ToArray();
            }

            return Data(model);
        }

        [HttpPost]
        public DataActionResult GetTimeline(Guid q)
        {
            IEnumerable<MissionTimelineEntry> model;
            using (var context = GetRepository())
            {
                model = context.Missions.Single(f => f.Id == q)
                    .Timeline.OrderBy(f => f.Attendance.EffectiveMemberId).ThenBy(f => f.Time).ToArray();
            }
            return Data(model);
        }
    }
}
