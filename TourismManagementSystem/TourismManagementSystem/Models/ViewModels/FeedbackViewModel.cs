using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models.ViewModels
{
    public class FeedbackViewModel
    {
        public int FeedbackId { get; set; }
        public string CustomerName { get; set; }
        public string PackageTitle { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}