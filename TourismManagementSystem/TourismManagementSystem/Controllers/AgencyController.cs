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
    public class AgencyController : BaseController
    {
        // Helper to load the current user with role & profile
        private User GetMe()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;

            return db.Users
                     .Include(u => u.Role)
                     .Include(u => u.AgencyProfile) // shared PK (UserId)
                     .FirstOrDefault(u => u.Email == email);
        }

        // ==== Profile (Create/Update) ====
        [HttpGet]
        public ActionResult Profile()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            // Create stub if missing so Dashboard never hits a null
            var profile = db.AgencyProfiles.FirstOrDefault(p => p.UserId == me.UserId);
            if (profile == null)
            {
                profile = new AgencyProfile
                {
                    UserId = me.UserId,            // PK = FK to User
                    AgencyName = me.FullName,          // sensible default
                    Status = "PendingVerification"
                };
                db.AgencyProfiles.Add(profile);
                db.SaveChanges();
            }

            ViewBag.ActivePage = "Profile";
            ViewBag.ActivePageGroup = "Agency";
            return View(profile);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Profile(AgencyProfile form)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(form);

            var profile = db.AgencyProfiles.FirstOrDefault(p => p.UserId == me.UserId);
            if (profile == null) return RedirectToAction("Profile"); // safety

            // Update allowed fields
            profile.AgencyName = form.AgencyName;
            profile.Description = form.Description;
            profile.Phone = form.Phone;
            profile.Website = form.Website;
            profile.VerificationDocPath = form.VerificationDocPath;

            db.SaveChanges();
            TempData["Success"] = "Agency profile saved. Awaiting admin approval.";
            return RedirectToAction("Dashboard");
        }

        // ==== Dashboard ====
        [HttpGet]
        public ActionResult Dashboard()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            ViewBag.ActivePage = "Dashboard";
            ViewBag.ActivePageGroup = "Agency";

            // If no profile yet, force them to complete it first
            if (me.AgencyProfile == null)
                return RedirectToAction("Profile");

            // If not approved yet, show your Pending screen
            if (!me.IsApproved)
                return View("NotApproved", model: me.AgencyProfile);

            var today = DateTime.Today;

            // Shared-PK pattern: AgencyProfile key == UserId
            var myAgencyKey = me.UserId;

            // Owned entities
            var myPackages = db.TourPackages
                               .Where(p => p.AgencyId == myAgencyKey);

            var mySessions = db.TourSessions
                               .Include(s => s.Package)
                               .Where(s => s.Package.AgencyId == myAgencyKey);

            var myBookings = db.Bookings
                               .Include(b => b.Session.Package)
                               .Where(b => b.Session.Package.AgencyId == myAgencyKey);

            // KPIs
            var totalPackages = myPackages.Count();
            var upcomingSessions = mySessions.Count(s => DbFunctions.TruncateTime(s.StartDate) >= today);
            var totalBookings = myBookings.Count();

            var paidRevenue = myBookings
                .Where(b => b.PaymentStatus == "Paid")
                .Select(b => (decimal?)(b.Session.Package.Price * b.Participants))
                .DefaultIfEmpty(0m)
                .Sum() ?? 0m;

            var pendingPayments = myBookings.Count(b => b.PaymentStatus != "Paid");

            // Lists
            var nextSessions = mySessions
                .Where(s => DbFunctions.TruncateTime(s.StartDate) >= today)
                .OrderBy(s => s.StartDate)
                .Take(5)
                .Select(s => new UpcomingSessionItem
                {
                    SessionId = s.SessionId,
                    PackageTitle = s.Package.Title,
                    StartDate = s.StartDate,
                    // your model uses AvailableSlots; map to VM's Capacity
                    Capacity = s.AvailableSlots,
                    Booked = db.Bookings
                                     .Where(b => b.SessionId == s.SessionId)
                                     .Select(b => (int?)b.Participants)
                                     .DefaultIfEmpty(0)
                                     .Sum() ?? 0
                })
                .ToList();

            var recentBookings = myBookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new RecentBookingItem
                {
                    BookingId = b.BookingId,
                    PackageTitle = b.Session.Package.Title,
                    StartDate = b.Session.StartDate,
                    Participants = b.Participants,
                    PaymentStatus = b.PaymentStatus,
                    Amount = (b.Session.Package.Price * b.Participants)
                })
                .ToList();

            var recentFeedback = db.Feedbacks
                .Include(f => f.Booking.Session.Package)
                .Where(f => f.Booking.Session.Package.AgencyId == myAgencyKey)
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .Select(f => new RecentFeedbackItem
                {
                    FeedbackId = f.FeedbackId,
                    PackageTitle = f.Booking.Session.Package.Title,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt
                })
                .ToList();

            var vm = new AgencyDashboardVm
            {
                AgencyName = me.AgencyProfile.AgencyName ?? me.FullName,
                IsApproved = true,
                TotalPackages = totalPackages,
                UpcomingSessions = upcomingSessions,
                TotalBookings = totalBookings,
                PaidRevenue = paidRevenue,
                PendingPayments = pendingPayments,
                FeedbackCount = recentFeedback.Count,
                NextSessions = nextSessions,
                RecentBookings = recentBookings,
                RecentFeedback = recentFeedback
            };

            return View(vm);
        }
    }
}
