using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize(Roles = "Agency,Guide")]
    public class ProviderController : BaseController
    {
        // Helper to fetch current logged-in user + profiles
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

        // ========= PROFILE =========
        [HttpGet]
        public ActionResult Profile()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            bool isAgency = IsAgency(me);
            bool isGuide = IsGuide(me);

            object profile;

            if (isAgency)
            {
                profile = me.AgencyProfile ?? CreateDefaultAgencyProfile(me);
                ViewBag.IsAgency = true;
                ViewBag.IsGuide = false;
                ViewBag.RoleName = "Agency";
                ViewBag.ActivePageGroup = "Agency";
                ViewBag.ActivePage = "AgencyProfile";
            }
            else if (isGuide)
            {
                profile = me.GuideProfile ?? CreateDefaultGuideProfile(me);
                ViewBag.IsAgency = false;
                ViewBag.IsGuide = true;
                ViewBag.RoleName = "Guide";
                ViewBag.ActivePageGroup = "Guide";
                ViewBag.ActivePage = "GuideProfile";
            }
            else
            {
                // fallback: if neither agency nor guide, redirect
                return RedirectToAction("Dashboard", "Tourist");
            }

            ViewBag.IsApproved = me.IsApproved;

            // Profile.cshtml is shared, dynamic model will be AgencyProfile OR GuideProfile
            return View("Profile", profile);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Profile(FormCollection form)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            bool isAgency = IsAgency(me);

            if (isAgency)
            {
                var profile = db.AgencyProfiles.Include(p => p.User)
                                .FirstOrDefault(p => p.UserId == me.UserId)
                             ?? CreateDefaultAgencyProfile(me);

                // Map values from form
                profile.AgencyName = form["AgencyName"]?.Trim();
                profile.Description = form["Description"]?.Trim();
                profile.Phone = form["Phone"]?.Trim();
                profile.Website = form["Website"]?.Trim();
                profile.VerificationDocPath = form["VerificationDocPath"]?.Trim();

                // Update account owner name
                var ownerName = form["OwnerName"]?.Trim();
                if (!string.IsNullOrEmpty(ownerName) && profile.User != null)
                    profile.User.FullName = ownerName;

                db.SaveChanges();
                TempData["Success"] = me.IsApproved ? "Agency profile updated." : "Saved. Awaiting admin approval.";
            }
            else // Guide
            {
                var profile = db.GuideProfiles.Include(p => p.User)
                                .FirstOrDefault(p => p.UserId == me.UserId)
                             ?? CreateDefaultGuideProfile(me);

                // Map values from form
                profile.FullNameOnLicense = form["FullNameOnLicense"]?.Trim();
                profile.Bio = form["Bio"]?.Trim();
                profile.Phone = form["Phone"]?.Trim();
                profile.VerificationDocPath = form["VerificationDocPath"]?.Trim();

                // Update account owner name
                var ownerName = form["OwnerName"]?.Trim();
                if (!string.IsNullOrEmpty(ownerName) && profile.User != null)
                    profile.User.FullName = ownerName;

                db.SaveChanges();
                TempData["Success"] = me.IsApproved ? "Guide profile updated." : "Saved. Awaiting admin approval.";
            }

            return RedirectToAction("Profile");
        }


        private AgencyProfile CreateDefaultAgencyProfile(User me)
        {
            var profile = new AgencyProfile
            {
                UserId = me.UserId,
                AgencyName = me.FullName,
                Status = "PendingVerification"
            };
            db.AgencyProfiles.Add(profile);
            db.SaveChanges();
            return profile;
        }

        private GuideProfile CreateDefaultGuideProfile(User me)
        {
            var profile = new GuideProfile
            {
                UserId = me.UserId,
                FullNameOnLicense = me.FullName,
                Status = "PendingVerification"
            };
            db.GuideProfiles.Add(profile);
            db.SaveChanges();
            return profile;
        }

        // ========= DASHBOARD =========
        [HttpGet]
        public ActionResult Dashboard()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var providerId = me.UserId;
            var today = DateTime.Today;

            var myPackages = db.TourPackages.Where(p => p.AgencyId == providerId);
            var mySessions = db.Sessions.Include(s => s.Package)
                               .Where(s => s.Package.AgencyId == providerId);
            var myBookings = db.Bookings.Include(b => b.Session.Package)
                               .Where(b => b.Session.Package.AgencyId == providerId);

            var totalPackages = myPackages.Count();
            var upcomingSessions = mySessions.Count(s => DbFunctions.TruncateTime(s.StartDate) >= today);
            var totalBookings = myBookings.Count();
            var paidRevenue = myBookings.Where(b => b.PaymentStatus == "Paid")
                .Select(b => (decimal?)(b.Session.Package.Price * b.Participants))
                .DefaultIfEmpty(0m).Sum() ?? 0m;

            var pendingPayments = myBookings.Count(b => b.PaymentStatus != "Paid");

            var nextSessions = mySessions
                .Where(s => DbFunctions.TruncateTime(s.StartDate) >= today)
                .OrderBy(s => s.StartDate).Take(5)
                .Select(s => new UpcomingSessionItem
                {
                    SessionId = s.SessionId,
                    PackageTitle = s.Package.Title,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Capacity = s.Capacity,
                    Booked = db.Bookings.Where(b => b.SessionId == s.SessionId)
                              .Select(b => (int?)b.Participants)
                              .DefaultIfEmpty(0).Sum() ?? 0
                }).ToList();

            var recentBookings = myBookings.OrderByDescending(b => b.CreatedAt)
                .Take(5).Select(b => new RecentBookingItem
                {
                    BookingId = b.BookingId,
                    PackageTitle = b.Session.Package.Title,
                    StartDate = b.Session.StartDate,
                    Participants = b.Participants,
                    PaymentStatus = b.PaymentStatus,
                    CustomerName = b.CustomerName,
                    IsApproved = b.IsApproved,
                    Amount = (b.Session.Package.Price * b.Participants)
                }).ToList();

            var recentFeedback = db.Feedbacks.Include(f => f.Booking.Session.Package)
                .Where(f => f.Booking.Session.Package.AgencyId == providerId)
                .OrderByDescending(f => f.CreatedAt).Take(5)
                .Select(f => new RecentFeedbackItem
                {
                    FeedbackId = f.FeedbackId,
                    PackageTitle = f.Booking.Session.Package.Title,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt
                }).ToList();

            var vm = new AgencyDashboardVm
            {
                AgencyName = ViewBag.IsAgency
                    ? me.AgencyProfile?.AgencyName ?? me.FullName
                    : me.GuideProfile?.FullNameOnLicense ?? me.FullName,
                IsApproved = me.IsApproved,
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
            ViewBag.ActivePageGroup = ViewBag.RoleName;
            //ViewBag.ActivePage = "AgencyDashboard"; // reuse Agency/Dashboard.cshtml
            return View("Dashboard", vm);
        }

        // ========= BOOKINGS =========
        [HttpGet]
        public ActionResult Bookings()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var providerId = me.UserId;
            var bookings = db.Bookings.Include(b => b.Session.Package)
                             .Where(b => b.Session.Package.AgencyId == providerId)
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
                             }).ToList();

            ViewBag.ActivePage = "AgencyBookings";
            return View("Bookings", bookings); // reuse Agency/Bookings.cshtml
        }

        // ========= FEEDBACK =========
        public ActionResult Feedback()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var providerId = me.UserId;
            var feedback = db.Feedbacks.Include(f => f.Booking.Session.Package)
                             .Where(f => f.Booking.Session.Package.AgencyId == providerId)
                             .OrderByDescending(f => f.CreatedAt)
                             .Select(f => new FeedbackViewModel
                             {
                                 FeedbackId = f.FeedbackId,
                                 CustomerName = f.Booking.CustomerName,
                                 PackageTitle = f.Booking.Session.Package.Title,
                                 Rating = f.Rating,
                                 Comment = f.Comment,
                                 CreatedAt = f.CreatedAt
                             }).ToList();

            ViewBag.ActivePage = "AgencyFeedback";
            return View("Feedback", feedback); // reuse Agency/Feedback.cshtml
        }

        private bool IsAgency(User me) => me.Role.RoleName == "Agency";
        private bool IsGuide(User me) => me.Role.RoleName == "Guide";

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateProfileFromNotApproved(GuideProfile guideForm, AgencyProfile agencyForm)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            if (IsGuide(me))
            {
                var profile = db.GuideProfiles.FirstOrDefault(p => p.UserId == me.UserId);
                if (profile != null)
                {
                    profile.Bio = guideForm.Bio;
                    profile.Phone = guideForm.Phone;
                    profile.VerificationDocPath = guideForm.VerificationDocPath;
                    db.SaveChanges();
                    TempData["Success"] = "Profile updated. Awaiting admin approval.";
                }
            }
            else if (IsAgency(me))
            {
                var profile = db.AgencyProfiles.FirstOrDefault(p => p.UserId == me.UserId);
                if (profile != null)
                {
                    profile.Description = agencyForm.Description;
                    profile.Phone = agencyForm.Phone;
                    profile.Website = agencyForm.Website;
                    profile.VerificationDocPath = agencyForm.VerificationDocPath;
                    db.SaveChanges();
                    TempData["Success"] = "Profile updated. Awaiting admin approval.";
                }
            }

            return RedirectToAction("Profile"); // back to profile check
        }

    }
}
