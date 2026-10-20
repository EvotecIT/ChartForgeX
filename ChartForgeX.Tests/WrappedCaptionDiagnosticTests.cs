using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WrappedCaptionDiagnosticTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void CompletePyramidRailCaptionsDoNotReportTextLossWhenTheyWrap(VisualThemeMode mode) {
        var chart = Chart.Create().WithDataLabels().WithXLabels("Regional platform services in North America", "Regional platform services in South America", "Regional platform services in Western Europe")
            .AddPyramid("Allocation", new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20) });
        chart.Options.DataLabelPlacement = ChartDataLabelPlacement.Right;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(390, 420), 14),
            VisualTheme.Graphite(), mode, new VisualFrame("", "", showLegend: false), Font()));
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "pyramid-label").ToArray();
        Assert.Equal(3, captions.Length);
        Assert.Contains(captions, caption => caption.Text.Lines.Count > 1);
        Assert.Equal(new[] { "Regional platform services in North America: 50", "Regional platform services in South America: 30", "Regional platform services in Western Europe: 20" },
            captions.Select(caption => string.Join(" ", caption.Text.Lines.Select(line => line.Text))));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "text.overflow");
        ScatterBubbleColorRenderingTests.Capture(prepared, "pyramid-wrapped-diagnostics-" + mode);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData("North America services", "North America\nservices", 260, false)]
    [InlineData("North America services", "North America\nservices", 40, true)]
    [InlineData("North America services", "North America", 260, true)]
    public void SharedTextReportsActualContentLossWhileAcceptingWordPreservingReflow(string source, string displayed, double width, bool overflow) {
        var font = Font();
        var builder = new VisualSceneBuilder(new VisualSize(300, 120), font);
        VisualStateSceneTools.Text(builder, source, new ChartRect(5, 5, width, 100),
            new TextStyle { Font = font, FontSize = 13.5 }, "caption", "caption", displayedText: displayed);
        var scene = builder.Build();
        Assert.Equal(overflow, scene.Diagnostics.Any(diagnostic => diagnostic.Code == "text.overflow"));
        Assert.Equal(source, Assert.Single(scene.Regions).Label);
        var caption = Assert.Single(scene.Nodes.OfType<VisualSceneText>());
        if (!overflow) Assert.Equal(displayed, string.Join("\n", caption.Text.Lines.Select(line => line.Text)));
        else if (width == 40) Assert.Contains("…", string.Join("\n", caption.Text.Lines.Select(line => line.Text)));
    }

    private static FontSpec Font() => FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
}
