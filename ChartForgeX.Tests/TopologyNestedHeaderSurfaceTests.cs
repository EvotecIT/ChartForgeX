using System.Text;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyNestedHeaderSurfaceTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void DefaultNestedCaptionUsesTheActualComposedSurfaceAndKeepsExplicitInk(bool dark, bool translucent) {
        var chart = Fixture(dark, translucent); var options = Options();
        var prepared = chart.Prepare(options);
        var surfaces = prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "topology-group-surface").ToArray();
        Assert.Equal(2, surfaces.Length);
        var background = chart.Theme.Resolve(chart.ThemeMode).Background;
        var parentFill = ChartColorMath.CompositeOverOpaque(surfaces[0].Fill!.Value, background);
        var effective = ChartColorMath.CompositeOverOpaque(surfaces[1].Fill!.Value, parentFill);
        var child = Captions(prepared).Where(text => Text(text) is "Order" or "records" or "All facts" or "retained").ToArray();
        Assert.Equal(4, child.Length);
        Assert.All(child, text => Assert.True(ChartColorMath.ContrastRatio(text.Color, effective) >= 4.5));
        Assert.Equal(ChartColor.Parse("#14532D"), Captions(prepared).Single(text => Text(text) == "Domain").Color);
        Assert.Equal("Order\nrecords", chart.Groups[1].Label);
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal("Order\nrecords", envelope.Groups.Single(group => group.Id == "orders").Label);
        Assert.Equal("All facts\nretained", envelope.Groups.Single(group => group.Id == "orders").Subtitle);
        Assert.Equal("domain", envelope.Groups.Single(group => group.Id == "orders").Topology!.ParentGroupId);
        Capture(prepared, "nested-header-surface-" + dark + "-" + translucent);
        AssertPaintGeometry(prepared);
        var savedSvg = prepared.ToSvg(); var savedPng = prepared.ToPng();
        chart.Groups[1].LabelColor = "#7C3AED";
        Assert.All(Captions(chart.Prepare(options)).Where(text => Text(text) is "Order" or "records" or "All facts" or "retained"),
            text => Assert.Equal(ChartColor.Parse("#7C3AED"), text.Color));
        Assert.Equal(savedSvg, prepared.ToSvg()); Assert.Equal(savedPng, prepared.ToPng());
    }

    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void AuthoredErHeaderRowsPaintThroughDirectFenceNativeAndSemanticOwners(bool dark, int width) {
        var options = MermaidClassErCaptionTests.Options(dark, width); var source = MermaidClassErCaptionTests.Source("er");
        var rendered = MermaidRenderer.Render(source, options); Assert.False(rendered.HasErrors);
        var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors); Assert.Equal(svg, Assert.Single(fence.Artifacts).ToSvg()); Assert.Equal(png, fence.Artifacts[0].ToPng());
        var xml = XDocument.Parse(svg);
        foreach (var word in new[] { "Order", "records" }) Assert.Contains(xml.Descendants(), item => item.Name.LocalName == "text" && item.Value == word);
        Assert.DoesNotContain(xml.Descendants(), item => item.Name.LocalName is "script" or "foreignObject" or "image");
        var chart = Assert.IsType<TopologyChart>(artifact.Model); var native = chart.Prepare();
        var order = native.ToInterchangeEnvelope().Groups.Single(group => group.Id == "Order group");
        Assert.Equal("Order\nrecords", order.Label); Assert.Equal("Order<br>records", order.Extensions["mermaid.source.label"]);
        Assert.Equal("Domain: core", order.Topology!.ParentGroupId);
        var surfaces = native.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "topology-group-surface").ToArray();
        var composed = ChartColorMath.CompositeOverOpaque(surfaces[1].Fill!.Value, surfaces[0].Fill!.Value);
        Assert.All(Captions(native).Where(text => Text(text) is "Order" or "records"), text => Assert.True(ChartColorMath.ContrastRatio(text.Color, composed) >= 4.5));
        AssertPaintGeometry(native);
        ScatterBubbleColorRenderingTests.Capture(native.Visual, "nested-er-header-" + width + (dark ? "-dark" : "-light"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnauthoredSingleLineGroupsRetainTheirIncumbentCoordinatesAndThemeInk(bool dark) {
        var chart = TopologyChart.Create().WithViewport(420, 300, 16).WithLegend(null)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddGroup("plain", "Plain", 32, 40, 300, 180, subtitle: "Description");
        var prepared = chart.Prepare(Options()); var captions = Captions(prepared);
        Assert.Equal(2, captions.Length);
        Assert.Equal(chart.Theme.Resolve(chart.ThemeMode).Foreground, captions[0].Color);
        Assert.Equal(chart.Theme.Resolve(chart.ThemeMode).MutedForeground, captions[1].Color);
        Assert.Equal(49, TopologyGroupHeader.TitleBounds(chart.Groups[0], Options()).Top);
        Assert.Equal(24, TopologyGroupHeader.TitleBounds(chart.Groups[0], Options()).Height);
        Assert.Equal(71, TopologyGroupHeader.SubtitleBounds(chart.Groups[0], Options(), false).Top);
        Assert.DoesNotContain("derived-ink", prepared.ToSvg());
    }

    [Fact]
    public void ComposedSurfaceBindingsReevaluatePreferredInkAndAlphaInTheOtherTheme() {
        var light = new SvgColorVariables().Add("--ink", ChartColor.Parse("#222222"), SvgColorRole.Text)
            .Add("--base", ChartColor.Parse("#FFFFFF"), SvgColorRole.Surface)
            .Add("--shell", ChartColor.Parse("#DCFCE780"), SvgColorRole.Any);
        var dark = new SvgColorVariables().Add("--ink", ChartColor.Parse("#F2F3F5"), SvgColorRole.Text)
            .Add("--base", ChartColor.Parse("#101010"), SvgColorRole.Surface)
            .Add("--shell", ChartColor.Parse("#DCFCE7"), SvgColorRole.Any);
        var preferred = SvgPaint.Of(ChartColor.Parse("#222222"), SvgColorRole.Text);
        var layers = new[] { SvgPaint.Of(ChartColor.Parse("#FFFFFF"), SvgColorRole.Surface), SvgPaint.Of(ChartColor.Parse("#DCFCE780"), SvgColorRole.Any) };
        var paint = SvgPaint.ContrastOver(ChartColor.Parse("#222222"), preferred, layers);
        var resolved = SvgPaint.Resolve(paint.Value!, light);
        Assert.Contains("var(--cfx-derived-ink-v1-", resolved);
        var svg = "<svg><text fill=\"" + resolved + "\">Read</text></svg>";
        var nativeLight = light.GetVariablesForSvg(svg).Last(); var nativeDark = dark.GetVariablesForSvg(svg).Last();
        Assert.Equal("#222222", nativeLight.Color.ToHex(), ignoreCase: true);
        Assert.Equal(ChartColorMath.AccessibleTextOnBackground(ChartColor.Parse("#DCFCE7")), nativeDark.Color);
        Assert.Equal(nativeLight.Name, nativeDark.Name);
    }

    [Theory]
    [InlineData("A|LFFFFFFFF|1|X|NaN|LFFFFFFFF")]
    [InlineData("A|LFFFFFFFF|1|X|Infinity|LFFFFFFFF")]
    [InlineData("A|LFFFFFFFF|33|LFFFFFFFF")]
    [InlineData("A|LFFFFFFFF|1|Q|--x|2|FFFFFFFF")]
    [InlineData("A|LFFFFFFFF|1|LFFFFFFFF|surplus")]
    public void MalformedDerivedSurfaceOperandsAreIgnoredWithoutPartialVariables(string descriptor) {
        var name = "--cfx-derived-ink-v1-" + Convert.ToBase64String(Encoding.UTF8.GetBytes(descriptor)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var variables = new SvgColorVariables().Add("--base", ChartColor.Parse("#FFFFFF"));
        Assert.Single(variables.GetVariablesForSvg("<text fill=\"var(" + name + ", #000000)\">Read</text>"));
    }

    [Fact]
    public void BoundedNestedMixesRetainLegacyBytesAndFallBackExactlyAtResourceLimits() {
        var a = ChartColor.Parse("#FFFFFF"); var b = ChartColor.Parse("#000000");
        var variables = new SvgColorVariables().Add("--from", a, SvgColorRole.Surface).Add("--to", b, SvgColorRole.Series);
        var fill = ChartColorMath.Blend(a, b, .2);
        var legacy = SvgPaint.Contrast(fill, SvgPaint.Mix(fill, SvgPaint.Opaque(a, SvgColorRole.Surface), SvgPaint.Opaque(b, SvgColorRole.Series), .2));
        var descriptor = "M|O--from|O--to|0.2";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(descriptor)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal("var(--cfx-derived-ink-v1-" + encoded + ", " + ChartColorMath.AccessibleTextOnBackground(fill).ToHex() + ")", SvgPaint.Resolve(legacy.Value!, variables));
        var nested = SvgPaint.Mix(fill, SvgPaint.Of(a, SvgColorRole.Surface), SvgPaint.Of(b, SvgColorRole.Series), .2);
        var adaptive = SvgPaint.ContrastOver(b, SvgPaint.Of(b, SvgColorRole.Text), new[] { nested.WithOpacity(fill, .5) });
        Assert.Contains("derived-ink", SvgPaint.Resolve(adaptive.Value!, variables));
        var tooMany = Enumerable.Repeat(SvgPaint.Literal(a), 33).ToArray();
        Assert.Equal(b.ToCss(), SvgPaint.Resolve(SvgPaint.ContrastOver(b, SvgPaint.Literal(b), tooMany).Value!, variables));
        var deep = "A|LFFFFFFFF|1|" + string.Concat(Enumerable.Repeat("X|1|", 18)) + "LFFFFFFFF";
        var name = "--cfx-derived-ink-v1-" + Convert.ToBase64String(Encoding.UTF8.GetBytes(deep)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal(2, variables.GetVariablesForSvg("var(" + name + ", #000000)").Count);
    }

    internal static TopologyChart Fixture(bool dark, bool translucent) {
        var chart = TopologyChart.Create().WithViewport(660, 530, 16).WithLegend(null)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddGroup("domain", "Domain", 24, 30, 600, 440)
            .AddGroup("orders", "Order\nrecords", 60, 125, 500, 320, subtitle: "All facts\nretained")
            .AddNode("member", "Member", 100, 136, TopologyNodeKind.Generic, groupId: "orders", width: 80, height: 40)
            .AddNode("outside", "Outside", 570, 490, width: 40, height: 20)
            .AddEdge("relation", "member", "outside", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        chart.Groups[0].BackgroundColor = translucent ? "#DCFCE7C0" : "#DCFCE7"; chart.Groups[0].LabelColor = "#14532D";
        chart.Groups[1].ParentGroupId = "domain";
        return chart;
    }
    internal static TopologyRenderOptions Options() => new() { IncludeLegend = false, IncludeTitle = false, IncludeGroupStatusDots = false,
        IncludeStatusBadges = false, AllowMultilineNodeLabels = true, WrapNodeLabels = true, CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Plain };
    private static VisualSceneText[] Captions(PreparedTopology prepared) => prepared.Visual.Scene.Nodes.OfType<VisualSceneText>()
        .Where(mark => mark.Role is "topology-group-label" or "topology-group-subtitle").ToArray();
    private static string Text(VisualSceneText text) => Assert.Single(text.Text.Lines).Text;
    private static void AssertPaintGeometry(PreparedTopology prepared) {
        var report = prepared.Analyze();
        Assert.DoesNotContain(report.RouteCrossings, crossing => crossing.Kind == TopologyLayoutRouteCrossingKind.GroupHeader);
        foreach (var text in Captions(prepared)) {
            var rect = new ChartRect(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
            Assert.Contains(report.Groups, group => group.HeaderBounds.HasValue && Contains(group.HeaderBounds.Value, rect));
            Assert.DoesNotContain(report.Nodes, node => rect.Left < node.Bounds.Right && rect.Right > node.Bounds.Left && rect.Top < node.Bounds.Bottom && rect.Bottom > node.Bounds.Top);
        }
        var png = PngReader.Decode(prepared.ToPng()); Assert.True(png.Width > 0 && png.Height > 0);
        foreach (var text in Captions(prepared)) {
            var bounds = new ChartRect(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
            // A small antialiased glyph need not contain a fully covered pixel. Compare
            // the actual export with the same ordered scene omitting only this row.
            // Exact text/ink and composed-surface contrast are checked independently.
            var scene = prepared.Visual.Scene;
            var withoutRow = new VisualScene(scene.Size, scene.Nodes.Where(node => !ReferenceEquals(node, text)).ToArray(),
                scene.Diagnostics, scene.Regions, scene.DataProjection);
            var omitted = VisualSceneRasterRenderer.Render(withoutRow);
            Assert.Equal(png.Width, omitted.Width); Assert.Equal(png.Height, omitted.Height);
            var found = false;
            for (var y = Math.Max(0, (int)Math.Floor(bounds.Top)); y < Math.Min(png.Height, (int)Math.Ceiling(bounds.Bottom)); y++)
                for (var x = Math.Max(0, (int)Math.Floor(bounds.Left)); x < Math.Min(png.Width, (int)Math.Ceiling(bounds.Right)); x++) {
                    var offset = (y * png.Width + x) * 4;
                    if (png.Pixels[offset] != omitted.Pixels[offset] || png.Pixels[offset + 1] != omitted.Pixels[offset + 1]
                        || png.Pixels[offset + 2] != omitted.Pixels[offset + 2] || png.Pixels[offset + 3] != omitted.Pixels[offset + 3]) found = true;
                }
            Assert.True(found, "Removing a measured header row must change its native PNG pixels: " + Text(text)
                + "; ink=" + text.Color.ToCss() + "; bounds=" + bounds);
        }
        var xml = XDocument.Parse(prepared.ToSvg());
        foreach (var text in Captions(prepared)) Assert.Contains(xml.Descendants(), element => element.Name.LocalName == "text" && element.Value == Text(text)
            && string.Equals((string?)element.Attribute("fill"), text.Color.ToCss(), StringComparison.OrdinalIgnoreCase));
    }
    private static void Capture(PreparedTopology prepared, string name) {
        var root = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(root, name + ".png"), prepared.ToPng());
    }
    private static bool Contains(ChartRect outer, ChartRect inner) => inner.Left >= outer.Left - .01 && inner.Top >= outer.Top - .01
        && inner.Right <= outer.Right + .01 && inner.Bottom <= outer.Bottom + .01;
}
