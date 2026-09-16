
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using SideQuestApp.Models;

namespace SideQuestApp.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Please choose a username.")]
            [StringLength(
                20,
                MinimumLength = 3,
                ErrorMessage = "Username must be between 3 and 20 characters.")]
            [RegularExpression(
                @"^[a-zA-Z0-9_]+$",
                ErrorMessage = "Username can contain only letters, numbers and underscores.")]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Please enter your email.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Please enter a password.")]
            [StringLength(
                100,
                ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
                MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Please confirm your password.")]
            [DataType(DataType.Password)]
            [Compare(
                "Password",
                ErrorMessage = "The password and confirmation password do not match.")]
            [Display(Name = "Confirm password")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            ExternalLogins =
                (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins =
                (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var username = Input.Username.Trim();
            var email = Input.Email.Trim();

            // --------------------------------------------------
            // USERNAME CHECK
            // --------------------------------------------------

            var existingUser =
                await _userManager.FindByNameAsync(username);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Input.Username",
                    "That username is already taken.");

                return Page();
            }

            // --------------------------------------------------
            // EMAIL CHECK
            // --------------------------------------------------

            var existingEmail =
                await _userManager.FindByEmailAsync(email);

            if (existingEmail != null)
            {
                ModelState.AddModelError(
                    "Input.Email",
                    "An account with this email already exists.");

                return Page();
            }

            // --------------------------------------------------
            // CREATE USER
            // --------------------------------------------------

            var user = CreateUser();

            /*
             * UserName = public SideQuest username.
             * Email    = account/login email.
             */

            await _userStore.SetUserNameAsync(
                user,
                username,
                CancellationToken.None);

            await _emailStore.SetEmailAsync(
                user,
                email,
                CancellationToken.None);

            // --------------------------------------------------
            // CREATE IDENTITY ACCOUNT
            // --------------------------------------------------

            var result =
                await _userManager.CreateAsync(
                    user,
                    Input.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return Page();
            }

            _logger.LogInformation(
                "User created a new account with username '{Username}'.",
                username);

            // --------------------------------------------------
            // DEFAULT USER ROLE
            // --------------------------------------------------

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                var roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        "User");

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        _logger.LogWarning(
                            "Could not add User role to '{Username}': {Error}",
                            username,
                            error.Description);
                    }
                }
            }

            // --------------------------------------------------
            // AUTOMATIC LOGIN
            // --------------------------------------------------

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            _logger.LogInformation(
                "User '{Username}' logged in automatically after registration.",
                username);

            return LocalRedirect(returnUrl);
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class " +
                    $"and has a parameterless constructor, or alternatively override " +
                    $"the register page in " +
                    $"/Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException(
                    "The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}

