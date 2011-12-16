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

    public class HomeController : ControllerBase
    {
        public ActionResult Index()
        {
            bool auth = User.Identity.IsAuthenticated;
            bool hasAccount = this.Account != null;

            bool hasMember = this.AccountMetadata.LinkedMember != Guid.Empty;
            Dictionary<Guid, string> orgs = this.GetUsersDatabaseOrgs(this.AccountMetadata.LinkedMember, Permissions.Username);
            bool hasGroups = orgs.Count > 0;

            HomePageViewModel model = new HomePageViewModel
            {
                LoggedIn = auth,
                HasAccount = hasAccount,
                LinkedMember = this.AccountMetadata.LinkedMember,
                MyDetails = hasMember,
                MyTraining = hasMember,
                MyMissions = hasMember,
                UnitRoster = hasGroups,
                UnitTraining = hasGroups,
                UnitMissions = hasGroups,
                PublicMapping = true,
                PublicReports = true,
                MyUnits = orgs.Select(f => new NameIdPair { Id = f.Key, Name = f.Value }).ToArray()
            };
            
            ViewData["hideMenu"] = true;
            return View(model);
        }

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

    //        // Getting the complete workbook...
    //        NPOI.HSSF.UserModel.HSSFWorkbook workbook = new NPOI.HSSF.UserModel.HSSFWorkbook();

    //    // Getting the worksheet by its name...
    //    var sheet = workbook.CreateSheet("Sheet1");

    //    if (sheet.LastRowNum < 4)
    //    {
    //        sheet.CreateRow(4);
    //    }

    //    // Getting the row... 0 is the first row.
    //    var dataRow = sheet.GetRow(4);

    //    if (dataRow.LastCellNum < 0)
    //    {
    //        dataRow.CreateCell(0);
    //    }

    //    // Setting the value 77 at row 5 column 1
    //    dataRow.GetCell(0).SetCellValue(77);

    //    // Forcing formula recalculation...
    ////    sheet.ForceFormulaRecalculation = true;

    //    //MemoryStream ms = new MemoryStream();

    //    //// Writing the workbook content to the FileStream...
    //    //templateWorkbook.Write(ms);

    //    //TempData["Message"] = "Excel report created successfully!";

    //    //// Sending the server processed data back to the user computer...
    //    //return File(ms.ToArray(), "application/vnd.ms-excel", "NPOINewFile.xls");
 

    //        System.IO.MemoryStream ms = new System.IO.MemoryStream();
    //        workbook.Write(ms);
    //        ms.Seek(0, System.IO.SeekOrigin.Begin);
    //        return this.File(ms, "application/vnd.ms-excel", filename);

        }

        public ActionResult About()
        {
            return View();
        }
    }
}
