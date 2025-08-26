namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using TourismManagementSystem.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<TourismManagementSystem.Data.TourismDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(TourismManagementSystem.Data.TourismDbContext context)
        {
            // 1) Ensure roles
            string[] roleNames = { "Admin", "Agency", "Guide", "Tourist" };
            foreach (var r in roleNames)
            {
                if (!context.Roles.Any(x => x.RoleName == r))
                    context.Roles.Add(new Role { RoleName = r });
            }
            context.SaveChanges();

            // 2) Ensure admin user
            var adminEmail = "admin@gmail.com";
            if (!context.Users.Any(u => u.Email == adminEmail))
            {
                var adminRoleId = context.Roles.Single(x => x.RoleName == "Admin").RoleId;

                context.Users.Add(new User
                {
                    FullName = "Admin User",
                    Email = adminEmail,
                    PasswordHash = Sha256("Admin@123"),    // change later if you like
                    RoleId = adminRoleId,
                    IsActive = true,
                    IsApproved = true,                     // admin doesn’t need approval
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                });
                context.SaveChanges();
            }
        }


        // same hashing as your AccountController
        private static string Sha256(string s)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s ?? ""));
                return string.Concat(bytes.Select(b => b.ToString("x2")));
            }
        }
    }
}
