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
        private static Dictionary<string, AuthIdentityService> permissionsCache = new Dictionary<string, AuthIdentityService>();

        protected AuthIdentityService Permissions { get; private set; }
        protected DataStoreFactory StoreFactory { get; private set; }

        public ControllerBase()
        {
            this.StoreFactory = new DataStoreFactory();
        }

        public ControllerBase(AuthIdentityService permissions, DataStoreFactory storeFactory)
        {
            this.Permissions = permissions;
            this.StoreFactory = storeFactory;
        }

        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
            if (this.Permissions == null)
            {
                this.Permissions = AuthIdentityServiceCache.GetInstance(this.User, this.StoreFactory);
            }
            ViewData["IsAdmin"] = Permissions.HasPermission(PermissionType.SiteAdmin, null);
            ViewData["UserName"] = Permissions.UserName;

            if (Request != null && Request.QueryString["msgOk"] != null)
            {
                ViewData["success"] = new MvcHtmlString(Request.QueryString["msgOk"]);
            }
        }

        protected IDataStoreService GetRepository()
        {
            return this.StoreFactory.Create(Permissions.UserLogin);
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

        protected Dictionary<Guid, string> GetUsersDatabaseOrgs()
        {
            Dictionary<Guid, string> orgs = new Dictionary<Guid, string>();

            using (var ctx = GetRepository())
            {
                foreach (var org in ctx.Organizations.Select(f => new NameIdPair { Id = f.Id, Name = f.LongName }))
                {
                    if (Permissions.HasPermission(PermissionType.ViewOrganizationBasic, org.Id))
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
