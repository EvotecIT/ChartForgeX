using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

// Apply after the frozen Preserve API prerequisites, before any Reflow product hunk. No new Reflow types or members are referenced here.
[Collection(nameof(FontRegistryCollection))]
[Trait("BrowserSuite", "Compatibility")]
public sealed class NativeReflowPreserveBeforeControls {
    [Theory]
    [InlineData("line", false, false)] [InlineData("bar", true, false)] [InlineData("line", false, true)] [InlineData("bar", true, true)]
    public void IncumbentWholeSeriesVisibilityAndGroupedLabelBytesRemainExact(string kind, bool hidden, bool labels) {
        var chart = NativeReflowVisibilityBeforeControls.Model(kind).WithLegend(true).WithDataLabels(labels);
        var renderer = new HtmlInteractiveChartRenderer(); var view = new ChartVisibilityView(hidden ? new[] { new ChartSeriesId("delivery") } : null,
            labels ? ChartVisibilityLabelLayering.GroupWithSeries : ChartVisibilityLabelLayering.PreserveOverlay);
        var first = renderer.PrepareVisibilitySection(chart, view); var second = renderer.PrepareVisibilitySection(chart, view);
        NativeReflowVisibilityBeforeControls.Capture(first, "reflow-before-preserve-" + kind + "-" + hidden + "-" + labels);
        Assert.Equal(first.Html, second.Html); Assert.Equal(first.PreparedVisual.ToSvg(), second.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), second.PreparedVisual.ToPng());
    }
}
