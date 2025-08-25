using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TourismManagementSystem.Models
{

    public class AgencyProfile
    {
        [Key]
        public int ProfileId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string AgencyName { get; set; }

        public string Description { get; set; }

        public string LogoPath { get; set; }

        [Phone]
        public string Phone { get; set; }

        [Url]
        public string Website { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}