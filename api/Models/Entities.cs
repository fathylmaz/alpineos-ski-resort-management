using System.ComponentModel.DataAnnotations;

namespace SkiResort.Api.Models;

public abstract class Record
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StaffAccount : Record
{
    [MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(120)] public string DisplayName { get; set; } = "";
    [MaxLength(250)] public string PasswordHash { get; set; } = "";
    [MaxLength(30)] public string Role { get; set; } = "Reception";
    public bool IsActive { get; set; } = true;
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
}

public class Customer : Record
{
    [MaxLength(120)] public string FullName { get; set; } = "";
    [MaxLength(160)] public string Email { get; set; } = "";
    [MaxLength(35)] public string Phone { get; set; } = "";
    [MaxLength(500)] public string Notes { get; set; } = "";
    // Legacy customers are contact records; an empty hash means no portal login.
    [MaxLength(250)] public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
}

public class Department : Record
{
    [MaxLength(100)] public string Name { get; set; } = "";
}

public class Employee : Record
{
    [MaxLength(120)] public string FullName { get; set; } = "";
    [MaxLength(120)] public string Position { get; set; } = "";
    [MaxLength(120)] public string Email { get; set; } = "";
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
}

public class Equipment : Record
{
    [MaxLength(80)] public string AssetCode { get; set; } = "";
    [MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(60)] public string Category { get; set; } = "";
    [MaxLength(30)] public string Size { get; set; } = "";
    [MaxLength(25)] public string Status { get; set; } = "Available";
    public decimal DailyRate { get; set; }
}

public class Maintenance : Record
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    [MaxLength(500)] public string Description { get; set; } = "";
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public decimal Cost { get; set; }
}

public class Rental : Record
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    [MaxLength(25)] public string Status { get; set; } = "Reserved";
    public decimal Total { get; set; }
    [MaxLength(500)] public string CancellationReason { get; set; } = "";
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}

public class Room : Record
{
    [MaxLength(80)] public string Code { get; set; } = "";
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(300)] public string Description { get; set; } = "";
    public int Capacity { get; set; }
    public decimal NightlyRate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Stay : Record
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid RoomId { get; set; }
    public Room? Room { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int GuestCount { get; set; }
    [MaxLength(25)] public string Status { get; set; } = "Reserved";
    public decimal Total { get; set; }
    [MaxLength(500)] public string CancellationReason { get; set; } = "";
}

public class Slope : Record
{
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(25)] public string Difficulty { get; set; } = "Easy";
    [MaxLength(25)] public string Status { get; set; } = "Open";
    public int SnowDepthCm { get; set; }
    [MaxLength(120)] public string Weather { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Activity : Record
{
    [MaxLength(130)] public string Name { get; set; } = "";
    [MaxLength(35)] public string Kind { get; set; } = "Lesson";
    [MaxLength(120)] public string Instructor { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int Capacity { get; set; }
    public decimal Price { get; set; }
    [MaxLength(25)] public string Status { get; set; } = "Scheduled";
    public List<Enrollment> Enrollments { get; set; } = [];
}

public class Enrollment : Record
{
    public Guid ActivityId { get; set; }
    public Activity? Activity { get; set; }
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    [MaxLength(25)] public string Status { get; set; } = "Enrolled";
}

// A manually recorded ledger: this is NOT an online payment gateway.
public class Payment : Record
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid ReferenceId { get; set; }
    [MaxLength(15)] public string ReferenceType { get; set; } = "Rental";
    [MaxLength(15)] public string Kind { get; set; } = "Charge";
    [MaxLength(30)] public string Method { get; set; } = "Cash";
    [MaxLength(70)] public string ExternalReference { get; set; } = "";
    public decimal Amount { get; set; }
}

public class AuditEvent : Record
{
    [MaxLength(100)] public string Actor { get; set; } = "";
    [MaxLength(90)] public string Action { get; set; } = "";
    [MaxLength(90)] public string Entity { get; set; } = "";
    public Guid EntityId { get; set; }
    [MaxLength(1000)] public string Details { get; set; } = "";
}
