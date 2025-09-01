using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TourismManagementSystem.Data;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize(Roles = "Tourist")]
    [RoutePrefix("Tourist")]
    public class TouristController : BaseController
    {
        // ---------- Helpers ----------
        private User GetMe()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;

            return db.Users
                     .Include(u => u.Role)
                     .FirstOrDefault(u => u.Email == email);
        }

        private int GetCurrentTouristId()
            => db.Users.Where(u => u.Email == User.Identity.Name)
                       .Select(u => u.UserId)
                       .FirstOrDefault();

        private static bool IsCompleted(Session s) => s.EndDate.Date < DateTime.Today;

        private void RehydrateUpcomingSessions_old(PackageBookingVm model, int? packageIdOpt = null)
        {
            int pkgId = packageIdOpt ?? model.PackageId;

            var package = db.TourPackages
                            .Include(p => p.Sessions.Select(s => s.Bookings))
                            .FirstOrDefault(p => p.PackageId == pkgId);

            model.UpcomingSessions = new System.Collections.Generic.List<UpcomingSessionItem>();
            if (package == null) return;

            model.PackageTitle = package.Title;
            model.UpcomingSessions = package.Sessions
                .Where(s => s.StartDate >= DateTime.Today && !s.IsCanceled)
                .OrderBy(s => s.StartDate)
                .Select(s => new UpcomingSessionItem
                {
                    SessionId = s.SessionId,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Capacity = s.Capacity,
                    // approved-only seats
                    Booked = s.Bookings
                              .Where(b => b.IsApproved == true)
                              .Select(b => (int?)b.Participants)
                              .DefaultIfEmpty(0)
                              .Sum() ?? 0
                })
                .ToList();
        }

        // ---------- Landing ----------
        [HttpGet, Route("")]
        public ActionResult Index() => RedirectToAction("MyBookings");

        // ---------- Packages list (Tourist URL) ----------
        // GET /Tourist/Packages
        [HttpGet, Route("Packages", Name = "TouristPackages")]
        public ActionResult Packages(string q, decimal? minPrice, decimal? maxPrice, int? minDays, int? maxDays, bool upcomingOnly = true)
        {
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Tourist";
            ViewBag.Q = q;

            var today = DateTime.Today;

            var query = db.TourPackages
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Sessions.Select(s => s.Bookings.Select(b => b.Feedbacks)))
                .Include(p => p.Agency.User)
                .Include(p => p.Guide.User);

            // only approved+active owners
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

            var model = query
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
           HasUpcoming = p.Sessions.Any(s => DbFunctions.TruncateTime(s.StartDate) >= today),
           HasAvailableSession = p.Sessions.Any(s =>
               DbFunctions.TruncateTime(s.StartDate) >= today &&
               !s.IsCanceled &&
               (s.Bookings.Sum(b => (int?)b.Participants) ?? 0) < s.Capacity
           ),
           AvgRating = p.Sessions
                        .SelectMany(s => s.Bookings)
                        .SelectMany(b => b.Feedbacks)
                        .Select(f => (double?)f.Rating)
                        .Average()
       })
       .ToList();


            return View("~/Views/PublicPackage/Index.cshtml", model);
        }


        // ---------- Package details (Tourist URL) ----------
        // GET /Tourist/Packages/Details/{id}
        [HttpGet, Route("Packages/Details/{id:int}", Name = "TouristPackageDetails")]
        public ActionResult PackageDetails(int id)
        {
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Tourist";
            ViewBag.PackageId = id; // helps build navbar "Book Now" link

            var p = db.TourPackages
                .Include(x => x.Images)
                .Include(x => x.Sessions.Select(s => s.Bookings.Select(b => b.Feedbacks)))
                .Include(x => x.Sessions.Select(s => s.Bookings.Select(b => b.Tourist)))
                .Include(x => x.Agency.User)
                .Include(x => x.Guide.User)
                .FirstOrDefault(x => x.PackageId == id);

            if (p == null) return HttpNotFound();

            // ensure only approved+active agency/guide
            var ownerApproved =
                (p.AgencyId != null && p.Agency?.User?.IsApproved == true && p.Agency.User.IsActive) ||
                (p.GuideId != null && p.Guide?.User?.IsApproved == true && p.Guide.User.IsActive);
            if (!ownerApproved) return HttpNotFound();

            int currentUserId = GetCurrentTouristId();

            // tourist’s existing booking for this package
            var existingBooking = p.Sessions
                .SelectMany(s => s.Bookings)
                .FirstOrDefault(b => b.TouristId == currentUserId);

            // all reviews across sessions
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

            // build main VM
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

            // tourist’s own bookings for THIS package (to show under "My Bookings")
            if (currentUserId > 0)
            {
                var myBookings = p.Sessions
                    .SelectMany(s => s.Bookings)
                    .Where(b => b.TouristId == currentUserId)
                    .Select(b => new PackageBookingVm
                    {
                        BookingId = b.BookingId,
                        SessionId = b.SessionId,
                        SelectedSessionDate = b.Session.StartDate,
                        SelectedSessionEnd = b.Session.EndDate,
                        Participants = b.Participants,
                        Status = b.IsApproved == true ? "Approved"
                                : b.IsApproved == false ? "Rejected"
                                : "Pending",
                        PaymentStatus = b.PaymentStatus
                    })
                    .ToList();

                ViewBag.MyBookings = myBookings;
            }

            // reuse the PublicPackage/Details view file
            return View("~/Views/PublicPackage/Details.cshtml", vm);
        }



        // ================= GET ==================
        // GET /Tourist/Packages/Book?packageId=...&sessionId=...
        [HttpGet, Route("Packages/Book", Name = "TouristPackageBook")]
        [Authorize(Roles = "Tourist")] // ensure only Tourists can book
        public ActionResult PackageBook(int packageId, int? sessionId)
        {
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Tourist";

            var package = db.TourPackages
                .Include(p => p.Sessions.Select(s => s.Bookings))
                .FirstOrDefault(p => p.PackageId == packageId);

            if (package == null) return HttpNotFound();

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

            // Show seat availability in dropdown directly
            var vm = new PackageBookingVm
            {
                PackageId = packageId,
                PackageTitle = package.Title,
                Participants = 1,
                SelectedSessionId = sessionId ?? upcomingSessions.FirstOrDefault()?.SessionId ?? 0,
                UpcomingSessions = upcomingSessions
            };

            // Reuse the PublicPackage/Book view file (form posts to Tourist route)
            return View("~/Views/PublicPackage/Book.cshtml", vm);
        }


        // ================= POST ==================
        // POST /Tourist/Packages/Book
        [HttpPost, ValidateAntiForgeryToken, Route("Packages/Book", Name = "TouristPackageBookPost")]
        [Authorize(Roles = "Tourist")] // must be logged in as Tourist
        public ActionResult PackageBook(PackageBookingVm model)
        {
            ViewBag.ActivePage = "Packages";
            ViewBag.ActivePageGroup = "Tourist";

            if (!ModelState.IsValid)
            {
                RehydrateUpcomingSessions(model);
                return View("~/Views/PublicPackage/Book.cshtml", model);
            }

            var session = db.Sessions
                            .Include(s => s.Bookings)
                            .FirstOrDefault(s => s.SessionId == model.SelectedSessionId && !s.IsCanceled);

            if (session == null)
            {
                ModelState.AddModelError("", "The selected session does not exist or has been canceled.");
                RehydrateUpcomingSessions(model);
                return View("~/Views/PublicPackage/Book.cshtml", model);
            }

            var touristId = GetCurrentTouristId();
            if (touristId <= 0)
            {
                TempData["ErrorMessage"] = "Please log in as a Tourist to book.";
                return RedirectToAction("Login", "Account", new
                {
                    returnUrl = Url.RouteUrl("TouristPackageBook", new { packageId = model.PackageId, sessionId = model.SelectedSessionId })
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
                RehydrateUpcomingSessions(model, session.PackageId);
                return View("~/Views/PublicPackage/Book.cshtml", model);
            }

            // Prevent double-booking the same session by the same tourist
            var alreadyBooked = session.Bookings.Any(b => b.TouristId == touristId);
            if (alreadyBooked)
            {
                TempData["ErrorMessage"] = "You have already booked this session.";
                return RedirectToRoute("TouristPackageDetails", new { id = session.PackageId });
            }

            var booking = new Booking
            {
                TouristId = touristId,
                SessionId = session.SessionId,
                Participants = model.Participants,
                Status = "Pending",          // to be approved by Agency/Admin
                PaymentStatus = "Pending",   // handled later
                CustomerName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                IsApproved = null
            };

            db.Bookings.Add(booking);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking created successfully!";

            // Redirect to details page under Tourist route
            return RedirectToRoute("TouristPackageDetails", new { id = session.PackageId });
        }


        // ================= Helper ==================
        private void RehydrateUpcomingSessions(PackageBookingVm model, int? packageIdOpt = null)
        {
            int pkgId = packageIdOpt ?? model.PackageId;

            var package = db.TourPackages
                .Include(p => p.Sessions.Select(s => s.Bookings))
                .FirstOrDefault(p => p.PackageId == pkgId);

            if (package == null)
            {
                model.UpcomingSessions = new List<UpcomingSessionItem>();
                return;
            }

            model.PackageTitle = package.Title;

            model.UpcomingSessions = package.Sessions
        .Where(s => s.StartDate >= DateTime.Today && !s.IsCanceled)
        .OrderBy(s => s.StartDate)
        .Select(s => new UpcomingSessionItem
        {
            SessionId = s.SessionId,
            PackageTitle = package.Title,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Capacity = s.Capacity,
            Booked = s.Bookings
                      .Where(b => b.IsApproved == true)
                      .Select(b => (int?)b.Participants)
                      .DefaultIfEmpty(0)
                      .Sum() ?? 0
            // no need to set AvailableSeats
        })
        .ToList();

        }



        // ---------- Book (Tourist URL) ----------
        // GET /Tourist/Packages/Book?packageId=...&sessionId=...
        //[HttpGet, Route("Packages/Book", Name = "TouristPackageBook")]
        //public ActionResult PackageBook(int packageId, int? sessionId)
        //{
        //    ViewBag.ActivePage = "Packages";
        //    ViewBag.ActivePageGroup = "Tourist";

        //    var package = db.TourPackages
        //        .Include(p => p.Sessions.Select(s => s.Bookings))
        //        .FirstOrDefault(p => p.PackageId == packageId);

        //    if (package == null) return HttpNotFound();

        //    var upcomingSessions = package.Sessions
        //        .Where(s => s.StartDate >= DateTime.Today && !s.IsCanceled)
        //        .OrderBy(s => s.StartDate)
        //        .Select(s => new UpcomingSessionItem
        //        {
        //            SessionId = s.SessionId,
        //            StartDate = s.StartDate,
        //            EndDate = s.EndDate,
        //            Capacity = s.Capacity,
        //            // approved-only seats
        //            Booked = s.Bookings
        //                      .Where(b => b.IsApproved == true)
        //                      .Select(b => (int?)b.Participants)
        //                      .DefaultIfEmpty(0)
        //                      .Sum() ?? 0
        //        })
        //        .ToList();

        //    var vm = new PackageBookingVm
        //    {
        //        PackageId = packageId,
        //        PackageTitle = package.Title,
        //        Participants = 1,
        //        SelectedSessionId = sessionId ?? upcomingSessions.FirstOrDefault()?.SessionId ?? 0,
        //        UpcomingSessions = upcomingSessions
        //    };

        //    // Reuse the PublicPackage/Book view file (form must post to Tourist route)
        //    return View("~/Views/PublicPackage/Book.cshtml", vm);
        //}

        // POST /Tourist/Packages/Book
        //[HttpPost, ValidateAntiForgeryToken, Route("Packages/Book", Name = "TouristPackageBookPost")]
        //public ActionResult PackageBook(PackageBookingVm model)
        //{
        //    ViewBag.ActivePage = "Packages";
        //    ViewBag.ActivePageGroup = "Tourist";

        //    if (!ModelState.IsValid)
        //    {
        //        RehydrateUpcomingSessions(model);
        //        return View("~/Views/PublicPackage/Book.cshtml", model);
        //    }

        //    var session = db.Sessions
        //                    .Include(s => s.Bookings)
        //                    .FirstOrDefault(s => s.SessionId == model.SelectedSessionId && !s.IsCanceled);

        //    if (session == null)
        //    {
        //        ModelState.AddModelError("", "The selected session does not exist or has been canceled.");
        //        RehydrateUpcomingSessions(model);
        //        return View("~/Views/PublicPackage/Book.cshtml", model);
        //    }

        //    var touristId = GetCurrentTouristId();
        //    if (touristId <= 0) return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);

        //    int approvedSeats = session.Bookings
        //        .Where(b => b.IsApproved == true)
        //        .Select(b => (int?)b.Participants)
        //        .DefaultIfEmpty(0)
        //        .Sum() ?? 0;

        //    int available = session.Capacity - approvedSeats;

        //    if (model.Participants > available)
        //    {
        //        ModelState.AddModelError("", $"Only {available} seat(s) are available for this session.");
        //        RehydrateUpcomingSessions(model, session.PackageId);
        //        return View("~/Views/PublicPackage/Book.cshtml", model);
        //    }

        //    var booking = new Booking
        //    {
        //        TouristId = touristId,
        //        SessionId = session.SessionId,
        //        Participants = model.Participants,
        //        Status = "Pending",
        //        PaymentStatus = "Pending",
        //        CustomerName = model.FullName,
        //        CreatedAt = DateTime.UtcNow,
        //        IsApproved = null
        //    };

        //    db.Bookings.Add(booking);
        //    db.SaveChanges();

        //    TempData["SuccessMessage"] = "Booking created successfully!";
        //    // IMPORTANT: redirect to Tourist route (not PublicPackage)
        //    return RedirectToRoute("TouristPackageDetails", new { id = session.PackageId });
        //}

        // ---------- Edit booking ----------
        // GET /Tourist/Packages/Booking/Edit/{id}
        [HttpGet, Route("Packages/Booking/Edit/{id:int}", Name = "TouristBookingEdit")]
        public ActionResult EditBooking(int id)
        {
            var meId = GetCurrentTouristId();

            var booking = db.Bookings
                            .Include(b => b.Session.Package.Sessions.Select(s => s.Bookings))
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
                        Booked = s.Bookings
                                  .Where(bk => bk.IsApproved == true)
                                  .Select(bk => (int?)bk.Participants)
                                  .DefaultIfEmpty(0)
                                  .Sum() ?? 0
                    })
                    .ToList()
            };

            return View("~/Views/PublicPackage/EditBooking.cshtml", vm);
        }

        // POST /Tourist/Packages/Booking/Edit
        [HttpPost, ValidateAntiForgeryToken, Route("Packages/Booking/Edit", Name = "TouristBookingEditPost")]
        public ActionResult EditBooking(PackageBookingVm model)
        {
            var meId = GetCurrentTouristId();

            if (!ModelState.IsValid)
            {
                RehydrateUpcomingSessions(model);
                return View("~/Views/PublicPackage/EditBooking.cshtml", model);
            }

            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == model.BookingId);
            if (booking == null) return HttpNotFound();
            if (booking.TouristId != meId) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var session = db.Sessions
                            .Include(s => s.Bookings)
                            .FirstOrDefault(s => s.SessionId == model.SelectedSessionId);

            if (session == null)
            {
                ModelState.AddModelError("", "The selected session does not exist.");
                RehydrateUpcomingSessions(model);
                return View("~/Views/PublicPackage/EditBooking.cshtml", model);
            }

            int approvedOthers = session.Bookings
                .Where(b => b.BookingId != booking.BookingId && b.IsApproved == true)
                .Select(b => (int?)b.Participants)
                .DefaultIfEmpty(0)
                .Sum() ?? 0;

            int available = session.Capacity - approvedOthers;

            if (model.Participants > available)
            {
                ModelState.AddModelError("", $"Only {available} seat(s) are available for this session.");
                RehydrateUpcomingSessions(model, session.PackageId);
                return View("~/Views/PublicPackage/EditBooking.cshtml", model);
            }

            booking.SessionId = model.SelectedSessionId;
            booking.Participants = model.Participants;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking updated successfully!";
            // IMPORTANT: redirect to Tourist route
            return RedirectToRoute("TouristPackageDetails", new { id = session.PackageId });
        }

        // ---------- Cancel booking ----------
        // POST /Tourist/Packages/Booking/Cancel/{id}
        [HttpPost, ValidateAntiForgeryToken, Route("Packages/Booking/Cancel/{id:int}", Name = "TouristBookingCancel")]
        public ActionResult CancelBooking(int id)
        {
            var meId = GetCurrentTouristId();

            var booking = db.Bookings
                            .Include(b => b.Session)
                            .FirstOrDefault(b => b.BookingId == id);

            if (booking == null) return HttpNotFound();
            if (booking.TouristId != meId) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            int pkgId = booking.Session.PackageId;

            booking.Status = "Cancelled";
            booking.PaymentStatus = "Refunded"; // optional
            db.SaveChanges();

            TempData["SuccessMessage"] = "Booking cancelled successfully!";
            // IMPORTANT: redirect to Tourist route
            return RedirectToRoute("TouristPackageDetails", new { id = pkgId });
        }

        // ---------- My Bookings (current/future) ----------
        [HttpGet, Route("MyBookings")]
        public ActionResult MyBookings()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var myId = me.UserId;

            var list = db.Bookings
                .AsNoTracking()
                .Include(b => b.Session.Package)
                .Include(b => b.Feedbacks)
                .Where(b => b.TouristId == myId)
                .OrderBy(b => b.Session.StartDate)
                .ToList()
                .Select(b => new TouristBookingRowVm
                {
                    BookingId = b.BookingId,
                    PackageId = b.Session.Package.PackageId,
                    PackageTitle = b.Session.Package.Title,
                    StartDate = b.Session.StartDate,
                    EndDate = b.Session.EndDate,
                    Participants = b.Participants,
                    Status = b.Status,
                    PaymentStatus = b.PaymentStatus,
                    IsApproved = b.IsApproved,
                    IsCompleted = IsCompleted(b.Session),
                    ExistingFeedbackId = b.Feedbacks?.Select(f => (int?)f.FeedbackId).FirstOrDefault(),
                    Amount = (b.Session.Package.Price * b.Participants)
                })
                .Where(vm => !vm.IsCompleted)
                .ToList();

            ViewBag.ActivePage = "MyBookings";
            ViewBag.ActivePageGroup = "Tourist";
            return View(list);
        }

        // ---------- History (completed) ----------
        [HttpGet, Route("History")]
        public ActionResult History()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var myId = me.UserId;

            var list = db.Bookings
                .AsNoTracking()
                .Include(b => b.Session.Package)
                .Include(b => b.Feedbacks)
                .Where(b => b.TouristId == myId)
                .OrderByDescending(b => b.Session.StartDate)
                .ToList()
                .Select(b =>
                {
                    var completed = IsCompleted(b.Session);
                    var existingFeedbackId = b.Feedbacks?.Select(f => (int?)f.FeedbackId).FirstOrDefault();
                    var canReview = completed && b.IsApproved == true && !existingFeedbackId.HasValue;

                    return new TouristBookingRowVm
                    {
                        BookingId = b.BookingId,
                        PackageId = b.Session.Package.PackageId,
                        PackageTitle = b.Session.Package.Title,
                        StartDate = b.Session.StartDate,
                        EndDate = b.Session.EndDate,
                        Participants = b.Participants,
                        Status = b.Status,
                        PaymentStatus = b.PaymentStatus,
                        IsApproved = b.IsApproved,
                        IsCompleted = completed,
                        ExistingFeedbackId = existingFeedbackId,
                        CanReview = canReview,
                        Amount = (b.Session.Package.Price * b.Participants),


                     // If feedback exists, take first one
                        Rating = b.Feedbacks.Select(f => (int?)f.Rating).FirstOrDefault(),
                        Comment = b.Feedbacks.Select(f => f.Comment).FirstOrDefault()
                    };
                })
                .Where(vm => vm.IsCompleted)
                .ToList();

            ViewBag.ActivePage = "History";
            ViewBag.ActivePageGroup = "Tourist";
            return View(list);
        }

        // ---------- Create Review ----------
        [HttpGet, Route("Review/Create/{bookingId:int}", Name = "TouristCreateReview")]
        public ActionResult CreateReview(int bookingId)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var b = db.Bookings
                      .Include(x => x.Session.Package)
                      .Include(x => x.Feedbacks)
                      .FirstOrDefault(x => x.BookingId == bookingId && x.TouristId == me.UserId);

            if (b == null) return HttpNotFound();

            if (b.IsApproved != true)
            {
                TempData["Error"] = "You can only review approved bookings.";
                return RedirectToAction("History");
            }
            if (!IsCompleted(b.Session))
            {
                TempData["Error"] = "You can only review after the session has completed.";
                return RedirectToAction("History");
            }
            if (b.Feedbacks.Any())
            {
                TempData["Error"] = "You have already submitted feedback for this booking.";
                return RedirectToAction("History");
            }

            var vm = new CreateReviewVm
            {
                BookingId = b.BookingId,
                PackageId = b.Session.Package.PackageId,
                PackageTitle = b.Session.Package.Title,
                SessionStart = b.Session.StartDate
            };

            ViewBag.ActivePage = "History";
            ViewBag.ActivePageGroup = "Tourist";
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken, Route("Review/Create")]
        public ActionResult CreateReview(CreateReviewVm vm)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.ActivePage = "History";
                ViewBag.ActivePageGroup = "Tourist";
                return View(vm);
            }

            var b = db.Bookings
                      .Include(x => x.Session.Package)
                      .Include(x => x.Feedbacks)
                      .FirstOrDefault(x => x.BookingId == vm.BookingId && x.TouristId == me.UserId);

            if (b == null) return HttpNotFound();

            if (b.IsApproved != true || !IsCompleted(b.Session) || b.Feedbacks.Any())
            {
                TempData["Error"] = "This booking is not eligible for a new review.";
                return RedirectToAction("History");
            }

            var feedback = new Feedback
            {
                BookingId = b.BookingId,
                Rating = vm.Rating,
                Comment = vm.Comment,
                CreatedAt = DateTime.UtcNow
            };

            db.Feedbacks.Add(feedback);
            db.SaveChanges();

            TempData["Success"] = "Thank you! Your review has been submitted.";
            return RedirectToAction("History");
        }

        // ---------- Profile ----------
        [HttpGet, Route("Profile")]
        public ActionResult Profile()
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            var vm = new TouristProfileVm
            {
                FullName = me.FullName,
                Email = me.Email,
                Phone = me.Phone
            };

            ViewBag.ActivePage = "TouristProfile";
            ViewBag.ActivePageGroup = "Tourist";
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken, Route("Profile")]
        public ActionResult Profile(TouristProfileVm vm)
        {
            var me = GetMe();
            if (me == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.ActivePage = "TouristProfile";
                ViewBag.ActivePageGroup = "Tourist";
                return View(vm);
            }

            me.FullName = vm.FullName?.Trim();
            me.Phone = vm.Phone?.Trim();
            db.SaveChanges();

            TempData["Success"] = "Profile updated.";
            return RedirectToAction("Profile");
        }


   

    }
}
