using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Models;

namespace SkiResort.Api.Data;

public sealed class ResortDb(DbContextOptions<ResortDb> options) : DbContext(options)
{
    public DbSet<StaffAccount> Accounts => Set<StaffAccount>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Maintenance> Maintenance => Set<Maintenance>();
    public DbSet<Rental> Rentals => Set<Rental>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Stay> Stays => Set<Stay>();
    public DbSet<Slope> Slopes => Set<Slope>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<StaffAccount>().HasIndex(x => x.Email).IsUnique();
        // Existing installations can have legacy duplicate contact emails; registration enforces uniqueness.
        model.Entity<Customer>().HasIndex(x => x.Email);
        model.Entity<Equipment>().HasIndex(x => x.AssetCode).IsUnique();
        model.Entity<Room>().HasIndex(x => x.Code).IsUnique();
        model.Entity<Enrollment>().HasIndex(x => new { x.ActivityId, x.CustomerId }).IsUnique();
        model.Entity<Payment>().HasIndex(x => x.ExternalReference)
            .IsUnique().HasFilter("\"ExternalReference\" <> ''");
        model.Entity<Rental>().HasIndex(x => new { x.EquipmentId, x.StartAt, x.EndAt });
        model.Entity<Stay>().HasIndex(x => new { x.RoomId, x.StartAt, x.EndAt });
        model.Entity<Payment>().HasIndex(x => new { x.ReferenceId, x.ReferenceType });
        model.Entity<Employee>().HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        model.Entity<Enrollment>().HasOne(x => x.Activity).WithMany(x => x.Enrollments).HasForeignKey(x => x.ActivityId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Enrollment>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Rental>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Rental>().HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Stay>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Stay>().HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Payment>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Maintenance>().HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Equipment>().Property(x => x.DailyRate).HasPrecision(12, 2);
        model.Entity<Maintenance>().Property(x => x.Cost).HasPrecision(12, 2);
        model.Entity<Rental>().Property(x => x.Total).HasPrecision(12, 2);
        model.Entity<Room>().Property(x => x.NightlyRate).HasPrecision(12, 2);
        model.Entity<Stay>().Property(x => x.Total).HasPrecision(12, 2);
        model.Entity<Activity>().Property(x => x.Price).HasPrecision(12, 2);
        model.Entity<Payment>().Property(x => x.Amount).HasPrecision(12, 2);
    }
}
