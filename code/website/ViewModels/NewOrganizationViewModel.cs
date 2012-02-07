using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SarTracks.Website.Models;
using System.Web.Mvc;

namespace SarTracks.Website.ViewModels
{
    public class NewOrganizationViewModel
    {
        public Organization Org { get; set; }
        public string Visibility { get; set; }
    }
}