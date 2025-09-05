using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize(Roles = "Agency")]
    public class AgencyController : BaseController
    {
        // Helper: current user (+ role + profile)
        private User GetMe()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;

            return db.Users
                     .Include(u => u.Role)
                     .Include(u => u.AgencyProfile) // shared PK = UserId
                     .FirstOrDefault(u => u.Email == email);
        }

        // ========= PROFILE (Create/Update) =========

        [HttpGet]
        public ActionResult Profile()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            // Ensure an AgencyProfile row exists (was created at register; just in case)
            //var profile = db.AgencyProfiles
            //                .Include(p => p.User)
            //                .FirstOrDefault(p => p.UserId == me.UserId);

            var profile = db.AgencyProfiles
                    .Include(p => p.User)   // <— important
                    .FirstOrDefault(p => p.UserId == me.UserId);

            if (profile == null)
            {
                //profile = new AgencyProfile
                //{
                //    UserId = me.UserId,
                //    AgencyName = me.FullName,
                //    Status = "PendingVerification"
                //};

                profile = new AgencyProfile
                {
                    UserId = me.UserId,
                    AgencyName = me.FullName,   // temp default to satisfy [Required]
                    Status = "PendingVerification"
                };

                db.AgencyProfiles.Add(profile);
                db.SaveChanges();

                // reload with User included
                profile = db.AgencyProfiles
                            .Include(p => p.User)
                            .FirstOrDefault(p => p.UserId == me.UserId);
            }
            ViewBag.IsApproved = me.IsApproved; // if not already set by BaseController

            ViewBag.ActivePage = "Profile";
            ViewBag.ActivePageGroup = "Agency";
            return View(profile);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Profile(AgencyProfile form, string OwnerName)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var profile = db.AgencyProfiles
                            .Include(p => p.User)
                            .FirstOrDefault(p => p.UserId == me.UserId);

            if (profile == null)
            {
                profile = new AgencyProfile
                {
                    UserId = me.UserId,
                    Status = "PendingVerification"
                };
                db.AgencyProfiles.Add(profile);
            }

            // Validation
            if (string.IsNullOrWhiteSpace(form.AgencyName))
                ModelState.AddModelError("AgencyName", "Agency Name is required.");

            if (string.IsNullOrWhiteSpace(OwnerName))
                ModelState.AddModelError("OwnerName", "Owner / Contact Person is required.");

            if (!ModelState.IsValid)
            {
                profile.User = me;
                profile.AgencyName = form.AgencyName;
                profile.Description = form.Description;
                profile.Phone = form.Phone;
                profile.Website = form.Website;
                profile.VerificationDocPath = form.VerificationDocPath;

                ViewBag.ActivePage = "Profile";
                ViewBag.ActivePageGroup = "Agency";
                return View(profile);
            }

            // ✅ Update agency profile
            profile.AgencyName = form.AgencyName?.Trim();
            profile.Description = form.Description;
            profile.Phone = form.Phone;
            profile.Website = form.Website;
            profile.VerificationDocPath = form.VerificationDocPath;

            // ✅ Update owner name
            if (profile.User == null) profile.User = db.Users.Find(me.UserId);
            if (profile.User != null && !string.IsNullOrWhiteSpace(OwnerName))
                profile.User.FullName = OwnerName.Trim();

            db.SaveChanges();

            if (!me.IsApproved)
            {
                TempData["Success"] = "Profile saved. Your account is still pending admin approval.";
                return RedirectToAction("Profile");
            }

            TempData["Success"] = "Profile updated.";
            return RedirectToAction("Dashboard");
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ProfileOld(AgencyProfile form)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            // Load current persisted profile
            var profile = db.AgencyProfiles
                            .Include(p => p.User)
                            .FirstOrDefault(p => p.UserId == me.UserId);

            if (profile == null)
            {
                // Safety net: create if missing
                profile = new AgencyProfile
                {
                    UserId = me.UserId,
                    Status = "PendingVerification"
                };
                db.AgencyProfiles.Add(profile);
            }

            // Server-side validation (AgencyName required by model)
            if (string.IsNullOrWhiteSpace(form.AgencyName))
                ModelState.AddModelError("AgencyName", "Agency Name is required.");

            if (!ModelState.IsValid)
            {
                // Rebind the current user so the view can show Owner name
                profile.User = me;

                // Echo user's input back into the model so form preserves values
                profile.AgencyName = form.AgencyName;
                profile.Description = form.Description;
                profile.Phone = form.Phone;
                profile.Website = form.Website;
                profile.VerificationDocPath = form.VerificationDocPath;

                ViewBag.ActivePage = "Profile";
                ViewBag.ActivePageGroup = "Agency";
                return View(profile);
            }

            // Persist allowed changes
            profile.AgencyName = form.AgencyName?.Trim();
            profile.Description = form.Description;
            profile.Phone = form.Phone;
            profile.Website = form.Website;
            profile.VerificationDocPath = form.VerificationDocPath;

            // NOTE: Do not flip approval here. Admin will approve in Admin area.
            // profile.Status remains "PendingVerification" until approved.
            if (profile.User == null) profile.User = db.Users.Find(me.UserId);

            db.SaveChanges();

            if (!me.IsApproved)
            {
                TempData["Success"] = "Profile saved. Your account is still pending admin approval.";
                return RedirectToAction("Profile");   // stay on Profile while pending
            }

            TempData["Success"] = "Profile updated.";
            return RedirectToAction("Dashboard");
        }

        // ========= DASHBOARD =========

        [HttpGet]
        public ActionResult Dashboard()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            ViewBag.ActivePage = "AgencyDashboard";
            ViewBag.ActivePageGroup = "Agency";

            // Ensure profile exists
            if (me.AgencyProfile == null)
                return RedirectToAction("Profile");

            // Not approved yet? Show the "NotApproved" view you already have.
            if (!me.IsApproved)
                return View("NotApproved", model: me.AgencyProfile);

            var today = DateTime.Today;
            var myAgencyKey = me.UserId; // shared PK pattern

            // Owned entities
            var myPackages = db.TourPackages.Where(p => p.AgencyId == myAgencyKey);

            var mySessions = db.Sessions
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

            // Next sessions
            var nextSessions = mySessions
                .Where(s => DbFunctions.TruncateTime(s.StartDate) >= today)
                .OrderBy(s => s.StartDate)
                .Take(5)
                .Select(s => new UpcomingSessionItem
                {
                    SessionId = s.SessionId,
                    PackageTitle = s.Package.Title,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Capacity = s.Capacity,
                    Booked = db.Bookings
                                    .Where(b => b.SessionId == s.SessionId)
                                    .Select(b => (int?)b.Participants)
                                    .DefaultIfEmpty(0)
                                    .Sum() ?? 0
                })
                .ToList();

            // Recent bookings
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
                    CustomerName = b.CustomerName,
                    IsApproved = b.IsApproved,
                    Amount = (b.Session.Package.Price * b.Participants)
                })
                .ToList();

            // Recent feedback
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

        // ========= BOOKINGS LIST =========

        [HttpGet]
        public ActionResult Bookings()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var myAgencyKey = me.UserId;

            var bookings = db.Bookings
                             .Include(b => b.Session)
                             .Include(b => b.Session.Package)
                             .Where(b => b.Session.Package.AgencyId == myAgencyKey)
                             .OrderByDescending(b => b.CreatedAt)
                             .Select(b => new BookingViewModel
                             {
                                 BookingId = b.BookingId,
                                 PackageTitle = b.Session.Package.Title,
                                 CustomerName = b.CustomerName,
                                 StartDate = b.Session.StartDate,
                                 Participants = b.Participants,
                                 PaymentStatus = b.PaymentStatus,
                                 Amount = (b.Session.Package.Price * b.Participants),
                                 IsApproved = b.IsApproved
                             })
                             .ToList();

            ViewBag.ActivePage = "AgencyBookings";
            ViewBag.ActivePageGroup = "Agency";
            return View(bookings);
        }

        // ========= BOOKING APPROVALS =========

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ApproveBooking(int bookingId)
        {
            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null)
                return Json(new { ok = false, error = "Booking not found" });

            booking.IsApproved = true;
            db.SaveChanges();

            return Json(new { ok = true, status = "Approved" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RejectBooking(int bookingId)
        {
            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null)
                return Json(new { ok = false, error = "Booking not found" });

            booking.IsApproved = false;
            db.SaveChanges();

            return Json(new { ok = true, status = "Rejected" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult TogglePayment(int bookingId)
        {
            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null)
                return Json(new { ok = false, error = "Booking not found" });

            booking.PaymentStatus =
                string.Equals(booking.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                    ? "Pending" : "Paid";

            db.SaveChanges();

            return Json(new { ok = true, status = booking.PaymentStatus });
        }


        [Authorize(Roles = "Agency")]
        public ActionResult Feedback()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var myAgencyKey = me.UserId;

            var feedback = db.Feedbacks
                             .Include(f => f.Booking.Session.Package)   // include related entities
                             .Where(f => f.Booking.Session.Package.AgencyId == myAgencyKey)
                             .OrderByDescending(f => f.CreatedAt)
                             .Select(f => new FeedbackViewModel
                             {
                                 FeedbackId = f.FeedbackId,
                                 CustomerName = f.Booking.CustomerName,      // from Booking
                                 PackageTitle = f.Booking.Session.Package.Title, // from Package
                                 Rating = f.Rating,
                                 Comment = f.Comment,
                                 CreatedAt = f.CreatedAt
                             })
                             .ToList();

            ViewBag.ActivePage = "AgencyFeedback";
            ViewBag.ActivePageGroup = "Agency";
            return View(feedback);
        }





    }
}
