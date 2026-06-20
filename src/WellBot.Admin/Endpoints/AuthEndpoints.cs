using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WellBot.Admin.Security;

namespace WellBot.Admin.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            [FromForm] string username, 
            [FromForm] string password, 
            [FromForm] string returnUrl,
            AuthService authService, 
            HttpContext context) =>
        {
            var (isValid, role) = authService.ValidateCredentials(username, password);
            if (isValid)
            {
                if (role == "DesktopClient")
                {
                    return Results.Redirect("/login?error=2"); // Access Denied
                }

                var claims = new[] { 
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                
                return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
            }

            return Results.Redirect("/login?error=1");
        }).AllowAnonymous().DisableAntiforgery();

        app.MapGet("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });
    }
}
