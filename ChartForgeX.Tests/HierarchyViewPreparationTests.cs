using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HierarchyViewPreparationTests {
    internal static Chart Source(ChartSeriesKind kind, bool dark = false) {
        var chart = Chart.Create().WithSize(720, 460).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Allocation").WithLegend(false).WithDataLabels(false);
        var items = new[] {
            new ChartHierarchyItem("root", "All work", value: kind == ChartSeriesKind.Sunburst ? 120 : null, colorValue: 0),
            new ChartHierarchyItem("services", "Services", "root", kind == ChartSeriesKind.Sunburst ? 80 : null, 2),
            new ChartHierarchyItem("api", "API", "services", 30, 4),
            new ChartHierarchyItem("worker", "Worker", "services", 20, 6),
            new ChartHierarchyItem("other", "Other", "root", 40, 10),
            new ChartHierarchyItem("zero", "Reserve", "services", 0, 8)
        };
        if (kind == ChartSeriesKind.Sunburst) {
            chart.ConfigureSunburst(options => options.ParentValuePolicy = ChartHierarchyValuePolicy.AuthoredTotal);
            chart.AddSeries(ChartSeries.CreateSunburst(new ChartSeriesId("allocation"), "Allocation", items));
        } else chart.AddSeries(ChartSeries.CreateTreemap(new ChartSeriesId("allocation"), "Allocation", items));
        chart.Series[0].WithNodeState("api", ChartSeriesState.Warning).WithPointColor(3, ChartColor.FromHex("#7c3aed"));
        return chart;
    }
    private static VisualRenderContext Context(bool dark = false, bool showLegend = false) => new(new VisualLayoutOptions(new VisualSize(720, 460)),
        VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light, new VisualFrame(showLegend: showLegend));
    private static XElement Node(PreparedVisual prepared, string id) => Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), node => (string?)node.Attribute("data-cfx-node") == id);
    private static double Number(XElement node, string attribute) => double.Parse(node.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(ChartSeriesKind.Treemap)]
    [InlineData(ChartSeriesKind.Sunburst)]
    public void FullAndResetPreparationPreserveOriginalNativeBytesAndSourceIdentities(ChartSeriesKind kind) {
        var chart = Source(kind); var context = Context();
        var original = chart.Prepare(context); var reset = chart.PrepareHierarchy(context, null);
        Assert.Equal(original.ToSvg(), reset.ToSvg()); Assert.Equal(original.ToPng(), reset.ToPng());
        var facts = chart.Series[0].HierarchyItems; var relationship = chart.Series[0].Relationships;
        var child = chart.PrepareHierarchy(context, new ChartHierarchyView("services"));
        Assert.Equal("services", child.HierarchyView!.RootId);
        Assert.Same(facts, chart.Series[0].HierarchyItems); Assert.Same(relationship, chart.Series[0].Relationships);
        Assert.Equal("root", (string?)Node(child, "services").Attribute("data-cfx-parent"));
        Assert.Equal("1", (string?)Node(child, "services").Attribute("data-cfx-depth"));
        Assert.Equal("0", (string?)Node(child, "services").Attribute("data-cfx-view-depth"));
        foreach (var id in new[] { "root", "other" }) {
            var outside = Node(child, id);
            Assert.Equal("outside-view", (string?)outside.Attribute("data-cfx-geometry-status"));
            Assert.Equal("true", (string?)outside.Attribute("data-cfx-passive")); Assert.Empty(outside.Elements());
        }
        Assert.Equal("zero", (string?)Node(child, "zero").Attribute("data-cfx-geometry-status"));
        Assert.Equal(original.ToSvg(), chart.PrepareHierarchy(context, null).ToSvg());
        var saved = child.ToSvg(); var png = child.ToPng();
        chart.Series[0].ReplaceHierarchyItems(new[] { new ChartHierarchyItem("replacement", "Replacement", value: 1) });
        Assert.Equal(saved, child.ToSvg()); Assert.Equal(png, child.ToPng());
        Assert.Throws<ArgumentException>(() => chart.PrepareHierarchy(context, new ChartHierarchyView("services")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SunburstSubtreeFillsTheCircleWithoutNormalizingAwayAuthoredRemainders(bool dark) {
        var chart = Source(ChartSeriesKind.Sunburst, dark);
        var calls = new List<string>();
        chart.ConfigureSunburst(options => options.SecondaryLabelFormatter = item => { calls.Add(item.Item.Id); return item.Item.Label; });
        var prepared = chart.PrepareHierarchy(Context(dark), new ChartHierarchyView("services"));
        Assert.Equal(chart.Series[0].HierarchyItems.Select(item => item.Id).OrderBy(id => id), calls.OrderBy(id => id));
        var root = Node(prepared, "services"); var api = Node(prepared, "api"); var worker = Node(prepared, "worker");
        Assert.Equal(Math.PI * 2, Number(root, "data-cfx-sweep"), 10);
        Assert.Equal(0, Number(root, "data-cfx-inner-radius")); Assert.Equal(30, Number(root, "data-cfx-remainder-value"));
        Assert.Equal(30d / 80, Number(api, "data-cfx-sweep") / (Math.PI * 2), 10);
        Assert.Equal(20d / 80, Number(worker, "data-cfx-sweep") / (Math.PI * 2), 10);
        Assert.Equal(Number(root, "data-cfx-outer-radius"), Number(api, "data-cfx-inner-radius"), 10);
        Assert.Equal("api", (string?)api.Attribute("data-cfx-branch-root"));
        Assert.Equal("Warning", (string?)api.Attribute("data-cfx-state"));
        var leaf = chart.PrepareHierarchy(Context(dark), new ChartHierarchyView("api"));
        Assert.Equal(Math.PI * 2, Number(Node(leaf, "api"), "data-cfx-sweep"), 10);
        var empty = chart.PrepareHierarchy(Context(dark), new ChartHierarchyView("zero"));
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.no-data");
        Assert.Empty(Node(empty, "zero").Elements());
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void TreemapSubtreeUsesItsPlotAndPreservesSourcePaintAndLeafRatios() {
        var chart = Source(ChartSeriesKind.Treemap);
        chart.ConfigureTreemap(options => { options.GroupPadding = 0; options.Gap = 0; options.ShowGroupLabels = false; });
        var context = Context(); var full = chart.Prepare(context);
        var child = chart.PrepareHierarchy(context, new ChartHierarchyView("services"));
        var api = child.Regions.Single(region => region.Id == "series-0-node-api").Bounds;
        var worker = child.Regions.Single(region => region.Id == "series-0-node-worker").Bounds;
        Assert.Equal(1.5, api.Width * api.Height / (worker.Width * worker.Height), 10);
        var before = full.Regions.Single(region => region.Id == "series-0-node-services").Bounds;
        var after = child.Regions.Single(region => region.Id == "series-0-node-services").Bounds;
        Assert.True(after.Width * after.Height > before.Width * before.Height);
        var oldPaint = Node(full, "worker").Elements().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-tile-mark").Attribute("fill")!.Value;
        Assert.Equal(oldPaint, Node(child, "worker").Elements().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-tile-mark").Attribute("fill")!.Value);
        var zero = chart.PrepareHierarchy(context, new ChartHierarchyView("zero"));
        Assert.Contains(zero.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.no-data");
        Assert.Equal("zero", (string?)Node(zero, "zero").Attribute("data-cfx-geometry-status"));
    }

    [Fact]
    public void TreemapForestViewDoesNotRequireOrInventAConnectedSourceRoot() {
        var chart = Chart.Create().WithDataLabels(false).WithLegend(false).AddTreemap("Forest", new[] {
            new ChartHierarchyItem("team", "Team"), new ChartHierarchyItem("a", "A", "team", 4),
            new ChartHierarchyItem("b", "B", "team", 6), new ChartHierarchyItem("other-root", "Other", value: 8)
        });
        var full = chart.PrepareHierarchy(Context()); var selected = chart.PrepareHierarchy(Context(), new ChartHierarchyView("team"));
        Assert.Null(Node(selected, "team").Attribute("data-cfx-parent"));
        Assert.Equal("outside-view", (string?)Node(selected, "other-root").Attribute("data-cfx-geometry-status"));
        Assert.Equal(4, XDocument.Parse(selected.ToSvg()).Descendants().Count(node => node.Attribute("data-cfx-node") != null));
        Assert.Equal(full.ToSvg(), chart.PrepareHierarchy(Context(), null).ToSvg());
    }

    [Fact]
    public void LeafAggregateViewRetainsAuthoredParentFactsWhileUsingOnlyDescendantSizes() {
        var chart = Source(ChartSeriesKind.Sunburst).ConfigureSunburst(options => options.ParentValuePolicy = ChartHierarchyValuePolicy.LeafAggregate);
        var selected = chart.PrepareHierarchy(Context(), new ChartHierarchyView("services"));
        Assert.Equal(50, Number(Node(selected, "services"), "data-cfx-value"));
        Assert.Equal(80, Number(Node(selected, "services"), "data-cfx-authored-value"));
        Assert.Equal(0, Number(Node(selected, "services"), "data-cfx-remainder-value"));
        Assert.Equal(.6, Number(Node(selected, "api"), "data-cfx-sweep") / (Math.PI * 2), 10);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Treemap)]
    [InlineData(ChartSeriesKind.Sunburst)]
    public void NumericColorDomainAndSourcePaintDoNotRebaseWithSubtreeGeometry(ChartSeriesKind kind) {
        var chart = Source(kind); var ramp = ChartColorScale.Sequential(ChartColor.FromHex("#ff0000"), ChartColor.FromHex("#0000ff"));
        if (kind == ChartSeriesKind.Treemap) chart.ConfigureTreemap(options => { options.ColorScale = ramp; options.ColorDimension.ShowLegend = true; });
        else chart.ConfigureSunburst(options => { options.ColorScale = ramp; options.ColorDimension.ShowLegend = true; });
        var full = chart.Prepare(Context(showLegend: true)); var child = chart.PrepareHierarchy(Context(showLegend: true), new ChartHierarchyView("services"));
        var role = kind == ChartSeriesKind.Treemap ? "treemap-color-scale" : "sunburst-color-scale";
        var first = Assert.Single(XDocument.Parse(full.ToSvg()).Descendants(), node => (string?)node.Attribute("data-cfx-role") == role);
        var next = Assert.Single(XDocument.Parse(child.ToSvg()).Descendants(), node => (string?)node.Attribute("data-cfx-role") == role);
        Assert.Equal("0", (string?)next.Attribute("data-cfx-min-value")); Assert.Equal("10", (string?)next.Attribute("data-cfx-max-value"));
        Assert.Equal((string?)first.Attribute("data-cfx-min-value"), (string?)next.Attribute("data-cfx-min-value"));
        Assert.Equal((string?)first.Attribute("data-cfx-max-value"), (string?)next.Attribute("data-cfx-max-value"));
        var mark = kind == ChartSeriesKind.Treemap ? "treemap-tile-mark" : "sunburst-segment-mark";
        Assert.Equal((string?)Node(full, "api").Elements().Single(node => (string?)node.Attribute("data-cfx-role") == mark).Attribute("fill"),
            (string?)Node(child, "api").Elements().Single(node => (string?)node.Attribute("data-cfx-role") == mark).Attribute("fill"));
    }

    [Fact]
    public void SunburstCategoricalPaintUsesSourceDepthWhileTheGeometryUsesViewDepth() {
        var chart = Source(ChartSeriesKind.Sunburst);
        chart.Series[0].ReplaceHierarchyItems(chart.Series[0].HierarchyItems.Select(item => new ChartHierarchyItem(item.Id, item.Label, item.ParentId, item.Value)));
        var full = chart.Prepare(Context()); var child = chart.PrepareHierarchy(Context(), new ChartHierarchyView("services"));
        foreach (var id in new[] { "services", "api", "worker" }) {
            string? Paint(PreparedVisual prepared) => (string?)Assert.Single(Node(prepared, id).Elements(),
                node => (string?)node.Attribute("data-cfx-role") == "sunburst-segment-mark").Attribute("fill");
            Assert.Equal(Paint(full), Paint(child));
        }
        Assert.Equal("0", (string?)Node(child, "services").Attribute("data-cfx-view-depth"));
        Assert.Equal("1", (string?)Node(child, "services").Attribute("data-cfx-depth"));
    }

    [Fact]
    public void PointLegendsShowOnlyVisibleSubtreeLeavesWithOriginalTargetIdentity() {
        var chart = Source(ChartSeriesKind.Treemap).WithPointLegend().WithLegend()
            .ConfigureTreemap(options => options.ColorDimension.ShowLegend = false);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720, 460)), frame: new VisualFrame(showLegend: true));
        var full = XDocument.Parse(chart.Prepare(context).ToSvg());
        var child = XDocument.Parse(chart.PrepareHierarchy(context, new ChartHierarchyView("services")).ToSvg());
        string[] Targets(XDocument xml) => xml.Descendants().Where(node => node.Attribute("data-cfx-legend-target-id") != null)
            .Select(node => node.Attribute("data-cfx-legend-target-id")!.Value).ToArray();
        Assert.Contains("other", Targets(full)); Assert.DoesNotContain("other", Targets(child));
        Assert.Equal(new[] { "api", "worker", "zero" }, Targets(child));
    }

    [Fact]
    public void RootValidationIsExactAndCannotMutateTheSourceOrAcceptOtherFamilies() {
        var chart = Source(ChartSeriesKind.Treemap); var original = chart.ToSvg();
        Assert.Throws<ArgumentException>(() => new ChartHierarchyView(" "));
        Assert.Throws<ArgumentException>(() => chart.PrepareHierarchy(Context(), new ChartHierarchyView("Services")));
        Assert.Throws<NotSupportedException>(() => Chart.Create().AddLine("Readings", ChartPoints.FromValues(1, 2)).PrepareHierarchy(Context(), new ChartHierarchyView("services")));
        Assert.Equal(original, chart.ToSvg());
    }

    [Fact]
    public void MatchingAdaptiveSectionsCaptureOneRootAndLocalizableOptInControls() {
        var chart = Source(ChartSeriesKind.Treemap); var renderer = new HtmlInteractiveChartRenderer();
        var section = renderer.PrepareHierarchySection(chart, new ChartHierarchyView("services"), options => {
            options.Interaction.Enable(ChartInteractionFeatures.HierarchyNavigation);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Localization[HtmlChartText.HierarchyUp] = "Parent allocation";
        });
        Assert.Equal("services", section.PreparedVisual.HierarchyView!.RootId);
        Assert.Equal("services", section.NarrowPreparedVisual!.HierarchyView!.RootId);
        Assert.Contains("Parent allocation", section.Html); Assert.Contains("data-cfx-hierarchy-control=\"up\"", section.Html);
        Assert.DoesNotContain("data-cfx-hierarchy-control", renderer.PrepareHierarchySection(chart).Html);
    }
}
