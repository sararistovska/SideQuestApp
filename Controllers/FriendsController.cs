using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuestApp.Data;
using SideQuestApp.Models;
using SideQuestApp.Services;
using SideQuestApp.ViewModels;

namespace SideQuestApp.Controllers
{
    [Authorize]
    public class FriendsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public FriendsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }


        // INDEX

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();


            // FRIENDSHIPS

            var friendships = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f =>
                    f.Status == FriendshipStatus.Accepted &&
                    (
                        f.RequesterId == userId ||
                        f.AddresseeId == userId
                    ))
                .ToListAsync();


            var friends = friendships
                .Select(f =>
                    f.RequesterId == userId
                        ? f.Addressee
                        : f.Requester)
                .Where(u => u != null)
                .ToList();


            var friendIds = friends
                .Select(u => u!.Id)
                .ToHashSet();


            // INCOMING REQUESTS

            var incomingRequests = await _context.Friendships
                .Include(f => f.Requester)
                .Where(f =>
                    f.AddresseeId == userId &&
                    f.Status == FriendshipStatus.Pending)
                .ToListAsync();


            var incomingUserIds = incomingRequests
                .Select(f => f.RequesterId)
                .ToHashSet();


            // OUTGOING REQUESTS

            var outgoingRequests = await _context.Friendships
                .Where(f =>
                    f.RequesterId == userId &&
                    f.Status == FriendshipStatus.Pending)
                .ToListAsync();


            var outgoingUserIds = outgoingRequests
                .Select(f => f.AddresseeId)
                .ToHashSet();


            // RELATED USERS

            var relatedUserIds = await _context.Friendships
                .Where(f =>
                    f.RequesterId == userId ||
                    f.AddresseeId == userId)
                .Select(f =>
                    f.RequesterId == userId
                        ? f.AddresseeId
                        : f.RequesterId)
                .ToListAsync();


            var excludedIds = relatedUserIds.ToHashSet();

            excludedIds.Add(userId);


            // SEARCH

            var searchResults = new List<ApplicationUser>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                searchResults = await _userManager.Users
                    .Where(u =>
                        u.Id != userId &&
                        u.UserName != null &&
                        u.UserName.Contains(search))
                    .OrderBy(u => u.UserName)
                    .Take(20)
                    .ToListAsync();
            }


            // CURRENT USER GROUPS

            var currentGroupIds = await _context.GroupMemberships
                .Where(m => m.UserId == userId)
                .Select(m => m.GroupId)
                .ToListAsync();

            var currentGroupIdSet = currentGroupIds.ToHashSet();


            // CANDIDATES FOR SUGGESTIONS

            var candidates = await _userManager.Users
                .Where(u => !excludedIds.Contains(u.Id))
                .ToListAsync();


            var candidateIds = candidates
                .Select(u => u.Id)
                .ToHashSet();


            // MUTUAL FRIENDS

            var mutualFriendships = await _context.Friendships
                .Where(f =>
                    f.Status == FriendshipStatus.Accepted &&
                    (
                        (
                            friendIds.Contains(f.RequesterId) &&
                            candidateIds.Contains(f.AddresseeId)
                        )
                        ||
                        (
                            friendIds.Contains(f.AddresseeId) &&
                            candidateIds.Contains(f.RequesterId)
                        )
                    ))
                .Select(f => new
                {
                    UserA = f.RequesterId,
                    UserB = f.AddresseeId
                })
                .ToListAsync();


            var mutualFriendCounts = new Dictionary<string, int>();

            foreach (var friendship in mutualFriendships)
            {
                string? candidateId = null;

                if (candidateIds.Contains(friendship.UserA))
                {
                    candidateId = friendship.UserA;
                }
                else if (candidateIds.Contains(friendship.UserB))
                {
                    candidateId = friendship.UserB;
                }

                if (candidateId == null)
                    continue;

                if (!mutualFriendCounts.ContainsKey(candidateId))
                    mutualFriendCounts[candidateId] = 0;

                mutualFriendCounts[candidateId]++;
            }


            // CANDIDATE GROUP MEMBERSHIPS

            var candidateGroupMemberships = await _context.GroupMemberships
                .Where(m => candidateIds.Contains(m.UserId))
                .Select(m => new
                {
                    m.UserId,
                    m.GroupId
                })
                .ToListAsync();


            var candidateGroups = candidateGroupMemberships
                .GroupBy(m => m.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.GroupId).ToHashSet()
                );


            // SUGGESTIONS

            var suggestions = candidates
                .Select(user =>
                {
                    var mutualFriends =
                        mutualFriendCounts.TryGetValue(
                            user.Id,
                            out var mutualCount)
                            ? mutualCount
                            : 0;

                    var score = mutualFriends * 5;

                    var sameGroup =
                        candidateGroups.TryGetValue(
                            user.Id,
                            out var groups) &&
                        groups.Any(groupId =>
                            currentGroupIdSet.Contains(groupId));

                    if (sameGroup)
                        score += 3;

                    return new
                    {
                        User = user,
                        Score = score,
                        MutualFriends = mutualFriends,
                        SameGroup = sameGroup
                    };
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.MutualFriends)
                .ThenByDescending(x => x.SameGroup)
                .ThenBy(x => x.User.UserName)
                .Take(6)
                .Select(x => x.User)
                .ToList();


            // VIEW MODEL

            var viewModel = new FriendsViewModel
            {
                CurrentUserId = userId,
                Friendships = friendships,
                Friends = friends,
                IncomingRequests = incomingRequests,
                SearchResults = searchResults,
                Suggestions = suggestions,
                SearchTerm = search,
                FriendIds = friendIds,
                IncomingUserIds = incomingUserIds,
                OutgoingUserIds = outgoingUserIds
            };

            return View(viewModel);
        }


        // SEND REQUEST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(string username)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid username."
                });
            }

            var requester = await _userManager.FindByIdAsync(currentUserId);

            if (requester == null)
                return Unauthorized();

            var targetUser = await _userManager.FindByNameAsync(username);

            if (targetUser == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found."
                });
            }

            if (targetUser.Id == currentUserId)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "You cannot add yourself."
                });
            }

            var existingFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == currentUserId &&
                     f.AddresseeId == targetUser.Id)
                    ||
                    (f.RequesterId == targetUser.Id &&
                     f.AddresseeId == currentUserId));

            if (existingFriendship != null)
            {
                if (existingFriendship.Status == FriendshipStatus.Rejected)
                {
                    existingFriendship.RequesterId = currentUserId;
                    existingFriendship.AddresseeId = targetUser.Id;
                    existingFriendship.Status = FriendshipStatus.Pending;
                    existingFriendship.CreatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    await _notificationService.CreateAsync(
                        targetUser.Id,
                        "FriendRequest",
                        "New friend request",
                        $"{requester.UserName} sent you a friend request.",
                        $"/Profile/{requester.UserName}"
                    );

                    return Json(new
                    {
                        success = true,
                        status = "pending"
                    });
                }

                return Json(new
                {
                    success = false,
                    status = existingFriendship.Status
                        .ToString()
                        .ToLower()
                });
            }

            var friendship = new Friendship
            {
                RequesterId = currentUserId,
                AddresseeId = targetUser.Id,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Friendships.Add(friendship);

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                targetUser.Id,
                "FriendRequest",
                "New friend request",
                $"{requester.UserName} sent you a friend request.",
                $"/Profile/{requester.UserName}"
            );

            return Json(new
            {
                success = true,
                status = "pending"
            });
        }


        // ACCEPT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            var friendship = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .FirstOrDefaultAsync(f =>
                    f.Id == id &&
                    f.AddresseeId == userId &&
                    f.Status == FriendshipStatus.Pending);

            if (friendship == null)
            {
                return NotFound(new
                {
                    success = false
                });
            }

            friendship.Status = FriendshipStatus.Accepted;

            await _context.SaveChangesAsync();

            var accepter = friendship.Addressee;

            if (accepter != null)
            {
                await _notificationService.CreateAsync(
                    friendship.RequesterId,
                    "FriendAccepted",
                    "Friend request accepted",
                    $"{accepter.UserName} accepted your friend request.",
                    $"/Profile/{accepter.UserName}"
                );
            }

            return Json(new
            {
                success = true,
                username = friendship.Requester?.UserName
            });
        }


        // REJECT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            var friendship = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .FirstOrDefaultAsync(f =>
                    f.Id == id &&
                    f.AddresseeId == userId &&
                    f.Status == FriendshipStatus.Pending);

            if (friendship == null)
            {
                return NotFound(new
                {
                    success = false
                });
            }

            friendship.Status = FriendshipStatus.Rejected;

            await _context.SaveChangesAsync();

            var rejecter = friendship.Addressee;

            if (rejecter != null)
            {
                await _notificationService.CreateAsync(
                    friendship.RequesterId,
                    "FriendRejected",
                    "Friend request declined",
                    $"{rejecter.UserName} declined your friend request.",
                    $"/Profile/{rejecter.UserName}"
                );
            }

            return Json(new
            {
                success = true
            });
        }


        // REMOVE FRIEND

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            var friendship = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .FirstOrDefaultAsync(f =>
                    f.Id == id &&
                    f.Status == FriendshipStatus.Accepted &&
                    (
                        f.RequesterId == userId ||
                        f.AddresseeId == userId
                    ));

            if (friendship == null)
            {
                return NotFound(new
                {
                    success = false
                });
            }

            var otherUser =
                friendship.RequesterId == userId
                    ? friendship.Addressee
                    : friendship.Requester;

            var username = otherUser?.UserName;

            _context.Friendships.Remove(friendship);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                username
            });
        }


        // CANCEL REQUEST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(string username)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid username."
                });
            }

            var targetUser = await _userManager.FindByNameAsync(username);

            if (targetUser == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found."
                });
            }

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    f.RequesterId == currentUserId &&
                    f.AddresseeId == targetUser.Id &&
                    f.Status == FriendshipStatus.Pending);

            if (friendship == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Friend request not found."
                });
            }

            _context.Friendships.Remove(friendship);

            var requester = await _userManager.FindByIdAsync(currentUserId);

            if (requester != null)
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.UserId == targetUser.Id &&
                        n.Type == "FriendRequest" &&
                        n.Message != null &&
                        n.Message ==
                        $"{requester.UserName} sent you a friend request.");

                if (notification != null)
                {
                    _context.Notifications.Remove(notification);
                }
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                username = targetUser.UserName
            });
        }
    }
}
