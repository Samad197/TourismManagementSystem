// Models/ViewModels/SessionViewModels.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace TourismManagementSystem.Models.ViewModels
{
    public class SessionFormVm
    {
        public int? SessionId { get; set; }
        public int PackageId { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Capacity { get; set; }

        [Display(Name = "Cancel this session")]
        public bool IsCanceled { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
    }
}
