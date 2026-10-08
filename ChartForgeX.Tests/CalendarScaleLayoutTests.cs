using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CalendarScaleLayoutTests {
    [Theory]
    [InlineData(800, 440, false)]
    [InlineData(360, 300, false)]
    [InlineData(800, 440, true)]
    [InlineData(360, 300, true)]
    public void MeasuredCalendarKeyStaysSeparateFromSevenRowsAndKeepsCaptionsWithTheirBuckets(int width, int height, bool dark) {
        var first = new DateTime(2026, 1, 5);
        var items = Enumerable.Range(0, 119).Where(day => day % 8 != 7)
            .Select(day => new ChartCalendarHeatmapItem(first.AddDays(day), day % 13)).ToArray();
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font), "The existing Carlito example fixture must be available.");
        var chart = Chart.Create().WithSize(width, height).WithPngFont(font)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Daily activity").WithSubtitle("Observations across four months")
            .AddCalendarHeatmap("Observations", items, firstDayOfWeek: DayOfWeek.Monday);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var document = XDocument.Parse(prepared.ToSvg());
        var days = prepared.Regions.Where(region => region.Role == "calendar-cell").ToArray();
        Assert.Equal(days.Length, days.Select(day => day.Id).Distinct().Count());
        var dateMarks = Role(document, "calendar-cell");
        Assert.Equal(Enumerable.Range(0, 7).Select(row => row.ToString(CultureInfo.InvariantCulture)),
            dateMarks.Select(mark => mark.Attribute("data-cfx-row")!.Value).Distinct().OrderBy(row => row, StringComparer.Ordinal));
        var steps = prepared.Regions.Where(region => region.Role == "calendar-scale-step").ToArray();
        Assert.Equal(7, steps.Length);
        Assert.True(steps.Min(step => step.Bounds.Top) - days.Max(day => day.Bounds.Bottom) >= 12 - .001,
            "A full theme gap separates the calendar data from its measured key.");
        var key = Assert.Single(prepared.Regions, region => region.Id == "calendar-scale");
        Assert.Equal((days.Min(day => day.Bounds.Left) + days.Max(day => day.Bounds.Right)) / 2,
            key.Bounds.Left + key.Bounds.Width / 2, 8);
        var missing = Assert.Single(prepared.Regions, region => region.Id == "calendar-scale-empty");
        var zero = Assert.Single(prepared.Regions, region => region.Id == "calendar-scale-zero-label");
        var range = Assert.Single(prepared.Regions, region => region.Id == "calendar-scale-range");
        Assert.Equal(steps[0].Bounds.Left + steps[0].Bounds.Width / 2, missing.Bounds.Left + missing.Bounds.Width / 2, 8);
        Assert.Equal(steps[1].Bounds.Left + steps[1].Bounds.Width / 2, zero.Bounds.Left + zero.Bounds.Width / 2, 8);
        Assert.Equal(steps[2].Bounds.Left, range.Bounds.Left, 8);
        Assert.InRange(range.Bounds.Right - steps[^1].Bounds.Right, 0, 3.001);
        Assert.All(new[] { missing, zero, range }, label => Assert.True(label.Bounds.Top > steps[0].Bounds.Bottom));
        Assert.Equal(new[] { "No data", "0", "Less – More" }, prepared.Scene.Nodes.OfType<VisualSceneText>()
            .Where(text => text.Id is "calendar-scale-empty" or "calendar-scale-zero-label" or "calendar-scale-range")
            .Select(text => Assert.Single(text.Text.Lines).Text));
        var scaleMarks = Role(document, "calendar-scale-step");
        Assert.Equal("true", scaleMarks[0].Attribute("data-cfx-empty")?.Value);
        Assert.Equal("true", scaleMarks[1].Attribute("data-cfx-zero")?.Value);
        Assert.Equal("1", scaleMarks[2].Attribute("data-cfx-value")?.Value);
        Assert.Equal("12", scaleMarks[^1].Attribute("data-cfx-value")?.Value);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "calendar.scale-overflow");

        chart.WithHeatmapScaleLegend(false);
        var hidden = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(hidden.Regions, region => region.Role is "calendar-scale" or "calendar-scale-step");
        Assert.Equal(days.Select(day => day.Id), hidden.Regions.Where(region => region.Role == "calendar-cell").Select(day => day.Id));
    }

    private static XElement[] Role(XDocument document, string role) => document.Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
