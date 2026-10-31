using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Data;
using SkiResort.Api.Models;

namespace SkiResort.Api.Security;

public static class AuthEndpoints
{
    public record LoginRequest(string Email, string Password);
    public record CreateStaffRequest(string Email, string DisplayName, string Password, string Role);

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { token = tokens.RequestToken });
        });
        group.MapGet("/me", (ClaimsPrincipal user) =>
            Results.Ok(new { name = user.Identity!.Name, role = user.FindFirst(ClaimTypes.Role)?.Value, email = user.FindFirst(ClaimTypes.Email)?.Value }))
            .RequireAuthorization();
        group.MapPost("/login", async (LoginRequest req, ResortDb db,
            IPasswordHasher<StaffAccount> hasher, HttpContext context) =>
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var acc = await db.Accounts.SingleOrDefaultAsync(a => a.Email == email);
            if (acc == null || !acc.IsActive || (acc.LockedUntil.HasValue && acc.LockedUntil > DateTime.UtcNow))
                return Results.Problem("E-posta veya parola hatalı.", statusCode: 401);
            var result = hasher.VerifyHashedPassword(acc, acc.PasswordHash, req.Password ?? "");
            if (result == PasswordVerificationResult.Failed)
            {
                acc.FailedAttempts++;
                if (acc.FailedAttempts >= 5) { acc.LockedUntil = DateTime.UtcNow.AddMinutes(15); acc.FailedAttempts = 0; }
                await db.SaveChangesAsync();
                return Results.Problem("E-posta veya parola hatalı.", statusCode: 401);
            }
            acc.FailedAttempts = 0;
            acc.LockedUntil = null;
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
                acc.PasswordHash = hasher.HashPassword(acc, req.Password);
            await db.SaveChangesAsync();
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, acc.Id.ToString()),
                new Claim(ClaimTypes.Name, acc.DisplayName),
                new Claim(ClaimTypes.Email, acc.Email),
                new Claim(ClaimTypes.Role, acc.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Results.Ok(new { name = acc.DisplayName, role = acc.Role, email = acc.Email });
        }).RequireRateLimiting("login");
        group.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { success = true });
        }).RequireAuthorization();
        group.MapGet("/staff", async (ResortDb db) => await db.Accounts.OrderBy(a => a.DisplayName)
            .Select(a => new { a.Id, a.Email, a.DisplayName, a.Role, a.IsActive }).ToListAsync()).RequireAuthorization("Admin");
        group.MapPost("/staff", async (CreateStaffRequest req, ResortDb db, IPasswordHasher<StaffAccount> hasher) =>
        {
            var roles = new[] { "Admin", "Reception", "Equipment", "Slope", "Cashier" };
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            if (!email.Contains('@') || req.Password is null || req.Password.Length < 12 ||
                req.DisplayName is null || req.DisplayName.Trim().Length < 2 || !roles.Contains(req.Role))
                return Results.BadRequest(new { error = "Geçerli e-posta, en az 12 karakter parola, ad ve rol gereklidir." });
            if (await db.Accounts.AnyAsync(x => x.Email == email)) return Results.Conflict(new { error = "E-posta kullanımda." });
            var account = new StaffAccount { Email = email, DisplayName = req.DisplayName.Trim(), Role = req.Role };
            account.PasswordHash = hasher.HashPassword(account, req.Password);
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
            return Results.Created($"/api/auth/staff/{account.Id}", new { account.Id, account.Email, account.DisplayName, account.Role });
        }).RequireAuthorization("Admin");
    }
}
