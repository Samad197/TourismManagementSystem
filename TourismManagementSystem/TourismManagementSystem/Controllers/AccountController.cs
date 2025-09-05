using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using System.Security.Cryptography;
using System.Data.Entity;                     // <-- needed for Include
using TourismManagementSystem.Data;
using TourismManagementSystem.Models;
using TourismManagementSystem.Models.ViewModels;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data;
namespace TourismManagementSystem.Controllers
{
    // Inherit BaseController so ViewBag.IsApproved is set automatically
    public class AccountController : BaseController
    {
        // Use the db from BaseController if you like; keeping your local for clarity:
        private readonly TourismDbContext db = new TourismDbContext();

        // GET: /Account
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }

        // GET: /Account/Register
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            ViewBag.Roles = db.Roles
                              .Where(r => r.RoleName != "Admin") // hide Admin in UI
                              .OrderBy(r => r.RoleName)
                              .ToList();
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [AllowAnonymous]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel vm)
        {
            ViewBag.Roles = db.Roles
                              .Where(r => r.RoleName != "Admin")
                              .OrderBy(r => r.RoleName)
                              .ToList();

            if (!ModelState.IsValid) return View(vm);

            // 1) Fast checks
            if (db.Users.AsNoTracking().Any(u => u.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(vm);
            }

            var role = db.Roles.Find(vm.RoleId);
            if (role == null || role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("RoleId", "Invalid role selection.");
                return View(vm);
            }

            var roleName = role.RoleName.Trim();

            // 2) Build entities (do NOT SaveChanges yet)
            var user = new User
            {
                FullName = vm.FullName.Trim(),
                Email = vm.Email.Trim(),
                PasswordHash = HashPassword(vm.Password),
                RoleId = vm.RoleId,
                EmailConfirmed = false,
                IsActive = true,
                IsApproved = roleName.Equals("Tourist", StringComparison.OrdinalIgnoreCase),
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);

            if (roleName.Equals("Tourist", StringComparison.OrdinalIgnoreCase))
            {
                db.TouristProfiles.Add(new TouristProfile { User = user });
            }
            else if (roleName.Equals("Agency", StringComparison.OrdinalIgnoreCase))
            {
                // ✅ satisfy [Required] AgencyName
                var agencyName = (vm.FullName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(agencyName)) agencyName = "New Agency";
                if (agencyName.Length > 100) agencyName = agencyName.Substring(0, 100);

                db.AgencyProfiles.Add(new AgencyProfile
                {
                    User = user,
                    AgencyName = user.FullName,
                    Description = "",
                    Status = "PendingVerification"
                });
            }
            else if (roleName.Equals("Guide", StringComparison.OrdinalIgnoreCase))
            {
                db.GuideProfiles.Add(new GuideProfile
                {
                    User = user,
                    FullNameOnLicense = user.FullName,
                    GuideLicenseNo = "",
                    Bio = "",
                    Status = "PendingVerification"
                });
            }
            else
            {
                ModelState.AddModelError("RoleId", "Unsupported role selected.");
                return View(vm);
            }

            // 3) Atomic commit
            using (var tx = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (DbEntityValidationException vex)
                {
                    tx.Rollback();
                    var sb = new StringBuilder();
                    foreach (var e in vex.EntityValidationErrors)
                        foreach (var ve in e.ValidationErrors)
                            sb.AppendLine($"{ve.PropertyName}: {ve.ErrorMessage}");
                    ModelState.AddModelError("", "Validation failed: " + sb);
                    return View(vm);
                }
                catch (DbUpdateException)
                {
                    tx.Rollback();
                    ModelState.AddModelError("", "Could not complete registration. The email may already be registered or data is invalid.");
                    return View(vm);
                }
                catch (Exception)
                {
                    tx.Rollback();
                    ModelState.AddModelError("", "Unexpected error while registering. Please try again.");
                    return View(vm);
                }
            }

            // 4) Post-commit redirects (make it exhaustive!)
            if (roleName.Equals("Tourist", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Success"] = "Account created. Please login.";
                return RedirectToAction("Login", "Account");
            }
            else if (roleName.Equals("Agency", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Info"] = "Account created. Please login, then complete your Agency profile for approval.";
                var returnUrl = Url.Action("Profile", "Agency"); // or CompleteAgencyProfile if you added it
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            else if (roleName.Equals("Guide", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Info"] = "Account created. Please login, then complete your Guide profile for approval.";
                var returnUrl = Url.Action("Profile", "Guide"); // or CompleteGuideProfile
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            else
            {
                // Failsafe (should never hit because of earlier checks)
                TempData["Success"] = "Account created. Please login.";
                return RedirectToAction("Login", "Account");
            }
        }



        // GET: /Account/Login
        // GET: /Account/Login
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [AllowAnonymous]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel vm, string returnUrl)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = db.Users.Include(u => u.Role)
                               .FirstOrDefault(u => u.Email == vm.Email);

            if (user == null || user.PasswordHash != HashPassword(vm.Password) || !user.IsActive)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(vm);
            }

            var roleName = (user.Role.RoleName ?? "").Trim();

            // Issue Forms auth ticket WITH role in UserData
            var ticket = new FormsAuthenticationTicket(
                1,
                user.Email,
                DateTime.Now,
                DateTime.Now.AddHours(6),
                vm.RememberMe,
                roleName // <— put the role here
            );

            var enc = FormsAuthentication.Encrypt(ticket);
            var cookie = new System.Web.HttpCookie(FormsAuthentication.FormsCookieName, enc)
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL
            };
            Response.Cookies.Add(cookie);

            // (Optional) convenience for legacy code
            Session["RoleName"] = roleName;

            // Role-based landing with approval gates
            if (roleName.Equals("Tourist", StringComparison.OrdinalIgnoreCase))
                return SafeRedirect(returnUrl, "MyBookings", "Tourist");

            if (roleName.Equals("Agency", StringComparison.OrdinalIgnoreCase))
                return user.IsApproved
                    ? SafeRedirect(returnUrl, "Dashboard", "Provider")
                    : RedirectToAction("Dashboard", "Provider");

            if (roleName.Equals("Guide", StringComparison.OrdinalIgnoreCase))
                return user.IsApproved
                    ? SafeRedirect(returnUrl, "Dashboard", "Provider")
                    : RedirectToAction("Dashboard", "Provider");

            if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Index", "Home");
        }

        // Helper: prevents null URL issues & only allows local returnUrl
        private ActionResult SafeRedirect(string returnUrl, string fallbackAction, string fallbackController)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(fallbackAction, fallbackController);
        }


        // POST: /Account/Logout
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Settings()
        {
            var email = User.Identity.Name;
            var me = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == email);
            if (me == null) return RedirectToAction("Login");
            return View(me); // or a SettingsViewModel
        }

        // ===== helpers =====
        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private ActionResult SafeRedirect(string returnUrl, string fallBackUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect(fallBackUrl);
        }
    }
}
