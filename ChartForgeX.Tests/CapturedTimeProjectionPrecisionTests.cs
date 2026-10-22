using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CapturedTimeProjectionPrecisionTests {
    private static readonly DateTime First = new DateTime(2026, 9, 1, 0, 0, 10, DateTimeKind.Utc).AddTicks(5_000);

    [Fact]
    public void CapturedTicksAndBoundsKeepCanonicalInstantsWithoutRepeatingAuthoredCaptions() {
        var minimum = ChartDateTime.ToOADate(First);
        var middle = ChartDateTime.ToOADate(First.AddSeconds(5));
        var maximum = ChartDateTime.ToOADate(First.AddSeconds(10));
        var axis = new ChartAxis().WithTimeScale(TimeZoneInfo.CreateCustomTimeZone("TestPlusTwo", TimeSpan.FromHours(2), "TestPlusTwo", "TestPlusTwo"));
        var calls = new List<double>();
        var original = Assert.IsType<ChartDataProjection>(ChartDataProjection.TryCreate(new ChartRect(0, 0, 100, 100), minimum, maximum, axis));
        var projection = original.WithDisplayTicks(new[] { new ChartAxisLabel(minimum, string.Empty), new ChartAxisLabel(middle, "Middle") },
            formatX: value => { calls.Add(value); return "Upper"; });
        Assert.Equal(new[] { maximum }, calls);
        Assert.Equal(new[] { First.ToString("O"), First.AddSeconds(5).ToString("O") }, projection.X.DisplayTicks.Select(tick => tick.Instant));
        Assert.Equal(new[] { First.ToString("O"), First.AddSeconds(10).ToString("O") }, projection.X.DisplayBounds.Select(bound => bound.Instant));
        Assert.Equal(new[] { string.Empty, "Upper" }, projection.X.DisplayBounds.Select(bound => bound.Text));
        Assert.Equal(new[] { minimum, maximum }, projection.X.DisplayBounds.Select(bound => bound.Value));
        Assert.Empty(original.X.DisplayTicks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PublicDateTimePointsAndCategoryProjectionExposeTheSamePreciseInstant(bool horizontal) {
        var chart = CategoryTimeChart(horizontal);
        var prepared = chart.Prepare(new VisualRenderContext());
        var projection = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        var axis = horizontal ? Assert.IsType<ChartDataAxisProjection>(projection.Y) : projection.X;
        Assert.Equal(First.ToString("O"), axis.DisplayTicks[0].Instant);
        Assert.Equal(First.ToString("O"), axis.DisplayBounds[0].Instant);
        Assert.Equal(First.AddSeconds(10).ToString("O"), axis.DisplayBounds[1].Instant);
        var point = Marks(prepared.ToSvg()).First();
        Assert.Equal(First.ToString("O"), point.Attribute("data-cfx-x-instant")!.Value);
        Assert.Equal(new ChartPoint(First, 1).X, double.Parse(point.Attribute("data-cfx-x")!.Value, CultureInfo.InvariantCulture));
        using var metadata = Metadata(chart);
        var published = metadata.RootElement.GetProperty("dataProjection").GetProperty(horizontal ? "y" : "x");
        Assert.Equal(First.ToString("O"), published.GetProperty("displayTicks")[0].GetProperty("instant").GetString());
        Assert.Equal(First.ToString("O"), published.GetProperty("displayBounds")[0].GetProperty("instant").GetString());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void PhysicalTimeValueAxesCapturePreciseScalarFactsAndProjectionBounds(bool horizontal, bool secondary) {
        var first = ChartDateTime.ToOADate(First);
        var last = ChartDateTime.ToOADate(First.AddSeconds(10));
        var chart = Chart.Create().WithSize(900, 360).AddLine("Readings", new[] { new ChartPoint(1, first), new ChartPoint(2, last) });
        chart.Options.LineOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        var valueAxis = horizontal ? chart.Options.XAxis : secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
        var categoryAxis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        valueAxis.WithTimeScale().WithBounds(first, last);
        valueAxis.Labels.Add(new ChartAxisLabel(First, "Start"));
        valueAxis.Labels.Add(new ChartAxisLabel(First.AddSeconds(10), "End"));
        categoryAxis.WithBounds(0, 3);
        if (secondary) chart.Series[0].UseSecondaryValueAxis();
        var prepared = chart.Prepare(new VisualRenderContext());
        var projection = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        var axis = horizontal ? projection.X : Assert.IsType<ChartDataAxisProjection>(projection.Y);
        Assert.Equal(new[] { First.ToString("O"), First.AddSeconds(10).ToString("O") }, axis.DisplayBounds.Select(bound => bound.Instant));
        var point = Marks(prepared.ToSvg()).First();
        Assert.Equal(First.ToString("O"), point.Attribute("data-cfx-value-instant")!.Value);
        Assert.Equal(first, double.Parse(point.Attribute("data-cfx-y")!.Value, CultureInfo.InvariantCulture));
        Assert.Null(point.Attribute("data-cfx-x-instant"));
        using var metadata = Metadata(chart);
        Assert.Equal(First.ToString("O"), metadata.RootElement.GetProperty("dataProjection").GetProperty(horizontal ? "x" : "y")
            .GetProperty("displayBounds")[0].GetProperty("instant").GetString());
    }

    [Fact]
    public void StackedTimeValueFactsCaptureTheVisibleStackEndpoint() {
        var first = ChartDateTime.ToOADate(First);
        var chart = Chart.Create().WithSize(900, 360).WithStackedBars().WithXAxisBounds(0, 2).WithYAxisBounds(first - 1, first + 1)
            .AddBar("Base", new[] { new ChartPoint(1, first) })
            .AddBar("Elapsed", new[] { new ChartPoint(1, 1d / 86400) });
        chart.Options.YAxis.WithTimeScale();
        var point = Marks(chart.ToSvg()).Single(mark => (string?)mark.Attribute("data-cfx-series") == "1");
        Assert.Equal(First.AddSeconds(1).ToString("O"), point.Attribute("data-cfx-value-instant")!.Value);
        Assert.Equal(1d / 86400, double.Parse(point.Attribute("data-cfx-y")!.Value, CultureInfo.InvariantCulture));
        Assert.NotEqual(point.Attribute("data-cfx-y")!.Value, point.Attribute("data-cfx-stack-end")!.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyCrossEpochTimeDomainsKeepTicksBoundsAndFactsNumeric(bool horizontal) {
        var chart = Chart.Create().WithSize(900, 360).AddLine("Legacy", new[] { new ChartPoint(-1, -1), new ChartPoint(1, 1) });
        chart.Options.LineOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        chart.Options.XAxis.WithTimeScale().WithBounds(-2, 2);
        chart.Options.YAxis.WithTimeScale().WithBounds(-2, 2);
        chart.Options.XAxis.Labels.AddRange(new[] { new ChartAxisLabel(-1, "Before"), new ChartAxisLabel(1, "After") });
        chart.Options.YAxis.Labels.AddRange(new[] { new ChartAxisLabel(-1, "Before"), new ChartAxisLabel(1, "After") });
        var prepared = chart.Prepare(new VisualRenderContext());
        var projection = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        foreach (var axis in new[] { projection.X, Assert.IsType<ChartDataAxisProjection>(projection.Y) }) {
            Assert.All(axis.DisplayTicks, tick => Assert.Null(tick.Instant));
            Assert.All(axis.DisplayBounds, bound => Assert.Null(bound.Instant));
        }
        Assert.All(Marks(prepared.ToSvg()), point => {
            Assert.Null(point.Attribute("data-cfx-x-instant"));
            Assert.Null(point.Attribute("data-cfx-value-instant"));
        });
    }

    [Fact]
    public void CategoryProjectionCapturesDetachedBindingsAndDistinguishesEmptyFromContinuous() {
        var original = Assert.IsType<ChartDataProjection>(ChartDataProjection.TryCreate(new ChartRect(0, 0, 100, 100), 0, 3, new ChartAxis()));
        var labels = new List<ChartAxisLabel> { new(1, "First") };
        var projection = original.WithDisplayTicks(Array.Empty<ChartAxisLabel>(), categoryLabels: labels);
        labels.Clear();
        Assert.Equal("First", Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ChartAxisLabel>>(projection.CategoryLabels)).Text);
        Assert.Null(original.CategoryLabels);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<ChartAxisLabel>>(original.WithDisplayTicks(Array.Empty<ChartAxisLabel>(), categoryLabels: labels).CategoryLabels));
    }

    private static Chart CategoryTimeChart(bool horizontal) {
        var chart = Chart.Create().WithSize(900, 360).AddLine("Readings", new[] { new ChartPoint(First, 1), new ChartPoint(First.AddSeconds(10), 2) });
        chart.Options.LineOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        var category = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var value = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        category.WithTimeScale().WithBounds(ChartDateTime.ToOADate(First), ChartDateTime.ToOADate(First.AddSeconds(10)));
        category.Labels.Add(new ChartAxisLabel(First, "Start"));
        category.Labels.Add(new ChartAxisLabel(First.AddSeconds(10), "End"));
        value.WithBounds(0, 3);
        return chart;
    }

    private static IEnumerable<XElement> Marks(string svg) => XDocument.Parse(svg).Descendants().Where(node => node.Attribute("data-cfx-point") != null);

    private static JsonDocument Metadata(Chart chart) {
        var match = Regex.Match(chart.ToInteractiveHtmlFragmentWithoutAssets(), "data-cfx-prepared-chart=\"([^\"]+)\"");
        Assert.True(match.Success);
        return JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
    }
}
