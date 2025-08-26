using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models.ViewModels
{
    public class GuideListItemVm
    {
        public int ProfileId { get; set; }
        public string FullName { get; set; }
        public string Bio { get; set; }
        public string PhotoPath { get; set; }
        public string Phone { get; set; }

        public int TotalTours { get; set; }       // how many packages owned by this guide
        public double? AvgRating { get; set; }    // average feedback rating across their bookings
    }
}