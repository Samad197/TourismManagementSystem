using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourismManagementSystem.Data;

public class BaseController : Controller
{
    protected readonly TourismDbContext db = new TourismDbContext();

    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var email = User?.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(email))
        {
            // Load current user with role
            var me = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == email);
            if (me != null)
            {
                var role = me.Role?.RoleName ?? string.Empty;

                // Expose to _Layout.cshtml
                ViewBag.RoleName = role;                // "Admin" / "Agency" / "Guide" / "Tourist"
                ViewBag.IsAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
                ViewBag.IsAgency = role.Equals("Agency", StringComparison.OrdinalIgnoreCase);
                ViewBag.IsGuide = role.Equals("Guide", StringComparison.OrdinalIgnoreCase);
                ViewBag.IsTourist = role.Equals("Tourist", StringComparison.OrdinalIgnoreCase);

                // Only providers need approval
                ViewBag.IsApproved = (ViewBag.IsAgency || ViewBag.IsGuide) ? me.IsApproved : true;
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
