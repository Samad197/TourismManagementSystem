using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models
{

    public class Booking
    {
        [Key] public int BookingId { get; set; }

        public int TouristId { get; set; }
        [ForeignKey("TouristId")] public virtual User Tourist { get; set; }

        public int SessionId { get; set; }
        [ForeignKey("SessionId")]
        public virtual Session Session { get; set; }  // <-- use 'Session' instead of 'TourSession'

        // Add this property inside Booking class
        public bool? IsApproved { get; set; } // null = pending, true = approved, false = rejected


        [Range(1, 1000)] public int Participants { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";        // Pending | Confirmed | Completed | Cancelled

        [Required, StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";  // Pending | Paid | Refunded

        // ✅ Add this:
        public DateTime? PaidAt { get; set; }

        // **New property for quick access**
        [Required, StringLength(150)]
        public string CustomerName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // EF-level 1→many; DB enforces one-per-booking via unique index on Feedback.BookingId
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }





}