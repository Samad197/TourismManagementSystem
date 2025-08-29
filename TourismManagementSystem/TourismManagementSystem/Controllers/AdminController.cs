using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;

namespace TourismManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        // ========= DASHBOARD =========
        [HttpGet]
        public ActionResult Dashboard(string from = null, string to = null)
        {
            // Date range (optional)
            DateTime? fromDt = null, toDt = null;
            if (DateTime.TryParse(from, out var f)) fromDt = f.Date;
            if (DateTime.TryParse(to, out var t)) toDt = t.Date.AddDays(1).AddTicks(-1); // inclusive day-end

            // Base queries
            var usersQ = db.Users.Include(u => u.Role);
            var bookingsQ = db.Bookings.Include(b => b.Session.Package);

            // Filter bookings by CreatedAt if a range is provided
            if (fromDt.HasValue) bookingsQ = bookingsQ.Where(b => b.CreatedAt >= fromDt.Value);
            if (toDt.HasValue) bookingsQ = bookingsQ.Where(b => b.CreatedAt <= toDt.Value);

            var vm = new AdminDashboardVm
            {
                PendingAgencies = usersQ.Count(u => u.Role.RoleName == "Agency" && !u.IsApproved),
                PendingGuides = usersQ.Count(u => u.Role.RoleName == "Guide" && !u.IsApproved),
                TotalUsers = usersQ.Count(),
                TotalBookings = bookingsQ.Count(),
                PaidRevenue = bookingsQ
                    .Where(b => b.PaymentStatus == "Paid")
                    .Select(b => (decimal?)b.Session.Package.Price * b.Participants)
                    .DefaultIfEmpty(0)
                    .Sum() ?? 0m,

                // Recent bookings (last 8 after filters)
                RecentBookings = bookingsQ
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(8)
                    .Select(b => new AdminRecentBookingVm
                    {
                        BookingId = b.BookingId,
                        CustomerName = b.CustomerName,
                        PaymentStatus = b.PaymentStatus,
                        CreatedAt = b.CreatedAt,
                        Amount = b.Session.Package.Price * b.Participants,
                        Participants = b.Participants,
                        PackageTitle = b.Session.Package.Title,
                        StartDate = b.Session.StartDate
                    })
                    .ToList(),

                // Simple “payments to verify” queue (not Paid)
                PaymentQueue = db.Bookings
                    .Include(b => b.Session.Package)
                    .Where(b => b.PaymentStatus != "Paid")
                    .OrderBy(b => b.CreatedAt)
                    .Take(8)
                    .Select(b => new AdminPaymentQueueVm
                    {
                        BookingId = b.BookingId,
                        CustomerName = b.CustomerName,
                        Amount = b.Session.Package.Price * b.Participants,
                        PackageTitle = b.Session.Package.Title,
                        StartDate = b.Session.StartDate
                    })
                    .ToList()
            };

            // Keep the date filters in ViewBag if your view has the filter form
            ViewBag.From = from;
            ViewBag.To = to;

            ViewBag.ActivePage = "AdminDashboard";
            return View(vm);
        }


        // ========= APPROVALS =========
        [HttpGet]
        public ActionResult Approvals()
        {
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
                .OrderBy(p => p.RoleName)
                .ThenBy(p => p.FullName)
                .ToList();

            ViewBag.ActivePage = "Approvals";
            return View(pending);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
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

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Reject(int id)
        {
            var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.UserId == id);
            if (user == null) return HttpNotFound();

            user.IsApproved = false; // remains pending/rejected; you can also set IsActive=false if desired

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

        // ========= USERS =========
        [HttpGet]
        public ActionResult Users()
        {
            var users = db.Users.Include(u => u.Role)
                .OrderBy(u => u.Role.RoleName)
                .ThenBy(u => u.FullName)
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

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ToggleActive(int id)
        {
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

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ToggleApproved(int id)
        {
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

        // ========= PAYMENTS (Optional Admin Verification) =========

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult MarkPaymentPaid(int bookingId)
        {
            var b = db.Bookings.Include(x => x.Session.Package).FirstOrDefault(x => x.BookingId == bookingId);
            if (b == null) return HttpNotFound();

            b.PaymentStatus = "Paid";
            db.SaveChanges();

            TempData["Success"] = $"Marked booking #{b.BookingId} as Paid.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult MarkPaymentRefund(int bookingId)
        {
            var b = db.Bookings.Include(x => x.Session.Package).FirstOrDefault(x => x.BookingId == bookingId);
            if (b == null) return HttpNotFound();

            b.PaymentStatus = "Refunded";
            db.SaveChanges();

            TempData["Success"] = $"Marked booking #{b.BookingId} as Refunded.";
            return RedirectToAction("Dashboard");
        }

        // ========= REPORTS (basic + CSV export) =========
        [HttpGet]
        public ActionResult Reports(string from = null, string to = null, string export = null)
        {
            DateTime? fromDt = null, toDt = null;
            if (DateTime.TryParse(from, out var f)) fromDt = f.Date;
            if (DateTime.TryParse(to, out var t)) toDt = t.Date.AddDays(1).AddTicks(-1);

            var q = db.Bookings.Include(b => b.Session.Package).AsQueryable();
            if (fromDt.HasValue) q = q.Where(b => b.CreatedAt >= fromDt.Value);
            if (toDt.HasValue) q = q.Where(b => b.CreatedAt <= toDt.Value);

            var data = q
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new TourismManagementSystem.Models.ViewModels.ReportBookingRowVm
                {
                    BookingId = b.BookingId,
                    CustomerName = b.CustomerName,
                    PackageTitle = b.Session.Package.Title,
                    StartDate = b.Session.StartDate,
                    Participants = b.Participants,
                    Amount = (b.Session.Package.Price * b.Participants),
                    PaymentStatus = b.PaymentStatus,
                    CreatedAt = b.CreatedAt
                })
                .ToList();

            if (string.Equals(export, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("BookingId,Customer,Package,StartDate,Participants,Amount,PaymentStatus,CreatedAt");
                foreach (var r in data)
                {
                    sb.AppendLine(string.Join(",",
                        r.BookingId,
                        Csv(r.CustomerName),
                        Csv(r.PackageTitle),
                        r.StartDate.ToString("yyyy-MM-dd"),
                        r.Participants,
                        r.Amount.ToString("0.##"),
                        Csv(r.PaymentStatus),
                        r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                return File(bytes, "text/csv", $"booking-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
            }
            ViewBag.ActivePage = "Reports";
            ViewBag.From = from;
            ViewBag.To = to;
            return View(data);
        }

        private static string Csv(string s)
        {
            if (s == null) return "";
            var needsQuote = s.Contains(",") || s.Contains("\"") || s.Contains("\n");
            if (!needsQuote) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }


      

        [HttpGet]
        public ActionResult UserDetails(int id)
        {
            var u = db.Users
                .Include(x => x.Role)
                .Include(x => x.AgencyProfile)
                .Include(x => x.GuideProfile)
                .FirstOrDefault(x => x.UserId == id);
            if (u == null) return HttpNotFound();

            var vm = new UserDetailsVm
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                RoleName = u.Role.RoleName,
                IsActive = u.IsActive,
                IsApproved = u.IsApproved,
                CreatedAt = u.CreatedAt,
                AgencyName = u.AgencyProfile?.AgencyName,
                AgencyPhone = u.AgencyProfile?.Phone,
                AgencyWebsite = u.AgencyProfile?.Website,
                AgencyDocUrl = u.AgencyProfile?.VerificationDocPath,
                AgencyStatus = u.AgencyProfile?.Status,
                GuideFullNameOnLicense = u.GuideProfile?.FullNameOnLicense,
                GuideLicenseNo = u.GuideProfile?.GuideLicenseNo,
                GuideStatus = u.GuideProfile?.Status
            };

            ViewBag.ActivePage = "Users";
            return View(vm);
        }

    }
}
