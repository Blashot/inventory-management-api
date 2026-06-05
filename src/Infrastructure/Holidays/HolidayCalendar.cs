using Application.Abstractions.Holidays;

namespace Infrastructure.Holidays;

internal sealed class HolidayCalendar : IHolidayCalendar
{
    public bool IsBlackFriday(DateTime date)
    {
        if (date.Month != 11)
        {
            return false;
        }

        // Black Friday = day after US Thanksgiving (4th Thursday of November)
        DateTime thanksgiving = GetNthWeekdayOfMonth(date.Year, 11, DayOfWeek.Thursday, 4);
        DateTime blackFriday = thanksgiving.AddDays(1);

        return date.Date == blackFriday.Date;
    }

    public bool IsHolidaySale(DateTime date)
    {
        // Holiday Sale: December 24 through January 1
        return date.Month == 12 && date.Day >= 24
            || date.Month == 1 && date.Day == 1;
    }

    private static DateTime GetNthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int n)
    {
        var firstDay = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        int daysUntilTarget = ((int)dayOfWeek - (int)firstDay.DayOfWeek + 7) % 7;
        return firstDay.AddDays(daysUntilTarget + (n - 1) * 7);
    }
}
