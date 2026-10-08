using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedTopologySvgPolicyTests {
    [Fact]
    public void ExplicitPlainMapsByValueWhileUnboundStaysLiteralAndDimmedMixRetainsAlpha() {
        var accent = ChartColor.Parse("#204080");
        var mixed = ChartForgeX.Rendering.ChartColorMath.Blend(ChartColor.White, accent, .1).WithOpacity(.4);
        var builder = new VisualSceneBuilder(new VisualSize(160, 100), new FontSpec());
        builder.Rect(new ChartRect(0, 0, 20, 20), accent, role: "authored", paint: new VisualScenePaintBinding(fill: SvgPaint.Plain(accent)));
        builder.Rect(new ChartRect(30, 0, 20, 20), accent, role: "unbound");
        builder.Rect(new ChartRect(60, 0, 20, 20), mixed, role: "dimmed", paint: new VisualScenePaintBinding(fill: SvgPaint.Mix(mixed, ChartColor.White, SvgColorRole.Surface, accent, SvgColorRole.Status, .1)));
        var prepared = new PreparedVisual(builder.Build()); var png = prepared.ToPng();
        var variables = new SvgColorVariables().Add("--authored", accent).Add("--status", accent, SvgColorRole.Status).Add("--surface", ChartColor.White, SvgColorRole.Surface);
        var svg = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: variables)));
        string Fill(string role) => svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == role).Attribute("fill")!.Value;
        Assert.StartsWith("var(--authored,", Fill("authored"), StringComparison.Ordinal);
        Assert.Equal(accent.ToCss(), Fill("unbound"));
        Assert.Contains("var(--status,", Fill("dimmed")); Assert.EndsWith("40%, transparent)", Fill("dimmed"), StringComparison.Ordinal);
        Assert.Equal(png, prepared.ToPng());
        Assert.Contains(mixed.ToCss(), prepared.ToSvg());
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void TopologyRetainsScopedHostPolicyAndHiddenHeadingAccessibilityWithoutChangingPixels(VisualThemeMode mode) {
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(560, 310)), themeMode: mode,
            frame: new VisualFrame(showLegend: false, transparentBackground: true));
        var colors = context.Theme.Resolve(mode);
        var chart = TopologyChart.Create(); chart.Id = "policy"; chart.Title = "Source title"; chart.Subtitle = "Source description";
        chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Server", X = 24, Y = 48, Width = 160, Height = 80,
            Status = TopologyHealthStatus.Healthy, Href = "https://example.org/server" });
        var variables = new SvgColorVariables().Add("--series", colors.Status.Pass.Fill, SvgColorRole.Series)
            .Add("--status", colors.Status.Pass.Fill, SvgColorRole.Status).Add("--background", colors.Background, SvgColorRole.Surface);
        var options = new TopologyRenderOptions { IncludeTitle = false, IdScope = "host record:42", SvgColorVariables = variables,
            OpenLinksInNewTab = true, PinStateColorsInForcedColors = true, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Tinted };
        var prepared = chart.Prepare(context, options); var original = prepared.ToSvg();
        var document = XDocument.Parse(original);
        var anchor = Assert.Single(document.Descendants(), element => element.Name.LocalName == "a");
        Assert.Equal("_blank", (string?)anchor.Attribute("target")); Assert.Equal("noopener noreferrer", (string?)anchor.Attribute("rel"));
        Assert.Contains("var(--status,", original); Assert.Contains("var(--background,", original);
        Assert.DoesNotContain("var(--series,", original); Assert.Contains("forced-color-adjust:none", original);
        Assert.Contains("Source title", original); Assert.Contains("Source description", original);
        Assert.DoesNotContain(document.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "frame-heading");
        Assert.All(document.Descendants().Attributes("id"), attribute => Assert.StartsWith(VisualSvgOptions.NamespaceFromExternalId(options.IdScope)! + "-", attribute.Value, StringComparison.Ordinal));
        var semantics = prepared.ToArtifact("policy", VisualArtifactKind.Topology).ToInterchangeEnvelope();
        Assert.Equal("Source title", semantics.Title); Assert.Equal("Source title", semantics.AccessibleName); Assert.Equal("Source description", semantics.AccessibleDescription);
        var noVariables = options.CloneForRendering(); noVariables.SvgColorVariables = null;
        Assert.Equal(chart.Prepare(context, noVariables).ToPng(), prepared.ToPng()); Assert.Equal(0, prepared.ToRgba().Pixels[3]);
        variables.Add("--late", colors.Foreground); options.IdScope = "changed"; chart.Title = "Changed";
        Assert.Equal(original, prepared.ToSvg());
        var artifact = prepared.ToArtifact("policy", VisualArtifactKind.Topology); artifact.Accessibility.Name = "Host title";
        Assert.Contains("Host title", artifact.ToSvg()); Assert.Contains("var(--status,", artifact.ToSvg());
    }
}
