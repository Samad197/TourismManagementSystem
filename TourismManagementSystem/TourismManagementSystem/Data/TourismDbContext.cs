using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using TourismManagementSystem.Models;

namespace TourismManagementSystem.Data
{
    public class TourismDbContext : DbContext
    {
        public TourismDbContext() : base("name=TourismDbContext")
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TouristProfile> TouristProfiles { get; set; }
        public DbSet<AgencyProfile> AgencyProfiles { get; set; }
        public DbSet<TourPackage> TourPackages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<TourImages> TourImages { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Disable cascade delete from TourPackage to AgencyProfile
            modelBuilder.Entity<TourPackage>()
                .HasRequired(tp => tp.Agency)
                .WithMany()
                .HasForeignKey(tp => tp.AgencyId)
                .WillCascadeOnDelete(false);

            // Disable cascade delete from Booking to TourPackage
            modelBuilder.Entity<Booking>()
                .HasRequired(b => b.TourPackage)
                .WithMany()
                .HasForeignKey(b => b.PackageId)
                .WillCascadeOnDelete(false);

            // Disable cascade delete from Feedback to TourPackage
            modelBuilder.Entity<Feedback>()
                .HasRequired(f => f.TourPackage)
                .WithMany()
                .HasForeignKey(f => f.PackageId)
                .WillCascadeOnDelete(false);

            // Optionally disable other cascade delete paths if needed
            modelBuilder.Entity<Booking>()
                .HasRequired(b => b.Tourist)
                .WithMany()
                .HasForeignKey(b => b.TouristId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Feedback>()
                .HasRequired(f => f.Tourist)
                .WithMany()
                .HasForeignKey(f => f.TouristId)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
