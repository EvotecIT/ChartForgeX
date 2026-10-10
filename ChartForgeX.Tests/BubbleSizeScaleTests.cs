using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Protects cross-series size meaning, explicit-domain stability, retained raw facts and detached prepared exports.
/// Different source arrays expose the former per-series mismatch that the shared marker-shape fixture missed.
/// </summary>
public sealed class BubbleSizeScaleTests {
    [Fact]
    public void EqualRawSizesUseTheSameRadiusAcrossDifferentSeriesDomains() {
        var chart = Bare().AddBubble("First", new[] { new ChartBubble(1, 20, 1), new ChartBubble(3, 30, 100) })
            .AddBubble("Second", new[] { new ChartBubble(5, 40, 1), new ChartBubble(7, 50, 100), new ChartBubble(9, 60, 400) });
        var prepared = Prepare(chart);
        var marks = Marks(prepared);
        Capture(prepared, "automatic-shared");
        Assert.Equal(5, marks.Length);
        Assert.Equal(marks[0].Rx, marks[2].Rx, 8);
        Assert.Equal(marks[1].Rx, marks[3].Rx, 8);
        Assert.True(marks[0].Rx < marks[1].Rx && marks[1].Rx < marks[4].Rx);
        var sources = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal("100", sources[1].Metadata["data-cfx-size"]);
        Assert.Equal("100", sources[3].Metadata["data-cfx-size"]);
    }

    [Fact]
    public void ExplicitDomainAndRadiiKeepSizeMeaningAcrossSeriesAxesAndViewportChanges() {
        var chart = Bare().AddBubble("First", new[] { new ChartBubble(1, 20, 1), new ChartBubble(3, 30, 100) })
            .ConfigureBubble(bubble => { bubble.WithSizeDomain(0, 400); bubble.MinimumRadius = 2; bubble.MaximumRadius = 30; });
        var before = Marks(Prepare(chart));
        chart.AddBubble("Second", new[] { new ChartBubble(5, 40, 100), new ChartBubble(7, 50, 1000) });
        chart.Series[1].UseSecondaryYAxis();
        chart.Options.SecondaryYAxis.WithBounds(0, 100);
        chart.WithSize(340, 240);
        var prepared = Prepare(chart); var after = Marks(prepared);
        Assert.Equal(before.Select(mark => mark.Rx), after.Take(2).Select(mark => mark.Rx));
        Assert.Equal(16, after[1].Rx, 8); Assert.Equal(after[1].Rx, after[2].Rx, 8);
        Assert.Equal(30, after[3].Rx);
        Assert.Equal("1000", prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == "series-1-point-1").Metadata["data-cfx-size"]);
        Capture(prepared, "explicit-shared-secondary-compact");
    }

    [Fact]
    public void AutomaticDomainIncludesHiddenAndSecondarySeriesButNotEmptyTuples() {
        var chart = Bare().AddBubble("First", new[] { new ChartBubble(1, 20, 1), new ChartBubble(3, 30, 100) })
            .AddBubble("Hidden", new[] { new ChartBubble(5, 40, 400) });
        chart.Series[1].UseSecondaryYAxis().ConfigureMarkers(marker => marker.Enabled = false);
        chart.Series.Add(new ChartSeries("Empty", ChartSeriesKind.Bubble, Array.Empty<ChartPoint>()));
        chart.ConfigureBubble(bubble => { bubble.MinimumRadius = 2; bubble.MaximumRadius = 30; });
        var automatic = Marks(Prepare(chart));
        Assert.Equal(2, automatic.Length);
        Assert.Equal(2, automatic[0].Rx);
        Assert.Equal(2 + 28 * Math.Sqrt(99d / 399), automatic[1].Rx, 8);
        chart.Options.Bubble.WithSizeDomain(0, 100);
        Assert.Equal(30, Marks(Prepare(chart))[1].Rx);
        chart.Options.Bubble.UseAutomaticSizeDomain();
        Assert.Null(chart.Options.Bubble.MinimumValue); Assert.Null(chart.Options.Bubble.MaximumValue);
        Assert.Equal(automatic.Select(mark => mark.Rx), Marks(Prepare(chart)).Select(mark => mark.Rx));
    }

    [Theory]
    [InlineData(false, 5, 2)]
    [InlineData(false, 10, 2)]
    [InlineData(false, 20, 10)]
    [InlineData(false, 50, 18)]
    [InlineData(false, 100, 18)]
    [InlineData(true, 5, 18)]
    [InlineData(true, 10, 18)]
    [InlineData(true, 20, 15.856406460551018)]
    [InlineData(true, 50, 2)]
    [InlineData(true, 100, 2)]
    public void ExplicitSizeDomainClampsGeometryAndRetainsRawFacts(bool reversed, double size, double expectedRadius) {
        var chart = Bare().AddBubble("Sample", new[] { new ChartBubble(3, 30, size) })
            .ConfigureBubble(bubble => {
                bubble.WithSizeDomain(10, 50); bubble.MinimumRadius = 2; bubble.MaximumRadius = 18; bubble.Reversed = reversed;
            });
        var prepared = Prepare(chart); var mark = Assert.Single(Marks(prepared));
        Assert.Equal(expectedRadius, mark.Rx, 8);
        var source = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "point");
        Assert.Equal(size.ToString("G17", CultureInfo.InvariantCulture), source.Metadata["data-cfx-size"]);
        Assert.Contains("data-cfx-size=\"" + size.ToString("G17", CultureInfo.InvariantCulture) + "\"", prepared.ToSvg());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstantAutomaticDomainUsesTheRadiusMidpoint(bool reversed) {
        var chart = Bare().AddBubble("First", new[] { new ChartBubble(1, 20, 40) })
            .AddBubble("Second", new[] { new ChartBubble(3, 30, 40) });
        chart.ConfigureBubble(bubble => { bubble.MinimumRadius = 2; bubble.MaximumRadius = 30; bubble.Reversed = reversed; });
        Assert.All(Marks(Prepare(chart)), mark => Assert.Equal(16, mark.Rx));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LargeFiniteSourceDomainsKeepFiniteRadiiAndUnchangedRawSizes(bool reversed) {
        var chart = Bare().AddBubble("Samples", new[] { new ChartBubble(1, 20, double.MaxValue / 4), new ChartBubble(3, 30, double.MaxValue) })
            .ConfigureBubble(bubble => {
                bubble.WithSizeDomain(0, double.MaxValue); bubble.MinimumRadius = 2; bubble.MaximumRadius = 30; bubble.Reversed = reversed;
            });
        var prepared = Prepare(chart); var marks = Marks(prepared);
        Assert.Equal(2 + 28 * Math.Sqrt(reversed ? .75 : .25), marks[0].Rx, 8);
        Assert.Equal(reversed ? 2 : 30, marks[1].Rx);
        Assert.Contains("data-cfx-size=\"" + double.MaxValue.ToString("G17", CultureInfo.InvariantCulture) + "\"", prepared.ToSvg());
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void FixedZeroRadiusKeepsFactsAndLegendTextWithoutPaintedGlyphs() {
        var chart = Bare().WithLegend().AddBubble("Source facts", new[] { new ChartBubble(1, 20, 40) })
            .ConfigureBubble(bubble => { bubble.MinimumRadius = 0; bubble.MaximumRadius = 0; });
        var prepared = Prepare(chart);
        Assert.Empty(Marks(prepared));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "legend-swatch");
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "legend-label" && node.Text.Lines.Any(line => line.Text == "Source facts"));
        var source = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "point");
        Assert.Equal("40", source.Metadata["data-cfx-size"]);
        Assert.Contains(prepared.Regions, region => region.Id == "series-0-point-0" && region.Label?.Contains("40") == true);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(340, 240)]
    [InlineData(700, 440)]
    public void DisabledGlyphUsesPointBoundsAndTheSameLabelAnchorAsAZeroRadius(int width, int height) {
        var chart = Bare().WithSize(width, height).WithDataLabels(true)
            .AddBubble("Retained observation", new[] { new ChartBubble(5, 50, 200) })
            .ConfigureBubble(bubble => { bubble.WithSizeDomain(0, 100); bubble.MinimumRadius = 3; bubble.MaximumRadius = 48; });
        chart.Series[0].ConfigureMarkers(marker => marker.Enabled = false);
        var disabled = Prepare(chart);
        Capture(disabled, "disabled-label-" + width);
        var point = Assert.Single(disabled.Regions, region => region.Id == "series-0-point-0");
        Assert.Equal(0, point.Bounds.Width); Assert.Equal(0, point.Bounds.Height);
        Assert.Equal("200", Assert.Single(disabled.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "point").Metadata["data-cfx-size"]);
        Assert.Empty(Marks(disabled));
        var label = Assert.Single(disabled.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label");
        chart.Series[0].ConfigureMarkers(marker => marker.Enabled = true);
        chart.ConfigureBubble(bubble => { bubble.MinimumRadius = 0; bubble.MaximumRadius = 0; });
        var zero = Prepare(chart);
        var expected = Assert.Single(zero.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label");
        Assert.Equal(expected.X, label.X, 8); Assert.Equal(expected.Baseline, label.Baseline, 8);
        Assert.Equal("200", Assert.Single(label.Text.Lines).Text);
        Assert.Equal(disabled.ToPng(), zero.ToPng());
    }

    [Fact]
    public void PreparedScaleAndExportsAreDetachedFromLaterConfigurationAndSourceMutations() {
        var chart = Bare().WithLegend().AddBubble("Samples", new[] { new ChartBubble(1, 20, 25), new ChartBubble(3, 30, 100) })
            .ConfigureBubble(bubble => { bubble.WithSizeDomain(0, 100); bubble.MinimumRadius = 2; bubble.MaximumRadius = 30; });
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.ConfigureBubble(bubble => { bubble.WithSizeDomain(0, 200); bubble.MinimumRadius = 4; bubble.MaximumRadius = 40; bubble.Reversed = true; });
        chart.Series[0].Markers.Shape = ChartMarkerShape.Diamond;
        chart.Series[0].Points[1] = new ChartPoint(1, 200);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
    }

    [Fact]
    public void BubbleOptionsHaveOneOwnerAndRejectContradictoryPublicConfiguration() {
        var chart = Bare().AddBubble("Samples", new[] { new ChartBubble(1, 20, 40) });
        var options = chart.Options.Bubble;
        Assert.Same(chart, chart.ConfigureBubble(bubble => Assert.Same(options, bubble)));
        Assert.Equal(6, options.MinimumRadius); Assert.Null(options.MaximumRadius);
        Assert.Null(options.MinimumValue); Assert.Null(options.MaximumValue); Assert.False(options.Reversed);
        Assert.Throws<ArgumentNullException>(() => chart.ConfigureBubble(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MinimumRadius = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaximumRadius = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithSizeDomain(10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithSizeDomain(10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithSizeDomain(-1, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithSizeDomain(0, double.PositiveInfinity));
        Assert.Null(options.MinimumValue); Assert.Null(options.MaximumValue);
        options.WithSizeDomain(1, 10);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithSizeDomain(2, double.NaN));
        Assert.Equal(1, options.MinimumValue); Assert.Equal(10, options.MaximumValue);
        options.MaximumRadius = 1;
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void BubbleSeriesRejectConflictingRadiusAliasesAtPreparation(int route) {
        var chart = Bare().AddBubble("Samples", new[] { new ChartBubble(1, 20, 40) });
        var series = chart.Series[0];
        if (route == 0) series.Markers.Radius = 8;
        else if (route == 1) series.WithMarkerRadius(8);
        else series.MarkerRadius = 8;
        var error = Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        Assert.Contains("ChartOptions.Bubble", error.Message);
        series.UseThemeMarkerRadius().ConfigureMarkers(marker => marker.Enabled = false);
        Assert.Empty(Marks(Prepare(chart)));
    }

    private static Chart Bare() {
        var chart = Chart.Create().WithSize(960, 600).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 100);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        return chart;
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneEllipse[] Marks(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneEllipse>()
        .Where(node => node.Role == "bubble").ToArray();

    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BUBBLE_CAPTURE_DIRECTORY");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
        File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(Marks(prepared)
            .Select(mark => new { mark.Cx, mark.Cy, Radius = mark.Rx })));
    }
}
