using Microsoft.AspNetCore.Identity;


namespace SideQuestApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public int Points { get; set; } = 0;
        public int Level { get; set; } = 1;
        
        public bool IsPrivate { get; set; } = false;
        public DateTime? LastMonthlyBonusDate { get; set; }
        
        
        public List<QuestCompletion> QuestCompletions { get; set; } = new();
        public List<UserBadge> UserBadges { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }

}