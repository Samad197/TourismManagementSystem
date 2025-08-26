using System;
using System.Collections.Generic;

namespace TourismManagementSystem.Models.ViewModels
{
    public class TouristBookingItemVm
    {
        public int BookingId { get; set; }
        public string PackageTitle { get; set; }
        public DateTime? StartDate { get; set; }   // <-- nullable
        public DateTime? EndDate { get; set; }     // <-- nullable
        public int Participants { get; set; }
        public string Status { get; set; }        // Pending | Confirmed | Completed | Cancelled
        public string PaymentStatus { get; set; } // Pending | Paid | Refunded
        public bool CanLeaveFeedback { get; set; } // Completed & no feedback yet
    }

    public class TouristDashboardVm
    {
        public List<TouristBookingItemVm> Upcoming { get; set; } = new List<TouristBookingItemVm>();
        public List<TouristBookingItemVm> Past { get; set; } = new List<TouristBookingItemVm>();
    }
}
