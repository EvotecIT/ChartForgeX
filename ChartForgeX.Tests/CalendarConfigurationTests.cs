using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Calendar presentation callbacks publish a detached snapshot or leave the installed state intact.</summary>
public sealed class CalendarConfigurationTests {
    private static readonly string[] Days = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };
    private static readonly string[] Months = { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };

    [Fact]
    public void CallbackFailureOrInvalidNamesPreserveEveryInstalledSetting() {
        var chart = Chart.Create().ConfigureCalendar(options => {
            options.FirstDayOfWeek = DayOfWeek.Monday; options.DayNames = Days; options.MonthNames = Months;
            options.CellSize = 12; options.MaximumCellSize = 30; options.Gap = 2;
        });
        var installed = chart.Options.Calendar;

        Assert.Throws<InvalidOperationException>(() => chart.ConfigureCalendar(options => {
            options.FirstDayOfWeek = DayOfWeek.Saturday; options.DayNames = null; options.Gap = 0;
            throw new InvalidOperationException("The host rejected these settings.");
        }));
        Assert.Same(installed, chart.Options.Calendar);
        Assert.Throws<ArgumentException>(() => chart.ConfigureCalendar(options => {
            options.CellSize = 16; options.MonthNames = new[] { "January" };
        }));
        Assert.Same(installed, chart.Options.Calendar);
        Assert.Throws<ArgumentNullException>(() => chart.ConfigureCalendar(null!));
        Assert.Same(installed, chart.Options.Calendar);
        Assert.Equal(DayOfWeek.Monday, installed.FirstDayOfWeek);
        Assert.Equal(Days, installed.DayNames);
        Assert.Equal(Months, installed.MonthNames);
        Assert.Equal(12, installed.CellSize);
        Assert.Equal(30, installed.MaximumCellSize);
        Assert.Equal(2, installed.Gap);
    }

    [Fact]
    public void NamesAndOptionHandlesRemainDetachedAndExplicitDefaultsSurviveCloning() {
        var dayNames = Days.ToList();
        var monthNames = (string[])Months.Clone();
        var chart = Chart.Create().ConfigureCalendar(options => {
            options.FirstDayOfWeek = DayOfWeek.Monday; options.DayNames = dayNames; options.MonthNames = monthNames;
            options.CellSize = 12; options.MaximumCellSize = 30; options.Gap = 2;
        });
        var previous = chart.Options.Calendar;
        ChartCalendarOptions? retained = null;
        chart.ConfigureCalendar(options => {
            retained = options; options.FirstDayOfWeek = DayOfWeek.Sunday; options.Gap = 0; options.CellSize = 16;
        });

        dayNames[1] = "Changed input"; monthNames[0] = "Changed input";
        var returnedDays = Assert.IsType<string[]>(chart.Options.Calendar.DayNames);
        var returnedMonths = Assert.IsType<string[]>(chart.Options.Calendar.MonthNames);
        returnedDays[1] = "Changed output"; returnedMonths[0] = "Changed output";
        retained!.DayNames = null; retained.MonthNames = null; retained.Gap = 6;
        previous.FirstDayOfWeek = DayOfWeek.Thursday; previous.DayNames = null;

        Assert.Equal(Days, chart.Options.Calendar.DayNames);
        Assert.Equal(Months, chart.Options.Calendar.MonthNames);
        Assert.Equal(DayOfWeek.Sunday, chart.Options.Calendar.FirstDayOfWeek);
        Assert.Equal(0, chart.Options.Calendar.Gap);
        Assert.Equal(16, chart.Options.Calendar.CellSize);
        Assert.Equal(30, chart.Options.Calendar.MaximumCellSize);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WhitespaceNamesAreRejectedWithoutChangingPresentation(bool weekdays) {
        var chart = Chart.Create().ConfigureCalendar(options => { options.DayNames = Days; options.MonthNames = Months; });
        var names = (string[])(weekdays ? Days : Months).Clone();
        names[1] = " ";
        Assert.Throws<ArgumentException>(() => chart.ConfigureCalendar(options => {
            if (weekdays) options.DayNames = names;
            else options.MonthNames = names;
        }));
        Assert.Equal(Days, chart.Options.Calendar.DayNames);
        Assert.Equal(Months, chart.Options.Calendar.MonthNames);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteCellSettingsAreRejected(double value) {
        var options = new ChartCalendarOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.CellSize = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaximumCellSize = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Gap = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.FirstDayOfWeek = (DayOfWeek)7);
    }
}
