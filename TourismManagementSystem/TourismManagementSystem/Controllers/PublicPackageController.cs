using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;
using TourismManagementSystem.Data;
using static TourismManagementSystem.Models.ViewModels.PublicPackageDetailsVm;
using System.Data.Entity.Infrastructure;

namespace TourismManagementSystem.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("packages")]
    public class PublicPackageController : Controller
    {
        private readonly TourismDbContext db = new TourismDbContext();

        // GET: /packages
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

            // Filter: Only upcoming packages if upcomingOnly is true
            if (upcomingOnly)
            {
                query = query.Where(p =>
                    (p.Sessions.Any(s => DbFunctions.TruncateTime(s.StartDate) >= today))
                );
            }

            // Search filter: matching title or description
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Title.Contains(q) || p.Description.Contains(q));

            // Filters: Price & Duration
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
                    ThumbnailPath = p.Images.Any() ? p.Images.FirstOrDefault().ImagePath : "/images/placeholder.jpg",
                    OwnerType = p.AgencyId != null ? "Agency" : "Guide",
                    OwnerName = p.AgencyId != null ? p.Agency.User.FullName : p.Guide.User.FullName,
                    HasUpcoming = p.Sessions.Any(s => DbFunctions.TruncateTime(s.StartDate) >= today)
                })
                .ToList();

            return View(packages); // Views/PublicPackage/Index.cshtml
        }

        // GET: /packages/details/{id}
        [HttpGet, Route("details/{id:int}")]
        public ActionResult Details(int id)
        {
            // Load package with related data
            var p = db.TourPackages
                .Include(x => x.Images)
                .Include(x => x.Sessions.Select(s => s.Bookings))
                .Include(x => x.Agency.User)
                .Include(x => x.Guide.User)
                .Include(x => x.Reviews.Select(r => r.Tourist))
                .FirstOrDefault(x => x.PackageId == id);

            if (p == null) return HttpNotFound();

            // Hide details if owner not approved/active
            bool ownerApproved =
                (p.AgencyId != null && p.Agency.User.IsApproved && p.Agency.User.IsActive) ||
                (p.GuideId != null && p.Guide.User.IsApproved && p.Guide.User.IsActive);
            if (!ownerApproved) return HttpNotFound();

            // Get current user ID (Tourist)
            int currentUserId = GetCurrentTouristId(); // implement this method according to your auth

            // Check if user already has a booking for this package
            var existingBooking = p.Sessions
                .SelectMany(s => s.Bookings)
                .FirstOrDefault(b => b.TouristId == currentUserId);

            // Map to ViewModel
            var vm = new PublicPackageDetailsVm
            {
                PackageId = p.PackageId,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                MaxGroupSize = p.MaxGroupSize,
                OwnerName = p.AgencyId != null ? p.Agency.AgencyName : p.Guide.FullNameOnLicense,
                OwnerType = p.AgencyId != null ? "Agency" : "Guide",
                HeroImagePath = p.Images.FirstOrDefault()?.ImagePath,
                Gallery = p.Images.Skip(1).Select(i => i.ImagePath).ToList(),
                UpcomingSessions = p.Sessions.Select(s => new UpcomingSessionItem
                {
                    SessionId = s.SessionId,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Capacity = s.Capacity,
                    Booked = s.Bookings.Sum(b => b.Participants)
                }).ToList(),
                Reviews = p.Reviews.Select(r => new ReviewItem
                {
                    TouristName = r.Tourist.FullName,
                    Rating = r.Rating,
                    Comment = r.Comment
                }).ToList(),
                AvgRating = p.Reviews.Any() ? (double?)p.Reviews.Average(r => r.Rating) : null,

                ExistingBooking = existingBooking // NEW: send existing booking to view
            };

            return View(vm);
        }


        // GET: /packages/book?packageId=2&sessionId=2
        [HttpGet, Route("book")]
        public ActionResult Book(int packageId, int? sessionId)
        {
            var package = db.TourPackages
                .Include(p => p.Sessions)
                .FirstOrDefault(p => p.PackageId == packageId);

            if (package == null) return HttpNotFound();

            var upcomingSessions = package.Sessions
                .Where(s => s.StartDate >= DateTime.Today)
              .Select(s => new UpcomingSessionItem
              {
                    SessionId = s.SessionId,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Capacity = s.Capacity,
                    Booked = s.Bookings.Sum(b => b.Participants)
                }).ToList();

            var vm = new PackageBookingVm
            {
                PackageId = packageId,
                PackageTitle = package.Title,
                Participants = 1, // default
                SelectedSessionId = sessionId ?? upcomingSessions.FirstOrDefault()?.SessionId ?? 0,
                UpcomingSessions = upcomingSessions
            };

            return View(vm);
        }

        // POST: /packages/book
        [HttpPost, ValidateAntiForgeryToken, Route("book")]
        public ActionResult Book(PackageBookingVm model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Make sure the session exists in the database
            var session = db.Sessions
                .Include(s => s.Bookings)
                .FirstOrDefault(s => s.SessionId == model.SelectedSessionId && !s.IsCanceled);

            if (session == null)
            {
                // Show a user-friendly error instead of FK crash
                ModelState.AddModelError("", "The selected session does not exist or has been canceled.");
                return View(model);
            }

            // Calculate available seats
            int availableSeats = session.Capacity - session.Bookings.Sum(b => b.Participants);
            if (model.Participants > availableSeats)
            {
                ModelState.AddModelError("", $"Only {availableSeats} seat(s) are available for this session.");
                return View(model);
            }

            // Create the booking
            var booking = new Booking
            {
                TouristId = GetCurrentTouristId(), // method to get logged-in tourist
                SessionId = session.SessionId,
                Participants = model.Participants,
                Status = "Pending",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                // Handle FK errors explicitly
                ModelState.AddModelError("", "An error occurred while saving the booking. Please try again.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Booking created successfully!";
            return RedirectToAction("Details", new { id = session.PackageId });
        }


        // GET: /packages/booking/edit/{id}
        [HttpGet, Route("booking/edit/{id:int}")]
        public ActionResult EditBooking(int id)
        {
            var booking = db.Bookings
                .Include(b => b.Session.Package.Sessions)
                .Include(b => b.Session.Bookings)
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null) return HttpNotFound();

            var vm = new PackageBookingVm
            {
                BookingId = booking.BookingId,
                PackageId = booking.Session.PackageId,
                PackageTitle = booking.Session.Package.Title,
                Participants = booking.Participants,
                SelectedSessionId = booking.SessionId,
                UpcomingSessions = booking.Session.Package.Sessions
    .Where(s => s.StartDate >= DateTime.Today)
    .Select(s => new UpcomingSessionItem
    {
        SessionId = s.SessionId,
        StartDate = s.StartDate,
        EndDate = s.EndDate,
        Capacity = s.Capacity,
        Booked = s.Bookings.Sum(b => b.Participants)
    }).ToList()

        };

            return View(vm);
        }

        // POST: /packages/booking/edit
        [HttpPost, ValidateAntiForgeryToken, Route("booking/edit")]
        public ActionResult EditBooking(PackageBookingVm model)
        {
            if (!ModelState.IsValid) return View(model);

            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == model.BookingId);
            if (booking == null) return HttpNotFound();

            var session = db.Sessions
                .Include(s => s.Bookings)
                .FirstOrDefault(s => s.SessionId == model.SelectedSessionId);

            int availableSeats = session.Capacity - session.Bookings
                .Where(b => b.BookingId != model.BookingId)
                .Sum(b => b.Participants);

            if (model.Participants > availableSeats)
            {
                ModelState.AddModelError("", $"Only {availableSeats} seats are available for this session.");
                return View(model);
            }

            booking.SessionId = model.SelectedSessionId;
            booking.Participants = model.Participants;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking updated successfully!";
            return RedirectToAction("Details", new { id = booking.Session.PackageId });
        }

        // POST: /packages/booking/cancel/{id}
        [HttpPost, ValidateAntiForgeryToken, Route("booking/cancel/{id:int}")]
        public ActionResult CancelBooking(int id)
        {
            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == id);
            if (booking == null) return HttpNotFound();

            booking.Status = "Cancelled";
            booking.PaymentStatus = "Refunded"; // Optional
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking cancelled successfully!";
            return RedirectToAction("Details", new { id = booking.Session.PackageId });
        }

        [HttpGet, Route("book/confirm")]
        public ActionResult ConfirmBooking(PackageBookingVm model)
        {
            if (model.SelectedSessionId == 0) return HttpNotFound();

            // Map selected session details
            var session = db.Sessions.Include(s => s.Package).FirstOrDefault(s => s.SessionId == model.SelectedSessionId);
            if (session == null) return HttpNotFound();

            model.PackageTitle = session.Package.Title;
            model.SelectedSessionDate = session.StartDate;
            model.SelectedSessionEnd = session.EndDate;

            return View(model); // Views/PublicPackage/ConfirmBooking.cshtml
        }
        [HttpPost, ValidateAntiForgeryToken, Route("book/confirm")]
        public ActionResult ConfirmBookingPost(PackageBookingVm model)
        {
            if (!ModelState.IsValid) return View(model);

            var session = db.Sessions.Include(s => s.Bookings).FirstOrDefault(s => s.SessionId == model.SelectedSessionId);
            if (session == null) return HttpNotFound();

            int availableSeats = session.Capacity - session.Bookings.Sum(b => b.Participants);
            if (model.Participants > availableSeats)
            {
                ModelState.AddModelError("", $"Only {availableSeats} seats are available for this session.");
                return View(model);
            }

            var booking = new Booking
            {
                TouristId = GetCurrentTouristId(),
                SessionId = session.SessionId,
                Participants = model.Participants,
                Status = "Confirmed",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking confirmed successfully!";
            return RedirectToAction("Details", new { id = session.PackageId });
        }


        // Dummy method for current logged-in tourist
        private int GetCurrentTouristId()
        {
            // Replace with real authentication logic
            return 1;
        }
    }
}
