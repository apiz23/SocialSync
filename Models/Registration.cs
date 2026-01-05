using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialSync.Models
{
    public class Registration
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Event is required")]
        [Display(Name = "Event")]
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "User is required")]
        [Display(Name = "User")]
        public Guid UserId { get; set; }

        [Required]
        [Display(Name = "Registration Date")]
        [DataType(DataType.Date)]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Registration Status")]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}