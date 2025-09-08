using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using TourismManagementSystem.Data;

namespace TourismManagementSystem
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

  //          Database.SetInitializer(
  //    new MigrateDatabaseToLatestVersion<TourismDbContext, TourismManagementSystem.Migrations.Configuration>()
  //);
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null) return;

            var ticket = FormsAuthentication.Decrypt(authCookie.Value);
            if (ticket == null) return;

            var roles = (ticket.UserData ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            HttpContext.Current.User = new GenericPrincipal(new FormsIdentity(ticket), roles);
        }
    }
}
