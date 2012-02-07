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
    using System.Web.Mvc;
    using SarTracks.Website.Models;
    using SarTracks.Website.ViewModels;
    using SarTracks.Website.Services;

    public class HomeController : ControllerBase
    {
        public HomeController() : base() { }
        public HomeController(AuthIdentityService perms, DataStoreFactory factory) : base(perms, factory) { }

        public ActionResult Index()
        {
            Dictionary<Guid, string> orgs = this.GetUsersDatabaseOrgs();
            bool auth = Permissions.IsAuthenticated;
            bool hasAccount = Permissions.IsUser;
            bool hasMember = Permissions.User != null && Permissions.User.MemberId != null;

            HomePageViewModel model = new HomePageViewModel
            {
                LoggedIn = auth,
                HasAccount = hasAccount,
                LinkedMember = (Permissions.User == null || Permissions.User.MemberId == null) ? Guid.Empty : Permissions.User.MemberId.Value,
                MyDetails = hasMember,
                MyTraining = hasMember,
                MyMissions = hasMember,
                PublicMapping = true,
                PublicReports = true,
                MyUnits = orgs.Select(f => new HomePageUnitViewModel {
                    Id = f.Key, 
                    Name = f.Value,
                    Permissions = string.Format("{0}{1}{2}{3}",
                        Permissions.HasPermission(PermissionType.ViewOrganizationBasic, f.Key) ? "*" : " ",
                        Permissions.HasPermission(PermissionType.ViewOrganizationBasic, f.Key) ? "*" : " ",
                        Permissions.HasPermission(PermissionType.ViewOrganizationBasic, f.Key) ? "*" : " ",
                        Permissions.HasPermission(PermissionType.AdminOrganization, f.Key) ? "*" : " ")
                }).ToArray()
            };

            ViewData["hideMenu"] = true;
            return View(model);
        }

        /// <summary>
        /// code sandbox
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public ActionResult Test(string q)
        {
            string filename = "test";

            ExcelFileType fileType;
            if (!Enum.TryParse<ExcelFileType>(q, true, out fileType)) fileType = ExcelFileType.XLSX;

            ExcelFile file = ExcelService.Create(fileType);

            var sheet = file.CreateSheet("Sheet1");

            sheet.CellAt(0, 4).SetValue(77);

            sheet.CellAt(0, 0).SetValue(sheet.CellAt(0, 1).NumericValue ?? -42);

            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            file.Save(ms);
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            return this.File(ms, file.Mime, file.AddExtension(filename));
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
