using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using TourismManagementSystem.Data;
using TourismManagementSystem.Models;

namespace TourismManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly TourismDbContext db = new TourismDbContext();

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }


        // GET: Account/Register
        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            // Load roles from DB to show in dropdown
            ViewBag.Roles = db.Roles.ToList();
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User model, string ConfirmPassword)
        {
            // Reload roles in case of validation error
            ViewBag.Roles = db.Roles.ToList();

            // Model validation
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Confirm password check
            if (model.PasswordHash != ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View(model);
            }

            // Check if email already exists
            var existingUser = db.Users.FirstOrDefault(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ViewBag.Error = "Email already registered!";
                return View(model);
            }

            // Check if RoleId exists in DB (security)
            var selectedRole = db.Roles.FirstOrDefault(r => r.RoleId == model.RoleId);
            if (selectedRole == null)
            {
                ViewBag.Error = "Invalid role selected.";
                return View(model);
            }

            // Hash the password securely
            model.PasswordHash = HashPassword(model.PasswordHash);
            model.CreatedAt = DateTime.Now;

            // Save user to DB
            db.Users.Add(model);
            db.SaveChanges();

            TempData["Success"] = "Registration successful! Please login.";
            return RedirectToAction("Login", "Account");
        }

        // Password hashing method (unchanged)
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }



        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: Authenticate user here
                if (model.Email == "admin@example.com" && model.Password == "admin123")
                {
                    // Simulate login success (replace with real auth logic later)
                    TempData["Success"] = "Logged in successfully!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid email or password.");
            }
            return View(model);
        }
    }
}