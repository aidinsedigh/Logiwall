using Logiwall.Shared.Dtos.OrderItems;

namespace Logiwall.Infrastructure.Services.OrderItems;

/// <summary>The pickup times a customer can choose for an order.</summary>
// shortcut: fixed slots on Tehran time, no courier booking; replace when the Tipax booking API is integrated.
public static class PickupSlots
{
    // Iran has had no daylight saving time since 2022.
    private static readonly TimeSpan TehranOffset = TimeSpan.FromHours(3.5);
    private static readonly int[] StartHours = [9, 12, 15];
    private const int WindowHours = 3;
    private const int Days = 3;
    private const string TimeIdFormat = "yyyy-MM-dd'T'HH";

    private static readonly PersianCalendar Calendar = new();
    private static readonly string[] MonthNames = ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"];
    // Indexed by DayOfWeek, which starts on Sunday.
    private static readonly string[] DayNames = ["یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه"];

    /// <summary>The next <see cref="Days"/> days from tomorrow, without Fridays.</summary>
    public static IList<PickupDateDto> For(DateTimeOffset now)
    {
        var today = now.ToOffset(TehranOffset).Date;
        return Enumerable.Range(1, Days + 1)
            .Select(day => today.AddDays(day))
            .Where(date => date.DayOfWeek != DayOfWeek.Friday)
            .Take(Days)
            .Select(date => new PickupDateDto(
                date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                $"{DayNames[(int)date.DayOfWeek]} {Calendar.GetDayOfMonth(date)} {MonthNames[Calendar.GetMonth(date) - 1]}",
                StartHours.Select(hour => new PickupTimeDto(
                    date.AddHours(hour).ToString(TimeIdFormat, CultureInfo.InvariantCulture),
                    $"ساعت {hour} تا {hour + WindowHours}")).ToList()))
            .ToList();
    }

    /// <returns>The slot start and its description, or null when <paramref name="timeId"/> is not offered at <paramref name="now"/>.</returns>
    public static (DateTimeOffset Start, string Description)? Find(string? timeId, DateTimeOffset now)
    {
        foreach (var date in For(now))
        {
            var time = date.Times.FirstOrDefault(t => t.Id == timeId);
            if (time is not null)
            {
                var start = DateTime.ParseExact(time.Id, TimeIdFormat, CultureInfo.InvariantCulture);
                return (new DateTimeOffset(start, TehranOffset), $"{date.Description} {time.Description}");
            }
        }
        return null;
    }
}
