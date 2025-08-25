using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models
{

    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }

        // Foreign key for Roles table
        [Required]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        // Navigation property
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}