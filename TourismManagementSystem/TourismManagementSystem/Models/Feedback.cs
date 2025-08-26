using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models
{

    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }      // <— NEW identity PK

        [Required]
        [Index("IX_Feedback_Booking", IsUnique = true)]  // one feedback per booking
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public virtual Booking Booking { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; }      // <— renamed from Comments

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }


}