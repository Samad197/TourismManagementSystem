using System;

namespace TourismManagementSystem.Models.ViewModels
{
    public class AdminDashboardVm
    {
        public int PendingAgencies { get; set; }
        public int PendingGuides { get; set; }
        public int TotalUsers { get; set; }
        public int TotalBookings { get; set; }
        public decimal PaidRevenue { get; set; }
    }

    public class PendingUserVm
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }   // Agency or Guide
        public string ProfileStatus { get; set; } // PendingVerification / Approved
    }

    public class AdminUserListItemVm
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
