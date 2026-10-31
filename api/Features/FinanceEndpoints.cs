using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Data;
using SkiResort.Api.Models;
using static SkiResort.Api.Features.ApiHelpers;

namespace SkiResort.Api.Features;

public static class FinanceEndpoints
{
    public record PaymentInput(string ReferenceType, Guid ReferenceId, string Kind, string Method, decimal Amount, string? ExternalReference);

    public static void Map(WebApplication app)
    {
        var payments=app.MapGroup("/api/payments").RequireAuthorization("Staff");
        payments.MapGet("",async(ResortDb db)=>await db.Payments.AsNoTracking().OrderByDescending(x=>x.CreatedAt)
            .Select(x=>new{x.Id,x.CustomerId,Customer=x.Customer!.FullName,x.ReferenceId,x.ReferenceType,x.Kind,x.Method,x.Amount,x.ExternalReference,x.CreatedAt}).ToListAsync());
        payments.MapPost("",async(PaymentInput input,ResortDb db,ClaimsPrincipal actor)=>
        {
            if(input.ReferenceType is not ("Rental" or "Stay") || input.Kind is not ("Charge" or "Refund") ||
                input.Method is not ("Cash" or "Card" or "BankTransfer") || input.Amount <= 0 || input.Amount > 1_000_000 ||
                (input.ExternalReference?.Length??0)>70)
                return Invalid("Geçersiz işlem türü, tutar veya ödeme yöntemi.");
            // Serializable transactions prevent two concurrent payments exceeding the same remaining balance.
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            Guid customerId;
            decimal total;
            if(input.ReferenceType=="Rental")
            {
                var r=await db.Rentals.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==input.ReferenceId);
                if(r==null)return Results.NotFound();
                if(r.Status=="Cancelled" && input.Kind=="Charge")return Results.Conflict(new{error="İptal edilen rezervasyona tahsilat eklenemez."});
                customerId=r.CustomerId;total=r.Total;
            }
            else
            {
                var s=await db.Stays.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==input.ReferenceId);
                if(s==null)return Results.NotFound();
                if(s.Status=="Cancelled" && input.Kind=="Charge")return Results.Conflict(new{error="İptal edilen rezervasyona tahsilat eklenemez."});
                customerId=s.CustomerId;total=s.Total;
            }
            var history=await db.Payments.AsNoTracking().Where(x=>x.ReferenceId==input.ReferenceId&&x.ReferenceType==input.ReferenceType).ToListAsync();
            var paid=history.Sum(x=>x.Kind=="Charge"?x.Amount:-x.Amount);
            if(input.Kind=="Charge" && input.Amount>total-paid)
                return Results.Conflict(new{error="Ödeme, kalan bakiyeyi aşıyor.",remaining=total-paid});
            if(input.Kind=="Refund" && input.Amount>paid)
                return Results.Conflict(new{error="İade, önceki net tahsilatı aşıyor.",paid});
            var external=(input.ExternalReference??"").Trim();
            if(external.Length>0 && history.Any(x=>x.ExternalReference==external)) return Results.Conflict(new{error="Bu işlem referansı zaten kullanıldı."});
            var payment=new Payment {CustomerId=customerId,ReferenceType=input.ReferenceType,ReferenceId=input.ReferenceId,Kind=input.Kind,Method=input.Method,Amount=input.Amount,ExternalReference=external};
            db.Payments.Add(payment);await db.SaveChangesAsync();await Audit(db,actor,input.Kind,"Payment",payment.Id,$"{input.ReferenceType}:{input.ReferenceId}");await tx.CommitAsync();
            return Results.Created($"/api/payments/{payment.Id}",new{payment.Id,payment.Amount,payment.Kind,remaining=total-paid-(input.Kind=="Charge"?input.Amount:-input.Amount)});
        }).RequireAuthorization("Finance");
    }
}
