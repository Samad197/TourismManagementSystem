using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

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

        public bool HasUpcoming { get; set; }          // shows in future
        public bool HasAvailableSession { get; set; }  // available for booking
    }

    // Public details page
    public class PublicPackageDetailsVm
    {
        public int PackageId { get; set; }

        [Display(Name = "Package Title")]
        public string Title { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        public Booking ExistingBooking { get; set; } // NEW

        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "Duration (Days)")]
        public int DurationDays { get; set; }

        [Display(Name = "Max Group Size")]
        public int MaxGroupSize { get; set; }

        [Display(Name = "Owner Type")]
        public string OwnerType { get; set; } // Agency or Guide

        [Display(Name = "Owner Name")]
        public string OwnerName { get; set; }

        [Display(Name = "Hero Image")]
        public string HeroImagePath { get; set; }

      
        public List<string> Gallery { get; set; } // multiple images

        public List<UpcomingSessionItem> UpcomingSessions { get; set; }

        public List<ReviewItem> Reviews { get; set; }

        public double? AvgRating { get; set; }

        public PublicPackageDetailsVm()
        {
            Gallery = new List<string>();
            UpcomingSessions = new List<UpcomingSessionItem>();
            Reviews = new List<ReviewItem>();
        }
        public class ReviewItem
        {
            public string TouristName { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }

            public DateTime CreatedAt { get; set; }
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
        public int PackageId { get; set; } // ADD THIS LINE
        [Required, StringLength(100)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(1, 100)]
        public int DurationDays { get; set; }

        [Range(1, 1000)]
        public int MaxGroupSize { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // For image upload
        public IEnumerable<HttpPostedFileBase> Images { get; set; }

        // For edit view
        public List<TourImages> ExistingImages { get; set; }

        // For removing images
        public int[] removeImageIds { get; set; }
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
        public int? BookingId { get; set; } // nullable for new bookings

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }
        public int SessionId { get; set; }   // ✅ NEW, helps when you need to link back to the session


        [Required]
        [Range(1, 100, ErrorMessage = "Participants must be between 1 and 100")]
        public int Participants { get; set; } = 1;

        [Required(ErrorMessage = "Please select a session")]
        public int SelectedSessionId { get; set; }

        // Optional display
        public string PackageTitle { get; set; }
        public DateTime SelectedSessionDate { get; set; }
        public DateTime SelectedSessionEnd { get; set; }

        // Added for assignment
        public string Status { get; set; } = "Pending";        // Booking status (Pending / Approved / Rejected)
        public string PaymentStatus { get; set; } = "Pending"; // Payment status (Pending / Paid)

        public List<UpcomingSessionItem> UpcomingSessions { get; set; } = new List<UpcomingSessionItem>();
    }


}
