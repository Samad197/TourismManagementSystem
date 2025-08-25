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
        public int BookingId { get; set; }

        [Required]
        public int TouristId { get; set; }

        [Required]
        public int PackageId { get; set; }

        [Range(1, 100)]
        public int NumberOfPeople { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required]
        [RegularExpression("Pending|Confirmed|Completed|Cancelled")]
        public string Status { get; set; }

        [ForeignKey("TouristId")]
        public virtual TouristProfile Tourist { get; set; }

        [ForeignKey("PackageId")]
        public virtual TourPackage TourPackage { get; set; }
    }
}