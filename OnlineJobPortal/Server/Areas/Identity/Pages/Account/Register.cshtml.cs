﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using OnlineJobPortal.Server.Models;
using OnlineJobPortal.Shared;


namespace OnlineJobPortal.Server.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            public string uType { get; set; }

            [Required(ErrorMessage ="The Name is Required")]
            [StringLength(100, ErrorMessage = "The Name is too Long.")]
            [Display(Name ="First Name")]
            public string FirstName { get; set; }

            [StringLength(100, ErrorMessage = "The Name is too Long.")]
            public string LastName { get; set; }

            [Required]
            public bool isChecked { get; set; } 
            
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {

                    _logger.LogInformation("User created a new account with password.");
                   
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                    if (Input.uType == "Candidate")
                    {
                        await _userManager.AddToRoleAsync(user, "Candidate");
                        await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("role", "Candidate"));
                        var db = new OnlineJobPortalContext();
                        Candidate c = new Candidate();
                        c.Fname = Input.FirstName;
                        c.Lname = Input.LastName;
                        c.Email = Input.Email;
                        GlobalVar.emaill = Input.Email;
                        c.Password = Input.Password;
                        db.Candidates.Add(c);
                        await db.SaveChangesAsync();
                    }
                    if (Input.uType == "Employer")
                    {
                        await _userManager.AddToRoleAsync(user, "Employer");
                        await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("role", "Employer"));
                        var db = new OnlineJobPortalContext();
                        Employer e = new Employer();
                        e.CompName = Input.FirstName;
                        e.Email = Input.Email;
                        e.Password = Input.Password;
                        GlobalVar.emaill = Input.Email;
                        db.Employers.Add(e);
                        await db.SaveChangesAsync();
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }


               
            }


            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
