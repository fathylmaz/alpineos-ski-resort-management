using System.Security.Claims;
using SkiResort.Api.Data;
using SkiResort.Api.Models;

namespace SkiResort.Api.Features;

public static class ApiHelpers
{
    public static IResult Invalid(string message) => Results.BadRequest(new { error = message });
    public static bool Text(string? value, int limit = 120, int min = 2) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= min && value.Length <= limit;
    public static async Task Audit(ResortDb db, ClaimsPrincipal principal, string action, string entity, Guid id, string details = "")
    {
        db.AuditEvents.Add(new AuditEvent {
            Actor = principal.FindFirst(ClaimTypes.Email)?.Value ?? "system", Action = action,
            Entity = entity, EntityId = id, Details = details
        });
        await db.SaveChangesAsync();
    }
}
