namespace SideQuestApp.Models
{
    public class GroupMembership
    {
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        public int GroupId { get; set; }

        public Group? Group { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}