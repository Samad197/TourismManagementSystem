using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models.ViewModels
{
    public class AdminDashboardVm
    {
        public int PendingAgencies { get; set; }
        public int PendingGuides { get; set; }
        public int TotalUsers { get; set; }
        public int TotalBookings { get; set; }
        public decimal PaidRevenue { get; set; }

        public List<AdminRecentBookingVm> RecentBookings { get; set; } = new List<AdminRecentBookingVm>();
        public List<AdminPaymentQueueVm> PaymentQueue { get; set; } = new List<AdminPaymentQueueVm>();
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

    public class ReportBookingRowVm
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string PackageTitle { get; set; }
        public DateTime StartDate { get; set; }
        public int Participants { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Optional, only if you keep the "View Details" link in Approvals/Users
    public class UserDetailsVm
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }

        // Agency
        public string AgencyName { get; set; }
        public string AgencyPhone { get; set; }
        public string AgencyWebsite { get; set; }
        public string AgencyDocUrl { get; set; }
        public string AgencyStatus { get; set; }

        // Guide
        public string GuideFullNameOnLicense { get; set; }
        public string GuideLicenseNo { get; set; }
        public string GuideStatus { get; set; }
    }
    public class AdminRecentBookingVm
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Amount { get; set; }
        public int Participants { get; set; }
        public string PackageTitle { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class AdminPaymentQueueVm
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string PackageTitle { get; set; }
        public DateTime StartDate { get; set; }
    }
}
