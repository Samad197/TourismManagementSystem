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
        // PK = FK to User (shared primary key 1↔0..1)
        [Key, ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(100)]
        public string AgencyName { get; set; }

        public string Description { get; set; }
        public string LogoPath { get; set; }

        [Phone] public string Phone { get; set; }
        [Url] public string Website { get; set; }

        [Required, StringLength(30)]
        public string Status { get; set; } = "PendingVerification"; // or Approved/Rejected

        public string VerificationDocPath { get; set; }
    }

    public interface IProviderProfile
    {
        int UserId { get; set; }
        string Phone { get; set; }
        string VerificationDocPath { get; set; }
        string Status { get; set; }
    }


}