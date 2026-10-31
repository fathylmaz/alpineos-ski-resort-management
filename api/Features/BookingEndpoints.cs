using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Data;
using SkiResort.Api.Models;
using SkiResort.Api.Services;
using static SkiResort.Api.Features.ApiHelpers;

namespace SkiResort.Api.Features;

public static class BookingEndpoints
{
    public record RentalInput(Guid CustomerId, Guid EquipmentId, DateTime StartAt, DateTime EndAt);
    public record StayInput(Guid CustomerId, Guid RoomId, DateTime StartAt, DateTime EndAt, int GuestCount);
    public record Cancellation(string Reason);
    public static void Map(WebApplication app)
    {
        var rentals=app.MapGroup("/api/rentals").RequireAuthorization("Staff");
        rentals.MapGet("", async (ResortDb db) => await db.Rentals.AsNoTracking().OrderByDescending(x=>x.CreatedAt)
            .Select(x=>new {x.Id,x.CustomerId,Customer=x.Customer!.FullName,x.EquipmentId,Equipment=x.Equipment!.Name,AssetCode=x.Equipment!.AssetCode,x.StartAt,x.EndAt,x.Status,x.Total,x.CancellationReason,x.CheckedOutAt,x.ReturnedAt,x.CreatedAt}).ToListAsync());
        rentals.MapPost("", async (RentalInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            if (!Rules.ValidWindow(input.StartAt,input.EndAt) || input.EndAt > DateTime.UtcNow.AddYears(3)) return Invalid("UTC başlangıç/bitiş tarihlerini kontrol edin.");
            if (input.StartAt < DateTime.UtcNow.AddMinutes(-5)) return Invalid("Geçmiş tarih için rezervasyon açılamaz.");
            if (!await db.Customers.AnyAsync(x=>x.Id==input.CustomerId)) return Invalid("Müşteri bulunamadı.");
            var equipment=await db.Equipment.FindAsync(input.EquipmentId);
            if (equipment==null || equipment.Status!="Available") return Results.Conflict(new {error="Ekipman kiralamaya uygun değil."});
            if (await db.Rentals.AnyAsync(x=>x.EquipmentId==input.EquipmentId && (x.Status=="Reserved"||x.Status=="CheckedOut") && x.StartAt<input.EndAt && input.StartAt<x.EndAt))
                return Results.Conflict(new {error="Bu tarihlerde ekipman başka bir rezervasyonda."});
            var rental=new Rental {CustomerId=input.CustomerId,EquipmentId=input.EquipmentId,StartAt=input.StartAt,EndAt=input.EndAt,Total=Rules.RentalTotal(equipment.DailyRate,input.StartAt,input.EndAt)};
            db.Rentals.Add(rental); await db.SaveChangesAsync();await Audit(db,actor,"Create","Rental",rental.Id);
            return Results.Created($"/api/rentals/{rental.Id}",rental);
        }).RequireAuthorization("Bookings");
        rentals.MapPost("/{id:guid}/checkout",async (Guid id,ResortDb db,ClaimsPrincipal actor) =>
        {
            var rent=await db.Rentals.FindAsync(id);
            if (rent==null)return Results.NotFound();
            if (rent.Status!="Reserved")return Results.Conflict(new{error="Yalnız rezerve kiralamalar teslim edilebilir."});
            if (!await db.Equipment.AnyAsync(x => x.Id == rent.EquipmentId && x.Status == "Available"))
                return Results.Conflict(new { error = "Ekipman şu anda kullanıma uygun değil." });
            rent.Status="CheckedOut";rent.CheckedOutAt=DateTime.UtcNow;await db.SaveChangesAsync();await Audit(db,actor,"Checkout","Rental",id);
            return Results.Ok(rent);
        }).RequireAuthorization("Bookings");
        rentals.MapPost("/{id:guid}/return",async (Guid id,ResortDb db,ClaimsPrincipal actor) =>
        {
            var rent=await db.Rentals.FindAsync(id);
            if (rent==null)return Results.NotFound();
            if (rent.Status!="CheckedOut")return Results.Conflict(new{error="Yalnız teslim edilmiş ekipman iade edilebilir."});
            rent.Status="Returned";rent.ReturnedAt=DateTime.UtcNow;await db.SaveChangesAsync();await Audit(db,actor,"Return","Rental",id);
            return Results.Ok(rent);
        }).RequireAuthorization("Bookings");
        rentals.MapPost("/{id:guid}/cancel",async (Guid id,Cancellation input,ResortDb db,ClaimsPrincipal actor) =>
        {
            var rent=await db.Rentals.FindAsync(id);
            if (rent==null)return Results.NotFound();
            if (rent.Status!="Reserved")return Results.Conflict(new{error="Yalnız rezervasyon aşamasındayken iptal edilir."});
            if(!Text(input.Reason,500,4))return Invalid("En az 4 karakter iptal nedeni girin.");
            rent.Status="Cancelled";rent.CancellationReason=input.Reason.Trim();await db.SaveChangesAsync();await Audit(db,actor,"Cancel","Rental",id,input.Reason.Trim());
            return Results.Ok(rent);
        }).RequireAuthorization("Bookings");

        var stays=app.MapGroup("/api/stays").RequireAuthorization("Staff");
        stays.MapGet("",async (ResortDb db)=>await db.Stays.AsNoTracking().OrderByDescending(x=>x.CreatedAt)
            .Select(x=>new {x.Id,x.CustomerId,Customer=x.Customer!.FullName,x.RoomId,Room=x.Room!.Name,RoomCode=x.Room!.Code,x.StartAt,x.EndAt,x.GuestCount,x.Status,x.Total,x.CancellationReason,x.CreatedAt}).ToListAsync());
        stays.MapPost("",async (StayInput input,ResortDb db,ClaimsPrincipal actor)=>
        {
            if(!Rules.ValidWindow(input.StartAt,input.EndAt) || input.StartAt.TimeOfDay!=TimeSpan.Zero || input.EndAt.TimeOfDay!=TimeSpan.Zero || input.EndAt > DateTime.UtcNow.AddYears(3))
                return Invalid("Konaklama tarihlerini UTC gün başlangıcı olarak gönderin. Çıkış girişten sonra olmalı.");
            if (input.StartAt < DateTime.UtcNow.Date) return Invalid("Geçmiş tarihli konaklama açılamaz.");
            if(!await db.Customers.AnyAsync(x=>x.Id==input.CustomerId))return Invalid("Müşteri bulunamadı.");
            var room=await db.Rooms.FindAsync(input.RoomId);
            if(room==null || !room.IsActive)return Invalid("Oda kullanım dışı veya bulunamadı.");
            if(input.GuestCount<1 || input.GuestCount>room.Capacity)return Invalid("Oda kapasitesi aşıldı.");
            if(await db.Stays.AnyAsync(x=>x.RoomId==input.RoomId && (x.Status=="Reserved" || x.Status=="CheckedIn") && x.StartAt<input.EndAt && input.StartAt<x.EndAt))
                return Results.Conflict(new {error="Oda seçilen tarihlerde dolu."});
            var stay=new Stay {CustomerId=input.CustomerId,RoomId=input.RoomId,StartAt=input.StartAt,EndAt=input.EndAt,GuestCount=input.GuestCount,Total=Rules.StayTotal(room.NightlyRate,input.StartAt,input.EndAt)};
            db.Stays.Add(stay);await db.SaveChangesAsync();await Audit(db,actor,"Create","Stay",stay.Id);
            return Results.Created($"/api/stays/{stay.Id}",stay);
        }).RequireAuthorization("Bookings");
        stays.MapPost("/{id:guid}/checkin",async(Guid id,ResortDb db,ClaimsPrincipal actor)=>
        {
            var stay=await db.Stays.FindAsync(id);if(stay==null)return Results.NotFound();
            if(stay.Status!="Reserved")return Results.Conflict(new{error="Yalnız rezervasyon giriş yapabilir."});
            stay.Status="CheckedIn";await db.SaveChangesAsync();await Audit(db,actor,"Checkin","Stay",id);return Results.Ok(stay);
        }).RequireAuthorization("Bookings");
        stays.MapPost("/{id:guid}/checkout",async(Guid id,ResortDb db,ClaimsPrincipal actor)=>
        {
            var stay=await db.Stays.FindAsync(id);if(stay==null)return Results.NotFound();
            if(stay.Status!="CheckedIn")return Results.Conflict(new{error="Yalnız giriş yapmış konaklama çıkış yapabilir."});
            stay.Status="Completed";await db.SaveChangesAsync();await Audit(db,actor,"Checkout","Stay",id);return Results.Ok(stay);
        }).RequireAuthorization("Bookings");
        stays.MapPost("/{id:guid}/cancel",async(Guid id,Cancellation input,ResortDb db,ClaimsPrincipal actor)=>
        {
            var stay=await db.Stays.FindAsync(id);if(stay==null)return Results.NotFound();
            if(stay.Status!="Reserved")return Results.Conflict(new{error="Yalnız bekleyen rezervasyon iptal edilir."});
            if(!Text(input.Reason,500,4))return Invalid("İptal nedeni gerekli.");
            stay.Status="Cancelled";stay.CancellationReason=input.Reason.Trim();await db.SaveChangesAsync();await Audit(db,actor,"Cancel","Stay",id,input.Reason.Trim());return Results.Ok(stay);
        }).RequireAuthorization("Bookings");
    }
}
