using System.ComponentModel.DataAnnotations;

namespace SideQuestApp.Models
{
    public class QuestCompletion
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public int QuestId { get; set; }
        public Quest? Quest { get; set; }

        [Required]
        public string PhotoUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Caption { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }
        
        public List<QuestCompletionLike> Likes { get; set; } = new();
    }
}