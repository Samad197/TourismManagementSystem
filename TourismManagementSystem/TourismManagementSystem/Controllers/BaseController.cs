using System;
using System.Linq;
using System.Web.Mvc;
using TourismManagementSystem.Data;

public class BaseController : Controller
{
    protected readonly TourismDbContext db = new TourismDbContext();

    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        // Safe defaults for every request
        ViewBag.IsAuthenticated = User?.Identity?.IsAuthenticated ?? false;
        ViewBag.RoleName = null;     // "Admin" / "Agency" / "Guide" / "Tourist"
        ViewBag.IsAdmin = false;
        ViewBag.IsAgency = false;
        ViewBag.IsGuide = false;
        ViewBag.IsTourist = false;

        // Providers require approval/activation; others are implicitly enabled
        ViewBag.IsApproved = false;
        ViewBag.IsActive = false;

        // Canonical flag the navbar can use
        ViewBag.CanManageProvider = false;

        if (ViewBag.IsAuthenticated)
        {
            var email = User.Identity.Name;

            var info = db.Users
                         .AsNoTracking()
                         .Where(u => u.Email == email)
                         .Select(u => new
                         {
                             u.IsApproved,
                             u.IsActive,                 // ✅ add this
                             RoleName = u.Role.RoleName
                         })
                         .FirstOrDefault();

            if (info != null)
            {
                var role = info.RoleName ?? string.Empty;

                bool isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
                bool isAgency = role.Equals("Agency", StringComparison.OrdinalIgnoreCase);
                bool isGuide = role.Equals("Guide", StringComparison.OrdinalIgnoreCase);
                bool isTourist = role.Equals("Tourist", StringComparison.OrdinalIgnoreCase);

                ViewBag.RoleName = role;
                ViewBag.IsAdmin = isAdmin;
                ViewBag.IsAgency = isAgency;
                ViewBag.IsGuide = isGuide;
                ViewBag.IsTourist = isTourist;

                var isProvider = isAgency || isGuide;

                // Non-providers (Admin/Tourist) are implicitly approved+active for nav
                ViewBag.IsApproved = isProvider ? (info.IsApproved == true) : true;
                ViewBag.IsActive = isProvider ? (info.IsActive == true) : true;

                // ✅ Single source of truth for the nav (preview handled in layout)
                ViewBag.CanManageProvider = isAdmin || (isProvider && ViewBag.IsApproved && ViewBag.IsActive);

                // Optional: session fallbacks if your layout reads Session
                Session["RoleName"] = role;
                Session["IsApproved"] = (bool)ViewBag.IsApproved;
                Session["IsActive"] = (bool)ViewBag.IsActive;
            }
        }

        base.OnActionExecuting(filterContext);
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing) db.Dispose();
        base.Dispose(disposing);
    }
}

