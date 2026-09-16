
using SideQuestApp.Models;

namespace SideQuestApp.ViewModels
{
    public class FriendsViewModel
    {
        public string CurrentUserId { get; set; } = string.Empty;

        public List<Friendship> Friendships { get; set; } = new();

        public List<ApplicationUser> Friends { get; set; } = new();

        public List<Friendship> IncomingRequests { get; set; } = new();

        public List<ApplicationUser> SearchResults { get; set; } = new();

        public List<ApplicationUser> Suggestions { get; set; } = new();

        public string? SearchTerm { get; set; }

        public HashSet<string> FriendIds { get; set; } = new();

        public HashSet<string> IncomingUserIds { get; set; } = new();

        public HashSet<string> OutgoingUserIds { get; set; } = new();
    }
}