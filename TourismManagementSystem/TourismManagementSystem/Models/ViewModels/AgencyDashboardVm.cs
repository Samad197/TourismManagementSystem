using System;
using System.Collections.Generic;

namespace TourismManagementSystem.Models.ViewModels
{
    public class AgencyDashboardVm
    {
        // Header
        public string AgencyName { get; set; }
        public bool IsApproved { get; set; }

        // KPIs
        public int TotalPackages { get; set; }
        public int UpcomingSessions { get; set; }
        public int TotalBookings { get; set; }
        public decimal PaidRevenue { get; set; }
        public int PendingPayments { get; set; }
        public int FeedbackCount { get; set; }

        // Lists (lightweight)
        public List<UpcomingSessionItem> NextSessions { get; set; } = new List<UpcomingSessionItem>();
        public List<RecentBookingItem> RecentBookings { get; set; } = new List<RecentBookingItem>();
        public List<RecentFeedbackItem> RecentFeedback { get; set; } = new List<RecentFeedbackItem>();
    }

    public class UpcomingSessionItem
    {
        public int SessionId { get; set; }
        public string PackageTitle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Capacity { get; set; }
        public int Booked { get; set; }
        // Convenience property
        public int AvailableSeats => Capacity - Booked;
    }

    public class RecentBookingItem
    {
        public int BookingId { get; set; }
        public string PackageTitle { get; set; }
        public DateTime StartDate { get; set; }
        public int Participants { get; set; }
        public string PaymentStatus { get; set; } // Paid/Pending
        public decimal Amount { get; set; }
        // Add these two properties
        public string CustomerName { get; set; }
        public bool? IsApproved { get; set; }

    }

    public class RecentFeedbackItem
    {
        public int FeedbackId { get; set; }
        public string PackageTitle { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
