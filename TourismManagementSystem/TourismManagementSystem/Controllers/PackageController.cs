using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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
        // GET ME
        private User GetMe()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;

            return db.Users
                     .Include(u => u.Role)
                     .Include(u => u.AgencyProfile)
                     .FirstOrDefault(u => u.Email == email);
        }

        // GET /agency/packages
        [HttpGet, Route("")]
        public ActionResult Index()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Agency";

            if (me.AgencyProfile == null) return RedirectToAction("Profile", "Agency");
            if (!me.IsApproved) return View("~/Views/Agency/NotApproved.cshtml", me.AgencyProfile);

            var items = db.TourPackages
                          .Where(p => p.AgencyId == me.UserId)
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

            return View(items);
        }

        // GET /agency/packages/create
        [HttpGet, Route("create")]
        public ActionResult Create()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");
            if (me.AgencyProfile == null) return RedirectToAction("Profile", "Agency");

            ViewBag.ActivePage = "CreatePackage";
            ViewBag.ActivePageGroup = "Agency";

            return View(new PackageCreateVm { DurationDays = 1, MaxGroupSize = 10 });
        }

        // POST /agency/packages/create
        [HttpPost, ValidateAntiForgeryToken, Route("create")]
        public ActionResult Create(PackageCreateVm vm)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");
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
                AgencyId = me.UserId,
                CreatedAt = DateTime.UtcNow
            };

            db.TourPackages.Add(pkg);
            db.SaveChanges();

            // Save uploaded images
            SaveImages(vm.Images, pkg.PackageId);

            TempData["Success"] = "Package created. You can now add sessions (dates) and images.";
            return RedirectToAction("Index");
        }

        // GET /agency/packages/{id}/edit
        [HttpGet, Route("{id:int}/edit")]
        public ActionResult Edit(int id)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var pkg = db.TourPackages
                        .Include(p => p.Images)
                        .FirstOrDefault(p => p.PackageId == id && p.AgencyId == me.UserId);

            if (pkg == null) return HttpNotFound();

            var vm = new PackageCreateVm
            {
                PackageId = pkg.PackageId,
                Title = pkg.Title,
                Description = pkg.Description,
                Price = pkg.Price,
                DurationDays = pkg.DurationDays,
                MaxGroupSize = pkg.MaxGroupSize,
                StartDate = pkg.StartDate,
                EndDate = pkg.EndDate,
                ExistingImages = pkg.Images.ToList()
            };

            return View(vm);
        }

        // POST /agency/packages/{id}/edit
        [HttpPost, ValidateAntiForgeryToken, Route("{id:int}/edit")]
        public ActionResult Edit(int id, PackageCreateVm vm, int[] removeImageIds)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(vm);

            var pkg = db.TourPackages
                        .Include(p => p.Images)
                        .FirstOrDefault(p => p.PackageId == id && p.AgencyId == me.UserId);

            if (pkg == null) return HttpNotFound();

            pkg.Title = vm.Title?.Trim();
            pkg.Description = vm.Description;
            pkg.Price = vm.Price;
            pkg.DurationDays = vm.DurationDays;
            pkg.MaxGroupSize = vm.MaxGroupSize;
            pkg.StartDate = vm.StartDate;
            pkg.EndDate = vm.EndDate;

            // Remove selected images
            if (removeImageIds != null && removeImageIds.Length > 0)
            {
                var imagesToRemove = pkg.Images.Where(i => removeImageIds.Contains(i.ImageId)).ToList();
                foreach (var img in imagesToRemove)
                {
                    var fullPath = Server.MapPath(img.ImagePath);
                    if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                    db.TourImages.Remove(img);
                }
            }

            // Upload new images
            SaveImages(vm.Images, pkg.PackageId);

            db.SaveChanges();
            TempData["Success"] = "Package updated successfully.";
            return RedirectToAction("Index");
        }

        // DELETE
        [HttpPost, ValidateAntiForgeryToken, Route("{id:int}/delete")]
        public ActionResult Delete(int id)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var pkg = db.TourPackages
                        .Include(p => p.Sessions.Select(s => s.Bookings))
                        .Include(p => p.Images)
                        .FirstOrDefault(p => p.PackageId == id && p.AgencyId == me.UserId);
            if (pkg == null) return HttpNotFound();

            // Block if bookings exist
            if (pkg.Sessions.Any(s => s.Bookings.Any()))
            {
                TempData["Error"] = "Cannot delete a package that has bookings.";
                return RedirectToAction("Index");
            }

            // Delete images from server
            foreach (var img in pkg.Images.ToList())
            {
                var fullPath = Server.MapPath(img.ImagePath);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                db.TourImages.Remove(img);
            }

            db.TourPackages.Remove(pkg);
            db.SaveChanges();
            TempData["Success"] = "Package deleted.";
            return RedirectToAction("Index");
        }

        // IMAGE UPLOAD HELPER
        private void SaveImages(IEnumerable<HttpPostedFileBase> images, int packageId)
        {
            if (images == null || !images.Any()) return;

            var uploadsFolder = Server.MapPath("~/images/packages/");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            foreach (var image in images)
            {
                if (image != null && image.ContentLength > 0)
                {
                    var uniqueFileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    image.SaveAs(filePath);

                    var tourImage = new TourImages
                    {
                        PackageId = packageId,
                        ImagePath = "/images/packages/" + uniqueFileName
                    };
                    db.TourImages.Add(tourImage);
                }
            }
        }

    }
}
