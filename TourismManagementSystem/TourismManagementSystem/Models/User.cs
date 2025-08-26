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

        [Required, StringLength(100)]
        public string FullName { get; set; }

        [Required, EmailAddress, StringLength(256)]
        // If you’re on EF 6.1+, this creates a unique index in SQL
        [Index("IX_User_Email", IsUnique = true)]
        public string Email { get; set; }

        // Store a HASH here (not the plain password)
        // Don’t validate hash length with UI rules; do UI validation in a ViewModel.
        [Required, StringLength(512)]
        public string PasswordHash { get; set; }

        // Role FK
        [Required, Display(Name = "Role")]
        public int RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public virtual Role Role { get; set; }

        // === NEW / IMPORTANT FOR YOUR FLOW ===
        // Agency/Guide must be approved by Admin; Tourist can be auto-true at registration.
        public bool IsApproved { get; set; } = false;

        // Optional but useful (email confirmation link flow)
        public bool EmailConfirmed { get; set; } = false;

        // Optional status switch (handy in Admin -> Manage Users)
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // NEW: navigation properties (optional per role)
        public virtual AgencyProfile AgencyProfile { get; set; }
        public virtual GuideProfile GuideProfile { get; set; }
    }
}