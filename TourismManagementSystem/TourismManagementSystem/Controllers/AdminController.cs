using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourismManagementSystem.Data;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    public class AdminController : BaseController
    {
        private readonly TourismDbContext db = new TourismDbContext();

        // helper: check current user is admin
        private bool EnsureAdmin(out User me)
        {
            me = null;
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return false;

            me = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == email);
            return me != null && string.Equals(me.Role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
        }


        [Authorize]
        public ActionResult Dashboard()
        {
            if (!EnsureAdmin(out _)) return RedirectToAction("Index", "Home");

            var pendingAgencies = db.Users.Count(u => u.Role.RoleName == "Agency" && !u.IsApproved);
            var pendingGuides = db.Users.Count(u => u.Role.RoleName == "Guide" && !u.IsApproved);
            var totalUsers = db.Users.Count();
            var totalBookings = db.Bookings.Count();
            var paidRevenue = db.Bookings
                                    .Where(b => b.PaymentStatus == "Paid")
                                    .Select(b => (decimal?)b.Session.Package.Price * b.Participants)
                                    .DefaultIfEmpty(0)
                                    .Sum() ?? 0;

            var vm = new AdminDashboardVm
            {
                PendingAgencies = pendingAgencies,
                PendingGuides = pendingGuides,
                TotalUsers = totalUsers,
                TotalBookings = totalBookings,
                PaidRevenue = paidRevenue
            };

            ViewBag.ActivePage = "Dashboard";
            return View(vm);
        }

        //[Authorize]
        //[HttpGet]
        [Authorize]
        [HttpGet]
        public ActionResult Approvals()
        {
            if (!EnsureAdmin(out _)) return RedirectToAction("Index", "Home");

            var pending = db.Users
                .Include(u => u.Role)
                .Where(u => (u.Role.RoleName == "Agency" || u.Role.RoleName == "Guide") && !u.IsApproved)
                .Select(u => new PendingUserVm
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    RoleName = u.Role.RoleName,
                    ProfileStatus = (u.Role.RoleName == "Agency")
                        ? db.AgencyProfiles.Where(p => p.UserId == u.UserId).Select(p => p.Status).FirstOrDefault()
                        : db.GuideProfiles.Where(p => p.UserId == u.UserId).Select(p => p.Status).FirstOrDefault()
                })
                .OrderBy(p => p.RoleName).ThenBy(p => p.FullName)
                .ToList();

            ViewBag.ActivePage = "Approvals";
            return View(pending);
        }

        //[Authorize, HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            if (!EnsureAdmin(out _)) return RedirectToAction("Index", "Home");

            var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.UserId == id);
            if (user == null) return HttpNotFound();

            user.IsApproved = true;

            if (user.Role.RoleName == "Agency")
            {
                var prof = db.AgencyProfiles.FirstOrDefault(p => p.UserId == user.UserId);
                if (prof != null) prof.Status = "Approved";
            }
            else if (user.Role.RoleName == "Guide")
            {
                var prof = db.GuideProfiles.FirstOrDefault(p => p.UserId == user.UserId);
                if (prof != null) prof.Status = "Approved";
            }

            db.SaveChanges();
            TempData["Success"] = $"{user.FullName} approved.";
            return RedirectToAction("Approvals");
        }

       
        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Reject(int id)
        {
            if (!EnsureAdmin(out _)) return RedirectToAction("Index", "Home");

            var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.UserId == id);
            if (user == null) return HttpNotFound();

            user.IsApproved = false;    // stays pending/rejected; you can also deactivate:
            // user.IsActive = false;

            if (user.Role.RoleName == "Agency")
            {
                var prof = db.AgencyProfiles.FirstOrDefault(p => p.UserId == user.UserId);
                if (prof != null) prof.Status = "PendingVerification";
            }
            else if (user.Role.RoleName == "Guide")
            {
                var prof = db.GuideProfiles.FirstOrDefault(p => p.UserId == user.UserId);
                if (prof != null) prof.Status = "PendingVerification";
            }

            db.SaveChanges();
            TempData["Info"] = $"{user.FullName} remains pending / rejected.";
            return RedirectToAction("Approvals");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Users()
        {
            if (!EnsureAdmin(out _)) return RedirectToAction("Index", "Home");

            var users = db.Users.Include(u => u.Role)
                .OrderBy(u => u.Role.RoleName).ThenBy(u => u.FullName)
                .Select(u => new AdminUserListItemVm
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    RoleName = u.Role.RoleName,
                    IsActive = u.IsActive,
                    IsApproved = u.IsApproved,
                    CreatedAt = u.CreatedAt
                })
                .ToList();

            ViewBag.ActivePage = "Users";
            return View(users);
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ToggleActive(int id)
        {
            if (!EnsureAdmin(out _)) return RedirectToAction("Index", "Home");

            var meEmail = User?.Identity?.Name;
            var u = db.Users.Include(x => x.Role).FirstOrDefault(x => x.UserId == id);
            if (u == null) return HttpNotFound();

            if (string.Equals(u.Email, meEmail, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "You can’t toggle active for admin accounts or yourself.";
                return RedirectToAction("Users");
            }

            u.IsActive = !u.IsActive;
            db.SaveChanges();
            TempData["Success"] = $"{u.FullName} active = {u.IsActive}.";
            return RedirectToAction("Users");
        }


        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ToggleApproved(int id)
        {
            if (!EnsureAdmin(out _)) return RedirectToAction("Index", "Home");

            var u = db.Users.Include(x => x.Role).FirstOrDefault(x => x.UserId == id);
            if (u == null) return HttpNotFound();

            if (u.Role.RoleName != "Agency" && u.Role.RoleName != "Guide")
                return new HttpStatusCodeResult(400, "Only Agency/Guide can be approved.");

            u.IsApproved = !u.IsApproved;

            if (u.Role.RoleName == "Agency")
            {
                var prof = db.AgencyProfiles.FirstOrDefault(p => p.UserId == u.UserId);
                if (prof != null) prof.Status = u.IsApproved ? "Approved" : "PendingVerification";
            }
            else // Guide
            {
                var prof = db.GuideProfiles.FirstOrDefault(p => p.UserId == u.UserId);
                if (prof != null) prof.Status = u.IsApproved ? "Approved" : "PendingVerification";
            }

            db.SaveChanges();
            TempData["Success"] = $"{u.FullName} approved = {u.IsApproved}.";
            return RedirectToAction("Users");
        }

    }
}
