using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuestApp.Data;
using SideQuestApp.Models;
using SideQuestApp.Services;

namespace SideQuestApp.Controllers
{
    [Authorize]
    public class FeedController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public FeedController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }
        

        //INDEX
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            var currentUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser == null)
                return Unauthorized();

            var friendships = await _context.Friendships
                .Where(f =>
                    f.Status == FriendshipStatus.Accepted &&
                    (f.RequesterId == userId ||
                     f.AddresseeId == userId))
                .ToListAsync();

            var friendIds = friendships
                .Select(f =>
                    f.RequesterId == userId
                        ? f.AddresseeId
                        : f.RequesterId)
                .ToList();

            var completions = await _context.QuestCompletions
                .Include(c => c.User)
                .Include(c => c.Quest)
                    .ThenInclude(q => q!.Category)
                .Include(c => c.Quest)
                    .ThenInclude(q => q!.Group)
                        .ThenInclude(g => g!.Memberships)
                .Include(c => c.Likes)
                .Where(c =>
                    c.Status == "Approved" &&
                    (
                        // My own completions
                        c.UserId == userId

                        ||

                        // Public quests follow normal feed visibility
                        (
                            c.Quest != null &&
                            c.Quest.GroupId == null &&
                            (
                                (c.User != null && !c.User.IsPrivate) ||
                                friendIds.Contains(c.UserId)
                            )
                        )

                        ||

                        // Group quests are visible ONLY to members of that group
                        (
                            c.Quest != null &&
                            c.Quest.GroupId.HasValue &&
                            c.Quest.Group != null &&
                            c.Quest.Group.Memberships
                                .Any(m => m.UserId == userId)
                        )
                    )
                )
                .OrderByDescending(c => c.ReviewedAt ?? c.SubmittedAt)
                .ToListAsync();

            return View(completions);
        }

        
        // LIKE 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            var completion = await _context.QuestCompletions
                .Include(c => c.User)
                .Include(c => c.Quest)
                    .ThenInclude(q => q!.Group)
                        .ThenInclude(g => g!.Memberships)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.Status == "Approved");

            if (completion == null || completion.User == null)
                return NotFound();

            if (completion.User.IsPrivate &&
                completion.UserId != userId)
            {
                var friendshipExists = await _context.Friendships
                    .AnyAsync(f =>
                        f.Status == FriendshipStatus.Accepted &&
                        (
                            (f.RequesterId == userId &&
                             f.AddresseeId == completion.UserId)
                            ||
                            (f.RequesterId == completion.UserId &&
                             f.AddresseeId == userId)
                        ));

                if (!friendshipExists)
                    return Forbid();
            }

            if (completion.Quest?.GroupId != null)
            {
                var isMember = await _context.GroupMemberships
                    .AnyAsync(gm =>
                        gm.GroupId == completion.Quest.GroupId.Value &&
                        gm.UserId == userId);

                if (!isMember && completion.UserId != userId)
                    return Forbid();
            }

            var existingLike = await _context.QuestCompletionLikes
                .FirstOrDefaultAsync(l =>
                    l.QuestCompletionId == id &&
                    l.UserId == userId);

            if (existingLike != null)
            {
                var likeCount = await _context.QuestCompletionLikes
                    .CountAsync(l =>
                        l.QuestCompletionId == id);

                return Json(new
                {
                    success = true,
                    liked = true,
                    likeCount
                });
            }

            var like = new QuestCompletionLike
            {
                UserId = userId,
                QuestCompletionId = id,
                LikedAt = DateTime.UtcNow
            };

            _context.QuestCompletionLikes.Add(like);

            await _context.SaveChangesAsync();

            if (completion.UserId != userId)
            {
                var liker = await _userManager.FindByIdAsync(userId);

                if (liker != null)
                {
                    string? questLink;

                    if (completion.Quest?.GroupId.HasValue == true)
                    {
                        questLink = Url.Action(
                            nameof(QuestController.GroupQuestDetails),
                            "Quest",
                            new
                            {
                                id = completion.QuestId
                            });
                    }
                    else
                    {
                        questLink = Url.Action(
                            nameof(QuestController.Details),
                            "Quest",
                            new
                            {
                                id = completion.QuestId
                            });
                    }

                    await _notificationService.CreateAsync(
                        completion.UserId,
                        "CompletionLiked",
                        "New like",
                        $"{liker.UserName} liked your quest completion.",
                        questLink
                    );
                }
            }

            var updatedLikeCount = await _context.QuestCompletionLikes
                .CountAsync(l =>
                    l.QuestCompletionId == id);

            return Json(new
            {
                success = true,
                liked = true,
                likeCount = updatedLikeCount
            });
        }

        
        //UNLIKE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            var like = await _context.QuestCompletionLikes
                .FirstOrDefaultAsync(l =>
                    l.QuestCompletionId == id &&
                    l.UserId == userId);

            if (like == null)
            {
                var currentLikeCount = await _context.QuestCompletionLikes
                    .CountAsync(l =>
                        l.QuestCompletionId == id);

                return Json(new
                {
                    success = true,
                    liked = false,
                    likeCount = currentLikeCount
                });
            }

            var completion = await _context.QuestCompletions
                .Include(c => c.User)
                .FirstOrDefaultAsync(c =>
                    c.Id == like.QuestCompletionId);

            if (completion == null || completion.User == null)
                return NotFound();

            var liker = await _userManager.FindByIdAsync(userId);

            _context.QuestCompletionLikes.Remove(like);

            if (liker != null &&
                completion.UserId != userId &&
                !string.IsNullOrWhiteSpace(liker.UserName))
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.UserId == completion.UserId &&
                        n.Type == "CompletionLiked" &&
                        n.Message != null &&
                        n.Message.Contains(liker.UserName));

                if (notification != null)
                {
                    _context.Notifications.Remove(notification);
                }
            }

            await _context.SaveChangesAsync();

            var likeCount = await _context.QuestCompletionLikes
                .CountAsync(l =>
                    l.QuestCompletionId == id);

            return Json(new
            {
                success = true,
                liked = false,
                likeCount
            });
        }

        

       
    }
}