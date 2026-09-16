
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuestApp.Data;
using SideQuestApp.Models;
using Microsoft.AspNetCore.Hosting;

namespace SideQuestApp.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }


    // HOME / DASHBOARD

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var isLoggedIn =
            User.Identity?.IsAuthenticated == true;

        ViewBag.IsLoggedIn = isLoggedIn;


        // GUEST HOME

        if (!isLoggedIn)
        {
            // GENERAL STATS

            ViewBag.ActiveQuestCount =
                await _context.Quests
                    .CountAsync(q =>
                        q.IsActive &&
                        q.GroupId == null);

            ViewBag.TotalUsers =
                await _context.Users.CountAsync();

            ViewBag.TotalApproved =
                await _context.QuestCompletions
                    .CountAsync(qc =>
                        qc.Status == "Approved" &&
                        qc.Quest != null &&
                        qc.Quest.GroupId == null);


            // LATEST PUBLIC QUESTS

            var latestQuests =
                await _context.Quests
                    .Include(q => q.Category)
                    .Where(q =>
                        q.IsActive &&
                        q.GroupId == null)
                    .OrderByDescending(q => q.Id)
                    .Take(3)
                    .ToListAsync();

            ViewBag.LatestQuests = latestQuests;


            // BADGES

            var badges =
                await _context.Badges
                    .Include(b => b.UserBadges)
                    .OrderByDescending(b => b.UserBadges.Count)
                    .ThenBy(b => b.Name)
                    .Take(4)
                    .ToListAsync();

            ViewBag.FeaturedBadges = badges;


            // GROUP / SOCIAL STATS

            ViewBag.TotalGroups =
                await _context.Groups.CountAsync();

            ViewBag.TotalGroupQuests =
                await _context.Quests
                    .CountAsync(q =>
                        q.GroupId != null);


            // PUBLIC COMMUNITY ACTIVITY

            var publicActivity =
                await _context.QuestCompletions
                    .Include(c => c.User)
                    .Include(c => c.Quest)
                    .Where(c =>
                        c.Status == "Approved" &&
                        c.Quest != null &&
                        c.Quest.GroupId == null)
                    .OrderByDescending(c => c.SubmittedAt)
                    .Take(4)
                    .ToListAsync();

            ViewBag.PublicActivity =
                publicActivity;


            return View();
        }


        // LOGGED-IN USER

        var userId =
            _userManager.GetUserId(User);

        if (userId == null)
        {
            return RedirectToPage(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }


        // CURRENT USER

        var user =
            await _context.Users
                .Include(u => u.UserBadges)
                    .ThenInclude(ub => ub.Badge)
                .FirstOrDefaultAsync(u =>
                    u.Id == userId);

        if (user == null)
        {
            return RedirectToPage(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }

        ViewBag.CurrentUser = user;


        // FRIEND REQUEST NOTIFICATIONS

        var pendingFriendRequests =
            await _context.Friendships
                .Include(f => f.Requester)
                .Where(f =>
                    f.AddresseeId == userId &&
                    f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.Id)
                .ToListAsync();

        ViewBag.PendingFriendRequests =
            pendingFriendRequests;

        ViewBag.PendingFriendRequestCount =
            pendingFriendRequests.Count;


        // FRIENDS

        var friendships =
            await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f =>
                    f.Status == FriendshipStatus.Accepted &&
                    (
                        f.RequesterId == userId ||
                        f.AddresseeId == userId
                    ))
                .ToListAsync();

        var friends =
            friendships
                .Select(f =>
                    f.RequesterId == userId
                        ? f.Addressee
                        : f.Requester)
                .Where(u => u != null)
                .Cast<ApplicationUser>()
                .OrderByDescending(u => u.Points)
                .ToList();

        ViewBag.Friends = friends;
        ViewBag.FriendCount = friends.Count;


        // AVAILABLE PUBLIC QUESTS

        var availableQuests =
            await _context.Quests
                .Include(q => q.Category)
                .Where(q =>
                    q.IsActive &&
                    q.GroupId == null &&
                    !q.Completions.Any(c =>
                        c.UserId == userId &&
                        c.Status == "Approved"))
                .OrderByDescending(q => q.Id)
                .Take(4)
                .ToListAsync();

        ViewBag.AvailableQuests =
            availableQuests;

        ViewBag.NewQuests =
            availableQuests;


        // USER'S RECENT COMPLETIONS

        var recentCompletions =
            await _context.QuestCompletions
                .Include(qc => qc.Quest)
                .Where(qc =>
                    qc.UserId == userId)
                .OrderByDescending(qc => qc.SubmittedAt)
                .Take(6)
                .ToListAsync();

        ViewBag.RecentCompletions =
            recentCompletions;


        // PENDING COMPLETIONS

        var pendingCompletions =
            await _context.QuestCompletions
                .Include(qc => qc.Quest)
                .Where(qc =>
                    qc.UserId == userId &&
                    qc.Status == "Pending")
                .OrderByDescending(qc => qc.SubmittedAt)
                .ToListAsync();

        ViewBag.PendingCompletions =
            pendingCompletions;


        // APPROVED COMPLETIONS

        var approvedCompletions =
            await _context.QuestCompletions
                .Include(qc => qc.Quest)
                .Where(qc =>
                    qc.UserId == userId &&
                    qc.Status == "Approved")
                .OrderByDescending(qc => qc.SubmittedAt)
                .Take(6)
                .ToListAsync();

        ViewBag.ApprovedCompletions =
            approvedCompletions;


        // USER BADGES

        var earnedBadges =
            user.UserBadges
                .Where(ub => ub.Badge != null)
                .OrderByDescending(ub => ub.EarnedAt)
                .Take(4)
                .ToList();

        ViewBag.EarnedBadges =
            earnedBadges;


        // GROUP MEMBERSHIPS

        var groupMemberships =
            await _context.GroupMemberships
                .Include(m => m.Group)
                .Where(m =>
                    m.UserId == userId)
                .ToListAsync();

        var myGroups =
            groupMemberships
                .Where(m => m.Group != null)
                .Select(m => m.Group!)
                .Take(4)
                .ToList();

        ViewBag.MyGroups =
            myGroups;


        // FRIEND POSTS

        var friendIds =
            friends
                .Select(f => f.Id)
                .ToList();

        var friendPosts =
            await _context.QuestCompletions
                .Include(qc => qc.Quest)
                .Include(qc => qc.User)
                .Where(qc =>
                    qc.Status == "Approved" &&
                    qc.Quest != null &&
                    qc.Quest.GroupId == null &&
                    friendIds.Contains(qc.UserId))
                .OrderByDescending(qc => qc.SubmittedAt)
                .Take(6)
                .ToListAsync();

        ViewBag.FriendPosts =
            friendPosts;


        // RECENT PUBLIC COMMUNITY POSTS

        var recentCommunityPosts =
            await _context.QuestCompletions
                .Include(qc => qc.Quest)
                .Include(qc => qc.User)
                .Where(qc =>
                    qc.Status == "Approved" &&
                    qc.Quest != null &&
                    qc.Quest.GroupId == null)
                .OrderByDescending(qc => qc.SubmittedAt)
                .Take(6)
                .ToListAsync();

        ViewBag.RecentCommunityPosts =
            recentCommunityPosts;


        // DASHBOARD COUNTS

        ViewBag.PostCount =
            await _context.QuestCompletions
                .CountAsync(qc =>
                    qc.UserId == userId &&
                    qc.Status == "Approved");

        ViewBag.TotalApproved =
            await _context.QuestCompletions
                .CountAsync(qc =>
                    qc.Status == "Approved");

        ViewBag.TotalUsers =
            await _context.Users.CountAsync();


        // ADMIN DASHBOARD

        if (User.IsInRole("Admin"))
        {
            ViewBag.PendingCount =
                await _context.QuestCompletions
                    .CountAsync(qc =>
                        qc.Status == "Pending" &&
                        qc.Quest != null);

            ViewBag.TotalUsers =
                await _context.Users.CountAsync();

            ViewBag.TotalQuests =
                await _context.Quests
                    .CountAsync(q =>
                        q.GroupId == null);

            ViewBag.TotalApproved =
                await _context.QuestCompletions
                    .CountAsync(qc =>
                        qc.Status == "Approved" &&
                        qc.Quest != null);

            ViewBag.RecentActivity =
                await _context.QuestCompletions
                    .Include(qc => qc.Quest)
                    .Include(qc => qc.User)
                    .Where(qc =>
                        qc.Quest != null &&
                        qc.Quest.GroupId == null)
                    .OrderByDescending(qc => qc.SubmittedAt)
                    .Take(6)
                    .ToListAsync();
        }


        // RETURN HOME

        return View();
    }


    // PRIVACY

    [Authorize]
    public IActionResult Privacy()
    {
        return View();
    }


    // PROFILE

    [Authorize]
    [HttpGet("/Profile/{username?}")]
    public async Task<IActionResult> Profile(string? username)
    {
        var currentUserId =
            _userManager.GetUserId(User);

        if (currentUserId == null)
        {
            return RedirectToPage(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }

        ApplicationUser? user;


        // FIND PROFILE USER

        if (string.IsNullOrWhiteSpace(username))
        {
            user =
                await _context.Users
                    .Include(u => u.UserBadges)
                        .ThenInclude(ub => ub.Badge)
                    .FirstOrDefaultAsync(u =>
                        u.Id == currentUserId);
        }
        else
        {
            user =
                await _userManager.FindByNameAsync(
                    username);

            if (user != null)
            {
                user =
                    await _context.Users
                        .Include(u => u.UserBadges)
                            .ThenInclude(ub => ub.Badge)
                        .FirstOrDefaultAsync(u =>
                            u.Id == user.Id);
            }
        }

        if (user == null)
        {
            return NotFound();
        }


        // OWN PROFILE

        var isOwnProfile =
            user.Id == currentUserId;

        ViewBag.IsOwnProfile =
            isOwnProfile;


        // FRIENDSHIP STATUS

        var friendship =
            await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (
                        f.RequesterId == currentUserId &&
                        f.AddresseeId == user.Id
                    )
                    ||
                    (
                        f.RequesterId == user.Id &&
                        f.AddresseeId == currentUserId
                    ));

        string? friendshipStatus = null;

        if (friendship != null)
        {
            ViewBag.FriendshipId =
                friendship.Id;

            if (friendship.Status ==
                FriendshipStatus.Accepted)
            {
                friendshipStatus = "Accepted";
            }
            else if (
                friendship.Status ==
                FriendshipStatus.Pending)
            {
                if (friendship.RequesterId ==
                    currentUserId)
                {
                    friendshipStatus =
                        "PendingSent";
                }
                else if (
                    friendship.AddresseeId ==
                    currentUserId)
                {
                    friendshipStatus =
                        "PendingReceived";
                }
            }
        }

        ViewBag.FriendshipStatus =
            friendshipStatus;


        // CHECK IF THEY ARE FRIENDS

        var areFriends =
            friendship?.Status ==
            FriendshipStatus.Accepted;


        // PRIVATE PROFILE

        if (user.IsPrivate &&
            !isOwnProfile &&
            !areFriends)
        {
            ViewBag.ProfileUser =
                user;

            return View(
                "PrivateProfile",
                user);
        }


        // VIEWER GROUP MEMBERSHIPS

        var viewerGroupIds =
            await _context.GroupMemberships
                .Where(m =>
                    m.UserId == currentUserId)
                .Select(m => m.GroupId)
                .ToListAsync();


        // APPROVED QUEST COMPLETIONS

        var completions =
            await _context.QuestCompletions
                .Include(qc => qc.Quest)
                .Where(qc =>
                    qc.UserId == user.Id &&
                    qc.Status == "Approved" &&
                    qc.Quest != null &&
                    (
                        qc.Quest.GroupId == null
                        ||
                        (
                            qc.Quest.GroupId != null &&
                            viewerGroupIds.Contains(
                                qc.Quest.GroupId.Value)
                        )
                    ))
                .OrderByDescending(qc =>
                    qc.SubmittedAt)
                .ToListAsync();

        ViewBag.Completions =
            completions;

        ViewBag.PostCount =
            completions.Count;


        // PROFILE FRIENDS

        var profileFriendships =
            await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f =>
                    f.Status ==
                    FriendshipStatus.Accepted &&
                    (
                        f.RequesterId == user.Id ||
                        f.AddresseeId == user.Id
                    ))
                .ToListAsync();

        var profileFriends =
            profileFriendships
                .Select(f =>
                    f.RequesterId == user.Id
                        ? f.Addressee
                        : f.Requester)
                .Where(u => u != null)
                .Cast<ApplicationUser>()
                .ToList();

        ViewBag.Friends =
            profileFriends;

        ViewBag.FriendCount =
            profileFriends.Count;


        // RETURN PROFILE

        return View(
            "Profile",
            user);
    }


    // EDIT PROFILE - GET

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userId =
            _userManager.GetUserId(User);

        if (userId == null)
        {
            return RedirectToPage(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }

        var user =
            await _userManager.FindByIdAsync(
                userId);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }


    // EDIT PROFILE - POST

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(
        string? userName,
        string? bio,
        IFormFile? profileImage)
    {
        var userId =
            _userManager.GetUserId(User);

        if (userId == null)
        {
            return RedirectToPage(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }

        var user =
            await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }


        // USERNAME VALIDATION

        if (string.IsNullOrWhiteSpace(userName))
        {
            ModelState.AddModelError(
                "userName",
                "Username is required.");
        }
        else
        {
            userName =
                userName.Trim();

            var existingUser =
                await _userManager.FindByNameAsync(userName);

            if (existingUser != null &&
                existingUser.Id != user.Id)
            {
                ModelState.AddModelError(
                    "userName",
                    "This username is already taken.");
            }
        }


        // BIO

        if (bio != null &&
            bio.Length > 250)
        {
            ModelState.AddModelError(
                "bio",
                "Bio cannot be longer than 250 characters.");
        }


        // PROFILE IMAGE

        if (profileImage != null &&
            profileImage.Length > 0)
        {
            var allowedExtensions =
                new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

            var extension =
                Path.GetExtension(
                    profileImage.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "profileImage",
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            const long maxFileSize =
                10 * 1024 * 1024;

            if (profileImage.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "profileImage",
                    "The image cannot be larger than 10 MB.");
            }
        }


        // VALIDATION FAILED

        if (!ModelState.IsValid)
        {
            return View(user);
        }


        // UPDATE USERNAME

        var usernameResult =
            await _userManager.SetUserNameAsync(
                user,
                userName);

        if (!usernameResult.Succeeded)
        {
            foreach (var error in
                     usernameResult.Errors)
            {
                ModelState.AddModelError(
                    "userName",
                    error.Description);
            }

            return View(user);
        }


        // UPDATE BIO

        user.Bio =
            string.IsNullOrWhiteSpace(bio)
                ? null
                : bio.Trim();


        // SAVE PROFILE IMAGE

        if (profileImage != null &&
            profileImage.Length > 0)
        {
            var uploadsFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "profiles");

            Directory.CreateDirectory(
                uploadsFolder);


            if (!string.IsNullOrWhiteSpace(
                    user.ProfileImageUrl) &&
                user.ProfileImageUrl
                    .StartsWith(
                        "/uploads/profiles/",
                        StringComparison.OrdinalIgnoreCase))
            {
                var oldFileName =
                    Path.GetFileName(
                        user.ProfileImageUrl);

                var oldFilePath =
                    Path.Combine(
                        uploadsFolder,
                        oldFileName);

                if (System.IO.File.Exists(
                        oldFilePath))
                {
                    System.IO.File.Delete(
                        oldFilePath);
                }
            }


            var fileName =
                $"{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName).ToLowerInvariant()}";

            var filePath =
                Path.Combine(
                    uploadsFolder,
                    fileName);


            using (var stream =
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                await profileImage.CopyToAsync(
                    stream);
            }

            user.ProfileImageUrl =
                $"/uploads/profiles/{fileName}";
        }


        // UPDATE USER

        var updateResult =
            await _userManager.UpdateAsync(
                user);

        if (!updateResult.Succeeded)
        {
            foreach (var error in
                     updateResult.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            return View(user);
        }


        // BACK TO PROFILE

        return Redirect("/Profile");
    }


    // TOGGLE PROFILE PRIVACY

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePrivacy()
    {
        var userId =
            _userManager.GetUserId(User);

        if (userId == null)
        {
            return RedirectToPage(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }

        var user =
            await _userManager.FindByIdAsync(
                userId);

        if (user == null)
        {
            return NotFound();
        }

        user.IsPrivate =
            !user.IsPrivate;

        var result =
            await _userManager.UpdateAsync(
                user);

        if (!result.Succeeded)
        {
            foreach (var error in
                     result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            user =
                await _context.Users
                    .Include(u => u.UserBadges)
                        .ThenInclude(ub => ub.Badge)
                    .FirstOrDefaultAsync(u =>
                        u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.IsOwnProfile = true;

            ViewBag.Completions =
                new List<QuestCompletion>();

            ViewBag.Friends =
                new List<ApplicationUser>();

            return View(
                "Profile",
                user);
        }

        return Redirect("/Profile");
    }


    // ERROR

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
    }


    // 404

    [Route("/Home/Error404")]
    public IActionResult Error404()
    {
        return View("Error404");
    }
}
