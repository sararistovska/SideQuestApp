using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuestApp.Data;
using SideQuestApp.Models;

namespace SideQuestApp.Controllers
{
    [Authorize]
    public class LeaderboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaderboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var currentUserId = _userManager.GetUserId(User);

            var users = await _context.Users
                .AsNoTracking()
                .OrderByDescending(u => u.Points)
                .ThenByDescending(u => u.Level)
                .ThenBy(u => u.UserName)
                .ToListAsync();

            search = search?.Trim();

            ApplicationUser? searchedUser = null;

            if (!string.IsNullOrWhiteSpace(search))
            {
                searchedUser = users.FirstOrDefault(u =>
                    u.UserName != null &&
                    u.UserName.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.Search = search;
            ViewBag.SearchedUserId = searchedUser?.Id;

            return View(users);
        }
    }
}