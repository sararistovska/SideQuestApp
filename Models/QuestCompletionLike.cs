using System.ComponentModel.DataAnnotations;

namespace SideQuestApp.Models
{
    public class QuestCompletionLike
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        public int QuestCompletionId { get; set; }

        public QuestCompletion? QuestCompletion { get; set; }

        public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    }
}