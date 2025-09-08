namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using TourismManagementSystem.Models;

    internal sealed class Configuration
        : DbMigrationsConfiguration<TourismManagementSystem.Data.TourismDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(TourismManagementSystem.Data.TourismDbContext context)
        {
            // 1) Ensure Roles exist
            string[] roleNames = { "Admin", "Agency", "Guide", "Tourist" };

            foreach (var roleName in roleNames)
            {
                if (!context.Roles.Any(r => r.RoleName == roleName))
                {
                    context.Roles.Add(new Role { RoleName = roleName });
                }
            }
            context.SaveChanges();

            // 2) Ensure a default Admin user
            var adminEmail = "admin@gmail.com";
            if (!context.Users.Any(u => u.Email == adminEmail))
            {
                var adminRoleId = context.Roles.Single(r => r.RoleName == "Admin").RoleId;

                context.Users.Add(new User
                {
                    FullName = "Admin User",
                    Email = adminEmail,
                    PasswordHash = Sha256("admin123"),   // ⚠️ temporary password
                    RoleId = adminRoleId,
                    IsActive = true,
                    IsApproved = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                });

                context.SaveChanges();
            }
        }

        // helper for password hashing
        private static string Sha256(string input)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input ?? ""));
                return string.Concat(bytes.Select(b => b.ToString("x2")));
            }
        }
    }
}
