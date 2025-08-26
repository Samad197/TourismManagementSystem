using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TourismManagementSystem.Models.ViewModels
{
    // Public catalog list item
    public class PublicPackageListItemVm
    {
        public int PackageId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int MaxGroupSize { get; set; }
        public string ThumbnailPath { get; set; } // first image or placeholder
        public string OwnerType { get; set; }     // "Agency" / "Guide"
        public string OwnerName { get; set; }
        public double? AvgRating { get; set; }
        public bool HasUpcoming { get; set; }
    }

    // Public details page
    public class PublicPackageDetailsVm
    {
        public int PackageId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int MaxGroupSize { get; set; }
        public string OwnerType { get; set; }
        public string OwnerName { get; set; }
        public string HeroImagePath { get; set; }
        public List<string> Gallery { get; set; }
        public List<UpcomingSessionItem> UpcomingSessions { get; set; }
        public double? AvgRating { get; set; }

        public PublicPackageDetailsVm()
        {
            Gallery = new List<string>();
            UpcomingSessions = new List<UpcomingSessionItem>();
        }
    }

    //public class UpcomingSessionItem
    //{
    //    public int SessionId { get; set; }
    //    public DateTime StartDate { get; set; }
    //    public int AvailableSlots { get; set; }
    //    public int Booked { get; set; }
    //}

    // Agency: create/edit form
    public class PackageCreateVm
    {
        [Required, StringLength(100)]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Range(0, 999999)]
        public decimal Price { get; set; }

        [Range(1, 100)]
        public int DurationDays { get; set; }

        [Range(1, 1000)]
        public int MaxGroupSize { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }

    // Agency: my packages list row
    public class MyPackageListItemVm
    {
        public int PackageId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int MaxGroupSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SessionsCount { get; set; }
    }
    public class PackageBookingVm
    {
        public int PackageId { get; set; }
        [Required] public string FullName { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Range(1, 100)] public int Participants { get; set; }

        // Optional for displaying package title
        public string PackageTitle { get; set; }
    }

}
