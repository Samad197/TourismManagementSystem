using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize(Roles = "Agency")]
    [RoutePrefix("agency/sessions")]
    public class PackageSessionsController : BaseController
    {
     
        private User GetMe()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;

            return db.Users
                     .Include(u => u.Role)
                     .Include(u => u.AgencyProfile)
                     .FirstOrDefault(u => u.Email == email);
        }

        private TourPackage FindOwnedPackageOr404(int packageId)
        {
            var me = GetMe();
            if (me == null) return null;

            var myKey = me.UserId; // shared PK (AgencyProfile PK == UserId)
            return db.TourPackages
                     .Include(p => p.Agency.User)
                     .FirstOrDefault(p => p.PackageId == packageId && p.AgencyId == myKey);
        }

        // ========= Canonical routes =========

        // GET /agency/sessions
        [HttpGet, Route("")]
        public ActionResult Index(int page = 1, int pageSize = 10)
        {
            ViewBag.ActivePage = "Sessions";
            ViewBag.ActivePageGroup = "Agency";

            var me = db.Users.Include(u => u.AgencyProfile)
                             .FirstOrDefault(u => u.Email == User.Identity.Name);
            if (me == null || me.AgencyProfile == null)
                return HttpNotFound();

            var agencyId = me.UserId; // AgencyProfile PK == UserId

            var query = db.Sessions
                          .Include(s => s.Package)
                          .Where(s => s.Package.AgencyId == agencyId)
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


        // GET /agency/sessions/{packageId}/create
        [HttpGet, Route("{packageId:int}/create")]
        public ActionResult Create(int packageId)
        {
            ViewBag.ActivePage = "Sessions";            // CHANGED
            ViewBag.ActivePageGroup = "Agency";
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

        // POST /agency/sessions/{packageId}/create
        [HttpPost, ValidateAntiForgeryToken, Route("{packageId:int}/create")]
        public ActionResult Create(SessionFormVm vm)
        {
            var pkg = FindOwnedPackageOr404(vm.PackageId);
            if (pkg == null) return HttpNotFound();

            if (vm.EndDate < vm.StartDate)
                ModelState.AddModelError("EndDate", "End Date must be on or after Start Date.");

            if (!ModelState.IsValid)
            {
                ViewBag.ActivePage = "Sessions";        // CHANGED (so tab stays highlighted on errors)
                ViewBag.ActivePageGroup = "Agency";
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
            return RedirectToAction("Index", new { packageId = vm.PackageId });
        }

        // GET /agency/packages/{packageId}/sessions/{id}/edit
        [HttpGet, Route("{id:int}/edit")]
        public ActionResult Edit(int packageId, int id)
        {
            ViewBag.ActivePage = "Sessions";        // CHANGED (so tab stays highlighted on errors)
            ViewBag.ActivePageGroup = "Agency";
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

        // POST /agency/packages/{packageId}/sessions/{id}/edit
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
                ViewBag.ActivePage = "Sessions";        // CHANGED (so tab stays highlighted on errors)
                ViewBag.ActivePageGroup = "Agency";
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
            return RedirectToAction("Index", new { packageId });
        }

        // GET /agency/packages/{packageId}/sessions/{id}/delete
        [HttpGet, Route("{id:int}/delete")]
        public ActionResult Delete(int packageId, int id)
        {
            ViewBag.ActivePage = "Sessions";        // CHANGED (so tab stays highlighted on errors)
            ViewBag.ActivePageGroup = "Agency";
            var pkg = FindOwnedPackageOr404(packageId);
            if (pkg == null) return HttpNotFound();

            var entity = db.Sessions.FirstOrDefault(s => s.SessionId == id && s.PackageId == packageId);
            if (entity == null) return HttpNotFound();

            ViewBag.Package = pkg;
            return View(entity);
        }

        // POST /agency/packages/{packageId}/sessions/{id}/delete
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
            return RedirectToAction("Index", new { packageId });
        }

        // ========= Legacy top-menu route (no packageId in URL) =========
        // /Agency/Sessions  → auto-pick first owned package and redirect
        //[HttpGet, Route("~/Agency/Sessions", Name = "AgencySessionsLegacy")]
        //public ActionResult LegacyIndex()
        //{
        //    var me = GetMe();
        //    if (me == null) return RedirectToAction("Login", "Account");

        //    var myKey = me.UserId;
        //    var firstPkgId = db.TourPackages
        //                       .Where(p => p.AgencyId == myKey)
        //                       .OrderByDescending(p => p.CreatedAt)
        //                       .Select(p => p.PackageId)
        //                       .FirstOrDefault();

        //    if (firstPkgId == 0)
        //        return RedirectToAction("Index", "Package"); // no packages yet

        //    return RedirectToAction("Index", new { packageId = firstPkgId });
        //}
    }
}
