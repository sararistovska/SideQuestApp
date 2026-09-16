namespace SideQuestApp.Models
{
    public enum FriendshipStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class Friendship
    {
        public int Id { get; set; }

        public string RequesterId { get; set; } = string.Empty;
        public ApplicationUser? Requester { get; set; }

        public string AddresseeId { get; set; } = string.Empty;
        public ApplicationUser? Addressee { get; set; }

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}