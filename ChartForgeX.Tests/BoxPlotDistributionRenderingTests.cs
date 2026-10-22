using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BoxPlotDistributionRenderingTests {
    [Theory]
    [InlineData(ChartBoxPlotWhiskerPolicy.Iqr, false, true)]
    [InlineData(ChartBoxPlotWhiskerPolicy.Iqr, true, false)]
    [InlineData(ChartBoxPlotWhiskerPolicy.MinMax, false, false)]
    public void AccessibleSummaryDistinguishesInterpolatedWhiskersFromMeasuredExtrema(ChartBoxPlotWhiskerPolicy policy, bool noInliers, bool localized) {
        var samples = noInliers ? new[] { 0d, 100 } : new[] { 0d, 100, 101, 102 };
        var series = ChartSeries.CreateBoxPlot(new ChartSeriesId("sparse"), "Latency", 1, samples)
            .ConfigureBoxPlotStatistics(options => { options.WhiskerPolicy = policy; options.IqrMultiplier = noInliers ? .0001 : 1.5; });
        var chart = Chart.Create().WithSize(520, 320).WithHeader(false).WithLegend(false).WithDataLabels(false).WithXLabels("Service B").AddSeries(series);
        if (localized) chart.ConfigureLabels(labels => {
            labels.LowerWhisker = "Dolny wąs"; labels.UpperWhisker = "Górny wąs";
            labels.SourceMinimum = "Minimum próbek"; labels.SourceMaximum = "Maksimum próbek";
            labels.InlierMinimum = "Minimum wewnątrz"; labels.InlierMaximum = "Maksimum wewnątrz";
        });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point" && node.Metadata.ContainsKey("data-cfx-derived"));
        var description = group.Metadata["aria-label"];
        var region = Assert.Single(prepared.Regions, item => item.Id == group.Id && item.Role == "point");
        Assert.Equal(description, region.Label);
        Assert.Contains("Service B", description);
        Assert.Equal("0", group.Metadata["data-cfx-source-minimum"]);
        if (policy == ChartBoxPlotWhiskerPolicy.Iqr) {
            if (noInliers) {
                Assert.Contains("Lower whisker=25", description); Assert.Contains("Upper whisker=75", description);
                Assert.Contains("Source minimum=0", description); Assert.Contains("Source maximum=100", description);
                Assert.Contains("Inlier minimum=None", description); Assert.Contains("Inlier maximum=None", description);
            } else {
                Assert.Contains("Dolny wąs=75", description); Assert.Contains("Górny wąs=102", description);
                Assert.Contains("Minimum próbek=0", description); Assert.Contains("Minimum wewnątrz=100", description);
            }
        } else Assert.Contains(" minimum=0", description);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(description, svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point"
            && node.Attribute("data-cfx-derived") != null).Attribute("aria-label")!.Value);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AutomaticBoundsContainCompleteOutlierPaintAtBothSourceExtrema(bool horizontal, bool reverse) {
        var series = ChartSeries.CreateBoxPlot(new ChartSeriesId("edges"), "Measurements", 1,
            new[] { -100d, 0, 1, 2, 3, 4, 5, 6, 7, 8, 100 })
            .ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        var chart = Chart.Create().WithSize(360, 300).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .ConfigureBoxPlot(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical).AddSeries(series);
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithReversal(reverse);
        var doc = XDocument.Parse(chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg());
        var outliers = doc.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "boxplot-outlier").ToArray();
        Assert.Equal(2, outliers.Length);
        foreach (var ellipse in outliers) {
            var clipUrl = ellipse.Ancestors().Select(element => (string?)element.Attribute("clip-path")).First(value => value != null)!;
            var clipId = clipUrl.Substring(5, clipUrl.Length - 6);
            var clip = doc.Descendants().Single(element => (string?)element.Attribute("id") == clipId).Elements().Single();
            double N(XElement node, string key) => double.Parse(node.Attribute(key)!.Value, CultureInfo.InvariantCulture);
            var axis = horizontal ? "x" : "y"; var center = N(ellipse, horizontal ? "cx" : "cy");
            var radius = N(ellipse, horizontal ? "rx" : "ry");
            Assert.True(center - radius >= N(clip, axis) - .002);
            Assert.True(center + radius <= N(clip, axis) + N(clip, horizontal ? "width" : "height") + .002);
        }
    }
    [Theory]
    [InlineData(false, false, 800, VisualThemeMode.Light)]
    [InlineData(false, true, 360, VisualThemeMode.Dark)]
    [InlineData(true, false, 800, VisualThemeMode.Dark)]
    [InlineData(true, true, 360, VisualThemeMode.Light)]
    public void RawOutliersShareNativeGeometryAndKeepIndependentSummaryAndSampleFacts(bool horizontal, bool reversed, int width, VisualThemeMode mode) {
        var chart = Create(width, mode).ConfigureBoxPlot(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical);
        var valueAxis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        valueAxis.WithReversal(reversed);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(4, points.Length);
        var summaries = points.Where(group => group.Metadata.ContainsKey("data-cfx-derived")).ToArray();
        var outliers = points.Where(group => group.Metadata["data-cfx-kind"] == "BoxPlotOutlier").ToArray();
        Assert.Equal(2, summaries.Length); Assert.Equal(2, outliers.Length);
        Assert.Equal(new[] { "group-\"a", "group-b" }, summaries.Select(group => group.Metadata["data-cfx-group-id"]));
        Assert.All(summaries, group => {
            Assert.False(group.Metadata.ContainsKey("data-cfx-observation-id")); Assert.False(group.Metadata.ContainsKey("data-cfx-source-point"));
            Assert.Equal("Iqr", group.Metadata["data-cfx-whisker-policy"]);
            Assert.Equal("LinearInterpolated", group.Metadata["data-cfx-quartile-estimator"]);
        });
        Assert.Equal("10", summaries[0].Metadata["data-cfx-source-count"]);
        Assert.Equal("100", summaries[0].Metadata["data-cfx-source-maximum"]);
        Assert.Equal("8", summaries[0].Metadata["data-cfx-inlier-maximum"]);
        Assert.Equal("75", summaries[1].Metadata["data-cfx-minimum"]);
        Assert.Equal("0", summaries[1].Metadata["data-cfx-source-minimum"]);
        Assert.Equal("100", summaries[1].Metadata["data-cfx-inlier-minimum"]);
        Assert.Equal(new[] { "sample-9", "sample-10" }, outliers.Select(group => group.Metadata["data-cfx-observation-id"]));
        Assert.Equal(new[] { "9", "10" }, outliers.Select(group => group.Metadata["data-cfx-source-point"]));
        Assert.Equal(new[] { "0", "1" }, outliers.Select(group => group.Metadata["data-cfx-group-index"]));
        Assert.All(outliers, group => Assert.False(group.Metadata.ContainsKey("data-cfx-derived")));
        Assert.Equal(4, points.Select(group => group.Id).Distinct().Count());
        var marks = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "boxplot-outlier").ToArray();
        Assert.Equal(2, marks.Length);
        var range = ChartRange.FromChart(chart);
        Assert.True((horizontal ? range.MinX : range.MinY) <= 0); Assert.True((horizontal ? range.MaxX : range.MaxY) >= 102);
        Assert.NotEmpty(prepared.ToPng()); Assert.Contains("boxplot-outlier", prepared.ToSvg());
        Capture(prepared, (horizontal ? "horizontal" : "vertical") + "-" + width + "-" + mode);
    }

    [Fact]
    public void PreparedDistributionExportsRemainDetachedFromPolicySourcesIdentitiesAndLocalizedWords() {
        var chart = Create(800, VisualThemeMode.Light).ConfigureLabels(labels => {
            labels.LowerWhisker = "Dolny wąs"; labels.SourceMinimum = "Minimum pomiarów"; labels.Outlier = "Pomiar odstający";
        });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[0].ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.MinMax)
            .ReplaceBoxPlotSampleGroups(new[] { new ChartBoxPlotSampleGroup(3, new[] { 4d, 5 }) }).WithBoxPlotGroupId(0, new ChartObservationId("changed"));
        chart.ConfigureLabels(labels => labels.LowerWhisker = "Changed"); chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Contains("Dolny wąs", svg); Assert.Contains("Minimum pomiarów", svg); Assert.Contains("Pomiar odstający", svg);
        Assert.Contains("group-&quot;a", svg); Assert.DoesNotContain("changed", svg);
    }

    [Fact]
    public void PointLegendsUseGroupIdentityAndNeverBorrowAnOriginalSampleIdentity() {
        var prepared = Create(800, VisualThemeMode.Light).WithLegend(true).WithPointLegend().Prepare(new VisualRenderContext());
        var legends = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "legend-entry").ToArray();
        Assert.Equal(2, legends.Length);
        Assert.Equal(new[] { "group-\"a", "group-b" }, legends.Select(group => group.Metadata["data-cfx-group-id"]));
        Assert.All(legends, group => {
            Assert.Equal("five-number-summary", group.Metadata["data-cfx-derived"]);
            Assert.False(group.Metadata.ContainsKey("data-cfx-observation-id"));
            Assert.False(group.Metadata.ContainsKey("data-cfx-source-point"));
        });
    }

    [Fact]
    public void EmptyInliersRemainAbsentAndExplicitValueBoundsKeepOutliersClipped() {
        var series = ChartSeries.CreateBoxPlot(new ChartSeriesId("none"), "None", new[] { new ChartBoxPlotSampleGroup(1, new[] { 0d, 10 }) })
            .ConfigureBoxPlotStatistics(options => { options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr; options.IqrMultiplier = double.Epsilon; });
        var chart = Chart.Create().WithLegend(false).WithHeader(false).AddSeries(series);
        chart.Options.YAxis.WithBounds(2, 8);
        var prepared = chart.Prepare(new VisualRenderContext()); var document = XDocument.Parse(prepared.ToSvg());
        var summary = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.ContainsKey("data-cfx-derived") && group.Role == "point");
        Assert.False(summary.Metadata.ContainsKey("data-cfx-inlier-minimum")); Assert.False(summary.Metadata.ContainsKey("data-cfx-inlier-maximum"));
        Assert.Equal("2", summary.Metadata["data-cfx-outlier-count"]);
        var outliers = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "boxplot-outlier").ToArray();
        Assert.Equal(2, outliers.Length);
        Assert.All(outliers, element => Assert.Contains(element.Ancestors(), ancestor => ancestor.Attribute("clip-path") != null));
    }

    [Fact]
    public void VerticalSecondaryProjectionIncludesSourceExtremaAndHorizontalRestrictionsStayExplicit() {
        var chart = Create(800, VisualThemeMode.Light);
        chart.Series[0].ValueAxis = ChartAxisSide.Secondary;
        chart.AddLine("Primary", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) });
        var prepared = chart.Prepare(new VisualRenderContext());
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point" && group.Metadata.TryGetValue("data-cfx-kind", out var kind) && kind == "BoxPlotOutlier");
        Assert.All(points, group => Assert.Equal("secondary", group.Metadata["data-cfx-axis"]));
        chart.ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal);
        Assert.Throws<NotSupportedException>(() => chart.Prepare(new VisualRenderContext()));
    }

    internal static Chart Create(int width, VisualThemeMode mode) {
        var series = ChartSeries.CreateBoxPlot(new ChartSeriesId("raw-distribution"), "Latency", new[] {
            new ChartBoxPlotSampleGroup(1, new[] { 0d, 1, 2, 3, 4, 5, 6, 7, 8, 100 }),
            new ChartBoxPlotSampleGroup(2, new[] { 0d, 100, 101, 102 })
        }).ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr)
            .WithBoxPlotGroupIds(new[] { new ChartObservationId("group-\"a"), new ChartObservationId("group-b") })
            .WithObservationIds(Enumerable.Range(0, 14).Select(index => new ChartObservationId("sample-" + index)));
        return Chart.Create().WithSize(width, 420).WithTitle("Measurement distributions").WithXLabels("Service A", "Service B")
            .WithTheme(VisualTheme.Graphite(), mode).WithDataLabels(false).WithLegend(false).AddSeries(series);
    }

    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BOXPLOT_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
    }
}
