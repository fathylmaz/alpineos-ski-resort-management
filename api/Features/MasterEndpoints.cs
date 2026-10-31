using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Data;
using SkiResort.Api.Models;
using static SkiResort.Api.Features.ApiHelpers;

namespace SkiResort.Api.Features;

public static class MasterEndpoints
{
    public record CustomerInput(string FullName, string Email, string Phone, string? Notes);
    public record EquipmentInput(string AssetCode, string Name, string Category, string Size, decimal DailyRate);
    public record EquipmentStatusInput(string Status);
    public record MaintenanceInput(string Description, decimal Cost);
    public record RoomInput(string Code, string Name, string Description, int Capacity, decimal NightlyRate);
    public record DepartmentInput(string Name);
    public record EmployeeInput(string FullName, string Position, string Email, Guid? DepartmentId);
    public record SlopeInput(string Name, string Difficulty, string Status, int SnowDepthCm, string Weather);
    public record SlopeStatusInput(string Status, int SnowDepthCm, string Weather);

    public static void Map(WebApplication app)
    {
        var customers = app.MapGroup("/api/customers").RequireAuthorization("Staff");
        customers.MapGet("", async (ResortDb db) => await db.Customers.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.FullName, x.Email, x.Phone, x.Notes, x.CreatedAt, HasPortalAccount = x.PasswordHash != "" }).ToListAsync());
        customers.MapPost("", async (CustomerInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            if (!Text(input.FullName) || !Text(input.Email, 160, 5) || !input.Email.Contains('@') ||
                (input.Phone?.Length ?? 0) > 35 || (input.Notes?.Length ?? 0) > 500) return Invalid("Ad, geçerli e-posta ve uygun telefon bilgisi girin.");
            if (await db.Customers.AnyAsync(c => c.Email == input.Email.Trim().ToLowerInvariant()) || await db.Accounts.AnyAsync(a => a.Email == input.Email.Trim().ToLowerInvariant()))
                return Results.Conflict(new { error = "E-posta daha önce kullanılmış." });
            var item = new Customer { FullName=input.FullName.Trim(), Email=input.Email.Trim().ToLowerInvariant(), Phone=(input.Phone ?? "").Trim(), Notes=(input.Notes ?? "").Trim() };
            db.Customers.Add(item); await db.SaveChangesAsync();
            await Audit(db, actor, "Create", "Customer", item.Id);
            return Results.Created($"/api/customers/{item.Id}", new { item.Id, item.FullName, item.Email, item.Phone, item.Notes });
        }).RequireAuthorization("Bookings");
        customers.MapPut("/{id:guid}", async (Guid id, CustomerInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            var item = await db.Customers.FindAsync(id);
            if (item == null) return Results.NotFound();
            if (!Text(input.FullName) || !Text(input.Email,160,5) || !input.Email.Contains('@') ||
                (input.Phone?.Length ?? 0) > 35 || (input.Notes?.Length ?? 0) > 500) return Invalid("Geçersiz müşteri bilgisi.");
            if (await db.Customers.AnyAsync(c => c.Id != id && c.Email == input.Email.Trim().ToLowerInvariant()) || await db.Accounts.AnyAsync(a => a.Email == input.Email.Trim().ToLowerInvariant()))
                return Results.Conflict(new { error = "E-posta daha önce kullanılmış." });
            item.FullName = input.FullName.Trim(); item.Email = input.Email.Trim().ToLowerInvariant(); item.Phone = input.Phone ?? ""; item.Notes = input.Notes ?? "";
            await db.SaveChangesAsync(); await Audit(db, actor, "Update", "Customer", id);
            return Results.Ok(new { item.Id, item.FullName, item.Email, item.Phone, item.Notes });
        }).RequireAuthorization("Bookings");

        var equip = app.MapGroup("/api/equipment").RequireAuthorization("Staff");
        equip.MapGet("", async (ResortDb db) => await db.Equipment.AsNoTracking().OrderBy(x => x.AssetCode).ToListAsync());
        equip.MapPost("", async (EquipmentInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            if (!Text(input.AssetCode, 80, 2) || !Text(input.Name) || !Text(input.Category,60) || input.DailyRate < 0 || input.DailyRate > 1_000_000 || (input.Size?.Length ?? 0)>30)
                return Invalid("Geçersiz ekipman bilgisi.");
            var item = new Equipment { AssetCode=input.AssetCode.Trim().ToUpperInvariant(), Name=input.Name.Trim(), Category=input.Category.Trim(), Size=input.Size ?? "", DailyRate=input.DailyRate };
            db.Equipment.Add(item); await db.SaveChangesAsync(); await Audit(db, actor, "Create", "Equipment", item.Id);
            return Results.Created($"/api/equipment/{item.Id}", item);
        }).RequireAuthorization("Inventory");
        equip.MapPatch("/{id:guid}/status", async (Guid id, EquipmentStatusInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            if (input.Status is not ("Available" or "Maintenance" or "Retired")) return Invalid("Geçersiz ekipman durumu.");
            var item = await db.Equipment.FindAsync(id);
            if (item == null) return Results.NotFound();
            if (input.Status != "Available" && await db.Rentals.AnyAsync(x => x.EquipmentId == id && x.Status == "CheckedOut"))
                return Results.Conflict(new { error = "Teslim edilmiş ekipman bakım veya emeklilik durumuna alınamaz." });
            item.Status = input.Status; await db.SaveChangesAsync(); await Audit(db, actor, "StatusChange", "Equipment", id, input.Status);
            return Results.Ok(item);
        }).RequireAuthorization("Inventory");
        equip.MapGet("/{id:guid}/maintenance", async (Guid id, ResortDb db) =>
            await db.Maintenance.AsNoTracking().Where(x=>x.EquipmentId==id).OrderByDescending(x=>x.PerformedAt).ToListAsync());
        equip.MapPost("/{id:guid}/maintenance", async (Guid id, MaintenanceInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            if (!Text(input.Description,500,4) || input.Cost < 0 || input.Cost > 1_000_000) return Invalid("Geçerli bakım açıklaması ve maliyet girin.");
            var item = await db.Equipment.FindAsync(id);
            if (item == null) return Results.NotFound();
            if (await db.Rentals.AnyAsync(x=>x.EquipmentId==id && x.Status=="CheckedOut")) return Results.Conflict(new { error="Kiradaki ekipmana bakım kaydedilemez." });
            var rec = new Maintenance { EquipmentId=id, Description=input.Description.Trim(), Cost=input.Cost };
            db.Maintenance.Add(rec); item.Status="Maintenance"; await db.SaveChangesAsync();
            await Audit(db, actor, "Maintenance", "Equipment", id, input.Description);
            return Results.Created($"/api/equipment/{id}/maintenance", rec);
        }).RequireAuthorization("Inventory");

        var rooms = app.MapGroup("/api/rooms").RequireAuthorization("Staff");
        rooms.MapGet("", async (ResortDb db) => await db.Rooms.AsNoTracking().OrderBy(x=>x.Code).ToListAsync());
        rooms.MapPost("", async (RoomInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            if (!Text(input.Code,80) || !Text(input.Name,100) || input.Capacity is < 1 or > 30 || input.NightlyRate < 0 || input.NightlyRate > 1_000_000 || (input.Description?.Length??0)>300)
                return Invalid("Geçersiz oda bilgisi.");
            var item = new Room { Code=input.Code.Trim().ToUpperInvariant(), Name=input.Name.Trim(), Description=input.Description ?? "", Capacity=input.Capacity, NightlyRate=input.NightlyRate };
            db.Rooms.Add(item); await db.SaveChangesAsync(); await Audit(db, actor, "Create", "Room", item.Id);
            return Results.Created($"/api/rooms/{item.Id}", item);
        }).RequireAuthorization("Bookings");

        var depts = app.MapGroup("/api/departments").RequireAuthorization("Staff");
        depts.MapGet("", async (ResortDb db) => await db.Departments.AsNoTracking().OrderBy(x=>x.Name).ToListAsync());
        depts.MapPost("", async (DepartmentInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            if (!Text(input.Name,100)) return Invalid("Departman adı gerekli.");
            var item=new Department {Name=input.Name.Trim()}; db.Departments.Add(item); await db.SaveChangesAsync(); await Audit(db,actor,"Create","Department",item.Id);
            return Results.Created($"/api/departments/{item.Id}",item);
        }).RequireAuthorization("Admin");
        var employees = app.MapGroup("/api/employees").RequireAuthorization("Staff");
        employees.MapGet("", async (ResortDb db) => await db.Employees.AsNoTracking().Include(x=>x.Department).OrderBy(x=>x.FullName)
            .Select(x=>new {x.Id,x.FullName,x.Position,x.Email,x.DepartmentId, Department=x.Department!=null?x.Department.Name:""}).ToListAsync());
        employees.MapPost("", async (EmployeeInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            if (!Text(input.FullName) || !Text(input.Position) || !Text(input.Email,120,5) || !input.Email.Contains('@')) return Invalid("Geçersiz personel bilgisi.");
            if (input.DepartmentId.HasValue && !await db.Departments.AnyAsync(x=>x.Id==input.DepartmentId)) return Invalid("Departman bulunamadı.");
            var item=new Employee {FullName=input.FullName.Trim(),Position=input.Position.Trim(),Email=input.Email.Trim(),DepartmentId=input.DepartmentId};
            db.Employees.Add(item); await db.SaveChangesAsync(); await Audit(db,actor,"Create","Employee",item.Id);
            return Results.Created($"/api/employees/{item.Id}",item);
        }).RequireAuthorization("Admin");

        var slopes = app.MapGroup("/api/slopes").RequireAuthorization("Staff");
        slopes.MapGet("", async (ResortDb db) => await db.Slopes.AsNoTracking().OrderBy(x=>x.Name).ToListAsync());
        slopes.MapPost("", async (SlopeInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            if (!Text(input.Name,100) || input.Difficulty is not ("Easy" or "Medium" or "Hard") ||
                input.Status is not ("Open" or "Closed" or "Maintenance") || input.SnowDepthCm is < 0 or > 1000 || (input.Weather?.Length ?? 0)>120)
                return Invalid("Geçersiz pist bilgisi.");
            var item = new Slope {Name=input.Name.Trim(),Difficulty=input.Difficulty,Status=input.Status,SnowDepthCm=input.SnowDepthCm,Weather=input.Weather??""};
            db.Slopes.Add(item);await db.SaveChangesAsync();await Audit(db,actor,"Create","Slope",item.Id);
            return Results.Created($"/api/slopes/{item.Id}",item);
        }).RequireAuthorization("Operations");
        slopes.MapPatch("/{id:guid}/status", async (Guid id, SlopeStatusInput input, ResortDb db, ClaimsPrincipal actor) =>
        {
            var item = await db.Slopes.FindAsync(id);
            if (item == null) return Results.NotFound();
            if (input.Status is not ("Open" or "Closed" or "Maintenance") || input.SnowDepthCm is < 0 or > 1000 || (input.Weather?.Length??0)>120)
                return Invalid("Geçersiz pist güncellemesi.");
            item.Status=input.Status;item.SnowDepthCm=input.SnowDepthCm;item.Weather=input.Weather??"";item.UpdatedAt=DateTime.UtcNow;
            await db.SaveChangesAsync(); await Audit(db,actor,"StatusChange","Slope",id,input.Status);
            return Results.Ok(item);
        }).RequireAuthorization("Operations");
    }
}
