namespace SideQuestApp.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string InviteCode { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }

        public string CreatedByUserId { get; set; } = string.Empty;
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<GroupMembership> Memberships { get; set; } = new();
        public List<Quest> Quests { get; set; } = new();
    }
}