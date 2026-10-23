using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartAxisWindowBoundaryTests {
    [Fact]
    public void ExplicitBoundaryPolicyIsImmutableSerializableAndDefaultsToInclusive() {
        var inclusive = new ChartAxisWindow(2, 8);
        var exclusive = new ChartAxisWindow(2, 8, maximumInclusive: false);
        Assert.True(inclusive.MaximumInclusive);
        Assert.False(exclusive.MaximumInclusive);
        var restored = JsonSerializer.Deserialize<ChartAxisWindow>(JsonSerializer.Serialize(exclusive))!;
        Assert.Equal(2, restored.Minimum); Assert.Equal(8, restored.Maximum); Assert.False(restored.MaximumInclusive);
        Assert.True(NativeWindowTransport.Read(JsonSerializer.SerializeToElement(new { minimum = 2, maximum = 8 })).MaximumInclusive);
        Assert.False(NativeWindowTransport.Read(JsonSerializer.SerializeToElement(new { minimum = 2, maximum = 8, maximumInclusive = false })).MaximumInclusive);
    }

    [Theory]
    [InlineData("x", false)]
    [InlineData("x", true)]
    [InlineData("y", false)]
    [InlineData("y", true)]
    [InlineData("secondaryX", false)]
    [InlineData("secondaryX", true)]
    [InlineData("secondaryY", false)]
    [InlineData("secondaryY", true)]
    public void EveryPhysicalAxisRetainsItsExclusiveEndpointAndDetachedMetadata(string slot, bool reversed) {
        var horizontal = slot is "y" or "secondaryX";
        var chart = Chart.Create().WithLegend(false);
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 100) };
        if (horizontal) chart.AddHorizontalBar("Primary", points); else chart.AddLine("Primary", points);
        chart.AddLine("Secondary", points).Series[1].UseSecondaryValueAxis();
        if (horizontal) chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var axis = slot == "x" ? chart.Options.XAxis : slot == "y" ? chart.Options.YAxis
            : slot == "secondaryX" ? chart.Options.SecondaryXAxis : chart.Options.SecondaryYAxis;
        axis.Reversed = reversed;
        var category = slot is "x" or "y";
        var window = new ChartAxisWindow(category ? 1 : 10, category ? 2 : 20, maximumInclusive: false);
        var viewport = new ChartCartesianViewport(slot == "x" ? window : null, slot == "y" ? window : null,
            slot == "secondaryX" ? window : null, slot == "secondaryY" ? window : null);
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart, viewport);
        var prepared = section.PreparedVisual;
        var projection = slot == "x" ? prepared.XProjection! : slot == "y" ? prepared.YProjection!
            : slot == "secondaryX" ? prepared.SecondaryXProjection! : prepared.SecondaryYProjection!;
        Assert.False(projection.MaximumInclusive);
        var confined = Assert.IsType<ChartAxisWindow>(projection.ConstrainViewport(window));
        Assert.False(confined.MaximumInclusive); Assert.Equal(window.Minimum, confined.Minimum); Assert.Equal(window.Maximum, confined.Maximum);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var endpoint = XDocument.Parse(svg).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point"
            && (string?)node.Attribute("data-cfx-series") == "1" && (string?)node.Attribute("data-cfx-point") == "1");
        Assert.Equal("false", (string?)endpoint.Attribute("data-cfx-visible"));
        using var metadata = Metadata(section.Html);
        Assert.False(metadata.RootElement.GetProperty(slot + "Projection").GetProperty("maximumInclusive").GetBoolean());
        axis.Reversed = !reversed; chart.Series[1].Points.Clear();
        Assert.False(projection.MaximumInclusive); Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericAndLogarithmicFullBoundsWithAnExcludedLastPointAreNotAFullReset(bool logarithmic) {
        var chart = Chart.Create().WithLegend(false).AddScatter("Points", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2), new ChartPoint(4, 40) })
            .WithXAxisBounds(1, 4);
        if (logarithmic) chart.Options.XAxis.WithLogarithmicScale(2);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)));
        var full = chart.Prepare(context);
        Assert.Null(full.XProjection!.ConstrainViewport(new ChartAxisWindow(1, 4)));
        var request = Assert.IsType<ChartAxisWindow>(full.XProjection.ConstrainViewport(new ChartAxisWindow(1, 4, maximumInclusive: false)));
        var narrowed = chart.Prepare(context, new ChartCartesianViewport(x: request));
        Assert.False(narrowed.XProjection!.MaximumInclusive);
        Assert.False(Assert.IsType<ChartAxisWindow>(narrowed.XProjection.ConstrainViewport(narrowed.XProjection.PlotBounds.Left, narrowed.XProjection.PlotBounds.Right)).MaximumInclusive);
        Assert.Equal(full.XProjection.Project(2), narrowed.XProjection.Project(2), 8);
        Assert.Equal(full.ToSvg(), chart.Prepare(context, null).ToSvg());
        var reset = chart.Prepare(context, null);
        Assert.True(reset.XProjection!.MaximumInclusive);
        var htmlReset = new HtmlInteractiveChartRenderer().PrepareSection(chart, null);
        Assert.True(htmlReset.PreparedVisual.XProjection!.MaximumInclusive);
        using var metadata = Metadata(htmlReset.Html);
        var projectionMetadata = metadata.RootElement.GetProperty("xProjection");
        if (projectionMetadata.TryGetProperty("maximumInclusive", out var flag)) Assert.True(flag.GetBoolean());
    }

    [Fact]
    public void OrdinalExclusiveMaximumRetainsRawDatesAndExcludesOnlyItsEndpointObservation() {
        var january = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var values = new[] { january, january.AddDays(1), january.AddDays(11) }.Select(ChartDateTime.ToOADate).ToArray();
        var chart = Chart.Create().WithLegend(false).AddScatter("Ordinal", values.Select((value, index) => new ChartPoint(value, index + 1)));
        chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)));
        var request = new ChartAxisWindow(values[0], values[2], maximumInclusive: false);
        var prepared = chart.Prepare(context, new ChartCartesianViewport(x: request));
        Assert.False(prepared.XProjection!.MaximumInclusive);
        Assert.Equal(values[2], prepared.XProjection.Maximum);
        var source = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(3, source.Length);
        Assert.Equal("false", (string?)source[2].Attribute("data-cfx-visible"));
        Assert.NotEqual("false", (string?)source[1].Attribute("data-cfx-visible"));
        Assert.Equal(values, chart.Series[0].Points.Select(point => point.X));
    }

    [Fact]
    public void ExclusiveCategoryEndpointCannotInflateScatterValueFitOrPaintAnEndpointAnnotation() {
        var chart = Chart.Create().WithLegend(false).AddScatter("Observed", new[] { new ChartPoint(1, 10), new ChartPoint(2, 12), new ChartPoint(3, 1000) });
        chart.AddVerticalLine(3, "Excluded edge").AddNote(new ChartPoint(3, 11), "Excluded note")
            .AddLineSegment(new ChartPoint(2, 11), new ChartPoint(3, 11), "Crossing line");
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)));
        var included = chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(1, 3), valueDomain: ChartValueDomain.VisibleCategories));
        var excluded = chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(1, 3, maximumInclusive: false), valueDomain: ChartValueDomain.VisibleCategories));
        Assert.True(included.YProjection!.Maximum > 1000);
        Assert.True(excluded.YProjection!.Maximum < 100);
        Assert.Contains("Excluded edge", included.ToSvg());
        var svg = XDocument.Parse(excluded.ToSvg());
        Assert.DoesNotContain(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "annotation-label" && node.Value.Contains("Excluded edge", StringComparison.Ordinal));
        var note = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "annotation"
            && (string?)node.Attribute("data-cfx-label") == "Excluded note");
        Assert.Equal("false", (string?)note.Attribute("data-cfx-visible"));
        var crossing = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "annotation"
            && (string?)node.Attribute("data-cfx-label") == "Crossing line");
        Assert.Equal("true", (string?)crossing.Attribute("data-cfx-visible"));
    }

    [Fact]
    public void CalendarExclusiveMaximumMustBeACapturedBoundaryWhileInclusiveRawBoundsRemainExact() {
        var january = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithLegend(false).AddLine("Monthly", new[] {
            new ChartPoint(ChartDateTime.ToOADate(january.AddDays(2)), 1),
            new ChartPoint(ChartDateTime.ToOADate(january.AddMonths(1).AddDays(9)), 2),
            new ChartPoint(ChartDateTime.ToOADate(january.AddMonths(2).AddDays(9)), 3) });
        chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)));
        var full = chart.Prepare(context);
        var completeExclusive = Assert.IsType<ChartAxisWindow>(full.XProjection!.ConstrainViewport(
            new ChartAxisWindow(full.XProjection.FullMinimum, full.XProjection.FullMaximum, maximumInclusive: false)));
        Assert.False(chart.Prepare(context, new ChartCartesianViewport(x: completeExclusive)).XProjection!.MaximumInclusive);
        var first = ChartDateTime.ToOADate(january.AddMonths(1));
        var boundary = ChartDateTime.ToOADate(january.AddMonths(2));
        var canonical = Assert.IsType<ChartAxisWindow>(full.XProjection!.ConstrainViewport(new ChartAxisWindow(first, boundary, maximumInclusive: false)));
        Assert.False(canonical.MaximumInclusive);
        var selected = chart.Prepare(context, new ChartCartesianViewport(x: canonical));
        Assert.Equal(selected.XProjection!.PlotBounds.Left + selected.XProjection.PlotBounds.Width / 2,
            selected.XProjection.Project(ChartDateTime.ToOADate(january.AddMonths(1).AddDays(9))), 8);
        var interior = boundary + 2;
        var invalid = new ChartAxisWindow(first, interior, maximumInclusive: false);
        Assert.Contains("canonical", Assert.Throws<ArgumentOutOfRangeException>(() => full.XProjection.ConstrainViewport(invalid)).Message);
        Assert.Contains("canonical", Assert.Throws<ArgumentOutOfRangeException>(() => chart.Prepare(context, new ChartCartesianViewport(x: invalid))).Message);
        var authored = new ChartAxisWindow(first, interior);
        Assert.Equal(interior, chart.Prepare(context, new ChartCartesianViewport(x: authored)).XProjection!.Maximum);

        chart.Options.XAxis.Maximum = interior;
        var clippedFull = chart.Prepare(context);
        var logical = clippedFull.XProjection!.Project(ChartDateTime.ToOADate(january.AddMonths(2).AddDays(9)));
        var clipped = Assert.IsType<ChartAxisWindow>(clippedFull.XProjection.ConstrainViewport(logical - 1, logical + 1));
        Assert.Equal(interior, clipped.Maximum); Assert.True(clipped.MaximumInclusive);
        var replay = chart.Prepare(context, new ChartCartesianViewport(x: clipped));
        Assert.Equal(interior, replay.XProjection!.Maximum); Assert.True(replay.XProjection.MaximumInclusive);
    }

    private static JsonDocument Metadata(string html) => JsonDocument.Parse(System.Net.WebUtility.HtmlDecode(
        System.Text.RegularExpressions.Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]*)\"").Groups[1].Value));
}
