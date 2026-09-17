using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SideQuestApp.Data;
using SideQuestApp.Models;
using Microsoft.AspNetCore.Identity;

namespace SideQuestApp.Controllers
{
    public class BadgeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public BadgeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }


        
       
        
        // INDEX
        

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var badges = await _context.Badges
                .Include(b => b.RequiredCategory)
                .Include(b => b.UserBadges)
                .ToListAsync();

            var approvedCompletions = await _context.QuestCompletions
                .Include(c => c.Quest)
                .Where(c =>
                    c.UserId == userId &&
                    c.Status == "Approved" &&
                    c.Quest != null)
                .ToListAsync();

            var earnedBadgeIds = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            var badgeProgress = badges.Select(badge =>
            {
                int currentProgress;

                if (badge.CriteriaType == BadgeCriteriaType.CompletionCount)
                {
                    currentProgress = approvedCompletions.Count;
                }
                else if (badge.CriteriaType == BadgeCriteriaType.CategoryCount)
                {
                    currentProgress = approvedCompletions.Count(c =>
                        c.Quest!.CategoryId == badge.RequiredCategoryId);
                }
                else
                {
                    currentProgress = 0;
                }

                var requiredCount = Math.Max(
                    badge.RequiredCount,
                    1
                );

                var isEarned = earnedBadgeIds.Contains(badge.Id);

                var remaining = Math.Max(
                    requiredCount - currentProgress,
                    0
                );

                var progressPercentage = Math.Min(
                    (double)currentProgress / requiredCount * 100,
                    100
                );

                return new
                {
                    Badge = badge,
                    CurrentProgress = currentProgress,
                    RequiredCount = requiredCount,
                    IsEarned = isEarned,
                    Remaining = remaining,
                    ProgressPercentage = progressPercentage
                };
            }).ToList();

            return View(badgeProgress);
        }


        
        // DETAILS
        

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var badge = await _context.Badges
                .Include(b => b.RequiredCategory)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (badge == null)
                return NotFound();

            return View(badge);
        }


        
        // CREATE - GET
        

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["RequiredCategoryId"] =
                new SelectList(
                    _context.Categories,
                    "Id",
                    "Name"
                );

            return View();
        }


        
        // CREATE - POST
        

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Name,Description,CriteriaType,RequiredCount,RequiredCategoryId")]
            Badge badge,
            IFormFile icon)
        {
           
            // VALIDATE IMAGE
           

            if (icon == null || icon.Length == 0)
            {
                ModelState.AddModelError(
                    "icon",
                    "Please upload a badge image."
                );
            }
            else
            {
                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                var extension = Path
                    .GetExtension(icon.FileName)
                    .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "icon",
                        "Only JPG, JPEG, PNG or WEBP images are allowed."
                    );
                }

                const long maxFileSize = 10 * 1024 * 1024;

                if (icon.Length > maxFileSize)
                {
                    ModelState.AddModelError(
                        "icon",
                        "The image must be smaller than 10 MB."
                    );
                }
            }


           
            // SAVE BADGE
           

            if (ModelState.IsValid)
            {
                var badgesFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "badges"
                );

                if (!Directory.Exists(badgesFolder))
                {
                    Directory.CreateDirectory(badgesFolder);
                }


                var extension = Path
                    .GetExtension(icon!.FileName)
                    .ToLowerInvariant();

                var fileName =
                    $"{Guid.NewGuid()}{extension}";

                var filePath = Path.Combine(
                    badgesFolder,
                    fileName
                );


                using (var stream = new FileStream(
                           filePath,
                           FileMode.Create))
                {
                    await icon.CopyToAsync(stream);
                }


                badge.IconUrl =
                    $"/uploads/badges/{fileName}";


                _context.Add(badge);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }


           
            // INVALID MODEL
           

            ViewData["RequiredCategoryId"] =
                new SelectList(
                    _context.Categories,
                    "Id",
                    "Name",
                    badge.RequiredCategoryId
                );

            return View(badge);
        }


        
        // EDIT - GET
        

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var badge = await _context.Badges.FindAsync(id);

            if (badge == null)
                return NotFound();

            ViewData["RequiredCategoryId"] =
                new SelectList(
                    _context.Categories,
                    "Id",
                    "Name",
                    badge.RequiredCategoryId
                );

            return View(badge);
        }


        
        // EDIT - POST
        

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Description,IconUrl,CriteriaType,RequiredCount,RequiredCategoryId")]
            Badge badge,
            IFormFile? icon)
        {
            if (id != badge.Id)
                return NotFound();


            if (ModelState.IsValid)
            {
                try
                {
                    
                    // NEW IMAGE UPLOADED
                    

                    if (icon != null && icon.Length > 0)
                    {
                        var allowedExtensions = new[]
                        {
                            ".jpg",
                            ".jpeg",
                            ".png",
                            ".webp"
                        };

                        var extension = Path
                            .GetExtension(icon.FileName)
                            .ToLowerInvariant();

                        const long maxFileSize =
                            10 * 1024 * 1024;


                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError(
                                "icon",
                                "Only JPG, JPEG, PNG or WEBP images are allowed."
                            );

                            throw new InvalidOperationException(
                                "Invalid badge image."
                            );
                        }


                        if (icon.Length > maxFileSize)
                        {
                            ModelState.AddModelError(
                                "icon",
                                "The image must be smaller than 10 MB."
                            );

                            throw new InvalidOperationException(
                                "Badge image is too large."
                            );
                        }


                        var badgesFolder = Path.Combine(
                            _environment.WebRootPath,
                            "uploads",
                            "badges"
                        );

                        if (!Directory.Exists(badgesFolder))
                        {
                            Directory.CreateDirectory(badgesFolder);
                        }


                        // Delete old image if it belongs
                        // to our badge upload folder.

                        if (!string.IsNullOrWhiteSpace(badge.IconUrl))
                        {
                            var oldRelativePath =
                                badge.IconUrl.TrimStart('/');

                            if (oldRelativePath.StartsWith(
                                    "uploads/badges/",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                var oldFilePath =
                                    Path.Combine(
                                        _environment.WebRootPath,
                                        oldRelativePath
                                    );

                                if (System.IO.File.Exists(oldFilePath))
                                {
                                    System.IO.File.Delete(oldFilePath);
                                }
                            }
                        }


                        var fileName =
                            $"{Guid.NewGuid()}{extension}";

                        var filePath = Path.Combine(
                            badgesFolder,
                            fileName
                        );


                        using (var stream = new FileStream(
                                   filePath,
                                   FileMode.Create))
                        {
                            await icon.CopyToAsync(stream);
                        }


                        badge.IconUrl =
                            $"/uploads/badges/{fileName}";
                    }


                    _context.Update(badge);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BadgeExists(badge.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }
                catch (InvalidOperationException)
                {
                    ViewData["RequiredCategoryId"] =
                        new SelectList(
                            _context.Categories,
                            "Id",
                            "Name",
                            badge.RequiredCategoryId
                        );

                    return View(badge);
                }

                return RedirectToAction(nameof(Index));
            }


            ViewData["RequiredCategoryId"] =
                new SelectList(
                    _context.Categories,
                    "Id",
                    "Name",
                    badge.RequiredCategoryId
                );

            return View(badge);
        }


        
        // DELETE - GET
        

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var badge = await _context.Badges
                .FirstOrDefaultAsync(m => m.Id == id);

            if (badge == null)
                return NotFound();

            return View(badge);
        }


        
        // DELETE - POST
        

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var badge = await _context.Badges
                .FindAsync(id);

            if (badge != null)
            {
                // Delete uploaded badge image.

                if (!string.IsNullOrWhiteSpace(badge.IconUrl))
                {
                    var relativePath =
                        badge.IconUrl.TrimStart('/');

                    if (relativePath.StartsWith(
                            "uploads/badges/",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var filePath =
                            Path.Combine(
                                _environment.WebRootPath,
                                relativePath
                            );

                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }


                _context.Badges.Remove(badge);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        
        // HELPERS
        

        private bool BadgeExists(int id)
        {
            return _context.Badges
                .Any(e => e.Id == id);
        }
    }
}