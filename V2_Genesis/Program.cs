using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using QuestPDF.Infrastructure;
using V2_Genesis.Data;

using V2_Genesis.Models;
using V2_Genesis.Models.Configuration;
using V2_Genesis.Models.Emails;
using V2_Genesis.Models.Entities;
using V2_Genesis.Services;
using V2_Genesis.Services.Attributes;
using V2_Genesis.Services.Implementations;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Notice;
using V2_Genesis.Services.Objection;
using V2_Genesis.Services.PropertySearch;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

QuestPDF.Settings.License = LicenseType.Community;
// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Session ───────────────────────────────────────────────────────────────────
var sessionMins = cfg.GetValue<int>("Session:TimeoutMinutes", 480);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(sessionMins);
    o.Cookie.Name = "V2Genesis.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(cfg.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<RebateDBContext>(o =>
    o.UseSqlServer(cfg.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<AttributesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AttributesConnection")));

builder.Services.AddDbContext<QueryDbContext>(o =>
              o.UseSqlServer(builder.Configuration
                 .GetConnectionString("QueryConnection")));

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
builder.Services.AddAuthentication().AddNegotiate();
// ── Cookie paths (clean URLs, no /Account/Login) ─────────────────────────────
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/login";
    o.LogoutPath = "/logout";
    o.AccessDeniedPath = "/access-denied";

    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;

    o.Cookie.Name = "V2Genesis.Auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// ── Settings POCOs ────────────────────────────────────────────────────────────
builder.Services.Configure<AppSettings>(cfg.GetSection("AppSettings"));
builder.Services.Configure<EmailSettings>(cfg.GetSection("Email"));
builder.Services.Configure<ReCaptchaSettings>(cfg.GetSection("ReCaptcha"));
builder.Services.Configure<SessionSettings>(cfg.GetSection("Session"));
builder.Services.Configure<ValuationRollSettings>(cfg.GetSection("ValuationRoll"));
builder.Services.Configure<DisclaimerSettings>(cfg.GetSection("Disclaimer"));
builder.Services.Configure<RollDatesSettings>(opts =>builder.Configuration.GetSection("RollDates").Bind(opts.Dates));
builder.Services.Configure<ObjectionRollSettings>(cfg => builder.Configuration.Bind(cfg));
builder.Services.Configure<NoticeRollSettings>(opts =>builder.Configuration.GetSection("NoticeRolls").Bind(opts.NoticeRolls));
builder.Services.Configure<AttributeStorageOptions>(
    builder.Configuration.GetSection("AttributeStorage"));


// ── Custom Services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddHttpClient<IReCaptchaService, ReCaptchaService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IPropertySearchService, PropertySearchService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IObjectionService, ObjectionService>();
builder.Services.AddScoped<IObjectionFormService, ObjectionFormService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INoticeService, NoticeService>();
builder.Services.AddScoped<IAttributesDashboardService, AttributesDashboardService>();
builder.Services.AddScoped<IEvidenceService, EvidenceService>();
builder.Services.AddScoped<ISection51Service, Section51Service>();
builder.Services.AddScoped<ILisSearchService, LisSearchService>();
builder.Services.AddScoped<IOmissionService, OmissionService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IHomeSearchService, HomeSearchService>();
builder.Services.AddScoped<IAttributesSearchService, AttributesSearchService>();
builder.Services.AddScoped<IAttributeSubmissionService, AttributeSubmissionService>();
builder.Services.AddScoped<IAttributeDocumentService, AttributeDocumentService>();
builder.Services.AddScoped<ISection78Service, Section78Service>();
builder.Services.AddScoped<IRebatesService, RebatesService>();
builder.Services.AddScoped<ISubmittedFormPdfService, SubmittedFormPdfService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddDataProtection();

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
app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
