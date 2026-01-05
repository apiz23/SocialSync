using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialSync.Models
{
    public class Group
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Group name is required")]
        [Display(Name = "Group Name")]
        [StringLength(150, ErrorMessage = "Group name cannot exceed 150 characters")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Display(Name = "Category")]
        [StringLength(50)]
        public string? Category { get; set; }

        [Required]
        [Display(Name = "Created By")]
        public Guid CreatedBy { get; set; }

        [Display(Name = "Date Created")]
        [DataType(DataType.DateTime)]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        // Collection navigation property
        public virtual ICollection<GroupMember>? GroupMembers { get; set; }
    }

    public class GroupMember
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Group")]
        public Guid GroupId { get; set; }

        [Required]
        [Display(Name = "Member")]
        public Guid UserId { get; set; }

        [Display(Name = "Joined Date")]
        [DataType(DataType.DateTime)]
        public DateTime JoinedDate { get; set; } = DateTime.Now;

        [Display(Name = "Role")]
        [StringLength(50)]
        public string? Role { get; set; } = "Member";

        // Navigation properties
        [ForeignKey("GroupId")]
        public virtual Group? Group { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}