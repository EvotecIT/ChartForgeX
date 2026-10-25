using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StaticExportNativeFontBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(1100, VisualThemeMode.Light)]
    [InlineData(1100, VisualThemeMode.Dark)]
    public async Task StructuredCaptionVectorAndPngRetainPermanentInkResources(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = TopologyChart.Create().WithViewport(width, 420).WithTheme(VisualTheme.Report().WithFontFamily(BrowserProofFonts.Family), mode);
        chart.Nodes.Add(new TopologyNode { Id = "caption", X = 30, Y = 50, Width = width - 70, Height = 260, ShowStatusBadge = false,
            LabelContent = TextContent.FromRuns(new[] { new TextContentRun("Regular "), new TextContentRun("bold", TextInlineEmphasis.Bold), new TextContentRun(" italic", TextInlineEmphasis.Italic), new TextContentRun("\nComplete native caption") }) });
        var options = new TopologyRenderOptions { EnableHtmlExportControls = true, WrapNodeLabels = true, AllowMultilineNodeLabels = true, MaxNodeLabelLines = 6 };
        var html = new HtmlInteractiveTopologyRenderer().RenderPresentationFragment(chart, prepared => prepared.ToSvg(), options);
        await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 650);
        var root = session.Page.Locator(".cfx-topology-viewport > svg[role=img]"); var before = await root.EvaluateAsync<string>("node=>node.outerHTML");
        var exported = await CaptureExports(session.Page, "[data-cfx-topology-export=svg]", "[data-cfx-topology-export=png]", "mixed-caption-" + width + "-" + mode);
        NativeInlineContentTests.AssertResources(exported.Svg); NativeInlineContentTests.AssertResources(exported.PngComposition);
        Assert.DoesNotContain("data-cfx-png-text-resources", exported.Svg, StringComparison.Ordinal);
        Assert.Equal(0, exported.Report.GetProperty("pngVersusCompletedCompositionDifferentPixels").GetInt32());
        Assert.Equal(0, exported.Report.GetProperty("pngVersusForcedMonospaceDifferentPixels").GetInt32());
        Assert.Equal(before, await root.EvaluateAsync<string>("node=>node.outerHTML"));
        Assert.Contains(XDocument.Parse(exported.Svg).Descendants(), node => (string?)node.Attribute("data-cfx-caption") == "Regular bold italic\nComplete native caption");
        AssertNoConsoleErrors(session);
    }
}
