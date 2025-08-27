using System.Data.Entity;
using TourismManagementSystem.Models;

namespace TourismManagementSystem.Data
{
    public class TourismDbContext : DbContext
    {
        public TourismDbContext() : base("name=TourismDbContext") { }

        // Core auth
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        // Profiles
        public DbSet<TouristProfile> TouristProfiles { get; set; }
        public DbSet<AgencyProfile> AgencyProfiles { get; set; }
        public DbSet<GuideProfile> GuideProfiles { get; set; }   // NEW

        // Tours domain
        public DbSet<TourPackage> TourPackages { get; set; }
        //public DbSet<TourSession> TourSessions { get; set; } // NEW

        // ✅ Add this line
        public DbSet<Session> Sessions { get; set; }
                                                    // NEW
        public DbSet<TourImages> TourImages { get; set; }         // FIX: singular class

        // Booking / Feedback
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // ----------------------------------------
            // TourPackage owned by Agency or Guide
            // Both optional; no cascade on delete
            // ----------------------------------------
            modelBuilder.Entity<TourPackage>()
                .HasOptional(tp => tp.Agency)
                .WithMany()
                .HasForeignKey(tp => tp.AgencyId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TourPackage>()
                .HasOptional(tp => tp.Guide)
                .WithMany()
                .HasForeignKey(tp => tp.GuideId)
                .WillCascadeOnDelete(false);

            // ----------------------------------------
            // Session → TourPackage (required)
            // ----------------------------------------
            modelBuilder.Entity<Session>()
                .HasRequired(s => s.Package)
                .WithMany(p => p.Sessions)
                .HasForeignKey(s => s.PackageId)
                .WillCascadeOnDelete(false);

            // ----------------------------------------
            // TourImages → TourPackage (required)
            // Cascade delete is OK for images
            // ----------------------------------------
            modelBuilder.Entity<TourImages>()
                .HasRequired(i => i.Package)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PackageId)
                .WillCascadeOnDelete(true);

            // ----------------------------------------
            // Booking → Session (required)
            // ----------------------------------------
            modelBuilder.Entity<Booking>()
                .HasRequired(b => b.Session)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.SessionId)
                .WillCascadeOnDelete(false);

            // Booking → Tourist (required)
            modelBuilder.Entity<Booking>()
                .HasRequired(b => b.Tourist)
                .WithMany()
                .HasForeignKey(b => b.TouristId)
                .WillCascadeOnDelete(false);

            // ----------------------------------------
            // Feedback → Booking (1 → many)
            // One feedback per booking enforced by unique index in DB
            // ----------------------------------------
            modelBuilder.Entity<Feedback>()
                .HasRequired(f => f.Booking)
                .WithMany(b => b.Feedbacks)
                .HasForeignKey(f => f.BookingId)
                .WillCascadeOnDelete(true);

            // ----------------------------------------
            // User → AgencyProfile (shared PK, 1 ↔ 0..1)
            // ----------------------------------------
            modelBuilder.Entity<User>()
                .HasOptional(u => u.AgencyProfile)
                .WithRequired(p => p.User);

            // User → GuideProfile (shared PK, 1 ↔ 0..1)
            modelBuilder.Entity<User>()
                .HasOptional(u => u.GuideProfile)
                .WithRequired(p => p.User);

            // ----------------------------------------
            // Optional: Price precision for TourPackage
            // ----------------------------------------
            modelBuilder.Entity<TourPackage>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            base.OnModelCreating(modelBuilder);
        }


    }
}
