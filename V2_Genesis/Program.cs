using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Emails;
using V2_Genesis.Models.Entities;
using V2_Genesis.Services;
using V2_Genesis.Services;
using V2_Genesis.Services.Implementations;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.PropertySearch;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Session ───────────────────────────────────────────────────────────────────
var sessionMins = cfg.GetValue<int>("Session:TimeoutMinutes", 30);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(sessionMins);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(cfg.GetConnectionString("DefaultConnection")));

// ── Identity ─────────────────────────────────────────────────────────────────
var idCfg = cfg.GetSection("Identity");
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.SignIn.RequireConfirmedEmail = idCfg.GetValue<bool>("RequireConfirmedEmail", true);
        o.Password.RequiredLength = idCfg.GetValue<int>("PasswordMinLength", 6);
        o.Password.RequireDigit = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = idCfg.GetValue<int>("MaxFailedAccessAttempts", 5);
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                                                    idCfg.GetValue<int>("LockoutMinutes", 15));
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ── Cookie paths (clean URLs, no /Account/Login) ─────────────────────────────
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/login";
    o.LogoutPath = "/logout";
    o.AccessDeniedPath = "/access-denied";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// ── Settings POCOs ────────────────────────────────────────────────────────────
builder.Services.Configure<AppSettings>(cfg.GetSection("AppSettings"));
builder.Services.Configure<EmailSettings>(cfg.GetSection("Email"));
builder.Services.Configure<ReCaptchaSettings>(cfg.GetSection("ReCaptcha"));
builder.Services.Configure<SessionSettings>(cfg.GetSection("Session"));
builder.Services.Configure<ValuationRollSettings>(cfg.GetSection("ValuationRoll"));
builder.Services.Configure<DisclaimerSettings>(cfg.GetSection("Disclaimer"));

builder.Services.Configure<RollDatesSettings>(opts =>cfg.GetSection("RollDates").Bind(opts.Dates));

// ── Custom Services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddHttpClient<IReCaptchaService, ReCaptchaService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IPropertySearchService, PropertySearchService>();
// ── App Pipeline ──────────────────────────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
