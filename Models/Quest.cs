using SideQuestApp.Models;

namespace SideQuestApp.Models
{
    public class Quest
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        
        public int PointsReward { get; set; } = 10;

        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Easy;

        public bool IsActive { get; set; } = true;
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public int? CategoryId { get; set; }

        public Category? Category { get; set; }

        public int? GroupId { get; set; }

        public Group? Group { get; set; }

        public List<QuestCompletion> Completions { get; set; } = new();
    }
}