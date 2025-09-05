using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize(Roles = "Agency,Guide")]
    [RoutePrefix("provider/sessions")]
    public class PackageSessionsController : BaseController
    {
        private User GetMe()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;

            return db.Users
                     .Include(u => u.Role)
                     .Include(u => u.AgencyProfile)
                     .Include(u => u.GuideProfile)
                     .FirstOrDefault(u => u.Email == email);
        }

        private bool IsAgency(User me) => me.Role.RoleName == "Agency";
        private bool IsGuide(User me) => me.Role.RoleName == "Guide";

        // ✅ FIX: Do role filtering outside EF query
        private TourPackage FindOwnedPackageOr404(int packageId)
        {
            var me = GetMe();
            if (me == null) return null;

            bool isAgency = IsAgency(me);

            IQueryable<TourPackage> query = db.TourPackages
                .Include(p => p.Agency.User)
                .Include(p => p.Guide.User);

            if (isAgency)
                return query.FirstOrDefault(p => p.PackageId == packageId && p.AgencyId == me.UserId);
            else
                return query.FirstOrDefault(p => p.PackageId == packageId && p.GuideId == me.UserId);
        }

        // ========= Index =========
        [HttpGet, Route("")]
        public ActionResult Index(int page = 1, int pageSize = 10)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            bool isAgency = IsAgency(me);
            ViewBag.ActivePage = "Sessions";
            ViewBag.ActivePageGroup = isAgency ? "Agency" : "Guide";

            if (isAgency && me.AgencyProfile == null) return RedirectToAction("Profile", "Provider");
            if (IsGuide(me) && me.GuideProfile == null) return RedirectToAction("Profile", "Provider");

            var query = db.Sessions
                          .Include(s => s.Package)
                          .Where(s => isAgency ? s.Package.AgencyId == me.UserId
                                               : s.Package.GuideId == me.UserId)
                          .OrderBy(s => s.StartDate);

            var totalCount = query.Count();
            var sessions = query.Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View(sessions);
        }

        // ========= Create =========
        [HttpGet, Route("{packageId:int}/create")]
        public ActionResult Create(int packageId)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            bool isAgency = IsAgency(me);
            ViewBag.ActivePage = "Sessions";
            ViewBag.ActivePageGroup = isAgency ? "Agency" : "Guide";

            var pkg = FindOwnedPackageOr404(packageId);
            if (pkg == null) return HttpNotFound();

            var vm = new SessionFormVm
            {
                PackageId = packageId,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(10),
                Capacity = 10
            };

            ViewBag.Package = pkg;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken, Route("{packageId:int}/create")]
        public ActionResult Create(SessionFormVm vm)
        {
            var pkg = FindOwnedPackageOr404(vm.PackageId);
            if (pkg == null) return HttpNotFound();

            if (vm.EndDate < vm.StartDate)
                ModelState.AddModelError("EndDate", "End Date must be on or after Start Date.");

            if (!ModelState.IsValid)
            {
                var me = GetMe();
                ViewBag.ActivePage = "Sessions";
                ViewBag.ActivePageGroup = IsAgency(me) ? "Agency" : "Guide";
                ViewBag.Package = pkg;
                return View(vm);
            }

            var entity = new Session
            {
                PackageId = vm.PackageId,
                StartDate = vm.StartDate,
                EndDate = vm.EndDate,
                Capacity = vm.Capacity,
                IsCanceled = vm.IsCanceled,
                Notes = vm.Notes
            };

            db.Sessions.Add(entity);
            db.SaveChanges();

            TempData["Success"] = "Session created.";
            return RedirectToAction("Index");
        }

        // ========= Edit =========
        [HttpGet, Route("{id:int}/edit")]
        public ActionResult Edit(int packageId, int id)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            bool isAgency = IsAgency(me);
            ViewBag.ActivePage = "Sessions";
            ViewBag.ActivePageGroup = isAgency ? "Agency" : "Guide";

            var pkg = FindOwnedPackageOr404(packageId);
            if (pkg == null) return HttpNotFound();

            var entity = db.Sessions.FirstOrDefault(s => s.SessionId == id && s.PackageId == packageId);
            if (entity == null) return HttpNotFound();

            var vm = new SessionFormVm
            {
                SessionId = entity.SessionId,
                PackageId = entity.PackageId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Capacity = entity.Capacity,
                IsCanceled = entity.IsCanceled,
                Notes = entity.Notes
            };

            ViewBag.Package = pkg;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken, Route("{id:int}/edit")]
        public ActionResult Edit(int packageId, int id, SessionFormVm vm)
        {
            var pkg = FindOwnedPackageOr404(packageId);
            if (pkg == null) return HttpNotFound();

            var entity = db.Sessions.FirstOrDefault(s => s.SessionId == id && s.PackageId == packageId);
            if (entity == null) return HttpNotFound();

            if (vm.EndDate < vm.StartDate)
                ModelState.AddModelError("EndDate", "End Date must be on or after Start Date.");

            if (!ModelState.IsValid)
            {
                var me = GetMe();
                ViewBag.ActivePage = "Sessions";
                ViewBag.ActivePageGroup = IsAgency(me) ? "Agency" : "Guide";
                ViewBag.Package = pkg;
                return View(vm);
            }

            entity.StartDate = vm.StartDate;
            entity.EndDate = vm.EndDate;
            entity.Capacity = vm.Capacity;
            entity.IsCanceled = vm.IsCanceled;
            entity.Notes = vm.Notes;

            db.SaveChanges();

            TempData["Success"] = "Session updated.";
            return RedirectToAction("Index");
        }

        // ========= Delete =========
        [HttpGet, Route("{id:int}/delete")]
        public ActionResult Delete(int packageId, int id)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            bool isAgency = IsAgency(me);
            ViewBag.ActivePage = "Sessions";
            ViewBag.ActivePageGroup = isAgency ? "Agency" : "Guide";

            var pkg = FindOwnedPackageOr404(packageId);
            if (pkg == null) return HttpNotFound();

            var entity = db.Sessions.FirstOrDefault(s => s.SessionId == id && s.PackageId == packageId);
            if (entity == null) return HttpNotFound();

            ViewBag.Package = pkg;
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken, ActionName("Delete"), Route("{id:int}/delete")]
        public ActionResult DeleteConfirmed(int packageId, int id)
        {
            var pkg = FindOwnedPackageOr404(packageId);
            if (pkg == null) return HttpNotFound();

            var entity = db.Sessions.FirstOrDefault(s => s.SessionId == id && s.PackageId == packageId);
            if (entity == null) return HttpNotFound();

            db.Sessions.Remove(entity);
            db.SaveChanges();

            TempData["Success"] = "Session deleted.";
            return RedirectToAction("Index");
        }
    }
}
