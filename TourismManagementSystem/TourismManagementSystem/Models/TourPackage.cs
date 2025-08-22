using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models
{
    public class TourPackage
    {
        [Key]
        public int PackageId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Range(0, 99999)]
        public decimal Price { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Range(1, 1000)]
        public int MaxGroupSize { get; set; }

        [Required]
        public int AgencyId { get; set; }

        [ForeignKey("AgencyId")]
        public virtual AgencyProfile Agency { get; set; }
    }
}