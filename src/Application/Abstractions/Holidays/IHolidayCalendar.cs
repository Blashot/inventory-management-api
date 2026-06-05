namespace Application.Abstractions.Holidays;

public interface IHolidayCalendar
{
    bool IsBlackFriday(DateTime date);

    bool IsHolidaySale(DateTime date);
}

