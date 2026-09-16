using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using SideQuestApp.Data;
using SideQuestApp.Models;
using SideQuestApp.Services;

namespace SideQuestApp.Controllers
{
    public class QuestCompletionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly BadgeService _badgeService;

        public QuestCompletionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            BadgeService badgeService)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _badgeService = badgeService;
        }

        
        // CREATE
        

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create(int? questId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });

            if (questId.HasValue)
            {
                var selectedQuest = await _context.Quests
                    .FirstOrDefaultAsync(q =>
                        q.Id == questId.Value &&
                        q.IsActive);

                if (selectedQuest == null)
                    return NotFound();

                var existingCompletion =
                    await _context.QuestCompletions
                        .FirstOrDefaultAsync(qc =>
                            qc.UserId == userId &&
                            qc.QuestId == questId.Value &&
                            (qc.Status == "Pending" ||
                             qc.Status == "Approved"));

                if (existingCompletion != null)
                {
                    return RedirectToAction(
                        "Details",
                        "Quest",
                        new { id = questId.Value });
                }
            }

            var activeSubmissionQuestIds =
                await _context.QuestCompletions
                    .Where(qc =>
                        qc.UserId == userId &&
                        (qc.Status == "Pending" ||
                         qc.Status == "Approved"))
                    .Select(qc => qc.QuestId)
                    .ToListAsync();

            var availableQuests =
                await _context.Quests
                    .Where(q =>
                        q.IsActive &&
                        (q.GroupId == null ||
                         q.Id == questId) &&
                        !activeSubmissionQuestIds.Contains(q.Id))
                    .ToListAsync();

            ViewData["QuestId"] = new SelectList(
                availableQuests,
                "Id",
                "Title",
                questId);

            return View();
        }

        
        // POST: QUEST COMPLETION
        

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int questId,
            IFormFile photo)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });

            var quest = await _context.Quests
                .FirstOrDefaultAsync(q => q.Id == questId);

            if (quest == null)
                return NotFound();

            if (!quest.IsActive)
            {
                return RedirectToAction(
                    "Details",
                    "Quest",
                    new { id = questId });
            }

            var existingCompletion =
                await _context.QuestCompletions
                    .FirstOrDefaultAsync(qc =>
                        qc.UserId == userId &&
                        qc.QuestId == questId &&
                        (qc.Status == "Pending" ||
                         qc.Status == "Approved"));

            if (existingCompletion != null)
            {
                return RedirectToAction(
                    "Details",
                    "Quest",
                    new { id = questId });
            }

            if (photo == null || photo.Length == 0)
            {
                ModelState.AddModelError(
                    "",
                    "A photo is required to submit a completion.");

                await PrepareCreateView(userId, questId);

                return View();
            }

            var uploadsFolder = Path.Combine(
                _env.WebRootPath,
                "uploads",
                "quests");

            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(photo.FileName);
            var uniqueFileName = Guid.NewGuid() + extension;
            var filePath = Path.Combine(
                uploadsFolder,
                uniqueFileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            var completion = new QuestCompletion
            {
                UserId = userId,
                QuestId = questId,
                PhotoUrl = "/uploads/quests/" + uniqueFileName,
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };

            _context.QuestCompletions.Add(completion);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyCompletions));
        }

        
        // MY COMPLETIONS
        

        [Authorize]
        public async Task<IActionResult> MyCompletions()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var completions =
                await _context.QuestCompletions
                    .Include(qc => qc.Quest)
                    .Where(qc => qc.UserId == userId)
                    .OrderByDescending(qc => qc.SubmittedAt)
                    .ToListAsync();

            return View(completions);
        }

        
        // ADMIN — PENDING
        

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            var pending =
                await _context.QuestCompletions
                    .Include(qc => qc.Quest)
                    .Include(qc => qc.User)
                    .Where(qc => qc.Status == "Pending")
                    .OrderBy(qc => qc.SubmittedAt)
                    .ToListAsync();

            return View(pending);
        }

        
        // ADMIN — APPROVE
        

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var completion =
                await _context.QuestCompletions
                    .Include(qc => qc.Quest)
                    .Include(qc => qc.User)
                    .FirstOrDefaultAsync(qc => qc.Id == id);

            if (completion == null)
            {
                if (Request.Headers["X-Requested-With"] ==
                    "XMLHttpRequest")
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Review not found."
                    });
                }

                return NotFound();
            }

            if (completion.Status != "Pending")
            {
                if (Request.Headers["X-Requested-With"] ==
                    "XMLHttpRequest")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "This review has already been processed."
                    });
                }

                return RedirectToAction(nameof(Pending));
            }

            if (completion.Quest == null)
            {
                if (Request.Headers["X-Requested-With"] ==
                    "XMLHttpRequest")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "The quest no longer exists."
                    });
                }

                return RedirectToAction(nameof(Pending));
            }

            completion.Status = "Approved";
            completion.ReviewedAt = DateTime.UtcNow;

            _context.Notifications.Add(
                new Notification
                {
                    UserId = completion.UserId,
                    Type = "QuestApproved",
                    Title = "Quest approved",
                    Message =
                        $"Your quest \"{completion.Quest.Title}\" was approved. " +
                        $"You earned {completion.Quest.PointsReward} points.",
                    Link =
                        $"/Quest/Details/{completion.Quest.Id}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

            if (completion.User != null)
            {
                completion.User.Points +=
                    completion.Quest.PointsReward;

                completion.User.Level =
                    1 + completion.User.Points / 100;
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(completion.UserId))
            {
                await _badgeService.CheckAndAwardBadgesAsync(
                    completion.UserId);
            }

            if (Request.Headers["X-Requested-With"] ==
                "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    action = "approved",
                    id
                });
            }

            return RedirectToAction(nameof(Pending));
        }

        
        // ADMIN — REJECT
        

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var completion =
                await _context.QuestCompletions
                    .FirstOrDefaultAsync(qc => qc.Id == id);

            if (completion == null)
            {
                if (Request.Headers["X-Requested-With"] ==
                    "XMLHttpRequest")
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Review not found."
                    });
                }

                return NotFound();
            }

            if (completion.Status != "Pending")
            {
                if (Request.Headers["X-Requested-With"] ==
                    "XMLHttpRequest")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "This review has already been processed."
                    });
                }

                return RedirectToAction(nameof(Pending));
            }

            completion.Status = "Rejected";
            completion.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] ==
                "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    action = "rejected",
                    id
                });
            }

            return RedirectToAction(nameof(Pending));
        }

        
        // PRIVATE HELPER
        

        private async Task PrepareCreateView(
            string userId,
            int? questId)
        {
            var activeSubmissionQuestIds =
                await _context.QuestCompletions
                    .Where(qc =>
                        qc.UserId == userId &&
                        (qc.Status == "Pending" ||
                         qc.Status == "Approved"))
                    .Select(qc => qc.QuestId)
                    .ToListAsync();

            var availableQuests =
                await _context.Quests
                    .Where(q =>
                        q.IsActive &&
                        (q.GroupId == null ||
                         q.Id == questId) &&
                        !activeSubmissionQuestIds.Contains(q.Id))
                    .ToListAsync();

            ViewData["QuestId"] = new SelectList(
                availableQuests,
                "Id",
                "Title",
                questId);
        }
    }
}

