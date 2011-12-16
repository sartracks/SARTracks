using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SarTracks.Website
{
    using SWS = System.Web.Security;
    using SarTracks.Website.Models;

    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{q}", // URL with parameters
                new { controller = "Home", action = "Index", q = UrlParameter.Optional } // Parameter defaults
            );
            
            routes.MapRoute("OpenIdDiscover", "Account/DiscoverOpenId");
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            HttpCookie authCookie = Request.Cookies[SWS.FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                SWS.FormsAuthenticationTicket authTicket = SWS.FormsAuthentication.Decrypt(authCookie.Value);

                //Cache cache = HttpContext.Current.Cache;
                //using (HgDataContext hg = new HgDataContext())
                //{
                //    user = cache[authTicket.Name] as User;
                //    if (user == null)
                //    {
                //        user = (from u in hg.Users where u.EmailAddress == authTicket.Name select u).Single();
                //        cache[authTicket.Name] = user;
                //    }
                //}
               // var principal = new SWS.RolePrincipal(user);
               // Context.User = principal;
            }
        }
    }
}