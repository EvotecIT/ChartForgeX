using System.Diagnostics;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

public sealed class GraphiteRenderingBudgetTests {
    private readonly ITestOutputHelper _output;
    public GraphiteRenderingBudgetTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GraphiteStaysWithinExistingChartReleaseBudgets(bool dark) {
        Chart Create(ChartTheme theme) => Chart.Create().WithTheme(theme).WithSize(960,540)
            .WithTitle("Release performance chart").WithSubtitle("Representative line, area, and bar marks")
            .AddLine("Observed",Points(48,19)).AddArea("Forecast",Points(42,13)).AddBar("Capacity",Points(56,7));
        var classic=Create(dark?ChartTheme.ReportDark():ChartTheme.ReportLight());
        var graphite=Create(dark?ChartTheme.GraphiteDark():ChartTheme.GraphiteLight());
        foreach(var chart in new[]{classic,graphite}) {
            chart.ToSvg(); chart.ToPng(); // Warm font caches and renderer code before measuring.
            var allocated=GC.GetAllocatedBytesForCurrentThread(); var watch=Stopwatch.StartNew();
            var svg=chart.ToSvg(); var png=chart.ToPng(); watch.Stop();
            allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
            _output.WriteLine($"{(chart.Options.Theme.UseGraphiteLayout?"Graphite":"Report")} {(dark?"dark":"light")}: {watch.ElapsedMilliseconds} ms, {allocated} allocated bytes, {svg.Length} SVG characters, {png.Length} PNG bytes; budgets 15000 ms / 67108864 bytes.");
            Assert.True(watch.ElapsedMilliseconds<=15000 && allocated<=64L*1024*1024);
        }
    }

    private static ChartPoint[] Points(double baseline,double amplitude) => Enumerable.Range(1,72).Select(i=>new ChartPoint(i,baseline+Math.Sin((i-1)*.31)*amplitude+(i-1)*.08)).ToArray();
}
