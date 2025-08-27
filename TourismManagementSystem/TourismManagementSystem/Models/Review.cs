using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourismManagementSystem.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int PackageId { get; set; } // Foreign key to TourPackage

        [ForeignKey("PackageId")]
        public virtual TourPackage Package { get; set; }

        [Required]
        public int TouristId { get; set; } // Foreign key to Tourist

        [ForeignKey("TouristId")]
        public virtual Tourist Tourist { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } // e.g., 1-5 stars

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
