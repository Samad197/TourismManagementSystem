using System.ComponentModel.DataAnnotations;

namespace TourismManagementSystem.Models
{
    public class Tourist
    {
        [Key]
        public int TouristId { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }
    }
}
