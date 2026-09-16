using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SideQuestApp.Data;
using SideQuestApp.Models;
using SideQuestApp.Services;

namespace SideQuestApp.Controllers
{
    [Authorize]
    public class QuestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly NotificationService _notificationService;
        private readonly BadgeService _badgeService;

        public QuestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            NotificationService notificationService,
            BadgeService badgeService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _notificationService = notificationService;
            _badgeService = badgeService;
        }


        // PUBLIC QUEST BOARD

        public async Task<IActionResult> Index(int? categoryId)
        {
            var quests = _context.Quests
                .Include(q => q.Category)
                .Include(q => q.Completions)
                .Where(q =>
                    q.GroupId == null &&
                    q.IsActive);

            if (categoryId.HasValue)
            {
                quests = quests.Where(q =>
                    q.CategoryId == categoryId.Value);
            }

            var result = await quests
                .OrderByDescending(q => q.Id)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.SelectedCategoryId = categoryId;

            return View(result);
        }


        // PUBLIC QUEST DETAILS

        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var quest = await _context.Quests
                .Include(q => q.Category)
                .Include(q => q.Completions)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.GroupId == null);

            if (quest == null)
                return NotFound();

            ViewBag.IsLoggedIn =
                User.Identity?.IsAuthenticated == true;

            ViewBag.IsAdmin =
                User.IsInRole("Admin");


            // COMMUNITY COMPLETIONS

            var approvedCompletions =
                quest.Completions
                    .Where(c =>
                        c.Status == "Approved" &&
                        c.User != null &&
                        (
                            !c.User.IsPrivate ||
                            c.UserId == userId
                        ))
                    .OrderByDescending(c => c.SubmittedAt)
                    .ToList();

            ViewBag.CommunityCompletions =
                approvedCompletions;

            ViewBag.CompletionCount =
                approvedCompletions.Count;


            // DEFAULT USER STATE

            ViewBag.HasCompleted = false;
            ViewBag.HasPendingCompletion = false;
            ViewBag.HasRejectedCompletion = false;
            ViewBag.CanStart = false;


            // LOGGED-IN USER

            if (userId != null)
            {
                var userCompletions =
                    await _context.QuestCompletions
                        .Where(c =>
                            c.QuestId == quest.Id &&
                            c.UserId == userId)
                        .OrderByDescending(c => c.SubmittedAt)
                        .ToListAsync();


                var hasCompleted =
                    userCompletions.Any(c =>
                        c.Status == "Approved");

                ViewBag.HasCompleted =
                    hasCompleted;


                var hasPendingCompletion =
                    userCompletions.Any(c =>
                        c.Status == "Pending");

                ViewBag.HasPendingCompletion =
                    hasPendingCompletion;


                var hasRejectedCompletion =
                    !hasCompleted &&
                    !hasPendingCompletion &&
                    userCompletions.Any(c =>
                        c.Status == "Rejected");

                ViewBag.HasRejectedCompletion =
                    hasRejectedCompletion;


                ViewBag.CanStart =
                    !hasCompleted &&
                    !hasPendingCompletion &&
                    quest.IsActive;
            }

            return View(quest);
        }


        // GROUP QUEST DETAILS

        [Authorize]
        public async Task<IActionResult> GroupQuestDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });

            var quest = await _context.Quests
                .Include(q => q.Group)
                    .ThenInclude(g => g!.Memberships)
                        .ThenInclude(m => m.User)
                .Include(q => q.CreatedByUser)
                .Include(q => q.Completions)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.GroupId != null);

            if (quest == null || quest.Group == null)
                return NotFound();

            var isMember =
                quest.Group.Memberships
                    .Any(m => m.UserId == userId);

            if (!isMember)
                return Forbid();

            var approvedCompletions =
                quest.Completions
                    .Where(c => c.Status == "Approved")
                    .OrderByDescending(c => c.SubmittedAt)
                    .ToList();

            var completionIds =
                approvedCompletions
                    .Select(c => c.Id)
                    .ToList();

            var likes =
                await _context.QuestCompletionLikes
                    .Where(l =>
                        completionIds.Contains(
                            l.QuestCompletionId))
                    .ToListAsync();

            var likeCounts =
                likes
                    .GroupBy(l => l.QuestCompletionId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Count());

            var likedByCurrentUser =
                likes
                    .Where(l => l.UserId == userId)
                    .Select(l => l.QuestCompletionId)
                    .ToHashSet();

            ViewBag.Group = quest.Group;
            ViewBag.Completions = approvedCompletions;
            ViewBag.LikeCounts = likeCounts;
            ViewBag.LikedByCurrentUser = likedByCurrentUser;
            ViewBag.IsMember = true;
            ViewBag.IsCreator =
                quest.CreatedByUserId == userId;

            return View(quest);
        }


        // COMPLETE QUEST - GET

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var quest = await _context.Quests
                .Include(q => q.Category)
                .Include(q => q.Group)
                    .ThenInclude(g => g!.Memberships)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quest == null)
                return NotFound();


            // GROUP QUEST

            if (quest.GroupId != null)
            {
                if (quest.Group == null)
                    return NotFound();

                var isMember =
                    quest.Group.Memberships
                        .Any(m => m.UserId == userId);

                if (!isMember)
                    return Forbid();
            }


            // PUBLIC QUEST

            if (quest.GroupId == null &&
                !quest.IsActive)
            {
                TempData["Error"] =
                    "This quest is no longer available.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            // APPROVED

            var hasApproved =
                await _context.QuestCompletions
                    .AnyAsync(c =>
                        c.QuestId == id &&
                        c.UserId == userId &&
                        c.Status == "Approved");

            if (hasApproved)
            {
                if (quest.GroupId != null)
                {
                    return RedirectToAction(
                        nameof(GroupQuestDetails),
                        new { id });
                }

                TempData["Error"] =
                    "You have already completed this quest.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            // PENDING

            var hasPending =
                await _context.QuestCompletions
                    .AnyAsync(c =>
                        c.QuestId == id &&
                        c.UserId == userId &&
                        c.Status == "Pending");

            if (hasPending)
            {
                if (quest.GroupId != null)
                {
                    return RedirectToAction(
                        nameof(GroupQuestDetails),
                        new { id });
                }

                TempData["Error"] =
                    "Your submission is already being reviewed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            return View(quest);
        }


        // COMPLETE QUEST - POST

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(
            int id,
            IFormFile photo,
            string? caption)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var quest = await _context.Quests
                .Include(q => q.Category)
                .Include(q => q.Group)
                    .ThenInclude(g => g!.Memberships)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quest == null)
                return NotFound();


            // GROUP ACCESS

            if (quest.GroupId != null)
            {
                if (quest.Group == null)
                    return NotFound();

                var isMember =
                    quest.Group.Memberships
                        .Any(m => m.UserId == userId);

                if (!isMember)
                    return Forbid();
            }


            // PUBLIC QUEST AVAILABILITY

            if (quest.GroupId == null &&
                !quest.IsActive)
            {
                TempData["Error"] =
                    "This quest is no longer available.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }


            // DUPLICATE APPROVED

            var hasApproved =
                await _context.QuestCompletions
                    .AnyAsync(c =>
                        c.QuestId == id &&
                        c.UserId == userId &&
                        c.Status == "Approved");

            if (hasApproved)
            {
                return RedirectToAction(
                    quest.GroupId != null
                        ? nameof(GroupQuestDetails)
                        : nameof(Details),
                    new { id });
            }


            // DUPLICATE PENDING

            var hasPending =
                await _context.QuestCompletions
                    .AnyAsync(c =>
                        c.QuestId == id &&
                        c.UserId == userId &&
                        c.Status == "Pending");

            if (hasPending)
            {
                return RedirectToAction(
                    quest.GroupId != null
                        ? nameof(GroupQuestDetails)
                        : nameof(Details),
                    new { id });
            }


            // CAPTION

            caption = caption?.Trim();

            if (!string.IsNullOrWhiteSpace(caption) &&
                caption.Length > 500)
            {
                ModelState.AddModelError(
                    "caption",
                    "Caption cannot exceed 500 characters.");

                return View(quest);
            }


            // PHOTO

            if (photo == null || photo.Length == 0)
            {
                ModelState.AddModelError(
                    "photo",
                    "Please upload a photo.");

                return View(quest);
            }

            if (photo.Length > 10 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    "photo",
                    "The photo must be smaller than 10 MB.");

                return View(quest);
            }

            var allowedExtensions =
                new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

            var extension =
                Path.GetExtension(photo.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "photo",
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");

                return View(quest);
            }


            // SAVE PHOTO

            var uploadFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "quest-proofs");

            Directory.CreateDirectory(uploadFolder);

            var fileName =
                $"{Guid.NewGuid()}{extension}";

            var filePath =
                Path.Combine(
                    uploadFolder,
                    fileName);

            await using (var stream =
                new FileStream(
                    filePath,
                    FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            var photoUrl =
                $"/uploads/quest-proofs/{fileName}";


            // CREATE COMPLETION

            var completion =
                new QuestCompletion
                {
                    UserId = userId,
                    QuestId = quest.Id,
                    PhotoUrl = photoUrl,
                    Caption = caption,
                    Status = "Pending",
                    SubmittedAt = DateTime.UtcNow
                };

            _context.QuestCompletions.Add(completion);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Your proof has been submitted and is waiting for review.";

            return RedirectToAction(
                quest.GroupId != null
                    ? nameof(GroupQuestDetails)
                    : nameof(Details),
                new { id });
        }


        // ADMIN - PENDING SUBMISSIONS

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PendingSubmissions()
        {
            var submissions =
                await _context.QuestCompletions
                    .Include(c => c.User)
                    .Include(c => c.Quest)
                        .ThenInclude(q => q.Category)
                    .Include(c => c.Quest)
                        .ThenInclude(q => q.Group)
                    .Where(c => c.Status == "Pending")
                    .OrderBy(c => c.SubmittedAt)
                    .ToListAsync();

            return View(submissions);
        }


        // ADMIN - REVIEW

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Review(int id)
        {
            var completion =
                await _context.QuestCompletions
                    .Include(c => c.User)
                    .Include(c => c.Quest)
                        .ThenInclude(q => q.Category)
                    .Include(c => c.Quest)
                        .ThenInclude(q => q.Group)
                    .FirstOrDefaultAsync(c => c.Id == id);

            if (completion == null)
                return NotFound();

            return View(completion);
        }


        // ADMIN - APPROVE

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var completion =
                await _context.QuestCompletions
                    .Include(c => c.User)
                    .Include(c => c.Quest)
                    .FirstOrDefaultAsync(c => c.Id == id);

            if (completion == null)
                return NotFound();

            if (completion.Status != "Pending")
            {
                TempData["Error"] =
                    "This submission has already been reviewed.";

                return RedirectToAction(
                    nameof(PendingSubmissions));
            }

            if (completion.User == null ||
                completion.Quest == null)
            {
                return NotFound();
            }

            completion.Status = "Approved";
            completion.ReviewedAt = DateTime.UtcNow;

            completion.User.Points +=
                completion.Quest.PointsReward;

            completion.User.Level =
                1 + completion.User.Points / 100;

            await _context.SaveChangesAsync();


            // CHECK AND AWARD BADGES

            var newBadges =
                await _badgeService.CheckAndAwardBadgesAsync(
                    completion.User.Id);


            // BADGE NOTIFICATIONS

            foreach (var badge in newBadges)
            {
                await _notificationService.CreateAsync(
                    completion.User.Id,
                    "BadgeEarned",
                    "New badge earned",
                    $"You earned the \"{badge.Name}\" badge.",
                    "/Badge"
                );
            }


            // QUEST APPROVED NOTIFICATION

            var questLink =
                completion.Quest.GroupId.HasValue
                    ? Url.Action(
                        nameof(GroupQuestDetails),
                        "Quest",
                        new { id = completion.QuestId })
                    : Url.Action(
                        nameof(Details),
                        "Quest",
                        new { id = completion.QuestId });

            await _notificationService.CreateAsync(
                completion.User.Id,
                "QuestApproved",
                "Quest approved",
                $"Your submission for \"{completion.Quest.Title}\" was approved. " +
                $"You earned {completion.Quest.PointsReward} points.",
                questLink
            );

            TempData["Success"] =
                "Submission approved successfully.";

            return RedirectToAction(
                nameof(PendingSubmissions));
        }


        // ADMIN - REJECT

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var completion =
                await _context.QuestCompletions
                    .Include(c => c.User)
                    .Include(c => c.Quest)
                    .FirstOrDefaultAsync(c => c.Id == id);

            if (completion == null)
                return NotFound();

            if (completion.Status != "Pending")
            {
                TempData["Error"] =
                    "This submission has already been reviewed.";

                return RedirectToAction(
                    nameof(PendingSubmissions));
            }

            if (completion.User == null ||
                completion.Quest == null)
            {
                return NotFound();
            }

            completion.Status = "Rejected";
            completion.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();


            // QUEST REJECTED NOTIFICATION

            var questLink =
                completion.Quest.GroupId.HasValue
                    ? Url.Action(
                        nameof(GroupQuestDetails),
                        "Quest",
                        new { id = completion.QuestId })
                    : Url.Action(
                        nameof(Details),
                        "Quest",
                        new { id = completion.QuestId });

            await _notificationService.CreateAsync(
                completion.User.Id,
                "QuestRejected",
                "Quest submission rejected",
                $"Your submission for \"{completion.Quest.Title}\" was not approved. " +
                $"You can review the quest and try again.",
                questLink
            );

            TempData["Success"] =
                "Submission rejected.";

            return RedirectToAction(
                nameof(PendingSubmissions));
        }


        // ADMIN - CREATE PUBLIC QUEST

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await LoadQuestFormData();

            return View(
                new Quest
                {
                    PointsReward = 10,
                    Difficulty = DifficultyLevel.Easy,
                    IsActive = true,
                    GroupId = null
                });
        }


        // ADMIN - CREATE POST

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quest quest)
        {
            quest.GroupId = null;
            quest.IsActive = true;

            if (quest.PointsReward <= 0)
            {
                ModelState.AddModelError(
                    nameof(Quest.PointsReward),
                    "Points reward must be greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                await LoadQuestFormData(quest);
                return View(quest);
            }

            _context.Quests.Add(quest);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Quest created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ADMIN - EDIT

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var quest =
                await _context.Quests
                    .FirstOrDefaultAsync(q =>
                        q.Id == id &&
                        q.GroupId == null);

            if (quest == null)
                return NotFound();

            await LoadQuestFormData(quest);

            return View(quest);
        }


        // ADMIN - EDIT POST

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Quest quest)
        {
            if (id != quest.Id)
                return NotFound();

            var existingQuest =
                await _context.Quests
                    .FirstOrDefaultAsync(q =>
                        q.Id == id &&
                        q.GroupId == null);

            if (existingQuest == null)
                return NotFound();

            if (quest.PointsReward <= 0)
            {
                ModelState.AddModelError(
                    nameof(Quest.PointsReward),
                    "Points reward must be greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                await LoadQuestFormData(quest);
                return View(quest);
            }

            existingQuest.Title =
                quest.Title;

            existingQuest.Description =
                quest.Description;

            existingQuest.PointsReward =
                quest.PointsReward;

            existingQuest.Difficulty =
                quest.Difficulty;

            existingQuest.CategoryId =
                quest.CategoryId;

            existingQuest.IsActive =
                quest.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Quest updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = existingQuest.Id });
        }


        // ADMIN - DELETE

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var quest =
                await _context.Quests
                    .Include(q => q.Category)
                    .Include(q => q.Completions)
                    .FirstOrDefaultAsync(q =>
                        q.Id == id &&
                        q.GroupId == null);

            if (quest == null)
                return NotFound();

            return View(quest);
        }


        // ADMIN - DELETE POST

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quest =
                await _context.Quests
                    .Include(q => q.Completions)
                    .FirstOrDefaultAsync(q =>
                        q.Id == id &&
                        q.GroupId == null);

            if (quest == null)
                return NotFound();

            if (quest.Completions.Any())
            {
                TempData["Error"] =
                    "This quest cannot be deleted because it already has submissions. " +
                    "You can edit it or leave it inactive instead.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            _context.Quests.Remove(quest);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Quest deleted successfully.";

            return RedirectToAction(
                nameof(Index));
        }


        // FORM DATA

        private async Task LoadQuestFormData(
            Quest? quest = null)
        {
            ViewData["CategoryId"] =
                new SelectList(
                    await _context.Categories
                        .OrderBy(c => c.Name)
                        .ToListAsync(),
                    "Id",
                    "Name",
                    quest?.CategoryId);
        }


        // QUEST EXISTS

        private async Task<bool> QuestExists(int id)
        {
            return await _context.Quests
                .AnyAsync(q =>
                    q.Id == id &&
                    q.GroupId == null);
        }
    }
}

