using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using TourismManagementSystem.Data;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    public class GuideController : Controller
    {
        private readonly TourismDbContext db = new TourismDbContext();

        // Public: directory of approved guides
        [AllowAnonymous]
        public ActionResult Index(string q)
        {
            ViewBag.ActivePageGroup = "Pages";
            ViewBag.Q = q;

            var baseQuery = db.GuideProfiles
                .AsNoTracking()
                .Include(g => g.User)
                .Where(g => g.Status == "Approved" && g.User.IsApproved && g.User.IsActive);

            if (!string.IsNullOrWhiteSpace(q))
            {
                baseQuery = baseQuery.Where(g =>
                    g.FullNameOnLicense.Contains(q) ||
                    g.User.FullName.Contains(q));
            }

            var guides = baseQuery
                .Select(g => new GuideListItemVm
                {
                    // shared PK: use UserId (keep the property name if your view expects ProfileId)
                    ProfileId = g.UserId,
                    FullName = string.IsNullOrEmpty(g.FullNameOnLicense) ? g.User.FullName : g.FullNameOnLicense,
                    Bio = g.Bio,
                    PhotoPath = g.PhotoPath,
                    Phone = g.Phone,

                    // tours owned by this guide (GuideId stores the guide's UserId)
                    TotalTours = db.TourPackages.Count(p => p.GuideId == g.UserId),

                    // average rating across this guide's packages
                    AvgRating = db.Feedbacks
                        .Where(f => f.Booking.Session.Package.GuideId == g.UserId)
                        .Select(f => (double?)f.Rating)
                        .DefaultIfEmpty()
                        .Average()
                })
                .OrderByDescending(x => x.AvgRating ?? 0)
                .ThenBy(x => x.FullName)
                .ToList();

            return View(guides);
        }

        // Guide completes their profile (requires Guide role)
        [Authorize(Roles = "Guide")]
        [HttpGet]
        public ActionResult CompleteGuideProfile()
        {
            var email = User?.Identity?.Name;
            var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == email);
            if (user == null) return RedirectToAction("Login", "Account");

            var profile = db.GuideProfiles.FirstOrDefault(p => p.UserId == user.UserId);
            if (profile == null)
            {
                profile = new GuideProfile
                {
                    UserId = user.UserId,
                    FullNameOnLicense = user.FullName,
                    Status = "PendingVerification"
                };
                db.GuideProfiles.Add(profile);
                db.SaveChanges();
            }

            return View(profile);
        }

        [Authorize(Roles = "Guide")]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CompleteGuideProfile(GuideProfile model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = User?.Identity?.Name;
            var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == email);
            if (user == null) return RedirectToAction("Login", "Account");

            var profile = db.GuideProfiles.FirstOrDefault(p => p.UserId == user.UserId);
            if (profile == null) return View(model);

            profile.FullNameOnLicense = model.FullNameOnLicense;
            profile.GuideLicenseNo = model.GuideLicenseNo;
            profile.Phone = model.Phone;
            profile.Bio = model.Bio;

            db.SaveChanges();

            TempData["Success"] = "Guide profile saved. Awaiting admin approval.";
            return RedirectToAction("Dashboard");
        }

        // Guide dashboard (gate by approval)
        [Authorize(Roles = "Guide")]
        public ActionResult Dashboard()
        {
            var email = User?.Identity?.Name;
            var me = db.Users
                       .Include(u => u.GuideProfile)
                       .FirstOrDefault(u => u.Email == email);

            if (me == null) return RedirectToAction("Login", "Account");

            if (me.GuideProfile == null || !me.IsApproved)
                return View("NotApproved", model: me.GuideProfile); // create a NotApproved.cshtml for Guide, similar to Agency

            // TODO: build a real VM like AgencyDashboardVm for guides if required
            return View();
        }
    }
}
