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
    using System.Data.Entity.Validation;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Web.Mvc;
    using System.Web.Security;
    using SarTracks.Website.Services;
    using SarTracks.Website.ViewModels;

    public class ControllerBase : Controller
    {        
        private IPermissionsService _permissions = null;
        protected IPermissionsService Permissions
        {
            get
            {
                if (this._permissions == null) this._permissions = PermissionsServiceCache.GetInstance(this.User.Identity.Name);

                return this._permissions;
            }
        }


        private MembershipUser account;
        private bool triedAccount = false;
        protected MembershipUser Account
        {
            get
            {
                if (!triedAccount)
                {
                    this.account = Membership.GetUser(User.Identity.Name);
                }
                return this.account;
            }
        }

        private UserMetadata userMetadata = null;
        private bool triedMetadata = false;
        protected UserMetadata AccountMetadata
        {
            get
            {
                if (!triedMetadata)
                {
                    this.userMetadata = (this.Account == null) ? new UserMetadata() : Account.GetMetadata();
                }
                return this.userMetadata;
            }
        }


        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
      //      ((PermissionsService)this.Permissions).SetRequest(Request);
        }

        protected IDataStoreService GetRepository()
        {
            return DataStoreService.Create(Permissions.Username);
        }

        protected DataActionResult GetLoginError()
        {
            Response.StatusCode = 403;
            return new DataActionResult("login");
        }

        protected RedirectResult GetLoginRedirect()
        {
            //use the current url for the redirect
            string redirectOnSuccess = this.Request.Url.AbsolutePath;

            //send them off to the login page
            string redirectUrl = string.Format("?ReturnUrl={0}", redirectOnSuccess);
            string loginUrl = FormsAuthentication.LoginUrl + redirectUrl;
            return new RedirectResult(loginUrl);
        }

        protected DataActionResult Data(object model)
        {
            Type t = model.GetType();
            if (t.IsArray) t = t.GetElementType();

            bool isDataContract = t.GetCustomAttributes(typeof(DataContractAttribute), true).Length > 0;

            if (Request.AcceptTypes != null && Request.AcceptTypes.Contains("application/json") || string.Equals(Request.QueryString["format"], "json", StringComparison.OrdinalIgnoreCase))
            {
                return isDataContract ? (DataActionResult)(new JsonDataContractResult(model)) : new JsonGenericDataResult(model);
            }

            return isDataContract ? (DataActionResult)(new XmlDataContractResult(model)) : new XmlDataResult(model);
        }

        protected Dictionary<Guid, string> GetUsersDatabaseOrgs(Guid memberId, string username)
        {
            Dictionary<Guid, string> orgs = new Dictionary<Guid, string>();

            using (var ctx = GetRepository())
            {
                if (memberId != Guid.Empty)
                {
                    DateTime now = DateTime.UtcNow;
                    foreach (var org in ctx.Organizations.Where(f => f.Memberships.Any(g => g.Member.Id == memberId && g.Status.IsActive && (g.Finish == null || g.Finish > now) && g.Start <= now)).Distinct())
                    {
                        orgs.Add(org.Id, org.LongName);
                    }
                }

                string[] roles = Permissions.GetRolesForUser(username, true);

                foreach (var org in ctx.Organizations.Select(f => new NameIdPair { Id = f.Id, Name = f.LongName }))
                {
                    if (!orgs.ContainsKey(org.Id) && roles.Any(f => f.Equals(string.Format("org{0}.users", org.Id), StringComparison.OrdinalIgnoreCase)))
                    {
                        orgs.Add(org.Id, org.Name);
                    }
                }
            }
            return orgs;
        }

        public void ValidationErrorsToModelState(IEnumerable<DbEntityValidationResult> errors)
        {
            foreach (var prop in errors)
            {
                foreach (var err in prop.ValidationErrors)
                {
                    ModelState.AddModelError(err.PropertyName, err.ErrorMessage);
                    object attempted = prop.Entry.CurrentValues[err.PropertyName];
                    ModelState.SetModelValue(err.PropertyName, new ValueProviderResult(attempted, string.Format("{0}", attempted), System.Globalization.CultureInfo.CurrentUICulture));
                }
            }
        }

        public void ModelStateToSubmitErrors(List<SubmitError> errors)
        {            
            foreach (var prop in ModelState)
            {
                
                foreach (var err in prop.Value.Errors)
                {
                    errors.Add(new SubmitError { Error = err.ErrorMessage, Property = prop.Key });
                }
            }
        }

    }
}
