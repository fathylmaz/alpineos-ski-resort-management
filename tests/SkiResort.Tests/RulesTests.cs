using SkiResort.Api.Services;
using Xunit;

namespace SkiResort.Tests;

public class RulesTests
{
    private static DateTime Utc(int year, int month, int day, int hour = 0) => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AdjacentReservationsDoNotOverlap() =>
        Assert.False(Rules.Overlap(Utc(2027,1,1), Utc(2027,1,3), Utc(2027,1,3), Utc(2027,1,5)));

    [Fact]
    public void IntersectingReservationsOverlap() =>
        Assert.True(Rules.Overlap(Utc(2027,1,1), Utc(2027,1,3), Utc(2027,1,2), Utc(2027,1,5)));

    [Fact]
    public void RentalPartialDayRoundsUp() =>
        Assert.Equal(1300m, Rules.RentalTotal(650m, Utc(2027,1,1,9), Utc(2027,1,2,10)));

    [Fact]
    public void RentalOneDayBilledOnce() =>
        Assert.Equal(650m, Rules.RentalTotal(650m, Utc(2027,1,1,9), Utc(2027,1,2,9)));

    [Fact]
    public void NightsBasedPricing() =>
        Assert.Equal(5800m, Rules.StayTotal(2900m, Utc(2027,1,1), Utc(2027,1,3)));

    [Fact]
    public void InvalidDatesRejected() =>
        Assert.Throws<ArgumentException>(() => Rules.StayTotal(300m, Utc(2027,1,2), Utc(2027,1,1)));

    [Fact]
    public void StayRequiresMidnightUtc() =>
        Assert.Throws<ArgumentException>(() => Rules.StayTotal(300m, Utc(2027,1,1,12), Utc(2027,1,2,12)));

    [Fact]
    public void UnspecifiedDatetimeNotAccepted() =>
        Assert.False(Rules.ValidWindow(new DateTime(2027,1,1), new DateTime(2027,1,2)));
}
