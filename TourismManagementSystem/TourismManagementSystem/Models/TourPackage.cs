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
        [Key] public int PackageId { get; set; }

        [Required, StringLength(100)] public string Title { get; set; }
        public string Description { get; set; }

        // Precision for currency (either data annotation or Fluent API)
        //[Column(TypeName = "money")]
        public decimal Price { get; set; }

        public virtual ICollection<Review> Reviews { get; set; }


        [Range(1, 100)] public int DurationDays { get; set; }
        [Range(1, 1000)] public int MaxGroupSize { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Index] public int? AgencyId { get; set; }     // stores AgencyProfile.UserId
        [Index] public int? GuideId { get; set; }     // stores GuideProfile.UserId

        [ForeignKey("AgencyId")] public virtual AgencyProfile Agency { get; set; }
        [ForeignKey("GuideId")] public virtual GuideProfile Guide { get; set; }

        public virtual ICollection<TourImages> Images { get; set; }
        public virtual ICollection<Session> Sessions { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }


}