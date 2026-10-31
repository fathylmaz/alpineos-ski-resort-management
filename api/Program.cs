using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkiResort.Api.Data;
using SkiResort.Api.Features;
using SkiResort.Api.Models;
using SkiResort.Api.Security;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Resort")
    ?? throw new InvalidOperationException("ConnectionStrings:Resort must be configured");
builder.Services.AddDbContext<ResortDb>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IPasswordHasher<StaffAccount>, PasswordHasher<StaffAccount>>();
builder.Services.AddScoped<IPasswordHasher<Customer>, PasswordHasher<Customer>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "skiresort.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", p => p.RequireRole("Admin"));
    options.AddPolicy("Staff", p => p.RequireRole("Admin", "Reception", "Equipment", "Slope", "Cashier"));
    options.AddPolicy("Customer", p => p.RequireRole("Customer"));
    options.AddPolicy("Bookings", p => p.RequireRole("Admin", "Reception"));
    options.AddPolicy("Inventory", p => p.RequireRole("Admin", "Equipment"));
    options.AddPolicy("Operations", p => p.RequireRole("Admin", "Slope"));
    options.AddPolicy("Finance", p => p.RequireRole("Admin", "Cashier"));
});
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
var keysPath = builder.Configuration["DATA_PROTECTION_KEYS_PATH"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", limiter => { limiter.PermitLimit = 8; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; });
});

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var conflict = ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pe && pe.SqlState is "23P01" or "23505" or "23503";
    conflict |= ex is PostgresException postgres && postgres.SqlState is "23P01" or "23505" or "23503" or "40001";
    context.Response.StatusCode = conflict ? 409 : 500;
    await Results.Problem(title: conflict ? "İşlem çakışması" : "Sunucu hatası",
        detail: conflict ? "Kayıt çakışıyor veya ilişkili veriye bağlı. Güncel verileri kontrol edin." : "İşlem tamamlanamadı.",
        statusCode: context.Response.StatusCode).ExecuteAsync(context);
}));
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
// The antiforgery header is required on every unsafe API request, even login/logout.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api") &&
        !(HttpMethods.IsGet(ctx.Request.Method) || HttpMethods.IsHead(ctx.Request.Method) || HttpMethods.IsOptions(ctx.Request.Method)))
    {
        try { await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx); }
        catch (AntiforgeryValidationException)
        {
            ctx.Response.StatusCode = 400;
            await Results.Problem("Güvenlik doğrulaması eksik. Sayfayı yenileyin.", statusCode: 400).ExecuteAsync(ctx);
            return;
        }
    }
    await next();
});

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "SkiResort Management", timestamp = DateTime.UtcNow }));
app.MapHealthChecks("/healthz");
AuthEndpoints.Map(app);
CustomerEndpoints.Map(app);
MasterEndpoints.Map(app);
BookingEndpoints.Map(app);
ActivityEndpoints.Map(app);
FinanceEndpoints.Map(app);
DashboardEndpoints.Map(app);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ResortDb>();
    // Initial deployment uses EnsureCreated; documented limitation: generate EF migrations before production.
    await db.Database.EnsureCreatedAsync();
    // Additive upgrade for already-running v1 PostgreSQL volumes. No customer records are deleted.
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE "Customers" ADD COLUMN IF NOT EXISTS "PasswordHash" varchar(250) NOT NULL DEFAULT '';
        ALTER TABLE "Customers" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;
        ALTER TABLE "Customers" ADD COLUMN IF NOT EXISTS "FailedAttempts" integer NOT NULL DEFAULT 0;
        ALTER TABLE "Customers" ADD COLUMN IF NOT EXISTS "LockedUntil" timestamp with time zone NULL;
        CREATE INDEX IF NOT EXISTS "IX_Customers_Email" ON "Customers" ("Email");
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_Customers_PortalEmail" ON "Customers" (lower("Email")) WHERE "PasswordHash" <> '';
        """);
    await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS btree_gist;");
    await db.Database.ExecuteSqlRawAsync("""
        DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'no_overlapping_active_rentals') THEN
                ALTER TABLE "Rentals" ADD CONSTRAINT no_overlapping_active_rentals
                  EXCLUDE USING gist ("EquipmentId" WITH =, tstzrange("StartAt", "EndAt", '[)') WITH &&)
                  WHERE ("Status" IN ('Reserved','CheckedOut'));
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'no_overlapping_active_stays') THEN
                ALTER TABLE "Stays" ADD CONSTRAINT no_overlapping_active_stays
                  EXCLUDE USING gist ("RoomId" WITH =, tstzrange("StartAt", "EndAt", '[)') WITH &&)
                  WHERE ("Status" IN ('Reserved','CheckedIn'));
            END IF;
        END $$;
        """);
    await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<StaffAccount>>(), builder.Configuration);
}
app.Run();

public partial class Program { }
