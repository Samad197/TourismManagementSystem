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
        [ForeignKey("SessionId")] public virtual TourSession Session { get; set; }

        [Range(1, 1000)] public int Participants { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";        // Pending | Confirmed | Completed | Cancelled

        [Required, StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";  // Pending | Paid | Refunded

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // EF-level 1→many; DB enforces one-per-booking via unique index on Feedback.BookingId
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }





}