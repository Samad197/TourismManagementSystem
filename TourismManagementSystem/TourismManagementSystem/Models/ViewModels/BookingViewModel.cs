using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models.ViewModels
{
    public class BookingViewModel
    {
        public int BookingId { get; set; }
        public string PackageTitle { get; set; }
        public string CustomerName { get; set; }
        public DateTime StartDate { get; set; }
        public int Participants { get; set; }
        public string PaymentStatus { get; set; }
        public decimal Amount { get; set; }
        public bool? IsApproved { get; set; } // null=pending
    }

}