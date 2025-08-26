using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using TourismManagementSystem.Models.ViewModels;
namespace TourismManagementSystem.Models
{
    public class TouristProfile
    {
        [Key]
        public int ProfileId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Phone]
        public string Phone { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }

}