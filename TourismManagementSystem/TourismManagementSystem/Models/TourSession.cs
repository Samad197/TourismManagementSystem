using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models
{
    public class TourSession
    {
        [Key]
        public int SessionId { get; set; }

        [Required]
        public int PackageId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Range(1, 1000)]
        public int AvailableSlots { get; set; }

        [ForeignKey("PackageId")]
        public virtual TourPackage Package { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; }
    }


}