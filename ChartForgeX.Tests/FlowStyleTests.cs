using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FlowStyleTests {
    [Theory]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    public void FlowOverridesUseExistingCaseSensitiveIdsAndReadOnlyLiveViews(ChartSeriesKind kind) {
        var series = Parallel(kind).Series[0];
        var styles = series.FlowStyles; var states = series.FlowStates;
        Assert.Empty(styles); Assert.Empty(states);
        var style = new ChartFlowStyle(fill: ChartColor.Black);
        Assert.Same(series, series.WithFlowStyle("Priority", style).WithFlowState("Priority", ChartSeriesState.None));
        Assert.Equal(style, styles["Priority"]); Assert.Equal(ChartSeriesState.None, states["Priority"]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, ChartFlowStyle>)styles).Clear());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, ChartSeriesState>)states)["standard"] = ChartSeriesState.Warning);
        foreach (var id in new[] { "", " ", "PRIORITY", "a", "Support", "0" }) {
            Assert.Throws<ArgumentException>(() => series.WithFlowStyle(id, style));
            Assert.Throws<ArgumentException>(() => series.WithFlowState(id, ChartSeriesState.Warning));
            Assert.Throws<ArgumentException>(() => series.WithFlowStyle(id, null));
            Assert.Throws<ArgumentException>(() => series.WithFlowState(id, null));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => series.WithFlowState("Priority", (ChartSeriesState)99));
        Assert.Equal(ChartSeriesState.None, states["Priority"]);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFlowStyle(fillPattern: (ChartFillPattern)99));
        series.WithFlowStyle("Priority", null).WithFlowState("Priority", null).WithFlowStyle("Priority", null).WithFlowState("Priority", null);
        Assert.Empty(styles); Assert.Empty(states);
        var line = Chart.Create().AddLine("Line", new[] { new ChartPoint(1, 2) }).Series[0];
        Assert.Empty(line.FlowStyles); Assert.Empty(line.FlowStates);
        Assert.Throws<InvalidOperationException>(() => line.WithFlowStyle("Priority", null));
        Assert.Throws<InvalidOperationException>(() => line.WithFlowState("Priority", null));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FlowPaintRejectsInvalidOpacityAndDimensions(double invalid) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFlowStyle(fillOpacity: invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFlowStyle(strokeOpacity: invalid));
        if (invalid < 0 || !double.IsFinite(invalid)) Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFlowStyle(strokeWidth: invalid));
        var boundary = new ChartFlowStyle(fillOpacity: 0, strokeWidth: 0, strokeOpacity: 1);
        Assert.Equal(0d, boundary.FillOpacity); Assert.Equal(0d, boundary.StrokeWidth); Assert.Equal(1d, boundary.StrokeOpacity);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    public void ParallelFlowPaintDoesNotChangeNativeGeometryOrRawFacts(ChartSeriesKind kind) {
        var chart = Parallel(kind); var series = chart.Series[0]; var facts = series.FlowLinks.ToArray();
        var before = Prepare(chart); var beforeSvg = before.ToSvg();
        series.WithFlowStyle("Priority", default(ChartFlowStyle));
        Assert.Equal(beforeSvg, Prepare(chart).ToSvg());
        var fill = ChartColor.FromRgba(41, 94, 131, 200);
        series.WithFlowStyle("Priority", new ChartFlowStyle(fill: fill, fillOpacity: .65, stroke: ChartColor.Black, strokeWidth: 2, strokeOpacity: .5));
        var after = Prepare(chart); var oldRibbons = Ribbons(before, kind); var newRibbons = Ribbons(after, kind);
        Assert.Equal(facts, series.FlowLinks);
        Assert.Equal(oldRibbons[0].Fill, newRibbons[0].Fill);
        Assert.Equal(ChartColorMath.WithOpacity(fill, .65), newRibbons[1].Fill);
        Assert.Equal(ChartColorMath.WithOpacity(ChartColor.Black, .5), newRibbons[1].Stroke);
        Assert.Equal(2, newRibbons[1].StrokeWidth);
        for (var i = 0; i < oldRibbons.Length; i++) Assert.Equal(oldRibbons[i].Commands, newRibbons[i].Commands);
        Assert.Equal(before.Regions.Select(region => region.Bounds), after.Regions.Select(region => region.Bounds));
        foreach (var id in new[] { "standard", "Priority" }) {
            var oldLink = Link(before, id); var newLink = Link(after, id);
            foreach (var name in new[] { "value", "source", "target", "source-link-index", "width", "source-start-angle", "source-sweep", "target-start-angle", "target-sweep" })
                Assert.Equal((string?)oldLink.Attribute("data-cfx-" + name), (string?)newLink.Attribute("data-cfx-" + name));
        }
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    public void SourceSemanticStateIsPublishedAndFlowNoneSuppressesInheritanceUntilCleared(ChartSeriesKind kind) {
        var chart = Parallel(kind); var series = chart.Series[0]; var colors = Context().Theme.Resolve(VisualThemeMode.Light);
        series.StateRole = ChartSeriesState.Info; series.WithNodeState("a", ChartSeriesState.Warning);
        var inherited = Prepare(chart);
        Assert.All(new[] { "standard", "Priority" }, id => Assert.Equal("Warning", (string?)Link(inherited, id).Attribute("data-cfx-state")));
        Assert.All(Ribbons(inherited, kind), ribbon => Assert.Equal(ChartColorMath.WithOpacity(colors.Status.Medium.Fill, .35), ribbon.Fill));
        series.WithFlowState("standard", ChartSeriesState.Danger).WithFlowState("Priority", ChartSeriesState.None);
        var overridden = Prepare(chart);
        Assert.Equal("Danger", (string?)Link(overridden, "standard").Attribute("data-cfx-state"));
        Assert.Equal("None", (string?)Link(overridden, "Priority").Attribute("data-cfx-state"));
        Assert.Equal(ChartColorMath.WithOpacity(colors.Status.Critical.Fill, .35), Ribbons(overridden, kind)[0].Fill);
        Assert.Equal(ChartColorMath.WithOpacity(colors.Palette[0], .35), Ribbons(overridden, kind)[1].Fill);
        series.WithFlowState("standard", null).WithFlowState("Priority", null);
        Assert.Equal(inherited.ToSvg(), Prepare(chart).ToSvg());
        series.WithNodeState("a", ChartSeriesState.None);
        Assert.Equal("None", (string?)Link(Prepare(chart), "standard").Attribute("data-cfx-state"));
        Assert.Equal(ChartColorMath.WithOpacity(colors.Palette[0], .35), Ribbons(Prepare(chart), kind)[0].Fill);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    public void ExplicitFlowSourceFamilyAndSeriesColorsPrecedeSemanticPaintAndKeepProvenance(ChartSeriesKind kind) {
        var chart = Parallel(kind); var series = chart.Series[0]; var context = Context();
        var critical = context.Theme.Resolve(context.ThemeMode).Status.Critical.Fill;
        var seriesColor = ChartColor.FromHex("#365F63"); var familyColor = ChartColor.FromHex("#607DAA"); var pointColor = ChartColor.FromHex("#7356BD");
        series.StateRole = ChartSeriesState.Warning; series.WithNodeState("a", ChartSeriesState.Info).WithFlowState("standard", ChartSeriesState.Danger);
        void Expect(ChartColor color) => Assert.Equal(ChartColorMath.WithOpacity(color, .35), Ribbons(Prepare(chart), kind)[0].Fill);
        Expect(critical);
        series.Color = seriesColor; Expect(seriesColor);
        if (kind == ChartSeriesKind.Sankey) { chart.Options.Sankey.RibbonFill = familyColor; Expect(familyColor); }
        series.WithPointColor(0, pointColor); Expect(pointColor);
        series.WithFlowStyle("standard", new ChartFlowStyle(fill: critical)); Expect(critical);
        Assert.Null(Ribbons(Prepare(chart), kind)[0].Stroke);
        var variables = new SvgColorVariables().Add("--flow-explicit", critical, SvgColorRole.Series).Add("--flow-state", critical, SvgColorRole.Status);
        string Fill() => Link(chart.Prepare(context), "standard", new VisualSvgOptions("flow", variables)).Descendants()
            .Single(node => (string?)node.Attribute("data-cfx-role") == Role(kind, "ribbon")).Attribute("fill")!.Value;
        Assert.Contains("var(--flow-explicit,", Fill(), StringComparison.Ordinal);
        Assert.DoesNotContain("var(--flow-state,", Fill(), StringComparison.Ordinal);
        series.WithFlowStyle("standard", new ChartFlowStyle(fill: critical, stroke: critical));
        var outline = Link(chart.Prepare(context), "standard", new VisualSvgOptions("flow", variables)).Descendants()
            .Single(node => (string?)node.Attribute("data-cfx-role") == Role(kind, "ribbon")).Attribute("stroke")!.Value;
        Assert.Contains("var(--flow-explicit,", outline, StringComparison.Ordinal);
        Assert.DoesNotContain("var(--flow-state,", outline, StringComparison.Ordinal);
        Assert.Equal("Danger", (string?)Link(Prepare(chart), "standard").Attribute("data-cfx-state"));
        series.WithFlowStyle("standard", null).WithPointColor(0, (ChartColor?)null); series.Color = null; chart.Options.Sankey.RibbonFill = null;
        Expect(critical);
        Assert.Contains("var(--flow-state,", Fill(), StringComparison.Ordinal);
        Assert.DoesNotContain("var(--flow-explicit,", Fill(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    public void PatternOpacityAndOutlineEnablingRetainFamilyDefaults(ChartSeriesKind kind) {
        var chart = Parallel(kind); var series = chart.Series[0]; var color = ChartColor.FromRgba(41, 94, 131, 200);
        series.WithPointColor(0, color).WithPointFillPattern(0, ChartFillPattern.Crosshatch);
        series.WithFlowStyle("standard", new ChartFlowStyle(fillOpacity: .2, strokeOpacity: .1))
            .WithFlowStyle("Priority", new ChartFlowStyle(fillPattern: ChartFillPattern.None));
        var prepared = Prepare(chart);
        Assert.Null(Ribbons(prepared, kind)[0].Stroke);
        Assert.Equal(ChartColorMath.WithOpacity(color, .2), Ribbons(prepared, kind)[0].Fill);
        if (kind == ChartSeriesKind.Chord) Assert.All(prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "chord-target-cue"), cue => Assert.Equal(color, cue.Fill));
        var patternLines = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == Role(kind, "ribbon-pattern")).ToArray();
        Assert.NotEmpty(patternLines);
        Assert.All(patternLines, line => Assert.Equal(ChartColorMath.WithOpacity(color, kind == ChartSeriesKind.Sankey ? .6 : .2), line.Stroke));
        Assert.DoesNotContain(Link(prepared, "Priority").Descendants(), node => (string?)node.Attribute("data-cfx-role") == Role(kind, "ribbon-pattern"));
        series.WithFlowStyle("standard", new ChartFlowStyle(strokeWidth: 2, strokeOpacity: .4));
        Assert.Equal(ChartColorMath.WithOpacity(color, .4), Ribbons(Prepare(chart), kind)[0].Stroke);
        Assert.Equal(2, Ribbons(Prepare(chart), kind)[0].StrokeWidth);
        series.WithFlowStyle("standard", new ChartFlowStyle(stroke: ChartColor.Black));
        Assert.Equal(ChartColor.Black, Ribbons(Prepare(chart), kind)[0].Stroke); Assert.Equal(1, Ribbons(Prepare(chart), kind)[0].StrokeWidth);
        series.WithFlowStyle("standard", new ChartFlowStyle(stroke: ChartColor.Black, strokeWidth: 0));
        Assert.Null(Ribbons(Prepare(chart), kind)[0].Stroke);
        series.WithFlowStyle("Priority", null);
        Assert.Contains(Link(Prepare(chart), "Priority").Descendants(), node => (string?)node.Attribute("data-cfx-role") == Role(kind, "ribbon-pattern"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)]
    public void PreparedSvgAndPngAreDetachedFromLaterFlowStyleAndStateEdits(ChartSeriesKind kind) {
        var chart = Parallel(kind); var series = chart.Series[0];
        series.WithFlowStyle("Priority", new ChartFlowStyle(fillOpacity: .65, fillPattern: ChartFillPattern.DiagonalForward, stroke: ChartColor.Black, strokeWidth: 2))
            .WithFlowState("Priority", ChartSeriesState.Danger);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        series.WithFlowStyle("Priority", null).WithFlowState("Priority", null)
            .WithFlowStyle("standard", new ChartFlowStyle(fill: ChartColor.Black)).WithFlowState("standard", ChartSeriesState.Warning);
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void ChordOverridesRetainReciprocalParallelSelfAndZeroIdentities() {
        var chart = Chart.Create().AddChord("Transfers", new[] { new ChartNode("a", "Support"), new ChartNode("b", "Support") }, new[] {
            new ChartFlowLink("priority", "a", "b", 7), new ChartFlowLink("Priority", "a", "b", 2),
            new ChartFlowLink("return", "b", "a", 3), new ChartFlowLink("self", "a", "a", 1), new ChartFlowLink("zero", "a", "b", 0)
        });
        var series = chart.Series[0]; var before = Prepare(chart);
        foreach (var flow in series.FlowLinks) series.WithFlowStyle(flow.Id, new ChartFlowStyle(fill: ChartColor.Black, strokeWidth: 1)).WithFlowState(flow.Id, ChartSeriesState.Warning);
        series.WithFlowStyle("Priority", new ChartFlowStyle(fill: ChartColor.White));
        var after = Prepare(chart);
        Assert.Equal(ChartColorMath.WithOpacity(ChartColor.Black, .35), Ribbons(after, ChartSeriesKind.Chord)[0].Fill);
        Assert.Equal(ChartColorMath.WithOpacity(ChartColor.White, .35), Ribbons(after, ChartSeriesKind.Chord)[1].Fill);
        foreach (var flow in series.FlowLinks) {
            var group = Link(after, flow.Id);
            Assert.Equal(flow.SourceId, (string?)group.Attribute("data-cfx-source")); Assert.Equal(flow.TargetId, (string?)group.Attribute("data-cfx-target"));
            Assert.Equal((string?)Link(before, flow.Id).Attribute("data-cfx-value"), (string?)group.Attribute("data-cfx-value"));
            Assert.Equal("Warning", (string?)group.Attribute("data-cfx-state"));
        }
        Assert.Equal("zero", (string?)Link(after, "zero").Attribute("data-cfx-geometry-status"));
        Assert.Empty(Link(after, "zero").Descendants());
        Assert.Equal(before.Regions.Select(region => region.Bounds), after.Regions.Select(region => region.Bounds));
    }

    private static Chart Parallel(ChartSeriesKind kind) {
        var nodes = new[] { new ChartNode("a", "Support"), new ChartNode("b", "Support") };
        var flows = new[] { new ChartFlowLink("standard", "a", "b", 5), new ChartFlowLink("Priority", "a", "b", 8) };
        return kind == ChartSeriesKind.Sankey ? Chart.Create().AddSankey("Flows", nodes, flows) : Chart.Create().AddChord("Flows", nodes, flows);
    }
    private static VisualRenderContext Context() => PreparedChordTests.Context(360, 280);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(Context());
    private static string Role(ChartSeriesKind kind, string suffix) => kind.ToString().ToLowerInvariant() + "-" + suffix;
    private static VisualScenePath[] Ribbons(PreparedVisual prepared, ChartSeriesKind kind) => prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == Role(kind, "ribbon")).ToArray();
    private static XElement Link(PreparedVisual prepared, string id, VisualSvgOptions? options = null) => XDocument.Parse(options == null ? prepared.ToSvg() : prepared.ToSvg(options)).Descendants()
        .Single(node => (string?)node.Attribute("data-cfx-target-kind") == "link" && (string?)node.Attribute("data-cfx-target-id") == id);
}
