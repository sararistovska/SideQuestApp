namespace SideQuestApp.Models
{
    public enum BadgeCriteriaType
    {
        CompletionCount,
        CategoryCount
    }

    public class Badge
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }

        public BadgeCriteriaType CriteriaType { get; set; }
        public int RequiredCount { get; set; }

        
        public int? RequiredCategoryId { get; set; }
        public Category? RequiredCategory { get; set; }

        public List<UserBadge> UserBadges { get; set; } = new();
    }
}