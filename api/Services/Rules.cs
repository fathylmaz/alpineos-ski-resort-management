namespace SkiResort.Api.Services;

public static class Rules
{
    public static bool ValidWindow(DateTime start, DateTime end) =>
        start.Kind == DateTimeKind.Utc && end.Kind == DateTimeKind.Utc && start < end;

    public static bool Overlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd) =>
        aStart < bEnd && bStart < aEnd;

    public static decimal RentalTotal(decimal dailyRate, DateTime start, DateTime end)
    {
        if (!ValidWindow(start, end) || dailyRate < 0) throw new ArgumentException("Invalid rental period or rate");
        return decimal.Round(dailyRate * (decimal)Math.Ceiling((end - start).TotalDays), 2);
    }

    public static decimal StayTotal(decimal nightlyRate, DateTime start, DateTime end)
    {
        if (!ValidWindow(start, end) || nightlyRate < 0 || start.TimeOfDay != TimeSpan.Zero || end.TimeOfDay != TimeSpan.Zero)
            throw new ArgumentException("Stay dates must be UTC midnight and end after start");
        return decimal.Round(nightlyRate * (decimal)(end - start).TotalDays, 2);
    }
}
