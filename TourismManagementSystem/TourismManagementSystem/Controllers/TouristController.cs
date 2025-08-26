using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using TourismManagementSystem.Data;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize]
    public class TouristController : BaseController
    {
        private readonly TourismDbContext db = new TourismDbContext();

        // GET: /Tourist/Dashboard
        public ActionResult Dashboard()
        {
            var email = User.Identity.Name;
            var me = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == email);
            if (me == null) return RedirectToAction("Login", "Account");

            // Only Tourists
            if (!string.Equals(me.Role.RoleName, "Tourist", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Home");

            // Get my bookings with Session + Package + Feedback
            var myBookings = db.Bookings
                .Include(b => b.Session.Package)
                .Include(b => b.Feedbacks)
                .Where(b => b.TouristId == me.UserId)
                .ToList();   // materialize so we can compute EndDate in memory

            var today = DateTime.Today;

            var vm = new TouristDashboardVm
            {
                Upcoming = myBookings
                    .Where(b => b.Status != "Cancelled" && b.Session.StartDate >= today)
                    .OrderBy(b => b.Session.StartDate)
                    .Select(b => new TouristBookingItemVm
                    {
                        BookingId = b.BookingId,
                        PackageTitle = b.Session.Package.Title,
                        StartDate = b.Session.StartDate, // DateTime
                        // compute end date from DurationDays (package-level)
                        EndDate = b.Session.StartDate.AddDays(b.Session.Package.DurationDays),
                        Participants = b.Participants,
                        Status = b.Status,
                        PaymentStatus = b.PaymentStatus,
                        CanLeaveFeedback = false
                    })
                    .ToList(),

                Past = myBookings
                    .Where(b => b.Session.StartDate < today)
                    .OrderByDescending(b => b.Session.StartDate)
                    .Select(b => new TouristBookingItemVm
                    {
                        BookingId = b.BookingId,
                        PackageTitle = b.Session.Package.Title,
                        StartDate = b.Session.StartDate,
                        EndDate = b.Session.StartDate.AddDays(b.Session.Package.DurationDays),
                        Participants = b.Participants,
                        Status = b.Status,
                        PaymentStatus = b.PaymentStatus,
                        // Feedback allowed only after completion and if none exists
                        CanLeaveFeedback = (b.Status == "Completed" && b.Feedbacks == null)
                    })
                    .ToList()
            };

            ViewBag.ActivePage = "Dashboard";
            return View(vm);
        }

        // GET: /Tourist/BookingDetails/{id}
        [HttpGet]
        public ActionResult BookingDetails(int id)
        {
            var email = User.Identity.Name;
            var me = db.Users.FirstOrDefault(u => u.Email == email);
            if (me == null) return RedirectToAction("Login", "Account");

            var booking = db.Bookings
                .Include(b => b.Session.Package)
                .Include(b => b.Feedbacks)
                .FirstOrDefault(b => b.BookingId == id && b.TouristId == me.UserId);

            if (booking == null) return HttpNotFound();

            var vm = new TouristBookingItemVm
            {
                BookingId = booking.BookingId,
                PackageTitle = booking.Session.Package.Title,
                StartDate = booking.Session.StartDate,
                EndDate = booking.Session.StartDate.AddDays(booking.Session.Package.DurationDays),
                Participants = booking.Participants,
                Status = booking.Status,
                PaymentStatus = booking.PaymentStatus,
                CanLeaveFeedback = (booking.Status == "Completed" && booking.Feedbacks == null)
            };

            return View(vm);
        }
    }
}
