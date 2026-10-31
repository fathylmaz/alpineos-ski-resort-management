using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Data;
using SkiResort.Api.Models;
using SkiResort.Api.Services;
using static SkiResort.Api.Features.ApiHelpers;

namespace SkiResort.Api.Features;

public static class ActivityEndpoints
{
    public record ActivityInput(string Name, string Kind, string Instructor, DateTime StartsAt, DateTime EndsAt, int Capacity, decimal Price);
    public record EnrollInput(Guid CustomerId);
    public static void Map(WebApplication app)
    {
        var activities=app.MapGroup("/api/activities").RequireAuthorization("Staff");
        activities.MapGet("",async(ResortDb db)=>await db.Activities.AsNoTracking().OrderBy(x=>x.StartsAt)
            .Select(x=>new {x.Id,x.Name,x.Kind,x.Instructor,x.StartsAt,x.EndsAt,x.Capacity,x.Price,x.Status,Enrolled=x.Enrollments.Count(e=>e.Status=="Enrolled")}).ToListAsync());
        activities.MapPost("",async(ActivityInput input,ResortDb db,ClaimsPrincipal actor)=>
        {
            if(!Text(input.Name,130) || !Text(input.Instructor,120) || input.Kind is not ("Lesson" or "Competition" or "Event") ||
                !Rules.ValidWindow(input.StartsAt,input.EndsAt) || input.Capacity is < 1 or > 500 || input.Price < 0 || input.Price > 1_000_000)
                return Invalid("Geçersiz etkinlik bilgisi veya tarih aralığı.");
            var item=new Activity {Name=input.Name.Trim(),Kind=input.Kind,Instructor=input.Instructor.Trim(),StartsAt=input.StartsAt,EndsAt=input.EndsAt,Capacity=input.Capacity,Price=input.Price};
            db.Activities.Add(item);await db.SaveChangesAsync();await Audit(db,actor,"Create","Activity",item.Id);
            return Results.Created($"/api/activities/{item.Id}",item);
        }).RequireAuthorization("Bookings");
        activities.MapGet("/{id:guid}/enrollments",async(Guid id,ResortDb db)=>await db.Enrollments.AsNoTracking().Where(x=>x.ActivityId==id)
            .Select(x=>new{x.Id,x.CustomerId,Customer=x.Customer!.FullName,x.Status,x.CreatedAt}).ToListAsync());
        activities.MapPost("/{id:guid}/enroll",async(Guid id,EnrollInput input,ResortDb db,ClaimsPrincipal actor)=>
        {
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var activity=await db.Activities.FindAsync(id);
            if(activity==null)return Results.NotFound();
            if(activity.Status!="Scheduled" || activity.StartsAt <= DateTime.UtcNow)
                return Results.Conflict(new{error="Etkinlik kayıt süresi kapalı."});
            if(!await db.Customers.AnyAsync(x=>x.Id==input.CustomerId))return Invalid("Müşteri bulunamadı.");
            var existing=await db.Enrollments.SingleOrDefaultAsync(x=>x.ActivityId==id&&x.CustomerId==input.CustomerId);
            if(existing!=null&&existing.Status=="Enrolled")return Results.Conflict(new{error="Müşteri zaten kayıtlı."});
            if(await db.Enrollments.CountAsync(x=>x.ActivityId==id&&x.Status=="Enrolled")>=activity.Capacity)
                return Results.Conflict(new{error="Kontenjan dolu."});
            if(existing==null)db.Enrollments.Add(new Enrollment {ActivityId=id,CustomerId=input.CustomerId});
            else existing.Status="Enrolled";
            await db.SaveChangesAsync();await Audit(db,actor,"Enroll","Activity",id,$"customer={input.CustomerId}");
            await tx.CommitAsync();return Results.Ok(new{success=true});
        }).RequireAuthorization("Bookings");
        activities.MapPost("/{id:guid}/enrollments/{customerId:guid}/cancel",async(Guid id,Guid customerId,ResortDb db,ClaimsPrincipal actor)=>
        {
            var enrollment=await db.Enrollments.SingleOrDefaultAsync(x=>x.ActivityId==id&&x.CustomerId==customerId);
            if(enrollment==null)return Results.NotFound();
            if(enrollment.Status!="Enrolled")return Results.Conflict(new{error="Kayıt zaten iptal edilmiş."});
            enrollment.Status="Cancelled";await db.SaveChangesAsync();await Audit(db,actor,"CancelEnrollment","Activity",id,$"customer={customerId}");
            return Results.Ok(new{success=true});
        }).RequireAuthorization("Bookings");
    }
}
