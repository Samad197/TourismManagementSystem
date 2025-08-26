using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourismManagementSystem.Data;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("packages")]
    public class PublicPackageController : Controller
    {
        private readonly TourismDbContext db = new TourismDbContext();

        // GET /packages
        [HttpGet, Route("")]
        public ActionResult Index(string q, decimal? minPrice, decimal? maxPrice, int? minDays, int? maxDays, bool upcomingOnly = true)
        {
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Packages";
            ViewBag.Q = q;

            var today = DateTime.Today;

            var query = db.TourPackages
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Sessions)
                .Include(p => p.Agency.User)
                .Include(p => p.Guide.User);
               

            // Filter: Only approved & active owners
            query = query.Where(p =>
                (p.AgencyId != null && p.Agency.User.IsApproved && p.Agency.User.IsActive) ||
                (p.GuideId != null && p.Guide.User.IsApproved && p.Guide.User.IsActive));

            // Filter: Only upcoming packages
            if (upcomingOnly)
                query = query.Where(p => p.Sessions.Any(s => DbFunctions.TruncateTime(s.StartDate) >= today));

            // Search filter
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Title.Contains(q) || p.Description.Contains(q));

            // Filters: Price & Days
            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
            if (minDays.HasValue) query = query.Where(p => p.DurationDays >= minDays.Value);
            if (maxDays.HasValue) query = query.Where(p => p.DurationDays <= maxDays.Value);

            // Map to ViewModel
            var packages = query
                .OrderBy(p => p.Title)
                .Select(p => new PublicPackageListItemVm
                {
                    PackageId = p.PackageId,
                    Title = p.Title,
                    Price = p.Price,
                    DurationDays = p.DurationDays,
                    MaxGroupSize = p.MaxGroupSize,
                    ThumbnailPath = p.Images.FirstOrDefault().ImagePath ?? "/images/placeholder.jpg",
                    OwnerType = p.AgencyId != null ? "Agency" : "Guide",
                    OwnerName = p.AgencyId != null ? p.Agency.User.FullName : p.Guide.User.FullName,
                    //AvgRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : (double?)null,
                    HasUpcoming = p.Sessions.Any(s => DbFunctions.TruncateTime(s.StartDate) >= today)
                })
                .ToList();

            return View(packages); // Views/PublicPackage/Index.cshtml
        }
    

    // GET /packages/{id}
    [HttpGet, Route("{id:int}")]
        public ActionResult Details(int id)
        {
            var p = db.TourPackages
                .Include(x => x.Images)
                .Include(x => x.Sessions)
                .Include(x => x.Agency.User)
                .Include(x => x.Guide.User)
                .FirstOrDefault(x => x.PackageId == id);

            if (p == null) return HttpNotFound();

            // Hide details if owner not approved/active
            bool ownerApproved =
                (p.AgencyId != null && p.Agency.User.IsApproved && p.Agency.User.IsActive) ||
                (p.GuideId != null && p.Guide.User.IsApproved && p.Guide.User.IsActive);
            if (!ownerApproved) return HttpNotFound();

            return View(p); // Views/Packages/Details.cshtml
        }
    }
}
