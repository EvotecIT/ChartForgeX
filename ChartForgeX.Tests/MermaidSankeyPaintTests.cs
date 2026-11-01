using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSankeyPaintTests {
    [Theory]
    [InlineData("source", false, 390)] [InlineData("target", true, 1100)]
    [InlineData("gradient", false, 390)] [InlineData("gradient", true, 1100)]
    [InlineData("#7954AA", false, 390)] [InlineData("#7954AA", true, 1100)]
    public void SourcePaintRetainsRawFactsAndDirectFenceStaticExports(string policy, bool dark, int width) {
        var source = Source(policy, "outlined"); var options = Options(width, dark);
        var result = MermaidRenderer.Render(source, options); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; var chart = Assert.IsType<Chart>(artifact.Model);
        var prepared = SankeyFlowColorTests.Prepare(chart); Capture("sankey-paint-" + (policy[0] == '#' ? "literal" : policy) + "-" + width + (dark ? "-dark" : "-light"), prepared);
        Assert.Equal(new[] { "Intake.eu[1]", "Review", "Ready 東京" }, chart.Series[0].Nodes.Select(node => node.Id));
        Assert.Equal(new[] { 6d, 2d, 8d }, chart.Series[0].FlowLinks.Select(link => link.Value));
        Assert.Equal(ChartSankeyLabelStyle.Outlined, chart.Options.Sankey.LabelStyle);
        Assert.Equal(new[] { Blue, Orange, Green }, chart.Series[0].PointColors.Select(color => color!.Value));
        Assert.Equal(policy == "gradient" ? 3 : 0, prepared.Scene.Nodes.OfType<VisualSceneGradient>().Count(node => node.Role == "sankey-ribbon"));
        Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualSceneText>().Count(node => node.Role == "sankey-node-label"));
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "sankey-node-label"), node => {
            Assert.Equal(4, node.StrokeWidth); var bounds = CartesianReversalLabelTests.Bounds(node);
            Assert.True(bounds.Left - 2 >= 0 && bounds.Top - 2 >= 0 && bounds.Right + 2 <= width && bounds.Bottom + 2 <= 460,
                "The real text-outline extent must remain inside the native frame.");
        });
        var strict = SvgRasterizer.Rasterize(prepared.ToSvg(), strict: true); Assert.Empty(strict.Diagnostics);
        var image = RasterImageDecoder.Decode(prepared.ToPng()); Assert.Equal(width, image.Width); Assert.Equal(460, image.Height);
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Diagnostics);
        Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg()); Assert.Equal(artifact.ToPng(), fenced.Artifacts[0].ToPng());
        Assert.DoesNotContain(XDocument.Parse(prepared.ToSvg()).Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
    }

    [Fact]
    public void DirectivePaintOverridesFrontmatterByExactDecodedNodeKeyAndLeavesRawDeclarationsUntouched() {
        const string source = "---\nconfig:\n  sankey:\n    linkColor: source\n    labelStyle: legacy\n    nodeColors:\n      'A].[B': '#2864B4'\n      'Žółć.東京[1]': '#279473'\n---\n%%{init: {\"sankey\":{\"linkColor\":\"target\",\"labelStyle\":\"outlined\",\"nodeColors\":{\"A].[B\":\"#BB6428\"}}}}%%\nsankey-beta\nA].[B,Žółć.東京[1],4";
        var parsed = new MermaidParser().ParseSankey(source); Assert.Empty(parsed.Diagnostics); var document = Assert.IsType<MermaidSankeyDocument>(parsed.Document); var chart = document.ToChart();
        Assert.Equal(ChartSankeyFlowColorMode.Target, chart.Options.Sankey.FlowColorMode); Assert.Equal(ChartSankeyLabelStyle.Outlined, chart.Options.Sankey.LabelStyle);
        Assert.Equal(new ChartColor?[] { Orange, Green }, chart.Series[0].PointColors);
        Assert.Equal(new[] { "#2864B4", "#BB6428" }, document.Configuration.Settings.Where(setting => setting.Path == "sankey.nodeColors.[A].[B]").Select(setting => setting.Value));
        chart.ConfigureSankey(options => { options.FlowColorMode = ChartSankeyFlowColorMode.Source; options.LabelStyle = ChartSankeyLabelStyle.Backdrop; });
        Assert.Equal(ChartSankeyFlowColorMode.Source, chart.Options.Sankey.FlowColorMode); Assert.Equal(ChartSankeyLabelStyle.Backdrop, chart.Options.Sankey.LabelStyle);
    }

    [Fact]
    public void NodeMapUsesTheExistingBoundedYamlFlowMappingReaderAndAcceptsAnEmptyObject() {
        const string body = "\n---\nsankey-beta\nA].[B,Ready 東京,4";
        var flow = new MermaidParser().ParseSankey("---\nconfig:\n  sankey:\n    nodeColors: {'A].[B': '#2864B4', 'Ready 東京': '#279473'}" + body);
        Assert.Empty(flow.Diagnostics); Assert.Equal(new ChartColor?[] { Blue, Green }, flow.Document!.ToChart().Series[0].PointColors);
        foreach (var source in new[] { "%%{init: {\"sankey\":{\"nodeColors\":{}}}}%%\nsankey-beta\nA,B,1", "---\nconfig:\n  sankey:\n    nodeColors: {}\n---\nsankey-beta\nA,B,1" })
            Assert.Empty(new MermaidParser().ParseSankey(source).Diagnostics);
    }

    [Theory]
    [InlineData("linkColor", "false")] [InlineData("linkColor", "\"url(https://example.invalid/paint)\"")]
    [InlineData("linkColor", "\"unknown-color\"")] [InlineData("labelStyle", "\"backdrop\"")]
    [InlineData("nodeColors", "[]")] [InlineData("nodeColors", "null")]
    [InlineData("nodeColors", "{\"A\":false}")] [InlineData("nodeColors", "{\"A\":{\"B\":\"#2864B4\"}}")]
    [InlineData("nodeColors", "{\"A]\":{\"[B\":\"#2864B4\"}}") ]
    [InlineData("nodeColors", "{\"A.\":{\"B.\":\"#2864B4\"}}") ]
    public void InvalidSourcePaintCannotBecomeAnExportableFallback(string key, string value) {
        var source = "%%{init: {\"sankey\":{\"" + key + "\":" + value + "}}}%%\nsankey-beta\nA].[B,B,4";
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 1);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
    }

    [Fact]
    public void NativeGradientCapturesEndpointPaintOutlineAndRawGeometryWithoutAColorMeasurement() {
        var chart = SankeyFlowColorTests.Model(null, null, null); var series = chart.Series[0];
        series.WithPointColor(0, Blue).WithPointColor(1, Orange).WithPointColor(2, Green).WithPointColor(3, Blue);
        var ordinary = SankeyFlowColorTests.Prepare(chart); var paths = SankeyFlowColorTests.Ribbons(ordinary);
        chart.ConfigureSankey(options => { options.FlowColorMode = ChartSankeyFlowColorMode.Gradient; options.RibbonOpacity = .6; });
        series.WithFlowStyle("p0", new ChartFlowStyle(fillOpacity: .4, outlineStroke: new ChartStrokeStyle(ChartColor.Black, 2, dash: new[] { 4d, 2d }), strokeDashOffset: 1));
        series.WithFlowStyle("p1", new ChartFlowStyle(fill: Orange));
        var prepared = SankeyFlowColorTests.Prepare(chart); var gradients = prepared.Scene.Nodes.OfType<VisualSceneGradient>().Where(node => node.Role == "sankey-ribbon").ToArray();
        Assert.Equal(2, gradients.Length); var first = gradients[0]; var path = Assert.IsType<VisualScenePath>(first.Shape);
        Assert.Equal(paths[0].Commands, path.Commands); Assert.Equal(2, path.StrokeWidth); Assert.Equal(new[] { 4d, 2d }, path.Dash); Assert.Equal(1, path.DashOffset);
        Assert.Equal(new[] { ChartColorMath.WithOpacity(Blue, .4), ChartColorMath.WithOpacity(Orange, .4) }, first.Stops.Select(stop => stop.Color));
        Assert.All(series.FlowLinks, link => Assert.Null(link.ColorValue)); Assert.Equal(ChartSankeyFlowColorMode.Gradient, chart.Options.Sankey.FlowColorMode);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "sankey-ribbon" && node.Fill is { } fill && fill.Equals(ChartColorMath.WithOpacity(Orange, .6)));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.WithPointColor(0, Green).WithFlowStyle("p0", new ChartFlowStyle(fill: ChartColor.Black)); chart.ConfigureSankey(options => options.FlowColorMode = ChartSankeyFlowColorMode.Target);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.NotEqual(svg, SankeyFlowColorTests.Prepare(chart).ToSvg());
    }

    [Fact]
    public void ExistingConfigurationOwnerKeepsAmbiguousDisplayPathsSeparateFromDirectRawKeys() {
        const string direct = "%%{init: {\"sankey\":{\"nodeColors\":{\"A.].[B.\":\"#2864B4\"}}}}%%\nsankey-beta\nA.].[B.,Ready,4";
        const string nested = "%%{init: {\"sankey\":{\"nodeColors\":{\"A.\":{\"B.\":\"#2864B4\"}}}}}%%\nsankey-beta\nA.].[B.,Ready,4";
        var parsed = new MermaidParser().ParseSankey(direct); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidSankeyDocument>(parsed.Document);
        var directSetting = Assert.Single(document.Configuration.Settings, setting => setting.IsString);
        var rejected = new MermaidParser().ParseSankey(nested);
        var nestedSetting = Assert.Single(rejected.Document!.Configuration.Settings, setting => setting.IsString);
        Assert.Equal(directSetting.Path, nestedSetting.Path);
        Assert.False(parsed.HasErrors); Assert.True(rejected.HasErrors);
        Assert.Equal("#2864B4", directSetting.Value); Assert.Equal(directSetting.Value, nestedSetting.Value);
        Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidConfiguration);
        Assert.Equal(Blue, document.ToChart().Series[0].PointColors[0]);
    }

    [Fact]
    public void NewLabelPolicyRejectsInvalidValuesWithoutReplacingTheInstalledSnapshot() {
        var chart = SankeyFlowColorTests.Model(null, null, null); var original = chart.Options.Sankey;
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ConfigureSankey(options => options.LabelStyle = (ChartSankeyLabelStyle)99));
        Assert.Same(original, chart.Options.Sankey); Assert.Equal(ChartSankeyLabelStyle.Outlined, original.LabelStyle);
        Assert.Equal(0, (int)ChartSankeyFlowColorMode.Source); Assert.Equal(1, (int)ChartSankeyFlowColorMode.Target);
        Assert.Equal(2, (int)ChartSankeyFlowColorMode.Quantitative); Assert.Equal(3, (int)ChartSankeyFlowColorMode.Gradient);
    }

    internal static readonly ChartColor Blue = ChartColor.FromHex("#2864B4"), Orange = ChartColor.FromHex("#BB6428"), Green = ChartColor.FromHex("#279473");
    internal static string Source(string policy = "gradient", string labels = "outlined") => "%%{init: {\"sankey\":{\"linkColor\":\"" + policy + "\",\"labelStyle\":\"" + labels + "\",\"showValues\":false,\"nodeColors\":{\"Intake.eu[1]\":\"#2864B4\",\"Review\":\"#BB6428\",\"Ready 東京\":\"#279473\"}}}}%%\nsankey-beta\nIntake.eu[1],Review,6\nIntake.eu[1],Review,2\nReview,Ready 東京,8";
    internal static MermaidRenderOptions Options(int width, bool dark) => new() { WarningPolicy = MermaidWarningPolicy.Reject,
        Sankey = new MermaidSankeyRenderOptions { Width = width, Height = 460 }, Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, Theme = VisualTheme.Graphite(), ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light } };
    internal static void Capture(string name, PreparedVisual prepared) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg()); File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
    }
}
