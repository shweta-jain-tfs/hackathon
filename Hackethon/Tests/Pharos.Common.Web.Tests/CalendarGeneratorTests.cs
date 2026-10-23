using Pharos.Common.Web.Components.DatePicker;

namespace Pharos.Common.Web.Tests;

public class CalendarGeneratorTests
{
    private const int _daysInSixWeeks = 6 * 7;

    [Fact]
    public void GetCalendar_ShouldHaveUninterruptedDates()
    {
        var calendar = CalendarGenerator.GetCalendar(2023, 5);

        var firstDay = calendar.First().Date;
        var lastDay = calendar.Last().Date;
        var expectedLastDay = firstDay.AddDays(_daysInSixWeeks - 1);

        lastDay.Should().Be(expectedLastDay);
    }

    [Fact]
    public void GetCalendar_ShouldReturnSixWeeks()
    {
        var calendar = CalendarGenerator.GetCalendar(2023, 10);

        calendar.Should().HaveCount(_daysInSixWeeks);
    }

    [Theory]
    [InlineData(1, DayOfWeek.Monday)]
    [InlineData(2, DayOfWeek.Monday)]
    [InlineData(5, DayOfWeek.Monday)]
    [InlineData(10, DayOfWeek.Sunday)]
    public void GetCalendar_ShouldStartAtSpecifiedWeekday(int month, DayOfWeek startDay)
    {
        var calendar = CalendarGenerator.GetCalendar(2023, month, startDay);
        var firstDayOfWeek = calendar.First().Date.DayOfWeek;

        firstDayOfWeek.Should().Be(startDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void GetCalendar_WithInvalidMonth_ShouldThrowException(int month)
    {
        var act = () => CalendarGenerator.GetCalendar(2023, month);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetCalendar_WithInvalidYear_ShouldThrowException()
    {
        var act = () => CalendarGenerator.GetCalendar(0, 12);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, DayOfWeek.Thursday, 3)]
    [InlineData(DayOfWeek.Monday, DayOfWeek.Sunday, 6)]
    [InlineData(DayOfWeek.Friday, DayOfWeek.Monday, 3)]
    [InlineData(DayOfWeek.Sunday, DayOfWeek.Sunday, 7)]
    public void GetDaysDifference_ShouldBeBetweenOneAndSeven(DayOfWeek startDay, DayOfWeek endDay, int expectedDayDifference)
    {
        var dayDifference = CalendarGenerator.GetDaysDifference(startDay, endDay);

        dayDifference.Should().Be(expectedDayDifference);
    }
}