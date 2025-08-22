using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models
{
    public class User
    {
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

        [Required]
        [RegularExpression("Tourist|Agency|Admin", ErrorMessage = "Role must be Tourist, Agency, or Admin")]
        public string Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}