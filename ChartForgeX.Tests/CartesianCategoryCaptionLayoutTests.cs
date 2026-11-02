using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Named categories retain useful identification without changing full source facts or numeric-axis policy.</summary>
public sealed class CartesianCategoryCaptionLayoutTests {
    private static readonly string[] Names = {
        "Regional processing North America", "Regional processing Western Europe",
        "Regional processing South America", "Regional processing East Asia"
    };
    private static readonly string[] Identifiers = { "North", "Western", "South", "East" };

    [Theory]
    [InlineData(ChartSeriesKind.Bar, 390)]
    [InlineData(ChartSeriesKind.Bar, 760)]
    [InlineData(ChartSeriesKind.RangeBar, 390)]
    [InlineData(ChartSeriesKind.RangeBar, 760)]
    [InlineData(ChartSeriesKind.BoxPlot, 390)]
    [InlineData(ChartSeriesKind.BoxPlot, 760)]
    [InlineData(ChartSeriesKind.Waterfall, 390)]
    [InlineData(ChartSeriesKind.Waterfall, 760)]
    public void MultiwordCategoriesKeepDistinctNamesAndCompleteSourceFacts(ChartSeriesKind kind, int width) {
        var chart = Create(kind, width);
        var source = IdentitySnapshot(chart);
        var prepared = Prepare(chart);
        var captions = Captions(prepared);
        Assert.Equal(4, captions.Length);
        Assert.All(captions, caption => {
            Assert.Equal(12, caption.Text.Size);
            Assert.InRange(caption.Text.Lines.Count, 1, 2);
        });
        var displayed = captions.Select(Text).ToArray();
        Assert.Equal(4, displayed.Distinct(StringComparer.Ordinal).Count());
        var facts = prepared.Regions.Where(region => region.Role == "axis-x-label").ToArray();
        Assert.Equal(4, facts.Length);
        for (var index = 0; index < Names.Length; index++) {
            var sourceFact = Assert.Single(facts, fact => fact.Label!.StartsWith(Names[index] + " (", StringComparison.Ordinal));
            var caption = Assert.Single(captions, item => Text(item).Contains(Identifiers[index], StringComparison.Ordinal));
            Assert.Equal(sourceFact.Bounds.Left, caption.X + caption.Text.Metrics.Width / 2, 8);
            if (width == 760) Assert.Equal(Names[index], string.Join(" ", caption.Text.Lines.Select(line => line.Text)));
        }
        Assert.Equal(source, IdentitySnapshot(chart));
        Assert.Equal(kind == ChartSeriesKind.BoxPlot ? 31 : 4, chart.Series.Sum(series => series.SourcePointCount));
        AssertCaptionBounds(prepared, captions);
    }

    [Fact]
    public void NormalWaterfallKeepsMixedRegionAndCalculatedTotalCaptions() {
        var chart = Create(ChartSeriesKind.Waterfall, 390, legacyWaterfall: true);
        var identity = IdentitySnapshot(chart); var prepared = Prepare(chart); var captions = Captions(prepared);
        Assert.Equal(5, captions.Length);
        Assert.Equal(5, captions.Select(Text).Distinct(StringComparer.Ordinal).Count());
        Assert.All(captions, caption => { Assert.Equal(12, caption.Text.Size); Assert.InRange(caption.Text.Lines.Count, 1, 2); });
        foreach (var identifier in new[] { "North", "West", "South", "East" })
            Assert.Single(captions, caption => Text(caption).Contains(identifier, StringComparison.Ordinal));
        Assert.Single(captions, caption => Text(caption) == "Total");
        var facts = prepared.Regions.Where(region => region.Role == "axis-x-label").ToArray();
        Assert.Equal(5, facts.Length);
        foreach (var name in Names.Concat(new[] { "Total" }))
            Assert.Single(facts, fact => fact.Label!.StartsWith(name + " (", StringComparison.Ordinal));
        var xml = XDocument.Parse(prepared.ToSvg());
        var sources = xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(4, sources.Length);
        Assert.Equal(new[] { "region-East", "region-North", "region-South", "region-Western" },
            sources.Select(node => (string)node.Attribute("data-cfx-observation-id")!).OrderBy(value => value, StringComparer.Ordinal));
        var total = Assert.Single(xml.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "waterfall-total");
        Assert.Equal("12", (string?)total.Attribute("data-cfx-value"));
        Assert.Equal("0", (string?)total.Attribute("data-cfx-start"));
        Assert.Equal("12", (string?)total.Attribute("data-cfx-end"));
        Assert.Equal("waterfall-total", (string?)total.Attribute("data-cfx-derived"));
        Assert.Equal("0,1,2,3", (string?)total.Attribute("data-cfx-source-points"));
        Assert.Equal("4", (string?)total.Attribute("data-cfx-source-count"));
        Assert.Null(total.Attribute("data-cfx-observation-id"));
        Assert.Equal(identity, IdentitySnapshot(chart));
        Assert.Equal(4, chart.Series[0].SourcePointCount);
        AssertCaptionBounds(prepared, captions);
    }

    [Theory]
    [InlineData(false, 12, ChartLabelDensity.Auto)]
    [InlineData(true, 12, ChartLabelDensity.Auto)]
    [InlineData(false, 6, ChartLabelDensity.Auto)]
    [InlineData(false, 12, ChartLabelDensity.Dense)]
    public void ReversalAndExplicitOrdinaryFontKeepTheirCategoryAnchors(bool reversed, double fontSize, ChartLabelDensity density) {
        var chart = Create(ChartSeriesKind.Bar, 390).WithXAxisLabelDensity(density).ConfigureXAxis(axis => axis.Reversed = reversed)
            .ConfigureTickLabelStyle(style => style.FontSize = fontSize);
        var prepared = Prepare(chart); var captions = Captions(prepared);
        Assert.Equal(4, captions.Length);
        Assert.All(captions, caption => Assert.Equal(fontSize, caption.Text.Size));
        Assert.Equal(fontSize, chart.Options.TickLabelStyle.FontSize);
        var positions = Identifiers.Select(identifier => {
            var caption = Assert.Single(captions, item => Text(item).Contains(identifier, StringComparison.Ordinal));
            return caption.X + caption.Text.Metrics.Width / 2;
        }).ToArray();
        for (var index = 1; index < positions.Length; index++) Assert.True(reversed ? positions[index] < positions[index - 1] : positions[index] > positions[index - 1]);
        AssertCaptionBounds(prepared, captions);
    }

    [Fact]
    public void NumericXUnitSuffixKeepsCompleteSingleLineScaleCaptions() {
        var chart = Create(ChartSeriesKind.Bar, 390);
        chart.Options.XAxis.Labels.Clear();
        chart.ConfigureXAxis(axis => axis.WithBounds(0, 24).WithValueFormat(ChartValueFormat.Number("0 'hours'")));
        var prepared = Prepare(chart); var captions = Captions(prepared);
        Assert.NotEmpty(captions);
        Assert.All(captions, caption => {
            Assert.Single(caption.Text.Lines);
            Assert.EndsWith(" hours", Text(caption), StringComparison.Ordinal);
            Assert.DoesNotContain("…", Text(caption), StringComparison.Ordinal);
            Assert.Equal(12, caption.Text.Size);
        });
        Assert.Equal(4, chart.Series[0].SourcePointCount);
        Assert.Empty(chart.Options.XAxis.Labels);
    }

    [Theory]
    [InlineData(ChartLabelDensity.All)]
    [InlineData(ChartLabelDensity.Relaxed)]
    public void ExplicitCompleteCaptionDensityKeepsItsExistingPolicy(ChartLabelDensity density) {
        var chart = Create(ChartSeriesKind.Bar, 390).WithXAxisLabelDensity(density);
        var captions = Captions(Prepare(chart));
        Assert.NotEmpty(captions);
        Assert.All(captions, caption => {
            Assert.Single(caption.Text.Lines);
            Assert.Contains(Text(caption), Names);
            Assert.DoesNotContain("…", Text(caption), StringComparison.Ordinal);
        });
        if (density == ChartLabelDensity.All) Assert.Equal(4, captions.Length);
        else Assert.True(captions.Length < 4);
    }

    [Fact]
    public void AuthoredLineBreaksAndRotationRemainAuthoritative() {
        var authored = Names.Select(name => name.Replace("processing ", "processing\n", StringComparison.Ordinal)).ToArray();
        var chart = Create(ChartSeriesKind.Bar, 390).WithXLabels(authored).WithXAxisLabelDensity(ChartLabelDensity.All);
        var captions = Captions(Prepare(chart));
        Assert.Equal(4, captions.Length);
        Assert.All(captions, caption => Assert.Contains(Text(caption), authored));
        var rotated = Create(ChartSeriesKind.Bar, 390).WithXAxisLabelAngle(45);
        var rotatedCaptions = Captions(Prepare(rotated));
        Assert.NotEmpty(rotatedCaptions);
        Assert.All(rotatedCaptions, caption => {
            Assert.Single(caption.Text.Lines);
            Assert.Contains(Text(caption), Names);
            Assert.Equal(12, caption.Text.Size);
        });
        Assert.Equal(45, rotated.Options.XAxis.LabelAngle);
    }

    [Fact]
    public void DistinctNamesDoNotCollapseIntoTheSameShortenedCaption() {
        var names = Enumerable.Range(1, 4).Select(index => "International processing identicalprefix000000" + index.ToString(CultureInfo.InvariantCulture)).ToArray();
        var chart = Create(ChartSeriesKind.Bar, 390).WithXLabels(names);
        var prepared = Prepare(chart); var captions = Captions(prepared);
        Assert.NotEmpty(captions);
        Assert.Equal(captions.Length, captions.Select(Text).Distinct(StringComparer.Ordinal).Count());
        // At this width the differing suffix cannot survive the shared end fitter. Full
        // captions on a coarser stride are more useful than four identical abbreviations.
        Assert.All(captions, caption => Assert.Contains(Text(caption), names));
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == "axis-x-label"));
    }

    [Fact]
    public void CategoryCallbacksRunOnceAndPreparedNamesStayDetached() {
        var chart = Create(ChartSeriesKind.BoxPlot, 390);
        chart.Options.XAxis.Labels.Clear();
        var calls = new Dictionary<double, int>();
        chart.Options.XAxis.LabelFormatter = value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return Names[(int)value - 1];
        };
        var prepared = Prepare(chart); var svg = prepared.ToSvg();
        Assert.Equal(new[] { 1d, 2, 3, 4 }, calls.Keys.OrderBy(value => value));
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        Assert.Equal(4, Captions(prepared).Length);
        chart.Options.XAxis.LabelFormatter = _ => "Changed after preparation";
        Assert.Equal(svg, prepared.ToSvg());
        foreach (var name in Names) Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label!.StartsWith(name + " (", StringComparison.Ordinal));
    }

    private static Chart Create(ChartSeriesKind kind, int width, bool legacyWaterfall = false) {
        var chart = Chart.Create().WithSize(width, 480).WithTheme(VisualTheme.Graphite(), width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark)
            .WithTitle("Timing deviation by region").WithLegend(false).WithDataLabels(false).WithXLabels(Names)
            .WithValueFormat(ChartValueFormat.Number("0.## 'ms'")).ConfigureTickLabelStyle(style => style.FontSize = 12);
        switch (kind) {
            case ChartSeriesKind.Bar:
                chart.AddBar("Change", new[] { new ChartPoint(1, 12), new ChartPoint(2, 20), new ChartPoint(3, 0), new ChartPoint(4, -3) });
                break;
            case ChartSeriesKind.RangeBar:
                chart.AddRangeBar("Range", new[] { new ChartInterval(1, 8, 16), new ChartInterval(2, 19, 21), new ChartInterval(3, -3, 3), new ChartInterval(4, 1, 10) });
                break;
            case ChartSeriesKind.BoxPlot:
                var groups = new[] {
                    new ChartBoxPlotSampleGroup(1, new[] { 8d, 9, 10, 11, 12, 13, 14, 15, 16, 80 }),
                    new ChartBoxPlotSampleGroup(2, new[] { 20d, 20, 20, 20 }),
                    new ChartBoxPlotSampleGroup(3, new[] { -30d, -3, -2, -1, 0, 1, 2, 3, 30 }),
                    new ChartBoxPlotSampleGroup(4, new[] { 1d, 4, 7, 10 })
                };
                chart.AddSeries(ChartSeries.CreateBoxPlot(new ChartSeriesId("observed-regions"), "Observed", groups)
                    .ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr)
                    .WithBoxPlotGroupIds(Identifiers.Select(value => new ChartObservationId("region-" + value)))
                    .WithObservationIds(Enumerable.Range(0, 27).Select(index => new ChartObservationId("sample-" + index.ToString(CultureInfo.InvariantCulture)))))
                    .AddSeries(ChartSeries.CreateBoxPlot(new ChartSeriesId("reference-regions"), "Reference", new[] {
                        new ChartBoxPlot(1, 9, 11, 13, 15, 17), new ChartBoxPlot(2, 18, 19, 20, 21, 22),
                        new ChartBoxPlot(3, -4, -2, 0, 2, 4), new ChartBoxPlot(4, 2, 4, 6, 8, 10)
                    }).WithObservationIds(Identifiers.Select(value => new ChartObservationId("reference-" + value))));
                return chart;
            case ChartSeriesKind.Waterfall:
                if (legacyWaterfall) chart.AddWaterfall("Change", new[] { new ChartPoint(1, 12), new ChartPoint(2, -4), new ChartPoint(3, 7), new ChartPoint(4, -3) });
                else chart.AddSeries(ChartSeries.CreateWaterfall(new ChartSeriesId("region-waterfall"), "Change", new[] {
                    ChartWaterfallItem.Delta(1, 12), ChartWaterfallItem.Delta(2, -4), ChartWaterfallItem.Delta(3, 7), ChartWaterfallItem.Delta(4, -3)
                }));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
        chart.Series[0].WithObservationIds(Identifiers.Select(value => new ChartObservationId("region-" + value)));
        return chart;
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(chart.Options.Size.Width, chart.Options.Size.Height), chart.Options.Padding),
        chart.Options.Theme, chart.Options.ThemeMode, new VisualFrame(chart.Title, chart.Subtitle, chart.Options.ShowLegend),
        new FontSpec { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") }));

    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "axis-x-label").ToArray();
    private static string Text(VisualSceneText caption) => string.Join("\n", caption.Text.Lines.Select(line => line.Text));
    private static string IdentitySnapshot(Chart chart) => string.Join(";", chart.Series.Select(series => series.Id.Value + ":" + series.SourcePointCount
        + ":" + string.Join(",", series.ObservationIds.OrderBy(pair => pair.Key).Select(pair => pair.Key + "=" + pair.Value.Value))
        + ":" + string.Join(",", series.BoxPlotGroupIds.OrderBy(pair => pair.Key).Select(pair => pair.Key + "=" + pair.Value.Value))));

    private static void AssertCaptionBounds(PreparedVisual prepared, VisualSceneText[] captions) {
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        var bounds = captions.Select(caption => new ChartRect(caption.X, caption.Baseline - caption.Text.Ascent, caption.Text.Metrics.Width, caption.Text.Metrics.Height))
            .OrderBy(rect => rect.Left).ToArray();
        foreach (var rect in bounds) {
            Assert.InRange(rect.Left, 0, prepared.Size.Width);
            Assert.InRange(rect.Right, 0, prepared.Size.Width);
            Assert.InRange(rect.Top, plot.Bottom, prepared.Size.Height);
            Assert.InRange(rect.Bottom, plot.Bottom, prepared.Size.Height);
        }
        for (var index = 1; index < bounds.Length; index++) Assert.True(bounds[index - 1].Right <= bounds[index].Left, "Neighbouring category captions must not overprint.");
    }
}
