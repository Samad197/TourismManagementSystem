// Models/Session.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourismManagementSystem.Models
{
    public class Session : IValidatableObject
    {
        [Key]
        public int SessionId { get; set; }

        [Required]
        [ForeignKey(nameof(Package))]
        public int PackageId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
        public int Capacity { get; set; }

        public bool IsCanceled { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual TourPackage Package { get; set; }

        // NEW: Add bookings to calculate available seats
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (EndDate < StartDate)
                yield return new ValidationResult("End Date must be on or after Start Date.", new[] { nameof(EndDate) });
        }
    }


}
