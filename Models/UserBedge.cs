namespace SideQuestApp.Models
{
    public class UserBadge
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public int BadgeId { get; set; }
        public Badge? Badge { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }
}