using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeJointStackValueBeforeControls {
    [Theory]
    [InlineData("stacked-bar", false)] [InlineData("stacked-bar", true)]
    [InlineData("default-stacked", false)] [InlineData("default-stacked", true)]
    [InlineData("normalized-bar", false)] [InlineData("normalized-bar", true)]
    [InlineData("negative-stack", false)] [InlineData("negative-stack", true)]
    [InlineData("negative-normalized", false)] [InlineData("negative-normalized", true)]
    public void ExplicitJointPreserveUsesNativeStackGeometryAndKeepsSharedFullSourceTotalsGlobal(string variant, bool labels) {
        var renderer = new HtmlInteractiveChartRenderer(); var negative = variant.StartsWith("negative-", StringComparison.Ordinal); var first = negative ? -30d : 30d; var last = negative ? -40d : 40d;
        var from = Section(renderer, Model(variant, first, labels), false); var to = Section(renderer, Model(variant, last, labels), true);
        var value = ChartMath.InterpolateRange(first, last, .5); var middleFrom = Section(renderer, Model(variant, value, labels), false); var middleTo = Section(renderer, Model(variant, value, labels), true);
        var expected = VisualSeriesVisibilityCapture.Frames(middleFrom.PreparedVisual, middleTo.PreparedVisual, 3)[1];
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null;
        var error = Record.Exception(() => { frames = VisualSeriesVisibilityCapture.ValueVisibilityFrames(from.PreparedVisual, to.PreparedVisual, 3); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValuesAndVisibility, 3)); });
        var name = "joint-stack-" + variant + "-" + labels;
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middleFrom, name + "-independent-values");
        NativeSeriesVisibilityDefaultControls.Save(name + "-independent-opacity", expected);
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { value, payload, error = error?.ToString(), expectedTotals = TotalFacts(expected), actualTotals = frames == null ? null : TotalFacts(frames[1]) }));
        if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, name);
        Assert.Null(error); Assert.Same(from.PreparedVisual, frames![0]); Assert.Same(to.PreparedVisual, frames[^1]);
        Assert.Equal(expected.ToSvg(), frames[1].ToSvg()); Assert.Equal(expected.ToSvg("joint-stack-check"), frames[1].ToSvg("joint-stack-check")); Assert.Equal(expected.ToPng(), frames[1].ToPng());
        Assert.Equal(expected.Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)), frames[1].Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)));
        var total = frames[1].Scene.Nodes.OfType<VisualSceneGroup>().Single(row => row.Role == "stack-total" && row.Metadata["data-cfx-x"] == "1");
        Assert.Equal(negative ? "-50" : "50", total.Metadata["data-cfx-source-total"]); Assert.Equal(variant.Contains("normalized", StringComparison.Ordinal) ? negative ? "-100" : "100" : negative ? "-50" : "50", total.Metadata["data-cfx-y"]);
        AssertGlobalTotalOwnership(frames[1]); AssertGlobalTotalOwnership(to.PreparedVisual);
        var hiddenPointIds = to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Role == "point" && row.Metadata["data-cfx-series-key"] == "delivery").Select(row => row.Id).ToHashSet();
        Assert.DoesNotContain(frames[^1].Regions, row => hiddenPointIds.Contains(row.Id));
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[^1].ToPng());
        Assert.Throws<NotSupportedException>(() => renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3)));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void OrdinaryPreserveEndpointsAlreadyRetainTheSameFullSourceTotalsWhenSeriesInkIsHidden(bool labels) {
        var renderer = new HtmlInteractiveChartRenderer(); var visible = Section(renderer, Model("stacked-bar", 35, labels), false); var hidden = Section(renderer, Model("stacked-bar", 35, labels), true);
        NativeRadialMotionBeforeControls.Capture(visible, "joint-stack-ordinary-" + labels + "-visible"); NativeRadialMotionBeforeControls.Capture(hidden, "joint-stack-ordinary-" + labels + "-hidden");
        NativeRadialMotionBeforeControls.Save("joint-stack-ordinary-" + labels + ".facts.json", JsonSerializer.Serialize(new { visible = TotalFacts(visible.PreparedVisual), hidden = TotalFacts(hidden.PreparedVisual) }));
        Assert.Equal(TotalFacts(visible.PreparedVisual), TotalFacts(hidden.PreparedVisual)); AssertGlobalTotalOwnership(visible.PreparedVisual); AssertGlobalTotalOwnership(hidden.PreparedVisual);
        var repeat = Section(renderer, Model("stacked-bar", 35, labels), true);
        Assert.Equal(hidden.Html, repeat.Html); Assert.Equal(hidden.PreparedVisual.ToSvg(), repeat.PreparedVisual.ToSvg()); Assert.Equal(hidden.PreparedVisual.ToPng(), repeat.PreparedVisual.ToPng());
    }

    internal static Chart Model(string variant, double value, bool labels, int width = 760, bool dark = false) {
        var mapped = variant == "default-stacked" || variant == "negative-stack" ? "stacked-bar" : variant == "negative-normalized" ? "normalized-bar" : variant;
        var chart = NativeCompleteCartesianValueRecipe.Create(mapped, value, width, dark ? VisualThemeMode.Dark : VisualThemeMode.Light, BrowserProofFonts.Family).WithDataLabels(labels);
        if (variant == "default-stacked") { foreach (var row in chart.Series) row.WithStackGroup(null); chart.WithBarMode(ChartBarMode.Stacked); }
        if (variant.StartsWith("negative-", StringComparison.Ordinal)) {
            foreach (var row in chart.Series) for (var index = 0; index < row.Points.Count; index++) { var point = row.Points[index]; row.Points[index] = new ChartForgeX.Primitives.ChartPoint(point.X, -Math.Abs(point.Y), point.BreakBefore); }
            chart.ConfigureYAxis(axis => { axis.Minimum = -160; axis.Maximum = 0; });
        }
        return chart;
    }
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, Chart chart, bool hidden) => renderer.PrepareVisibilitySection(chart,
        new ChartVisibilityView(hidden ? new[] { new ChartSeriesId("delivery") } : Array.Empty<ChartSeriesId>(),
            chart.Options.ShowDataLabels == true ? ChartVisibilityLabelLayering.GroupWithSeries : ChartVisibilityLabelLayering.PreserveOverlay));
    private static string[] TotalFacts(PreparedVisual drawing) => drawing.Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Role == "stack-total")
        .Select(row => row.Id + ":" + string.Join("|", row.Metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value))).ToArray();
    private static void AssertGlobalTotalOwnership(PreparedVisual drawing) {
        var tree = new VisualSeriesVisibilityCapture.SpanTree(drawing.Scene.Nodes);
        for (var index = 0; index < drawing.Scene.Nodes.Count; index++) if (drawing.Scene.Nodes[index] is VisualSceneGroup row && row.Role is "stack-total" or "stack-total-label-source") {
            Assert.Equal(1, row.Opacity); Assert.DoesNotContain(tree.Ancestors(index), parent => drawing.Scene.Nodes[parent] is VisualSceneGroup { Role: "series-visibility" });
        }
    }
}
