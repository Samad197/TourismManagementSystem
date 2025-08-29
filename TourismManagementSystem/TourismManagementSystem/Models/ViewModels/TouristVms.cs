using System;
using System.ComponentModel.DataAnnotations;

namespace TourismManagementSystem.Models.ViewModels
{
    public class TouristBookingRowVm
    {
        public int BookingId { get; set; }
        public int PackageId { get; set; }
        public string PackageTitle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Participants { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public bool? IsApproved { get; set; }

        // For UI
        public bool IsCompleted { get; set; }
        public bool CanReview { get; set; }
        public int? ExistingFeedbackId { get; set; }
        public decimal? Amount { get; set; }
    }

    public class TouristProfileVm
    {
        [Required, StringLength(150)]
        public string FullName { get; set; }

        [EmailAddress]
        public string Email { get; set; } // read-only in UI

        [StringLength(30)]
        public string Phone { get; set; }
    }

    public class CreateReviewVm
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public int PackageId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required, StringLength(1000)]
        public string Comment { get; set; }

        public string PackageTitle { get; set; } // for header
        public DateTime SessionStart { get; set; }
    }
}
