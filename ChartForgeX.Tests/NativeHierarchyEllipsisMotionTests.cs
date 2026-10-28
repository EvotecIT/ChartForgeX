using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeHierarchyEllipsisMotionTests {
    [Theory] [InlineData(390, false)] [InlineData(390, true)] [InlineData(760, false)] [InlineData(760, true)]
    public void NativeTreemapEllipsisFramesEqualIndependentPreparedGlyphsAndReplayWithoutRegistry(int width, bool dark) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareHierarchySection(Model(30, width, dark)); var to = renderer.PrepareHierarchySection(Model(40, width, dark));
        var middle = renderer.PrepareHierarchySection(Model(35, width, dark)); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)); var name = "hierarchy-ellipsis-" + width + "-" + dark;
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent");
        var text = frames[1].Scene.Nodes.OfType<VisualSceneText>().Single(row => row.Role == "treemap-group-label" && row.Text.Lines.Any(line => line.Text.EndsWith("…", StringComparison.Ordinal))).Text.Lines.Single().Text;
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { text, full = Model(35, width, dark).Series[0].PointLabels[0] }));
        Assert.EndsWith("…", text); Assert.NotEqual("…", text); Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        var svg = frames.Select(row => row.ToSvg()).ToArray(); var png = frames.Select(row => row.ToPng()).ToArray();
        try { FontRegistry.Clear(); var replay = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); Assert.Equal(svg, replay.Select(row => row.ToSvg()));
            for (var index = 0; index < replay.Count; index++) Assert.Equal(png[index], replay[index].ToPng());
        } finally { _ = BrowserProofFonts.Family; }
    }
    internal static Chart Model(double reading, int width, bool dark) {
        var chart = NativeHierarchyMotionBeforeControls.Model(false, false, reading).WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.Series[0].WithPointLabel(0, string.Concat(Enumerable.Repeat("Regional reconciliation ", 12)));
        chart.ConfigureTreemap(options => { options.LabelTrimming = TextTrimming.Ellipsis; options.MinimumLabelFontSize = 10; }); return chart;
    }
}
