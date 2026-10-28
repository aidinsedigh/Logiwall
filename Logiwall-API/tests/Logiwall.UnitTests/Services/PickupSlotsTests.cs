using Logiwall.Infrastructure.Services.OrderItems;

namespace Logiwall.UnitTests.Services;

public class PickupSlotsTests
{
    // Wednesday 2026-10-28, 23:00 in Tehran.
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 19, 30, 0, TimeSpan.Zero);

    [Fact]
    public void For_offers_three_days_from_tomorrow_without_friday()
    {
        var dates = PickupSlots.For(Now).Select(d => d.Id).ToList();

        Assert.Equal(new[] { "2026-10-29", "2026-10-31", "2026-11-01" }, dates);
    }

    [Fact]
    public void For_uses_the_tehran_date_not_the_utc_date()
    {
        // 21:00 UTC is 00:30 the next day in Tehran.
        var dates = PickupSlots.For(new DateTimeOffset(2026, 10, 28, 21, 0, 0, TimeSpan.Zero));

        Assert.Equal("2026-10-31", dates[0].Id);
    }

    [Fact]
    public void For_describes_the_day_in_the_persian_calendar()
    {
        var first = PickupSlots.For(Now)[0];

        Assert.Equal("پنجشنبه 7 آبان", first.Description);
        Assert.Equal(new[] { "2026-10-29T09", "2026-10-29T12", "2026-10-29T15" }, first.Times.Select(t => t.Id));
    }

    [Fact]
    public void Find_returns_the_slot_start_on_tehran_time()
    {
        var slot = PickupSlots.Find("2026-10-29T12", Now);

        Assert.NotNull(slot);
        Assert.Equal(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.FromHours(3.5)), slot.Value.Start);
        Assert.Equal("پنجشنبه 7 آبان ساعت 12 تا 15", slot.Value.Description);
    }

    [Theory]
    [InlineData("2026-10-28T09")] // today
    [InlineData("2026-10-30T09")] // Friday
    [InlineData("2026-10-29T10")] // not a window start
    [InlineData("garbage")]
    [InlineData(null)]
    public void Find_rejects_a_time_that_is_not_offered(string? timeId)
    {
        Assert.Null(PickupSlots.Find(timeId, Now));
    }
}
