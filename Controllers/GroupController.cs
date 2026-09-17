using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuestApp.Data;
using SideQuestApp.Models;

namespace SideQuestApp.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        
        // MY GROUPS
        

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var groups = await _context.Groups
                .Include(g => g.Memberships)
                .Include(g => g.Quests)
                .Where(g =>
                    g.Memberships.Any(m => m.UserId == userId))
                .OrderByDescending(g => g.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(groups);
        }


        
        // CREATE GROUP
        

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string name,
            string? description)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            name = name?.Trim() ?? string.Empty;
            description = description?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(
                    "name",
                    "Group name is required.");
            }

            if (name.Length > 100)
            {
                ModelState.AddModelError(
                    "name",
                    "Group name cannot exceed 100 characters.");
            }

            if (!string.IsNullOrWhiteSpace(description) &&
                description.Length > 500)
            {
                ModelState.AddModelError(
                    "description",
                    "Description cannot exceed 500 characters.");
            }

            if (!ModelState.IsValid)
                return View();

            string inviteCode;

            do
            {
                inviteCode = GenerateInviteCode();
            }
            while (await _context.Groups
                .AnyAsync(g => g.InviteCode == inviteCode));

            var group = new Group
            {
                Name = name,
                Description = description,
                CreatedByUserId = userId,
                InviteCode = inviteCode,
                CreatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);

            await _context.SaveChangesAsync();

            _context.GroupMemberships.Add(
                new GroupMembership
                {
                    UserId = userId,
                    GroupId = group.Id,
                    JoinedAt = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = group.Id });
        }


        
        // GROUP DETAILS
        

        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var group = await _context.Groups
                .Include(g => g.CreatedByUser)
                .Include(g => g.Memberships)
                    .ThenInclude(m => m.User)
                .Include(g => g.Quests)
                    .ThenInclude(q => q.Category)
                .Where(g => g.Id == id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (group == null)
                return NotFound();

            var isMember = group.Memberships
                .Any(m => m.UserId == userId);

            if (!isMember)
            {
                return RedirectToAction("Index", "Group");
            }

            ViewBag.CurrentUserId = userId;

            ViewBag.IsCreator =
                group.CreatedByUserId == userId;

            return View(group);
        }


        
        // JOIN BY CODE
        

        [HttpGet]
        public IActionResult JoinByCode()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinByCode(
            string inviteCode)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            inviteCode =
                inviteCode?.Trim().ToUpperInvariant()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                ModelState.AddModelError(
                    "inviteCode",
                    "Please enter a group code.");

                return View();
            }

            var group = await _context.Groups
                .FirstOrDefaultAsync(g =>
                    g.InviteCode == inviteCode);

            if (group == null)
            {
                ModelState.AddModelError(
                    "inviteCode",
                    "No group found with that code.");

                return View();
            }

            var alreadyMember =
                await _context.GroupMemberships
                    .AnyAsync(m =>
                        m.GroupId == group.Id &&
                        m.UserId == userId);

            if (!alreadyMember)
            {
                _context.GroupMemberships.Add(
                    new GroupMembership
                    {
                        UserId = userId,
                        GroupId = group.Id,
                        JoinedAt = DateTime.UtcNow
                    });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Details),
                new { id = group.Id });
        }


        
        // CREATE GROUP QUEST
        

        [HttpGet]
        public async Task<IActionResult> CreateQuest(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var group = await _context.Groups
                .Include(g => g.Memberships)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return NotFound();

            var isMember = group.Memberships
                .Any(m => m.UserId == userId);

            if (!isMember)
            {
                return RedirectToAction("Index", "Group");
            }

            ViewBag.GroupId = group.Id;
            ViewBag.GroupName = group.Name;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuest(
            int id,
            string title,
            string? description,
            int pointsReward)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var group = await _context.Groups
                .Include(g => g.Memberships)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return NotFound();

            var isMember = group.Memberships
                .Any(m => m.UserId == userId);

            if (!isMember)
            {
                return RedirectToAction("Index", "Group");
            }

            title = title?.Trim() ?? string.Empty;
            description = description?.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(
                    "title",
                    "Quest title is required.");
            }

            if (title.Length > 150)
            {
                ModelState.AddModelError(
                    "title",
                    "Quest title cannot exceed 150 characters.");
            }

            if (!string.IsNullOrWhiteSpace(description) &&
                description.Length > 1000)
            {
                ModelState.AddModelError(
                    "description",
                    "Description cannot exceed 1000 characters.");
            }

            if (pointsReward <= 0)
            {
                ModelState.AddModelError(
                    "pointsReward",
                    "Points reward must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.GroupId = group.Id;
                ViewBag.GroupName = group.Name;

                return View();
            }

            var quest = new Quest
            {
                Title = title,
                Description = description,
                PointsReward = pointsReward,

                CategoryId = null,

                Difficulty = DifficultyLevel.Easy,

                CreatedByUserId = userId,
                IsActive = true,
                GroupId = group.Id
            };

            _context.Quests.Add(quest);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = group.Id });
        }


        
        // DELETE GROUP
        

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var group = await _context.Groups
                .Include(g => g.CreatedByUser)
                .Include(g => g.Memberships)
                .Include(g => g.Quests)
                    .ThenInclude(q => q.Completions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return NotFound();

            if (group.CreatedByUserId != userId)
            {
                return RedirectToAction("Index", "Group");
            }

            return View(group);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var group = await _context.Groups
                .Include(g => g.Memberships)
                .Include(g => g.Quests)
                    .ThenInclude(q => q.Completions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return NotFound();

            if (group.CreatedByUserId != userId)
            {
                return RedirectToAction("Index", "Group");
            }

            var hasCompletions =
                group.Quests.Any(q =>
                    q.Completions.Any());

            if (hasCompletions)
            {
                TempData["Error"] =
                    "This group cannot be deleted because one or more group quests already have submissions.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            _context.GroupMemberships.RemoveRange(
                group.Memberships);

            _context.Quests.RemoveRange(
                group.Quests);

            _context.Groups.Remove(group);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        
        // EDIT GROUP QUEST
        

        [HttpGet]
        public async Task<IActionResult> EditQuest(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var quest = await _context.Quests
                .Include(q => q.Group)
                    .ThenInclude(g => g!.Memberships)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.GroupId != null);

            if (quest == null)
                return NotFound();

            if (quest.Group == null)
                return NotFound();

            var isMember = quest.Group.Memberships
                .Any(m => m.UserId == userId);

            if (!isMember)
            {
                return RedirectToAction("Index", "Group");
            }

            if (quest.CreatedByUserId != userId)
            {
                return RedirectToAction("Index", "Group");
            }

            ViewBag.GroupId = quest.GroupId;
            ViewBag.GroupName = quest.Group.Name;

            return View(quest);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuest(
            int id,
            string title,
            string? description,
            int pointsReward)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var quest = await _context.Quests
                .Include(q => q.Group)
                    .ThenInclude(g => g!.Memberships)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.GroupId != null);

            if (quest == null)
                return NotFound();

            if (quest.Group == null)
                return NotFound();

            var isMember = quest.Group.Memberships
                .Any(m => m.UserId == userId);

            if (!isMember)
            {
                return RedirectToAction("Index", "Group");
            }

            if (quest.CreatedByUserId != userId)
            {
                return RedirectToAction("Index", "Group");
            }

            title = title?.Trim() ?? string.Empty;
            description = description?.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(
                    "title",
                    "Quest title is required.");
            }

            if (title.Length > 150)
            {
                ModelState.AddModelError(
                    "title",
                    "Quest title cannot exceed 150 characters.");
            }

            if (!string.IsNullOrWhiteSpace(description) &&
                description.Length > 1000)
            {
                ModelState.AddModelError(
                    "description",
                    "Description cannot exceed 1000 characters.");
            }

            if (pointsReward <= 0)
            {
                ModelState.AddModelError(
                    "pointsReward",
                    "Points reward must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.GroupId = quest.GroupId;
                ViewBag.GroupName = quest.Group.Name;

                return View(quest);
            }

            quest.Title = title;
            quest.Description = description;
            quest.PointsReward = pointsReward;

            

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(QuestController.GroupQuestDetails),
                "Quest",
                new { id = quest.Id });
        }


        
        // DELETE GROUP QUEST
        

        [HttpGet]
        public async Task<IActionResult> DeleteQuest(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var quest = await _context.Quests
                .Include(q => q.Group)
                    .ThenInclude(g => g!.Memberships)
                .Include(q => q.Completions)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.GroupId != null);

            if (quest == null)
                return NotFound();

            if (quest.Group == null)
                return NotFound();

            var isMember = quest.Group.Memberships
                .Any(m => m.UserId == userId);

            if (!isMember)
            {
                return RedirectToAction("Index", "Group");
            }

            if (quest.CreatedByUserId != userId)
            {
                return RedirectToAction("Index", "Group");
            }

            ViewBag.GroupId = quest.GroupId;
            ViewBag.GroupName = quest.Group.Name;
            ViewBag.HasCompletions =
                quest.Completions.Any();

            return View(quest);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Redirect("/Identity/Account/Login");
            }


            var quest = await _context.Quests
                .Include(q => q.Group)
                    .ThenInclude(g => g!.Memberships)
                .Include(q => q.Completions)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.GroupId != null);

            if (quest == null)
                return NotFound();

            if (quest.Group == null)
                return NotFound();

            var isMember = quest.Group.Memberships
                .Any(m => m.UserId == userId);

            if (!isMember)
            {
                return RedirectToAction("Index", "Group");
            }

            if (quest.CreatedByUserId != userId)
            {
                return RedirectToAction("Index", "Group");
            }

            if (quest.Completions.Any())
            {
                TempData["Error"] =
                    "This quest cannot be deleted because it already has completion history.";

                return RedirectToAction(
                    nameof(QuestController.GroupQuestDetails),
                    "Quest",
                    new { id = quest.Id });
            }

            var groupId = quest.GroupId.Value;

            _context.Quests.Remove(quest);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = groupId });
        }


        
        // INVITE CODE
        

        private static string GenerateInviteCode()
        {
            const string chars =
                "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            return new string(
                Enumerable.Range(0, 6)
                    .Select(_ =>
                        chars[Random.Shared.Next(chars.Length)])
                    .ToArray());
        }
    }
}
