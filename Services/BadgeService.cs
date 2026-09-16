using Microsoft.EntityFrameworkCore;
using SideQuestApp.Data;
using SideQuestApp.Models;

namespace SideQuestApp.Services
{
    public class BadgeService
    {
        private readonly ApplicationDbContext _context;

        public BadgeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Badge>> CheckAndAwardBadgesAsync(string userId)
        {
            var allBadges = await _context.Badges
                .Include(b => b.RequiredCategory)
                .ToListAsync();

            var alreadyEarnedBadgeIds = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            var approvedCompletions = await _context.QuestCompletions
                .Include(qc => qc.Quest)
                .ThenInclude(q => q!.Category)
                .Where(qc =>
                    qc.UserId == userId &&
                    qc.Status == "Approved")
                .ToListAsync();

            var newlyEarnedBadges = new List<Badge>();

            foreach (var badge in allBadges)
            {
                if (alreadyEarnedBadgeIds.Contains(badge.Id))
                    continue;

                if (IsCriteriaMet(badge, approvedCompletions))
                {
                    _context.UserBadges.Add(new UserBadge
                    {
                        UserId = userId,
                        BadgeId = badge.Id,
                        EarnedAt = DateTime.UtcNow
                    });

                    newlyEarnedBadges.Add(badge);
                }
            }

            await _context.SaveChangesAsync();

            return newlyEarnedBadges;
        }

        private bool IsCriteriaMet(Badge badge, List<QuestCompletion> approvedCompletions)
        {
            if (badge.CriteriaType == BadgeCriteriaType.CompletionCount)
            {
                return approvedCompletions.Count >= badge.RequiredCount;
            }

            if (badge.CriteriaType == BadgeCriteriaType.CategoryCount && badge.RequiredCategoryId.HasValue)
            {
                var countInCategory = approvedCompletions
                    .Count(c => c.Quest != null && c.Quest.CategoryId == badge.RequiredCategoryId);
                return countInCategory >= badge.RequiredCount;
            }

            return false;
        }
    }
}