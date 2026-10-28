using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StaticExportNativeFontBrowserTests {
    [BrowserTheory]
    [MemberData(nameof(MermaidRemainingCaptionRolesTests.Poses), MemberType = typeof(MermaidRemainingCaptionRolesTests))]
    public async Task RemainingFamilyHeaderAndRelationshipInkUsesTheCapturedPngExportOwner(string family, int width, bool dark) {
        if (!Enabled) return;
        var artifact = MermaidRemainingCaptionRolesExample.Create(family, width == 390 ? "compact" : "wide", dark ? VisualThemeMode.Dark : VisualThemeMode.Light, BrowserProofFonts.Family);
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        var html = new HtmlInteractiveTopologyRenderer().RenderPresentationFragment(chart, prepared => prepared.ToSvg(), new TopologyRenderOptions { EnableHtmlExportControls = true });
        await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 760);
        var root = session.Page.Locator(".cfx-topology-viewport > svg[role=img]"); var before = await root.EvaluateAsync<string>("node=>node.outerHTML");
        var export = await CaptureExports(session.Page, "[data-cfx-topology-export=svg]", "[data-cfx-topology-export=png]", "remaining-caption-browser-" + family + "-" + width + "-" + dark);
        NativeInlineContentTests.AssertResources(export.Svg); NativeInlineContentTests.AssertResources(export.PngComposition);
        Assert.Equal(0, export.Report.GetProperty("pngVersusCapturedRasterDifferentPixels").GetInt32());
        Assert.Equal(0, export.Report.GetProperty("compositionVersusForcedMonospaceDifferentPixels").GetInt32());
        var xml = XDocument.Parse(export.Svg);
        Assert.Contains(xml.Descendants(), element => element.Attribute("data-cfx-caption") != null);
        if (family != "requirement") {
            Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-group-label" && element.Attribute("data-cfx-caption") != null);
            Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-edge-label-text" && element.Attribute("data-cfx-caption") != null);
        }
        Assert.Equal(before, await root.EvaluateAsync<string>("node=>node.outerHTML")); AssertNoConsoleErrors(session);
    }
}
