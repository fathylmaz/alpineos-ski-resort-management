using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Data;

namespace SkiResort.Api.Features;

public static class DashboardEndpoints
{
    public static void Map(WebApplication app)
    {
        var group=app.MapGroup("/api/dashboard").RequireAuthorization("Staff");
        group.MapGet("",async(ResortDb db)=>
        {
            var now=DateTime.UtcNow;
            var rentals=await db.Rentals.AsNoTracking().ToListAsync();
            var stays=await db.Stays.AsNoTracking().ToListAsync();
            var payments=await db.Payments.AsNoTracking().ToListAsync();
            var equipmentCount=await db.Equipment.CountAsync();
            var rooms=await db.Rooms.CountAsync(x=>x.IsActive);
            var activeStays=stays.Count(x=>x.Status=="CheckedIn");
            var openSlopes=await db.Slopes.CountAsync(x=>x.Status=="Open");
            var totalSlopes=await db.Slopes.CountAsync();
            var upcoming=await db.Activities.AsNoTracking().Where(x=>x.StartsAt>=now&&x.Status=="Scheduled")
                .OrderBy(x=>x.StartsAt).Take(5).Select(x=>new{x.Id,x.Name,x.StartsAt,x.Instructor,x.Capacity}).ToListAsync();
            var charged=payments.Where(x=>x.Kind=="Charge").Sum(x=>x.Amount);
            var refunded=payments.Where(x=>x.Kind=="Refund").Sum(x=>x.Amount);
            var recent=await db.AuditEvents.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(8).ToListAsync();
            var last7=Enumerable.Range(0,7).Select(offset=>
            {
                var day=DateTime.UtcNow.Date.AddDays(-6+offset);
                return new { date=day.ToString("yyyy-MM-dd"), value=payments.Where(x=>x.CreatedAt>=day&&x.CreatedAt<day.AddDays(1))
                    .Sum(x=>x.Kind=="Charge"?x.Amount:-x.Amount) };
            }).ToList();
            return Results.Ok(new {
                customers=await db.Customers.CountAsync(), equipmentCount,
                availableEquipment=await db.Equipment.CountAsync(x=>x.Status=="Available"),
                activeRentals=rentals.Count(x=>x.Status=="CheckedOut"),
                pendingRentals=rentals.Count(x=>x.Status=="Reserved"),
                rooms, occupiedRooms=activeStays, upcomingStays=stays.Count(x=>x.Status=="Reserved"),
                openSlopes,totalSlopes,netReceived=charged-refunded,
                pendingBalance=rentals.Where(x=>x.Status!="Cancelled").Sum(x=>x.Total) + stays.Where(x=>x.Status!="Cancelled").Sum(x=>x.Total)-(charged-refunded),
                upcoming,recent,trend=last7
            });
        });
        group.MapGet("/audit",async(ResortDb db)=>await db.AuditEvents.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync())
            .RequireAuthorization("Admin");
    }
}
