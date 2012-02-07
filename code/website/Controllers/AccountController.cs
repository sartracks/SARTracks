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
    using System.Configuration;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Net;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Security;
    using DotNetOpenAuth.Messaging;
    using DotNetOpenAuth.OpenId;
    using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
    using DotNetOpenAuth.OpenId.RelyingParty;
    using SarTracks.Website.Services;
    using SarTracks.Website.ViewModels;
    using SarTracks.Website.Models;
    using System.Globalization;

    public class AccountController : ControllerBase
    {
        public AccountController() : base() { }
        public AccountController(AuthIdentityService perms, DataStoreFactory factory) : base(perms, factory) { }

        /// <summary>
        /// Gets the OpenID relying party to use for logging users in.
        /// </summary>
        public IOpenIdRelyingParty RelyingParty
        {
            get
            {
                if (this._relyingParty == null)
                {
                    this._relyingParty = new OpenIdRelyingPartyService();
                }
                return this._relyingParty;
            }
        }
        private IOpenIdRelyingParty _relyingParty = null;


        private Uri PrivacyPolicyUrl
        {
            get
            {
                return Url.ActionFull("PrivacyPolicy", "Home");
            }
        }

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Welcome()
        {
            AccountInfo info = null;
            if (Permissions.IsAuthenticated)
            {
                info = new AccountInfo { Username = Permissions.UserLogin, HasAccount = false };
                MembershipUser user = Membership.GetUser(info.Username);
                if (user != null)
                {
                    info.HasAccount = true;

                }

            }

            return View(info);
        }

        [HttpGet]
        public ActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(string q)
        {
            using (var ctx = GetRepository())
            {
                User u = ctx.Users.SingleOrDefault(f => f.Username == q);
                if (u == null)
                {
                    ViewData["error"] = new MvcHtmlString("Username '" + q + "' is not found");
                    return View();
                }

                string password = AuthIdentityService.ResetPassword(u);
                ctx.SaveChanges();

                MailService.SendMail(u.Email, "SARTracks password reset", "Your password has been reset. Your new password is:\n" + password + "");

                return RedirectToAction("ResetPasswordSuccess", new { q = u.Email });
            }
        }

        public ActionResult ResetPasswordSuccess(string q)
        {
            return View("ResetPasswordSuccess", new MvcHtmlString(q) );
        }

        [Authorize]
        public ActionResult Settings()
        {
            return View();
        }

        [Authorize]
        public ActionResult MyRoles()
        {
            return View();
        }

        public const string ORG_ROLE_PATTERN = "^org(.{36})\\.(.*)$";

        [Authorize]
        public DataActionResult GetMyRoles()
        {
            string[] roles = null;

            roles = Permissions.GetRolesForUser(Permissions.UserLogin, true);

            return Data(roles.OrderBy(f => f));
        }

        [Authorize]
        public DataActionResult GetMyPermissions()
        {
            return Data(null);
        }

        //
        // GET: /Account/LogOn

        public ActionResult LogOn()
        {
            ViewData["hideMenu"] = true;
            return View();
        }

        //
        // POST: /Account/LogOn

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (Permissions.ValidateUser(model.UserName, model.Password) == LogonResult.Okay)
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Performs discovery on a given identifier.
        /// </summary>
        /// <param name="identifier">The identifier on which to perform discovery.</param>
        /// <returns>The JSON result of discovery.</returns>
        public ActionResult DiscoverOpenId(string identifier)
        {
            if (!this.Request.IsAjaxRequest())
            {
                throw new InvalidOperationException();
            }

            return RelyingParty.AjaxDiscovery(
                identifier,
                Realm.AutoDetect,
                Url.ActionFull("OpenIdPopUpReturnTo"),
                this.PrivacyPolicyUrl);
        }

        /// <summary>
        /// Handles the positive assertion that comes from Providers.
        /// </summary>
        /// <param name="openid_openidAuthData">The positive assertion obtained via AJAX.</param>
        /// <returns>The action result.</returns>
        /// <remarks>
        /// This method instructs ASP.NET MVC to <i>not</i> validate input
        /// because some OpenID positive assertions messages otherwise look like
        /// hack attempts and result in errors when validation is turned on.
        /// </remarks>
        [HttpPost, ValidateInput(false)]
        public ActionResult LogOnPostAssertion(string openid_openidAuthData)
        {
            IAuthenticationResponse response;
            if (!string.IsNullOrEmpty(openid_openidAuthData))
            {
                var auth = new Uri(openid_openidAuthData);
                var headers = new WebHeaderCollection();
                foreach (string header in Request.Headers)
                {
                    headers[header] = Request.Headers[header];
                }

                // Always say it's a GET since the payload is all in the URL, even the large ones.
                HttpRequestInfo clientResponseInfo = new HttpRequestInfo("GET", auth, auth.PathAndQuery, headers, null);
                response = RelyingParty.GetResponse(clientResponseInfo);
            }
            else
            {
                response = RelyingParty.GetResponse();
            }
            if (response != null)
            {
                switch (response.Status)
                {
                    case AuthenticationStatus.Authenticated:
                        string alias = response.FriendlyIdentifierForDisplay;
                        string email = response.ClaimedIdentifier;
                        var sreg = response.GetExtension<ClaimsResponse>();
                        if (sreg != null && sreg.MailAddress != null)
                        {
                            alias = sreg.MailAddress.User;
                            email = sreg.MailAddress.Address;
                        }
                        if (sreg != null && !string.IsNullOrEmpty(sreg.FullName))
                        {
                            alias = sreg.FullName;
                        }

                        FormsAuthenticationTicket authTicket = new
                            FormsAuthenticationTicket(1, //version
                            email, // user name
                            DateTime.Now,             //creation
                            DateTime.Now.AddMinutes(30), //Expiration
                            false, //Persistent
                            alias);


                        string encTicket = FormsAuthentication.Encrypt(authTicket);

                        this.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encTicket));


                        string returnUrl = Request.Form["returnUrl"];
                        if (!String.IsNullOrEmpty(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    case AuthenticationStatus.Canceled:
                        ModelState.AddModelError("OpenID", "It looks like you canceled login at your OpenID Provider.");
                        break;
                    case AuthenticationStatus.Failed:
                        ModelState.AddModelError("OpenID", response.Exception.Message);
                        string errorsAddr = ConfigurationManager.AppSettings["ErrorsTo"];
                        if (!string.IsNullOrWhiteSpace(errorsAddr))
                        {
                            MailService.SendMail(errorsAddr, "OpenID Auth Failure", response.Exception.ToStringDescriptive());
                        }
                        break;
                }
            }

            // If we're to this point, login didn't complete successfully.
            // Show the LogOn view again to show the user any errors and
            // give another chance to complete login.
            return View("LogOn");
        }

        /// <summary>
        /// Handles the positive assertion that comes from Providers to Javascript running in the browser.
        /// </summary>
        /// <returns>The action result.</returns>
        /// <remarks>
        /// This method instructs ASP.NET MVC to <i>not</i> validate input
        /// because some OpenID positive assertions messages otherwise look like
        /// hack attempts and result in errors when validation is turned on.
        /// </remarks>
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post), ValidateInput(false)]
        public ActionResult OpenIdPopUpReturnTo()
        {
            return RelyingParty.ProcessAjaxOpenIdResponse();
        }

        //
        // GET: /Account/Register

        public ActionResult Register()
        {

            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            return RegisterAccount(model);
        }

        private ActionResult RegisterAccount(RegisterModel model)
        {
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
            }

            if (ModelState.IsValid)
            {
                using (var ctx = this.GetRepository())
                {
                    User u = ctx.Users.SingleOrDefault(f => f.Username == model.UserName);
                    if (u != null)
                    {
                        ModelState.AddModelError("UserName", "Not available");
                        ModelState.SetModelValue("UserName", new ValueProviderResult(model.UserName, model.UserName, CultureInfo.CurrentUICulture));
                    }
                    else
                    {
                        
                        u = new User
                        {
                            Username = model.UserName,
                            Name = string.Format("{0} {1}", model.FirstName, model.LastName),
                            Email = model.Email,
                            Password = model.Password
                        };
                        
                        try
                        {
                            ctx.Users.Add(AuthIdentityService.SetupUser(u));
                            ctx.SaveChanges();

                            // Send email notice to user
                            MailService.SendMail(u.Email, "Verify SARTracks Account",
                                string.Format("Click the link to verify your account:\n{0}?UserName={1}&Key={2}", Url.ActionFull("Verify"), u.Username, u.ValidationKey));

                            return RedirectToAction("Verify", "Account");
                        }
                        catch
                        {
                            TempData["error"] = "Error saving user. Please try again.";
                        }
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        /// <summary>
        /// Verify an email address and unlock an account by supplying a key that was emailed to the address.
        /// </summary>
        /// <returns></returns>
        public ActionResult Verify(VerifyAccountModel model)
        {
            if (Request.HttpMethod == "GET" && model.UserName == null && model.Key == null)
            {
                ModelState.Clear();
            }
            else if (ModelState.IsValid)
            {
                using (var ctx = GetRepository())
                {
                    User u = ctx.Users.Single(f => f.Username == model.UserName);

                    if (u.State == UserState.Verification && u.ValidationKey.HasValue)
                    {
                        if (model.Key.Equals(u.ValidationKey.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            u.ValidationKey = null;
                            u.State = UserState.Okay;
                            ctx.SaveChanges();
                            Permissions.SetAuthCookie(model.UserName);

                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ModelState.AddModelError("Key", "Invalid verification key");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("UserName", "Invalid user or account is not pending verification");
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public ActionResult Verified()
        {
            VerifyAccountModel model = new VerifyAccountModel { UserName = Permissions.UserLogin };
            return View(model);
        }

        //
        // GET: /Account/Register

        [Authorize]
        public ActionResult RegisterOpenId()
        {
            RegisterModel model = new RegisterModel { UserName = Permissions.UserLogin };
            model.Email = model.UserName;

            MembershipUser user = Membership.GetUser(model.UserName);

            if (user != null) return new ContentResult { Content = string.Format("OpenId {0} is already registered", model.UserName), ContentType = "text/plain" };


            return View(model);
        }

        //
        // POST: /Account/Register

        [HttpPost]
        [Authorize]
        public ActionResult RegisterOpenId(RegisterModel model)
        {
            string openId = Permissions.UserLogin;
            MembershipUser user = Membership.GetUser(openId);

            if (user != null)
            {
                ModelState.AddModelError("", string.Format("OpenId {0} is already registered", openId));
            }

            model.UserName = openId;
            model.Password = Membership.GeneratePassword(16, 4);
            model.ConfirmPassword = model.Password;

            return RegisterAccount(model);
        }

        //
        // GET: /Account/ChangePassword

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (model.ConfirmPassword != model.NewPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords must match");                
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (var ctx = GetRepository())
                    {
                        User u = Permissions.User;
                        ctx.Users.Attach(u);
                        AuthIdentityService.ChangePassword(u, model.OldPassword, model.NewPassword);
                        ctx.SaveChanges();
                        return RedirectToAction("ChangePasswordSuccess");
                    }
                }
                catch
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePasswordSuccess

        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }
    }
}
