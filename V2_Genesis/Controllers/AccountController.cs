using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Web;
using V2_Genesis.Models.Entities;
using V2_Genesis.Models.ViewModels.Account;
using V2_Genesis.Services;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers
{
    [Controller]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _email;
        private readonly IReCaptchaService _captcha;
        private readonly IUserManagementService _umService;
        private readonly AppSettings _app;
        private readonly SessionSettings _session;
        private readonly string _captchaSiteKey;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IReCaptchaService reCaptcha,
            IUserManagementService umService,
            IOptions<AppSettings> appOpts,
            IOptions<SessionSettings> sessionOpts,
            IOptions<ReCaptchaSettings> captchaOpts,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _email = emailService;
            _captcha = reCaptcha;
            _umService = umService;
            _app = appOpts.Value;
            _session = sessionOpts.Value;
            _captchaSiteKey = captchaOpts.Value.SiteKey;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  REGISTER
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("register")]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToUserDashboard();

            ViewBag.ReturnUrl = returnUrl;
            return View(new RegisterPageViewModel { ActiveTab = "individual" });
        }

        [HttpPost]
        [Route("register/individual")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterIndividual(RegisterPageViewModel model, string? returnUrl = null)
        {
            model.ActiveTab = "individual";
            ViewBag.ReturnUrl = returnUrl;

            // Validate only the Individual sub-model
            ClearCompanyErrors();

            // Must have either ID or Passport
            if (string.IsNullOrWhiteSpace(model.Individual.IDNumber) &&
                string.IsNullOrWhiteSpace(model.Individual.PassportNumber))
            {
                ModelState.AddModelError("Individual.IDNumber",
                    "Please provide either a South African ID number or a passport number.");
            }

            if (!ModelState.IsValid)
                return View("Register", model);

            var user = new ApplicationUser
            {
                UserName = model.Individual.Email,
                Email = model.Individual.Email,
                FirstName = model.Individual.FirstName.Trim(),
                LastName = model.Individual.LastName.Trim(),
                PhoneNumber = model.Individual.PhoneNumber?.Trim(),
                IDNumber = model.Individual.IDNumber?.Trim(),
                PassportNumber = model.Individual.PassportNumber?.Trim(),
                CreationDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Individual.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View("Register", model);
            }

            // Assign Client role
            await EnsureRoleAsync("Client");
            await _userManager.AddToRoleAsync(user, "Client");

            // Send email confirmation
            await SendConfirmationEmailAsync(user);

            _logger.LogInformation("Individual account created for {Email}", user.Email);
            return RedirectToAction(nameof(RegisterConfirmation));
        }

        [HttpPost]
        [Route("register/company")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCompany(RegisterPageViewModel model, string? returnUrl = null)
        {
            model.ActiveTab = "company";
            ViewBag.ReturnUrl = returnUrl;

            // Validate only the Company sub-model
            ClearIndividualErrors();

            if (!ModelState.IsValid)
                return View("Register", model);

            var user = new ApplicationUser
            {
                UserName = model.Company.Email,
                Email = model.Company.Email,
                PhoneNumber = model.Company.PhoneNumber.Trim(),
                CompanyName = model.Company.CompanyName.Trim(),
                CompanyRegistration = model.Company.CompanyRegistration.Trim(),
                CreationDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Company.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View("Register", model);
            }

            await EnsureRoleAsync("Client");
            await _userManager.AddToRoleAsync(user, "Client");
            await SendConfirmationEmailAsync(user);

            _logger.LogInformation("Company account created for {Email}", user.Email);
            return RedirectToAction(nameof(RegisterConfirmation));
        }

        [HttpGet]
        [Route("register-success")]
        [AllowAnonymous]
        public IActionResult RegisterConfirmation() => View();

        // ══════════════════════════════════════════════════════════════════════
        //  CONFIRM EMAIL
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return RedirectToAction(nameof(Login));

            var token = HttpUtility.UrlDecode(code).Replace(" ", "+");
            var result = await _userManager.ConfirmEmailAsync(user, token);

            ViewBag.Success = result.Succeeded;
            return View();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  LOGIN
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("login")]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToUserDashboard();

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.RecaptchaSiteKey = _captchaSiteKey;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.RecaptchaSiteKey = _captchaSiteKey;
            // 1. Validate email + password fields first
            if (!ModelState.IsValid)
                return View(model);

            // 2. reCAPTCHA — read from g-recaptcha-response (widget posts under this name)
            var captchaToken = Request.Form["g-recaptcha-response"].ToString();
            if (string.IsNullOrWhiteSpace(captchaToken) || !await _captcha.VerifyAsync(captchaToken))
            {
                ModelState.AddModelError(string.Empty, "Please complete the reCAPTCHA verification.");
                return View(model);
            }

            // 3. Find user
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }


            // 3. Verify password (without signing in yet — for both admin and client)
            var passwordOk = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordOk)
            {
                await _userManager.AccessFailedAsync(user);
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // 4. Check lockout
            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError(string.Empty,
                    "Your account has been temporarily locked due to multiple failed attempts. Please try again later.");
                return View(model);
            }

            // 5. Admin flow — route to SAP verification step
            if (model.Email.Equals(_app.AdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.SetString(_session.AdminPendingKey, "true");
                HttpContext.Session.SetString(_session.AdminEmailKey, user.Id);
                return RedirectToAction(nameof(SapStep));
            }

            // 6. Client flow — require confirmed email
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty,
                    "Your email address has not been confirmed. Please check your inbox for the confirmation link.");
                return View(model);
            }

            // 7. Sign client in
            await _userManager.ResetAccessFailedCountAsync(user);
            await _signInManager.SignInAsync(user, model.RememberMe);

            _logger.LogInformation("Client {Email} signed in.", user.Email);

            return LocalRedirectOrDashboard(returnUrl);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ADMIN – SAP STEP
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("verify-identity")]
        [AllowAnonymous]
        public IActionResult SapStep()
        {
            // Must have a pending admin session
            if (HttpContext.Session.GetString(_session.AdminPendingKey) != "true")
                return RedirectToAction(nameof(Login));

            return View(new SapStepViewModel());
        }

        [HttpPost]
        [Route("verify-identity")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SapStep(SapStepViewModel model)
        {
            if (HttpContext.Session.GetString(_session.AdminPendingKey) != "true")
                return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
                return View(model);

            // Validate SAP via UserManagement DB
            var umResult = await _umService.ValidateAdminAsync(model.SapNumber);

            if (umResult is null)
            {
                ModelState.AddModelError(string.Empty,
                    "SAP number not recognised or you do not have access to this system.");
                return View(model);
            }

            // Retrieve the pending admin user from Objection DB
            var userId = HttpContext.Session.GetString(_session.AdminEmailKey)!;
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(Login));
            }

            // Ensure Admin role exists and assign (role table only — no claims table touched)
            await EnsureRoleAsync("Admin");
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
                await _userManager.AddToRoleAsync(user, "Admin");

            // ── Build extra claims in memory — NOT written to AspNetUserClaims ──────
            // This avoids the NULL Id error on the existing DB table
            var additionalClaims = new List<Claim>
    {
        new Claim("SAPNumber", $@"{_app.SapDomain}\{model.SapNumber.Trim()}"),
        new Claim("UMRole",    umResult.Role)
    };

            // ── Sign in with claims baked into the cookie — zero DB writes ──────────
            HttpContext.Session.Remove(_session.AdminPendingKey);
            HttpContext.Session.Remove(_session.AdminEmailKey);

            await _signInManager.SignInWithClaimsAsync(
                user,
                isPersistent: false,
                additionalClaims);

            _logger.LogInformation("Admin {SAP} signed in (role: {Role}).",
                model.SapNumber, umResult.Role);

            return RedirectToAction("Index", "Admin");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FORGOT / RESET PASSWORD
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet]
        [Route("forgot-password")]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [Route("forgot-password")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Always show success to prevent email enumeration
            if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = HttpUtility.UrlEncode(token);
                var resetLink = Url.Action(
                    nameof(ResetPassword), "Account",
                    new { userId = user.Id, code = encodedToken, email = user.Email }, // ✅ email added
                    Request.Scheme)!;

                await _email.SendPasswordResetEmailAsync(
                    user.Email!, user.DisplayName, resetLink);
            }

            TempData["ForgotSuccess"] = true;
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        [Route("reset-password")]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? userId = null, string? code = null, string? email = null) // ✅ email added
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                return RedirectToAction(nameof(Login));

            return View(new ResetPasswordViewModel
            {
                UserId = userId,
                Code = code,
                Email = email ?? string.Empty  // ✅ now populated
            });
        }

        [HttpPost]
        [Route("reset-password")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user is null)
            {
                TempData["ResetSuccess"] = true;
                return RedirectToAction(nameof(Login));
            }

            var token = HttpUtility.UrlDecode(model.Code).Replace(" ", "+");
            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            TempData["ResetSuccess"] = true;
            return RedirectToAction(nameof(Login));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  LOGOUT
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [Route("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private async Task EnsureRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        private async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            var confirmLink = Url.Action(
                nameof(ConfirmEmail), "Account",
                new { userId = user.Id, code = encodedToken },
                Request.Scheme)!;

            await _email.SendConfirmationEmailAsync(user.Email!, user.DisplayName, confirmLink);
        }

        private void ClearCompanyErrors()
        {
            var companyKeys = ModelState.Keys
                .Where(k => k.StartsWith("Company.", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in companyKeys) ModelState.Remove(key);
        }

        private void ClearIndividualErrors()
        {
            var individualKeys = ModelState.Keys
                .Where(k => k.StartsWith("Individual.", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in individualKeys) ModelState.Remove(key);
        }

        private IActionResult RedirectToUserDashboard() =>
            User.IsInRole("Admin")
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Dashboard");

        private IActionResult LocalRedirectOrDashboard(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToUserDashboard();
        }
    }

}
