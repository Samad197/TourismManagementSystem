using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models
{

    public class TourImages
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int PackageId { get; set; }

        [Required, StringLength(255)]
        public string ImagePath { get; set; }

        [ForeignKey("PackageId")]
        public virtual TourPackage Package { get; set; }
    }
}