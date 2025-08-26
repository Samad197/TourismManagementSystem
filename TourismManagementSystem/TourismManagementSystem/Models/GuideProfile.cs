using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourismManagementSystem.Models
{
    public class GuideProfile
    {
        // PK = FK to User (shared primary key pattern)
        [Key, ForeignKey("User")]
        public int UserId { get; set; }

        public virtual User User { get; set; }

        [Required, StringLength(100)]
        public string FullNameOnLicense { get; set; }

        [StringLength(50)]
        public string GuideLicenseNo { get; set; }

        public string Bio { get; set; }
        public string PhotoPath { get; set; }
        public string VerificationDocPath { get; set; }

        // Approval like Agency
        [Required, StringLength(30)]
        public string Status { get; set; } = "PendingVerification";

        [Phone]
        public string Phone { get; set; }
    }
}
