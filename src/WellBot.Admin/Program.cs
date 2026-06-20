using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WellBot.Admin.Data;
using WellBot.Admin.Endpoints;
using WellBot.Admin.Security;
using WellBot.Admin.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=wellbot.db"));

// Security
builder.Services.AddSingleton<AuthService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    })
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BasicAuthentication", policy =>
    {
        policy.AddAuthenticationSchemes("BasicAuthentication");
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("Admin");
    });

    options.AddPolicy("DesktopClient", policy =>
    {
        policy.RequireRole("DesktopClient", "Admin");
    });
});
builder.Services.AddCascadingAuthenticationState();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "WellBot Admin API",
        Version = "v1",
        Description = "API de gestion pour l'application WellBot - Assistant Wellbeing"
    });
});

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Enable detailed Blazor circuit errors in dev
builder.Services.AddServerSideBlazor().AddCircuitOptions(options => options.DetailedErrors = true);

// CORS for desktop client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDesktopClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<SimpleLocalizer>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Apply migrations and create DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    try { db.Database.ExecuteSqlRaw("ALTER TABLE HealthTips ADD COLUMN GroupId TEXT NOT NULL DEFAULT ''"); }
    catch { /* Column already exists */ }

    // Fix empty GroupIds for seeded data
    var emptyGroupTips = db.HealthTips.Where(t => t.GroupId == "").ToList();
    if (emptyGroupTips.Any())
    {
        foreach (var tip in emptyGroupTips)
        {
            tip.GroupId = tip.Id switch
            {
                1 or 11 or 16 => "posture",
                2 or 12 or 17 => "screen",
                3 or 13 or 18 => "nutrition",
                4 or 14 => "light",
                5 or 15 => "sleep",
                6 => "mental-break",
                7 => "keyboard",
                8 => "hydration",
                9 => "stairs",
                10 => "micro-breaks",
                _ => Guid.NewGuid().ToString("N")
            };
        }
        db.SaveChanges();
    }
}

// Middleware
app.UseStaticFiles();

app.UseRouting();

// Authentication and Authorization must be between UseRouting and UseEndpoints (or Map...)
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

var supportedCultures = new[] { "fr", "en", "ar" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Language switcher endpoint
app.MapGet("/culture/set", (string culture, string redirectUri, HttpContext context) =>
{
    if (culture != null)
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture, culture)));
    }
    return Results.Redirect(redirectUri ?? "/");
});

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "WellBot Admin API v1");
    options.RoutePrefix = "swagger";
});

// Map endpoints
var apiGroup = app.MapGroup("/api").RequireAuthorization("BasicAuthentication");
// Note: We need to modify Endpoints to use this group, or just require authorization globally for them.
// Let's just modify Endpoints to map to `apiGroup` or we can just apply RequireAuthorization on each.
app.MapHealthTipsEndpoints();
app.MapConfigEndpoints();
app.MapAnalyticsEndpoints();

app.MapAuthEndpoints();

app.MapRazorComponents<WellBot.Admin.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
