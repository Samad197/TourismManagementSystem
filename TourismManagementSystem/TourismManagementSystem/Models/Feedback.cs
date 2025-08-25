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
        public int FeedbackId { get; set; }

        [Required]
        public int TouristId { get; set; }

        [Required]
        public int PackageId { get; set; }

        [StringLength(1000)]
        public string Comments { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [ForeignKey("TouristId")]
        public virtual TouristProfile Tourist { get; set; }

        [ForeignKey("PackageId")]
        public virtual TourPackage TourPackage { get; set; }
    }
}