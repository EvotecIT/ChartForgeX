using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TimeAxisCalendarPhaseTests {
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void MultiDayCalendarTicksKeepOneDisplayDayPhaseAcrossOverlappingWindows(int step) {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Phase/Plus5", TimeSpan.FromHours(5), "Phase +05", "Phase +05");
        var axis = new ChartAxis().WithTimeScale(zone);
        axis.TickCount = 5;
        var start = new DateTime(2026, 1, 29, 0, 0, 0).AddHours(-5).ToOADate();
        var shiftedStart = new DateTime(2026, 2, 1, 0, 0, 0).AddHours(-5).ToOADate();
        foreach (var fitted in new[] { false, true }) {
            IReadOnlyList<double> Generate(double minimum) => fitted
                ? ChartTimeScale.GenerateFitting(axis, minimum, minimum + step * 4, _ => true, allowCompactDates: false)!
                : ChartTimeScale.Generate(axis, minimum, minimum + step * 4, inside: true)!;
            var first = Generate(start);
            var shifted = Generate(shiftedStart);
            Assert.True(first.Count >= 3 && shifted.Count >= 3);
            var overlapEnd = start + step * 4;
            Assert.Equal(first.Where(value => value >= shiftedStart), shifted.Where(value => value <= overlapEnd));
            foreach (var ticks in new[] { first, shifted }) {
                Assert.All(ticks, value => Assert.Equal(TimeSpan.Zero, ChartTimeScale.ToDisplayTime(axis, value)!.Value.TimeOfDay));
                for (var index = 1; index < ticks.Count; index++) Assert.Equal(step, ticks[index] - ticks[index - 1], 7);
            }
        }
    }
}
