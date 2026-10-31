using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Data;
using SkiResort.Api.Models;
using SkiResort.Api.Services;
using static SkiResort.Api.Features.ApiHelpers;

namespace SkiResort.Api.Features;

/// <summary>Customer-facing functions. Never accepts customerId from client; uses authenticated cookie identity.</summary>
public static class CustomerEndpoints
{
    public record RegisterInput(string Name, string Email, string Phone, string Password);
    public record LoginInput(string Email, string Password);
    public record ProfileInput(string Name, string Phone);
    public record RentalInput(Guid EquipmentId, DateTime StartAt, DateTime EndAt);
    public record StayInput(Guid RoomId, DateTime StartAt, DateTime EndAt, int GuestCount);
    public record CancelInput(string Reason);
    private static Guid CurrentId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string NormalizedEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();
    private static object SafeProfile(Customer customer) => new { customer.Id, Name = customer.FullName, customer.Email, customer.Phone, customer.CreatedAt };

    public static void Map(WebApplication app)
    {
        var auth = app.MapGroup("/api/customer");
        auth.MapPost("/register", async (RegisterInput input, ResortDb db, IPasswordHasher<Customer> hasher, HttpContext http) =>
        {
            var email = NormalizedEmail(input.Email);
            if (!Text(input.Name,120) || email.Length > 160 || email.Length < 5 || !email.Contains('@') ||
                (input.Phone?.Length ?? 0) > 35 || input.Password is null || input.Password.Length < 12 || input.Password.Length > 128)
                return Invalid("Ad, geçerli e-posta, telefon ve 12–128 karakter parola girin.");
            // Existing legacy contact records cannot be silently taken over without email verification.
            if (await db.Customers.AnyAsync(x => x.Email == email) || await db.Accounts.AnyAsync(x => x.Email == email))
                return Results.Conflict(new { error="Bu e-posta zaten kayıtlı. Mevcut kaydı bağlamak için tesis yetkilisiyle görüşün." });
            var customer = new Customer { FullName=input.Name.Trim(), Email=email, Phone=(input.Phone??"").Trim() };
            customer.PasswordHash=hasher.HashPassword(customer,input.Password);
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            await SignCustomer(http,customer);
            await Audit(db,http.User,"Register","Customer",customer.Id);
            return Results.Created("/api/customer/profile",new { name=customer.FullName, email=customer.Email, role="Customer" });
        }).RequireRateLimiting("login");
        auth.MapPost("/login", async (LoginInput input, ResortDb db, IPasswordHasher<Customer> hasher, HttpContext http) =>
        {
            var email=NormalizedEmail(input.Email);
            var customer=await db.Customers.FirstOrDefaultAsync(x=>x.Email==email && x.PasswordHash!="");
            if (customer==null || !customer.IsActive || (customer.LockedUntil.HasValue && customer.LockedUntil>DateTime.UtcNow))
                return Results.Problem("E-posta veya parola hatalı.",statusCode:401);
            var result=hasher.VerifyHashedPassword(customer,customer.PasswordHash,input.Password??"");
            if(result==PasswordVerificationResult.Failed)
            {
                customer.FailedAttempts++;
                if(customer.FailedAttempts>=5){customer.FailedAttempts=0;customer.LockedUntil=DateTime.UtcNow.AddMinutes(15);}
                await db.SaveChangesAsync();
                return Results.Problem("E-posta veya parola hatalı.",statusCode:401);
            }
            customer.FailedAttempts=0;customer.LockedUntil=null;
            if(result==PasswordVerificationResult.SuccessRehashNeeded)
                customer.PasswordHash=hasher.HashPassword(customer,input.Password);
            await db.SaveChangesAsync();
            await SignCustomer(http,customer);
            return Results.Ok(new { name=customer.FullName,email=customer.Email,role="Customer" });
        }).RequireRateLimiting("login");

        var group=app.MapGroup("/api/customer").RequireAuthorization("Customer");
        group.MapGet("/profile",async (ResortDb db, ClaimsPrincipal user) =>
        {
            var id=CurrentId(user);
            var profile=await db.Customers.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id && x.IsActive);
            return profile==null?Results.NotFound():Results.Ok(SafeProfile(profile));
        });
        group.MapPut("/profile",async (ProfileInput input,ResortDb db,ClaimsPrincipal user) =>
        {
            if(!Text(input.Name,120) || (input.Phone?.Length??0)>35)return Invalid("Ad veya telefon geçersiz.");
            var customer=await db.Customers.FindAsync(CurrentId(user));
            if(customer==null || !customer.IsActive)return Results.NotFound();
            customer.FullName=input.Name.Trim();customer.Phone=(input.Phone??"").Trim();
            await db.SaveChangesAsync();await Audit(db,user,"UpdateProfile","Customer",customer.Id);
            return Results.Ok(SafeProfile(customer));
        });
        group.MapGet("/catalog",async (ResortDb db) => Results.Ok(new {
            equipment=await db.Equipment.AsNoTracking().Where(x=>x.Status=="Available")
                .OrderBy(x=>x.Category).ThenBy(x=>x.AssetCode)
                .Select(x=>new {x.Id,x.Name,x.AssetCode,x.Category,x.Size,x.DailyRate,x.Status}).ToListAsync(),
            rooms=await db.Rooms.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.NightlyRate)
                .Select(x=>new{x.Id,x.Code,x.Name,x.Description,x.Capacity,x.NightlyRate}).ToListAsync(),
            slopes=await db.Slopes.AsNoTracking().OrderBy(x=>x.Difficulty).ThenBy(x=>x.Name)
                .Select(x=>new{x.Id,x.Name,x.Difficulty,x.Status,x.SnowDepthCm,x.Weather,x.UpdatedAt}).ToListAsync(),
            activities=await db.Activities.AsNoTracking().Where(x=>x.Status=="Scheduled" && x.StartsAt>DateTime.UtcNow)
                .OrderBy(x=>x.StartsAt).Select(x=>new{x.Id,x.Name,x.Kind,x.Instructor,x.StartsAt,x.EndsAt,x.Capacity,x.Price,
                    Enrolled=x.Enrollments.Count(e=>e.Status=="Enrolled")}).ToListAsync()
        }));
        // Advisory availability for date pickers. Booking still revalidates in the DB transaction.
        group.MapGet("/availability",async (string kind, Guid resourceId, DateTime startAt, DateTime endAt, ResortDb db) =>
        {
            if (!Rules.ValidWindow(startAt,endAt) || endAt>DateTime.UtcNow.AddYears(3) || startAt<DateTime.UtcNow.AddMinutes(-5))
                return Invalid("Geçersiz tarih aralığı.");
            if(kind=="rental")
            {
                var e=await db.Equipment.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==resourceId);
                if(e==null)return Results.NotFound();
                var overlap=await db.Rentals.AnyAsync(x=>x.EquipmentId==resourceId && (x.Status=="Reserved"||x.Status=="CheckedOut") && x.StartAt<endAt && startAt<x.EndAt);
                return Results.Ok(new { available=e.Status=="Available"&&!overlap, reason=overlap?"Seçilen tarihlerde ekipman rezerve.":e.Status!="Available"?"Ekipman kullanım dışı.":"Müsait" });
            }
            if(kind=="stay")
            {
                var room=await db.Rooms.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==resourceId);
                if(room==null)return Results.NotFound();
                var overlap=await db.Stays.AnyAsync(x=>x.RoomId==resourceId && (x.Status=="Reserved"||x.Status=="CheckedIn") && x.StartAt<endAt && startAt<x.EndAt);
                return Results.Ok(new{available=room.IsActive&&!overlap,reason=overlap?"Seçilen tarihlerde oda dolu.":!room.IsActive?"Oda kullanım dışı.":"Müsait"});
            }
            return Invalid("Geçersiz rezervasyon türü.");
        });
        group.MapGet("/rentals",async(ResortDb db,ClaimsPrincipal user)=>
            await db.Rentals.AsNoTracking().Where(x=>x.CustomerId==CurrentId(user)).OrderByDescending(x=>x.CreatedAt)
            .Select(x=>new{x.Id,x.EquipmentId,Equipment=x.Equipment!.Name,AssetCode=x.Equipment!.AssetCode,x.StartAt,x.EndAt,x.Status,x.Total,x.CancellationReason,x.CreatedAt}).ToListAsync());
        group.MapGet("/stays",async(ResortDb db,ClaimsPrincipal user)=>
            await db.Stays.AsNoTracking().Where(x=>x.CustomerId==CurrentId(user)).OrderByDescending(x=>x.CreatedAt)
            .Select(x=>new{x.Id,x.RoomId,Room=x.Room!.Name,RoomCode=x.Room!.Code,x.StartAt,x.EndAt,x.GuestCount,x.Status,x.Total,x.CancellationReason,x.CreatedAt}).ToListAsync());
        group.MapGet("/enrollments",async(ResortDb db,ClaimsPrincipal user)=>
            await db.Enrollments.AsNoTracking().Where(x=>x.CustomerId==CurrentId(user)).OrderByDescending(x=>x.CreatedAt)
            .Select(x=>new{x.Id,x.ActivityId,Activity=x.Activity!.Name,Kind=x.Activity!.Kind,StartsAt=x.Activity!.StartsAt,Instructor=x.Activity!.Instructor,Price=x.Activity!.Price,x.Status,x.CreatedAt}).ToListAsync());
        group.MapGet("/payments",async(ResortDb db,ClaimsPrincipal user)=>
            await db.Payments.AsNoTracking().Where(x=>x.CustomerId==CurrentId(user)).OrderByDescending(x=>x.CreatedAt)
            .Select(x=>new{x.Id,x.ReferenceId,x.ReferenceType,x.Kind,x.Method,x.Amount,x.CreatedAt}).ToListAsync());

        // All booking writes share the existing Rentals/Stays/Enrollments tables and server-side constraints.
        group.MapPost("/rentals",async(RentalInput input,ResortDb db,ClaimsPrincipal user)=>
        {
            if(!Rules.ValidWindow(input.StartAt,input.EndAt) || input.EndAt>DateTime.UtcNow.AddYears(3) || input.StartAt<DateTime.UtcNow.AddMinutes(-5))
                return Invalid("Gelecekte başlayan geçerli UTC kiralama tarihleri seçin.");
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var equipment=await db.Equipment.FindAsync(input.EquipmentId);
            if(equipment==null||equipment.Status!="Available")return Results.Conflict(new{error="Ekipman kiralamaya uygun değil."});
            if(await db.Rentals.AnyAsync(x=>x.EquipmentId==input.EquipmentId&&(x.Status=="Reserved"||x.Status=="CheckedOut")&&x.StartAt<input.EndAt&&input.StartAt<x.EndAt))
                return Results.Conflict(new{error="Bu ekipman seçilen tarihler arasında dolu."});
            var item=new Rental{CustomerId=CurrentId(user),EquipmentId=equipment.Id,StartAt=input.StartAt,EndAt=input.EndAt,
                Total=Rules.RentalTotal(equipment.DailyRate,input.StartAt,input.EndAt)};
            db.Rentals.Add(item);await db.SaveChangesAsync();await Audit(db,user,"CustomerRental","Rental",item.Id);
            await tx.CommitAsync();return Results.Created($"/api/customer/rentals/{item.Id}",new{item.Id,item.Total,item.Status});
        });
        group.MapPost("/stays",async(StayInput input,ResortDb db,ClaimsPrincipal user)=>
        {
            if(!Rules.ValidWindow(input.StartAt,input.EndAt)||input.StartAt.TimeOfDay!=TimeSpan.Zero||input.EndAt.TimeOfDay!=TimeSpan.Zero||
                input.StartAt<DateTime.UtcNow.Date||input.EndAt>DateTime.UtcNow.AddYears(3))
                return Invalid("Geçerli giriş ve çıkış tarihleri seçin.");
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var room=await db.Rooms.FindAsync(input.RoomId);
            if(room==null||!room.IsActive||input.GuestCount<1||input.GuestCount>room.Capacity)
                return Invalid("Oda bulunamadı veya kişi sayısı kapasiteyi aşıyor.");
            if(await db.Stays.AnyAsync(x=>x.RoomId==input.RoomId&&(x.Status=="Reserved"||x.Status=="CheckedIn")&&x.StartAt<input.EndAt&&input.StartAt<x.EndAt))
                return Results.Conflict(new{error="Oda seçilen tarihlerde dolu."});
            var item=new Stay{CustomerId=CurrentId(user),RoomId=room.Id,StartAt=input.StartAt,EndAt=input.EndAt,
                GuestCount=input.GuestCount,Total=Rules.StayTotal(room.NightlyRate,input.StartAt,input.EndAt)};
            db.Stays.Add(item);await db.SaveChangesAsync();await Audit(db,user,"CustomerStay","Stay",item.Id);
            await tx.CommitAsync();return Results.Created($"/api/customer/stays/{item.Id}",new{item.Id,item.Total,item.Status});
        });
        group.MapPost("/activities/{id:guid}/enroll",async(Guid id,ResortDb db,ClaimsPrincipal user)=>
        {
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var activity=await db.Activities.FindAsync(id);
            if(activity==null)return Results.NotFound();
            if(activity.Status!="Scheduled"||activity.StartsAt<=DateTime.UtcNow)return Results.Conflict(new{error="Kayıtlar kapalı."});
            var customerId=CurrentId(user);
            var existing=await db.Enrollments.SingleOrDefaultAsync(x=>x.ActivityId==id&&x.CustomerId==customerId);
            if(existing?.Status=="Enrolled")return Results.Conflict(new{error="Zaten kayıtlısınız."});
            if(await db.Enrollments.CountAsync(x=>x.ActivityId==id&&x.Status=="Enrolled")>=activity.Capacity)
                return Results.Conflict(new{error="Kontenjan dolu."});
            if(existing==null)db.Enrollments.Add(new Enrollment{CustomerId=customerId,ActivityId=id});
            else existing.Status="Enrolled";
            await db.SaveChangesAsync();await Audit(db,user,"CustomerEnroll","Activity",id);
            await tx.CommitAsync();return Results.Ok(new{success=true});
        });
        group.MapPost("/activities/{id:guid}/cancel",async(Guid id,ResortDb db,ClaimsPrincipal user)=>
        {
            var enrollment=await db.Enrollments.Include(x=>x.Activity)
                .FirstOrDefaultAsync(x=>x.ActivityId==id&&x.CustomerId==CurrentId(user));
            if(enrollment==null)return Results.NotFound();
            if(enrollment.Status!="Enrolled"||enrollment.Activity!.StartsAt<=DateTime.UtcNow)
                return Results.Conflict(new{error="Bu kaydın iptali çevrimiçi yapılamıyor."});
            enrollment.Status="Cancelled";await db.SaveChangesAsync();await Audit(db,user,"CustomerCancelEnrollment","Activity",id);
            return Results.Ok(new{success=true});
        });
        group.MapPost("/rentals/{id:guid}/cancel",async(Guid id,CancelInput input,ResortDb db,ClaimsPrincipal user)=>
        {
            var item=await db.Rentals.FirstOrDefaultAsync(x=>x.Id==id&&x.CustomerId==CurrentId(user));
            if(item==null)return Results.NotFound();
            if(item.Status!="Reserved"||item.StartAt<=DateTime.UtcNow)return Results.Conflict(new{error="Bu kiralama çevrimiçi iptal edilemez."});
            if(!Text(input.Reason,500,4))return Invalid("İptal nedeni en az 4 karakter olmalı.");
            if(await db.Payments.AnyAsync(x=>x.ReferenceType=="Rental"&&x.ReferenceId==id))
                return Results.Conflict(new{error="Ödeme hareketi bulunan rezervasyonu iptal etmek için tesis ile iletişime geçin."});
            item.Status="Cancelled";item.CancellationReason=input.Reason.Trim();await db.SaveChangesAsync();await Audit(db,user,"CustomerCancel","Rental",id);
            return Results.Ok(new{success=true});
        });
        group.MapPost("/stays/{id:guid}/cancel",async(Guid id,CancelInput input,ResortDb db,ClaimsPrincipal user)=>
        {
            var item=await db.Stays.FirstOrDefaultAsync(x=>x.Id==id&&x.CustomerId==CurrentId(user));
            if(item==null)return Results.NotFound();
            if(item.Status!="Reserved"||item.StartAt<=DateTime.UtcNow)return Results.Conflict(new{error="Bu konaklama çevrimiçi iptal edilemez."});
            if(!Text(input.Reason,500,4))return Invalid("İptal nedeni en az 4 karakter olmalı.");
            if(await db.Payments.AnyAsync(x=>x.ReferenceType=="Stay"&&x.ReferenceId==id))
                return Results.Conflict(new{error="Ödeme hareketi bulunan rezervasyonu iptal etmek için tesis ile iletişime geçin."});
            item.Status="Cancelled";item.CancellationReason=input.Reason.Trim();await db.SaveChangesAsync();await Audit(db,user,"CustomerCancel","Stay",id);
            return Results.Ok(new{success=true});
        });
    }

    private static async Task SignCustomer(HttpContext http,Customer customer)
    {
        var claims=new[]{new Claim(ClaimTypes.NameIdentifier,customer.Id.ToString()),new Claim(ClaimTypes.Name,customer.FullName),
            new Claim(ClaimTypes.Email,customer.Email),new Claim(ClaimTypes.Role,"Customer")};
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
    }
}
