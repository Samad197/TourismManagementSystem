using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;
using TourismManagementSystem.Data;
using static TourismManagementSystem.Models.ViewModels.PublicPackageDetailsVm;
using System.Data.Entity.Infrastructure;
using System.Net;

namespace TourismManagementSystem.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("packages")]
    public class PublicPackageController : Controller
    {
        private readonly TourismDbContext db = new TourismDbContext();

        // GET: /packages
        // GET: /packages
        [HttpGet, Route("")]
        [AllowAnonymous]
        public ActionResult Index(string q, decimal? minPrice, decimal? maxPrice, int? minDays, int? maxDays, bool upcomingOnly = true)
        {

            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Packages";
            ViewBag.Q = q;

            var today = DateTime.Today;

            var query = db.TourPackages
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Sessions.Select(s => s.Bookings.Select(b => b.Feedbacks)))
                .Include(p => p.Agency.User)
                .Include(p => p.Guide.User);

            // only approved + active providers
            query = query.Where(p =>
                (p.AgencyId != null && p.Agency.User.IsApproved && p.Agency.User.IsActive) ||
                (p.GuideId != null && p.Guide.User.IsApproved && p.Guide.User.IsActive));

            if (upcomingOnly)
                query = query.Where(p => p.Sessions.Any(s => DbFunctions.TruncateTime(s.StartDate) >= today));

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Title.Contains(q) || p.Description.Contains(q));

            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
            if (minDays.HasValue) query = query.Where(p => p.DurationDays >= minDays.Value);
            if (maxDays.HasValue) query = query.Where(p => p.DurationDays <= maxDays.Value);

            var packages = query
                .OrderBy(p => p.Title)
                .Select(p => new PublicPackageListItemVm
                {
                    PackageId = p.PackageId,
                    Title = p.Title,
                    Price = p.Price,
                    DurationDays = p.DurationDays,
                    MaxGroupSize = p.MaxGroupSize,
                    ThumbnailPath = p.Images.Any()
                        ? p.Images.FirstOrDefault().ImagePath
                        : "/images/placeholder.jpg",
                    OwnerType = p.AgencyId != null ? "Agency" : "Guide",
                    OwnerName = p.AgencyId != null ? p.Agency.User.FullName : p.Guide.User.FullName,

                    // mark if any future session exists
                    HasUpcoming = p.Sessions.Any(s =>
                        DbFunctions.TruncateTime(s.StartDate) >= today),

                    // mark if any future session still has seats
                    HasAvailableSession = p.Sessions.Any(s =>
                        DbFunctions.TruncateTime(s.StartDate) >= today &&
                        (s.Capacity > s.Bookings.Count)),

                    // average rating
                    AvgRating = p.Sessions
                                 .SelectMany(s => s.Bookings)
                                 .SelectMany(b => b.Feedbacks)
                                 .Select(f => (double?)f.Rating)
                                 .Average()
                })
                .ToList();

            return View(packages);
        }



        [HttpGet, Route("details/{id:int}")]
        [AllowAnonymous]
        public ActionResult Details(int id)
        {
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Packages";

            var p = db.TourPackages
                .Include(x => x.Images)
                .Include(x => x.Sessions.Select(s => s.Bookings.Select(b => b.Feedbacks)))
                .Include(x => x.Sessions.Select(s => s.Bookings.Select(b => b.Tourist)))
                .Include(x => x.Agency.User)
                .Include(x => x.Guide.User)
                .FirstOrDefault(x => x.PackageId == id);

            if (p == null) return HttpNotFound();

            bool ownerApproved =
                (p.AgencyId != null && p.Agency?.User?.IsApproved == true && p.Agency.User.IsActive) ||
                (p.GuideId != null && p.Guide?.User?.IsApproved == true && p.Guide.User.IsActive);
            if (!ownerApproved) return HttpNotFound();

            int currentUserId = GetCurrentTouristId();

            var existingBooking = p.Sessions
                .SelectMany(s => s.Bookings)
                .FirstOrDefault(b => b.TouristId == currentUserId);

            var reviewItems = p.Sessions
                .SelectMany(s => s.Bookings)
                .SelectMany(b => b.Feedbacks.Select(f => new PublicPackageDetailsVm.ReviewItem
                {
                    TouristName = b.Tourist != null ? b.Tourist.FullName : "Tourist",
                    Rating = f.Rating,
                    Comment = f.Comment
                }))
                .ToList();

            double? avgRating = reviewItems.Any() ? (double?)reviewItems.Average(r => r.Rating) : null;

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
                UpcomingSessions = p.Sessions
                    .OrderBy(s => s.StartDate)
                    .Select(s => new UpcomingSessionItem
                    {
                        SessionId = s.SessionId,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Capacity = s.Capacity,
                        Booked = s.Bookings
                                      .Where(b => b.IsApproved == true)
                                      .Select(b => (int?)b.Participants)
                                      .DefaultIfEmpty(0)
                                      .Sum() ?? 0
                    })
                    .ToList(),
                Reviews = reviewItems,
                AvgRating = avgRating,
                ExistingBooking = existingBooking
            };

            return View(vm);
        }



        // GET: /packages/book?packageId=2&sessionId=2
        [HttpGet, Route("book")]
        [Authorize(Roles = "Tourist")]
        public ActionResult Book(int packageId, int? sessionId)
        {
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Packages";

            var package = db.TourPackages
                .Include(p => p.Sessions.Select(s => s.Bookings))
                .FirstOrDefault(p => p.PackageId == packageId);

            if (package == null) return HttpNotFound();

            // Build upcoming sessions with capacity check
            var upcomingSessions = package.Sessions
                .Where(s => s.StartDate >= DateTime.Today && !s.IsCanceled)
                .OrderBy(s => s.StartDate)
                .Select(s => new UpcomingSessionItem
                {
                    SessionId = s.SessionId,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Capacity = s.Capacity,
                    Booked = s.Bookings
                                .Where(b => b.IsApproved == true)
                                .Select(b => (int?)b.Participants)
                                .DefaultIfEmpty(0)
                                .Sum() ?? 0
                })
                .ToList();

            // Default session = first with seats left
            var selectedSession = sessionId ??
                                  upcomingSessions.FirstOrDefault(s => s.Capacity > s.Booked)?.SessionId ?? 0;

            var vm = new PackageBookingVm
            {
                PackageId = packageId,
                PackageTitle = package.Title,
                Participants = 1,
                SelectedSessionId = selectedSession,
                UpcomingSessions = upcomingSessions
            };

            return View(vm);
        }



        // POST: /packages/book
        [HttpPost, ValidateAntiForgeryToken, Route("book")]
        [Authorize(Roles = "Tourist")] // recommended
        public ActionResult Book(PackageBookingVm model)
        {
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Packages";

            if (!ModelState.IsValid)
            {
                // reload sessions for dropdown if validation fails
                model.UpcomingSessions = db.Sessions
                    .Where(s => s.PackageId == model.PackageId && s.StartDate >= DateTime.Today && !s.IsCanceled)
                    .OrderBy(s => s.StartDate)
                    .Select(s => new UpcomingSessionItem
                    {
                        SessionId = s.SessionId,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Capacity = s.Capacity,
                        Booked = s.Bookings.Where(b => b.IsApproved == true)
                                           .Select(b => (int?)b.Participants)
                                           .DefaultIfEmpty(0)
                                           .Sum() ?? 0
                    })
                    .ToList();

                return View(model);
            }

            var session = db.Sessions
                .Include(s => s.Bookings)
                .FirstOrDefault(s => s.SessionId == model.SelectedSessionId && !s.IsCanceled);

            if (session == null)
            {
                ModelState.AddModelError("", "The selected session does not exist or has been canceled.");

                // repopulate sessions
                model.UpcomingSessions = db.Sessions
                    .Where(s => s.PackageId == model.PackageId && s.StartDate >= DateTime.Today && !s.IsCanceled)
                    .OrderBy(s => s.StartDate)
                    .Select(s => new UpcomingSessionItem
                    {
                        SessionId = s.SessionId,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Capacity = s.Capacity,
                        Booked = s.Bookings.Where(b => b.IsApproved == true)
                                           .Select(b => (int?)b.Participants)
                                           .DefaultIfEmpty(0)
                                           .Sum() ?? 0
                    })
                    .ToList();

                return View(model);
            }

            var touristId = GetCurrentTouristId();
            if (touristId <= 0)
            {
                TempData["ErrorMessage"] = "Please log in as a Tourist to book.";
                return RedirectToAction("Login", "Account", new
                {
                    returnUrl = Url.Action("Book", "Tourist", new { packageId = model.PackageId, sessionId = model.SelectedSessionId })
                });
            }

            int approvedSeats = session.Bookings
                .Where(b => b.IsApproved == true)
                .Select(b => (int?)b.Participants)
                .DefaultIfEmpty(0)
                .Sum() ?? 0;
            int availableSeats = session.Capacity - approvedSeats;

            if (model.Participants > availableSeats)
            {
                ModelState.AddModelError("Participants", $"Only {availableSeats} seat(s) are available for this session.");

                // reload sessions again
                model.UpcomingSessions = db.Sessions
                    .Where(s => s.PackageId == model.PackageId && s.StartDate >= DateTime.Today && !s.IsCanceled)
                    .OrderBy(s => s.StartDate)
                    .Select(s => new UpcomingSessionItem
                    {
                        SessionId = s.SessionId,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Capacity = s.Capacity,
                        Booked = s.Bookings.Where(b => b.IsApproved == true)
                                           .Select(b => (int?)b.Participants)
                                           .DefaultIfEmpty(0)
                                           .Sum() ?? 0
                    })
                    .ToList();

                return View(model);
            }

            var booking = new Booking
            {
                TouristId = touristId,
                SessionId = session.SessionId,
                Participants = model.Participants,
                Status = "Pending",
                PaymentStatus = "Pending",
                CustomerName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                IsApproved = null
            };

            db.Bookings.Add(booking);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking created successfully!";

            // ✅ redirect to tourist-friendly detail page
            return RedirectToRoute("TouristPackageDetails", new { id = session.PackageId });
        }





        // GET: /packages/booking/edit/{id}
        [HttpGet, Route("booking/edit/{id:int}")]
        [Authorize(Roles = "Tourist")]
        public ActionResult EditBooking(int id)
        {
            var meId = GetCurrentTouristId();

            var booking = db.Bookings
                .Include(b => b.Session.Package.Sessions.Select(s => s.Bookings)) // include bookings for all sessions of this package
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null) return HttpNotFound();
            if (booking.TouristId != meId) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var vm = new PackageBookingVm
            {
                BookingId = booking.BookingId,
                PackageId = booking.Session.PackageId,
                PackageTitle = booking.Session.Package.Title,
                Participants = booking.Participants,
                SelectedSessionId = booking.SessionId,
                UpcomingSessions = booking.Session.Package.Sessions
                    .Where(s => s.StartDate >= DateTime.Today && !s.IsCanceled)
                    .OrderBy(s => s.StartDate)
                    .Select(s => new UpcomingSessionItem
                    {
                        SessionId = s.SessionId,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Capacity = s.Capacity,
                        // Booked = approved-only
                        Booked = s.Bookings
                                      .Where(bk => bk.IsApproved == true)
                                      .Select(bk => (int?)bk.Participants)
                                      .DefaultIfEmpty(0)
                                      .Sum() ?? 0
                    })
                    .ToList()
            };

            return View(vm);
        }


        // POST: /packages/booking/edit
        // POST: /packages/booking/edit
        [HttpPost, ValidateAntiForgeryToken, Route("booking/edit")]
        [Authorize(Roles = "Tourist")]
        public ActionResult EditBooking(PackageBookingVm model)
        {
            if (!ModelState.IsValid) return View(model);

            var meId = GetCurrentTouristId();

            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == model.BookingId);
            if (booking == null) return HttpNotFound();
            if (booking.TouristId != meId) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var session = db.Sessions
                .Include(s => s.Bookings)
                .FirstOrDefault(s => s.SessionId == model.SelectedSessionId);

            if (session == null)
            {
                ModelState.AddModelError("", "The selected session does not exist.");
                return View(model);
            }

            // Seats taken by OTHER approved bookings
            int approvedOthers = session.Bookings
                .Where(b => b.BookingId != booking.BookingId && b.IsApproved == true)
                .Select(b => (int?)b.Participants)
                .DefaultIfEmpty(0)
                .Sum() ?? 0;

            int availableSeats = session.Capacity - approvedOthers;

            if (model.Participants > availableSeats)
            {
                ModelState.AddModelError("", $"Only {availableSeats} seat(s) are available for this session.");
                return View(model);
            }

            booking.SessionId = model.SelectedSessionId;
            booking.Participants = model.Participants;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking updated successfully!";
            return RedirectToAction("Details", new { id = session.PackageId });
        }


        // POST: /packages/booking/cancel/{id}
        [HttpPost, ValidateAntiForgeryToken, Route("booking/cancel/{id:int}")]
        [Authorize(Roles = "Tourist")]
        public ActionResult CancelBooking(int id)
        {
            var meId = GetCurrentTouristId();

            var booking = db.Bookings
                .Include(b => b.Session) // to read PackageId after update
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null) return HttpNotFound();
            if (booking.TouristId != meId) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            int pkgId = booking.Session.PackageId;

            booking.Status = "Cancelled";
            booking.PaymentStatus = "Refunded"; // optional
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking cancelled successfully!";
            return RedirectToAction("Details", new { id = pkgId });
        }


        [HttpGet, Route("book/confirm")]
        [Authorize(Roles = "Tourist")]
        public ActionResult ConfirmBooking(PackageBookingVm model)
        {
            if (model.SelectedSessionId == 0) return HttpNotFound();
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Packages";


            var session = db.Sessions.Include(s => s.Package)
                            .FirstOrDefault(s => s.SessionId == model.SelectedSessionId);
            if (session == null) return HttpNotFound();

            model.PackageTitle = session.Package.Title;
            model.SelectedSessionDate = session.StartDate;
            model.SelectedSessionEnd = session.EndDate;

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken, Route("book/confirm")]
        [Authorize(Roles = "Tourist")]
        public ActionResult ConfirmBookingPost(PackageBookingVm model)
        {
            if (!ModelState.IsValid) return View(model);
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Packages";

            var meId = GetCurrentTouristId();
            var session = db.Sessions.Include(s => s.Bookings)
                            .FirstOrDefault(s => s.SessionId == model.SelectedSessionId);
            if (session == null) return HttpNotFound();

            // Approved-only capacity
            int approvedSeats = session.Bookings
                .Where(b => b.IsApproved == true)
                .Select(b => (int?)b.Participants)
                .DefaultIfEmpty(0)
                .Sum() ?? 0;

            int availableSeats = session.Capacity - approvedSeats;
            if (model.Participants > availableSeats)
            {
                ModelState.AddModelError("", $"Only {availableSeats} seat(s) are available for this session.");
                return View(model);
            }

            var booking = new Booking
            {
                TouristId = meId,
                SessionId = session.SessionId,
                Participants = model.Participants,
                Status = "Pending",      // ← stays pending (agency approves)
                PaymentStatus = "Pending",
                CustomerName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                IsApproved = null
            };

            db.Bookings.Add(booking);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking submitted! You’ll be notified once the agency approves.";
            return RedirectToAction("Details", new { id = session.PackageId });
        }



        // Dummy method for current logged-in tourist
        private int GetCurrentTouristId()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return 0;
            return db.Users
                     .Where(u => u.Email == email)
                     .Select(u => u.UserId)
                     .FirstOrDefault();
        }


    }
}
