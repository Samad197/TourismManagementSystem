using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourismManagementSystem.Data;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize(Roles = "Agency")]
    [RoutePrefix("agency/packages")]
    public class PackageController : BaseController
    {
        // If BaseController doesn't expose db, add there:
        // protected readonly TourismDbContext db = new TourismDbContext();

        private User GetMe()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;

            return db.Users
                     .Include(u => u.Role)
                     .Include(u => u.AgencyProfile)
                     .FirstOrDefault(u => u.Email == email);
        }

        // GET /agency/packages  (My Packages)
        [HttpGet, Route("")]
        public ActionResult Index()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Agency";

            // Require profile first
            if (me.AgencyProfile == null)
                return RedirectToAction("Profile", "Agency");

            // Not approved -> show your pending screen (you already have this view)
            if (!me.IsApproved)
                return View("~/Views/Agency/NotApproved.cshtml", model: me.AgencyProfile);

            var myKey = me.UserId; // Shared PK pattern: AgencyProfile PK == UserId

            var items = db.TourPackages
                .Where(p => p.AgencyId == myKey)
                .Select(p => new MyPackageListItemVm
                {
                    PackageId = p.PackageId,
                    Title = p.Title,
                    Price = p.Price,
                    DurationDays = p.DurationDays,
                    MaxGroupSize = p.MaxGroupSize,
                    CreatedAt = p.CreatedAt,
                    SessionsCount = p.Sessions.Count()
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(items); // Views/Package/Index.cshtml (your “My Packages” table)
        }

        // GET /agency/packages/create
        [HttpGet, Route("create")]
        public ActionResult Create()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            ViewBag.ActivePage = "CreatePackage";
            ViewBag.ActivePageGroup = "Agency";

            if (me.AgencyProfile == null)
                return RedirectToAction("Profile", "Agency");

            // Agencies can create drafts before approval; public side filters by owner approval
            return View(new PackageCreateVm { DurationDays = 1, MaxGroupSize = 10 });
        }

        // POST /agency/packages/create
        [HttpPost, ValidateAntiForgeryToken, Route("create")]
        public ActionResult Create(PackageCreateVm vm)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");
            if (me.AgencyProfile == null) return RedirectToAction("Profile", "Agency");

            ViewBag.ActivePage = "CreatePackage";
            ViewBag.ActivePageGroup = "Agency";

            if (!ModelState.IsValid) return View(vm);

            var pkg = new TourPackage
            {
                Title = vm.Title?.Trim(),
                Description = vm.Description,
                Price = vm.Price,
                DurationDays = vm.DurationDays,
                MaxGroupSize = vm.MaxGroupSize,
                StartDate = vm.StartDate,
                EndDate = vm.EndDate,
                AgencyId = me.UserId,     // Owner = this agency user
                GuideId = null,
                CreatedAt = DateTime.UtcNow
            };

            db.TourPackages.Add(pkg);
            db.SaveChanges();

            TempData["Success"] = "Package created. You can now add sessions (dates) and images.";
            return RedirectToAction("Index");
        }

        // GET /agency/packages/{id}/edit
        [HttpGet, Route("{id:int}/edit")]
        public ActionResult Edit(int id)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            ViewBag.ActivePage = "MyPackages";
            ViewBag.ActivePageGroup = "Agency";

            var pkg = db.TourPackages.FirstOrDefault(p => p.PackageId == id && p.AgencyId == me.UserId);
            if (pkg == null) return HttpNotFound();

            var vm = new PackageCreateVm
            {
                Title = pkg.Title,
                Description = pkg.Description,
                Price = pkg.Price,
                DurationDays = pkg.DurationDays,
                MaxGroupSize = pkg.MaxGroupSize,
                StartDate = pkg.StartDate,
                EndDate = pkg.EndDate
            };

            return View(vm); // reuse Create.cshtml or make a dedicated Edit.cshtml
        }

        // POST /agency/packages/{id}/edit
        [HttpPost, ValidateAntiForgeryToken, Route("{id:int}/edit")]
        public ActionResult Edit(int id, PackageCreateVm vm)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(vm);

            var pkg = db.TourPackages.FirstOrDefault(p => p.PackageId == id && p.AgencyId == me.UserId);
            if (pkg == null) return HttpNotFound();

            pkg.Title = vm.Title?.Trim();
            pkg.Description = vm.Description;
            pkg.Price = vm.Price;
            pkg.DurationDays = vm.DurationDays;
            pkg.MaxGroupSize = vm.MaxGroupSize;
            pkg.StartDate = vm.StartDate;
            pkg.EndDate = vm.EndDate;

            db.SaveChanges();
            TempData["Success"] = "Package updated.";
            return RedirectToAction("Index");
        }

        // POST /agency/packages/{id}/delete
        [HttpPost, ValidateAntiForgeryToken, Route("{id:int}/delete")]
        public ActionResult Delete(int id)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var pkg = db.TourPackages
                        .Include(p => p.Sessions.Select(s => s.Bookings))
                        .FirstOrDefault(p => p.PackageId == id && p.AgencyId == me.UserId);
            if (pkg == null) return HttpNotFound();

            // (Optional) block delete if there are bookings
            bool hasBookings = pkg.Sessions.Any(s => s.Bookings.Any());
            if (hasBookings)
            {
                TempData["Error"] = "Cannot delete a package that has bookings.";
                return RedirectToAction("Index");
            }

            db.TourPackages.Remove(pkg);
            db.SaveChanges();
            TempData["Success"] = "Package deleted.";
            return RedirectToAction("Index");
        }
    }
}
