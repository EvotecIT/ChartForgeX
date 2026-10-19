using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Exercises grouped category facts through the completed native scene consumed by both exporters.</summary>
public sealed class GroupedCategoryAxisLayoutTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteIrregularCatalogRetainsMissingLeavesAndOriginalObservationBindings(bool horizontal) {
        var chart = CatalogChart(horizontal);
        var source = chart.Series[0].Points.ToArray();
        var prepared = Prepare(chart);
        var plot = Plot(prepared);
        var projection = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        var categoryAxis = horizontal ? projection.Y! : projection.X;
        Assert.True(categoryAxis.Minimum <= .25); Assert.True(categoryAxis.Maximum >= 14.75);
        var leafRole = horizontal ? "axis-y-label" : "axis-x-label";
        var leaves = prepared.Regions.Where(region => region.Role == leafRole).ToArray();
        Assert.Equal(6, leaves.Length);
        Assert.Contains(leaves, leaf => leaf.Label == "Missing first (0.25)");
        Assert.Contains(leaves, leaf => leaf.Label == "Missing middle (3.25)");
        Assert.Contains(leaves, leaf => leaf.Label == "Missing last (14.75)");
        var bindings = Assert.IsAssignableFrom<IReadOnlyList<ChartAxisLabel>>(projection.CategoryLabels);
        Assert.Equal(new[] { 1.5, 7.5, 11d }, bindings.Select(label => label.Value));
        Assert.Equal(new[] { "2026", "Q1" }, bindings[0].Groups);
        Assert.Equal(new[] { "2026", "Q2" }, bindings[1].Groups);
        Assert.Equal(new[] { "2027", "Q1" }, bindings[2].Groups);
        Assert.Equal(source, chart.Series[0].Points);
        Assert.Equal(3, chart.Series[0].SourcePointCount);
        var groups = Groups(prepared);
        Assert.Equal(5, groups.Length);
        Assert.All(groups, group => {
            foreach (var key in new[] { "data-cfx-series", "data-cfx-point", "data-cfx-target-id", "data-cfx-target-kind", "data-cfx-label" })
                Assert.False(group.Metadata.ContainsKey(key));
            Assert.True(double.IsFinite(Parse(group.Metadata["data-cfx-group-start"])));
            Assert.True(double.IsFinite(Parse(group.Metadata["data-cfx-group-end"])));
        });
        var captions = ParentCaptions(prepared);
        Assert.Equal(5, captions.Length);
        Assert.All(captions, caption => Assert.Equal(12, caption.Text.Size));
        var nearest = Assert.Single(prepared.Regions, region => region.Label == "2026 / Q1");
        var outer = Assert.Single(prepared.Regions, region => region.Role == GroupRole(horizontal) && region.Label == "2026");
        Assert.True(horizontal ? nearest.Bounds.Left > outer.Bounds.Left : nearest.Bounds.Top < outer.Bounds.Top);
        Assert.All(captions, caption => {
            Assert.InRange(caption.X, 0, prepared.Size.Width);
            Assert.InRange(caption.Y, 0, prepared.Size.Height);
            Assert.True(horizontal ? caption.X + caption.Text.Metrics.Width <= plot.Left : caption.Y >= plot.Bottom);
        });
        var points = PointGroups(prepared);
        Assert.Equal(new[] { "1.5", "7.5", "11" }, points.Select(point => (string?)point.Attribute("data-cfx-x")));
        Assert.Contains("2026 / Q1 / Jan", (string?)points[0].Attribute("aria-label"));
        Assert.Contains("Observed", (string?)points[0].Attribute("aria-label"));
        Assert.Contains(prepared.Regions, region => region.Role == "point" && region.Label!.Contains("2026 / Q1 / Jan", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeWindowClipsBeforeProjectionAndRetainsFullSourceSpansUnderReversal(bool reversed) {
        var chart = Bare().AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(4, 5), new ChartPoint(8, 3), new ChartPoint(12, 7) });
        chart.Series[0].Markers.Enabled = false;
        chart.WithXLabels(Enumerable.Range(0, 7).Select(index => new ChartAxisLabel(index * 2, "Leaf " + index,
            "Year", index <= 3 ? "First" : "Second")));
        chart.Options.XAxis.Reversed = reversed;
        var full = Prepare(chart);
        var view = chart.Prepare(VisualExportRequest.ForChart(chart).Context, new ChartXViewport(3, 7));
        var fullGroups = Groups(full); var viewGroups = Groups(view);
        Assert.Equal(fullGroups.Length, viewGroups.Length);
        for (var index = 0; index < fullGroups.Length; index++) Assert.Equal(fullGroups[index].Metadata, viewGroups[index].Metadata);
        var plot = Plot(view);
        var first = Assert.Single(view.Regions, region => region.Label == "Year / First");
        Assert.Equal(plot.Left, first.Bounds.Left, 8); Assert.Equal(plot.Right, first.Bounds.Right, 8);
        var outside = Assert.Single(view.Regions, region => region.Label == "Year / Second");
        Assert.Equal(0, outside.Bounds.Width); Assert.Equal(0, outside.Bounds.Height);
        var captions = ParentCaptions(view);
        Assert.Equal(2, captions.Length);
        Assert.All(captions, caption => Assert.Equal(plot.Left + plot.Width / 2, caption.X + caption.Text.Metrics.Width / 2, 8));
        Assert.Equal(new[] { "0", "4", "8", "12" }, PointGroups(view).Select(point => (string?)point.Attribute("data-cfx-x")));
        Assert.Equal(4, chart.Series[0].SourcePointCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitBoundsDeliberatelyClipMissingEdgeLeavesWithoutChangingMembership(bool horizontal) {
        var chart = CatalogChart(horizontal);
        if (horizontal) chart.WithYAxisBounds(1, 12); else chart.WithXAxisBounds(1, 12);
        var prepared = Prepare(chart);
        var projection = prepared.Scene.DataProjection!;
        Assert.Equal(1, horizontal ? projection.Y!.Minimum : projection.X.Minimum);
        Assert.Equal(12, horizontal ? projection.Y!.Maximum : projection.X.Maximum);
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == (horizontal ? "axis-y-label" : "axis-x-label")));
        Assert.Equal(5, Groups(prepared).Length);
        Assert.Contains(Groups(prepared), group => group.Metadata["data-cfx-group-minimum"] == ".25" || group.Metadata["data-cfx-group-minimum"] == "0.25");
        Assert.Equal(3, projection.CategoryLabels!.Count);
    }

    [Theory]
    [InlineData(false, "axes")]
    [InlineData(false, "axis")]
    [InlineData(false, "line")]
    [InlineData(true, "axes")]
    [InlineData(true, "axis")]
    [InlineData(true, "line")]
    public void VisibilityAndLinePoliciesKeepPassiveFactsAndRemoveOnlyTheirPaint(bool horizontal, string policy) {
        var grouped = CatalogChart(horizontal);
        var flat = CatalogChart(horizontal);
        var axis = horizontal ? grouped.Options.YAxis : grouped.Options.XAxis;
        var flatAxis = horizontal ? flat.Options.YAxis : flat.Options.XAxis;
        var flatLabels = flatAxis.Labels.Select(label => new ChartAxisLabel(label.Value, label.Text)).ToArray();
        flatAxis.Labels.Clear(); flatAxis.Labels.AddRange(flatLabels);
        // Match the explicitly chosen domain so automatic grouped catalog inclusion is not part of this comparison.
        if (horizontal) { grouped.WithYAxisBounds(0, 16); flat.WithYAxisBounds(0, 16); }
        else { grouped.WithXAxisBounds(0, 16); flat.WithXAxisBounds(0, 16); }
        if (policy == "axes") { grouped.WithAxes(false); flat.WithAxes(false); }
        else if (policy == "axis") { axis.Visible = false; flatAxis.Visible = false; }
        else { axis.ShowLine = false; flatAxis.ShowLine = false; }
        var prepared = Prepare(grouped);
        Assert.Equal(5, Groups(prepared).Length);
        Assert.Equal(5, prepared.Regions.Count(region => region.Role == GroupRole(horizontal)));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "axis-category-group-rule");
        if (policy == "line") Assert.NotEmpty(ParentCaptions(prepared));
        else {
            Assert.Empty(ParentCaptions(prepared));
            Assert.Equal(Plot(Prepare(flat)), Plot(prepared));
        }
    }

    [Fact]
    public void TinyBandsOmitOrdinaryCaptionsWithoutLosingFullPathsOrConsumingThePlot() {
        var chart = CatalogChart(false).WithSize(150, 130).ConfigureTickLabelStyle(style => style.FontSize = 18);
        var prepared = Prepare(chart);
        Assert.True(Plot(prepared).Width > 0); Assert.True(Plot(prepared).Height > 0);
        Assert.Equal(5, Groups(prepared).Length);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.category-group-overflow");
        Assert.All(ParentCaptions(prepared), caption => Assert.Equal(18, caption.Text.Size));
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-category-group" && region.Label == "2026 / Q1");
        Assert.All(prepared.Regions.Where(region => region.Role == "axis-x-category-group"), region => {
            Assert.True(double.IsFinite(region.Bounds.Width)); Assert.True(double.IsFinite(region.Bounds.Height));
        });
    }

    [Fact]
    public void SingletonAndMarkerPaddingRetainSourceCellsIndependentOfOutputSize() {
        var singleton = Bare().AddBar("Observed", new[] { new ChartPoint(2.5, 4) })
            .WithXLabels(new[] { new ChartAxisLabel(2.5, "Only leaf", "Parent") });
        var one = Prepare(singleton);
        var group = Assert.Single(Groups(one));
        var range = ChartRange.FromChart(singleton);
        Assert.Equal(range.MinX, Parse(group.Metadata["data-cfx-group-start"]));
        Assert.Equal(range.MaxX, Parse(group.Metadata["data-cfx-group-end"]));
        Assert.Single(ParentCaptions(one));
        var line = Bare().AddLine("Observed", new[] { new ChartPoint(1.5, 2), new ChartPoint(7.5, 3) })
            .WithXLabels(new[] { new ChartAxisLabel(1.5, "First", "Parent"), new ChartAxisLabel(7.5, "Last", "Parent") });
        line.Series[0].ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 16; });
        var wide = Prepare(line); line.WithSize(390, 450); var compact = Prepare(line);
        var wideGroup = Assert.Single(Groups(wide)); var compactGroup = Assert.Single(Groups(compact));
        Assert.Equal("1.5", wideGroup.Metadata["data-cfx-group-start"]); Assert.Equal("7.5", wideGroup.Metadata["data-cfx-group-end"]);
        Assert.Equal(wideGroup.Metadata, compactGroup.Metadata);
        Assert.True(wide.XProjection!.FullMinimum < 1.5); Assert.True(compact.XProjection!.FullMinimum < 1.5);
    }

    [Fact]
    public void PreparedParentInkAndContextStayDetachedForSvgAndNativeRaster() {
        var chart = CatalogChart(false).ConfigureTickLabelStyle(style => style.Color = new ChartColor(190, 20, 210));
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var image = prepared.ToRgba();
        var ink = 0;
        foreach (var region in prepared.Regions.Where(region => region.Role == "axis-x-category-group"))
            for (var y = Math.Max(0, (int)Math.Floor(region.Bounds.Top)); y < Math.Min(image.Height, (int)Math.Ceiling(region.Bounds.Bottom)); y++)
                for (var x = Math.Max(0, (int)Math.Floor(region.Bounds.Left)); x < Math.Min(image.Width, (int)Math.Ceiling(region.Bounds.Right)); x++) {
                    var offset = (y * image.Width + x) * 4;
                    if (image.Pixels[offset] > 100 && image.Pixels[offset + 1] < 90 && image.Pixels[offset + 2] > 100) ink++;
                }
        Assert.True(ink > 20);
        chart.Options.XAxis.Labels.Clear(); chart.Options.XAxis.Labels.Add(new ChartAxisLabel(1.5, "Changed", "Changed parent"));
        chart.Series[0].Points.Clear(); chart.Options.TickLabelStyle.Color = new ChartColor(0, 0, 0);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(new[] { "2026", "Q1" }, prepared.Scene.DataProjection!.CategoryLabels![0].Groups);
        Assert.Contains("data-cfx-group-0=\"2026\"", svg);
        Assert.DoesNotContain("Changed parent", svg);
    }

    private static Chart CatalogChart(bool horizontal) {
        var chart = Bare();
        var points = new[] { new ChartPoint(1.5, 10), new ChartPoint(7.5, 20), new ChartPoint(11, 15) };
        if (horizontal) chart.AddHorizontalBar("Observed", points); else chart.AddBar("Observed", points);
        var labels = new[] {
            new ChartAxisLabel(.25, "Missing first", "2026", "Q1"), new ChartAxisLabel(1.5, "Jan", "2026", "Q1"),
            new ChartAxisLabel(3.25, "Missing middle", "2026", "Q1"), new ChartAxisLabel(7.5, "Apr", "2026", "Q2"),
            new ChartAxisLabel(11, "Jan", "2027", "Q1"), new ChartAxisLabel(14.75, "Missing last", "2027", "Q1")
        };
        if (horizontal) chart.ConfigureYAxis(axis => { axis.Labels.AddRange(labels); axis.LabelDensity = ChartLabelDensity.All; });
        else { chart.WithXLabels(labels); chart.Options.XAxis.LabelDensity = ChartLabelDensity.All; }
        return chart;
    }

    private static Chart Bare() => Chart.Create().WithSize(960, 520).WithHeader(false).WithLegend(false).WithDataLabels(false)
        .WithTheme(VisualTheme.Graphite(), VisualThemeMode.Light).ConfigureTickLabelStyle(style => style.FontSize = 12);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static ChartRect Plot(PreparedVisual prepared) => Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
    private static string GroupRole(bool horizontal) => horizontal ? "axis-y-category-group" : "axis-x-category-group";
    private static VisualSceneGroup[] Groups(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>()
        .Where(group => group.Role is "axis-x-category-group" or "axis-y-category-group").ToArray();
    private static VisualSceneText[] ParentCaptions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role is "axis-x-category-group-label" or "axis-y-category-group-label").ToArray();
    private static XElement[] PointGroups(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
    private static double Parse(string value) => double.Parse(value, CultureInfo.InvariantCulture);
}
