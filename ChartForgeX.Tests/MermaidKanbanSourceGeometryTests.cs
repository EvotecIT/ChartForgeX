using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidKanbanSourceGeometryTests {
    [Theory]
    [InlineData(0, 1)] [InlineData(12, 48)] [InlineData(36, 200)] [InlineData(64, 320)]
    public void SourceDimensionsUseExistingViewportGroupAndMeasuredCardOwners(double padding, double sectionWidth) {
        var source = Source(padding, sectionWidth); var parsed = new MermaidParser().ParseKanban(source); Assert.False(parsed.HasErrors);
        var options = new MermaidTopologyRenderOptions { Width = 390, Height = 400, Padding = 17 };
        var document = Assert.IsType<MermaidKanbanDocument>(parsed.Document);
        var chart = document.ToTopologyChart(options);
        Assert.Equal(padding, chart.Viewport.Padding); Assert.Equal(17, options.Padding);
        Assert.All(chart.Groups, group => { Assert.Equal(sectionWidth, group.Width); Assert.Equal(TopologyGroupLayoutPolicy.VerticalStack, group.LayoutPolicy); });
        Assert.All(chart.Nodes, node => Assert.Equal(Math.Max(1, sectionWidth - 48), node.Width));
        var prepared = chart.Prepare(); var facts = prepared.ToInterchangeEnvelope();
        Capture(prepared, "kanban-source-dimensions-" + sectionWidth.ToString(CultureInfo.InvariantCulture));
        Assert.All(facts.Groups, group => Assert.True(group.Width >= Math.Max(190, sectionWidth)));
        AssertContained(facts); Assert.DoesNotContain(prepared.Visual.Diagnostics, item => item.Code == "topology.shape-content-overflow");
        Assert.All(facts.Groups, group => {
            Assert.Equal(sectionWidth.ToString("R", CultureInfo.InvariantCulture), group.Extensions["mermaid.config.kanban.sectionWidth"]);
            Assert.Equal(padding.ToString("R", CultureInfo.InvariantCulture), group.Extensions["mermaid.config.kanban.padding"]);
        });
        Assert.Equal(source, document.SourceText);
    }

    [Fact]
    public void LocatedSourcePrecedenceIsExplicitAndDoesNotMutateHostOptions() {
        var source = Source(24, 200).Replace("kanban\n", "%%{init: {'kanban': {'padding': 40, 'sectionWidth': 3.2e2}}}%%\nkanban\n", StringComparison.Ordinal);
        var parsed = new MermaidParser().ParseKanban(source); Assert.False(parsed.HasErrors);
        var document = Assert.IsType<MermaidKanbanDocument>(parsed.Document);
        var chart = document.ToTopologyChart(new MermaidTopologyRenderOptions { Padding = 9 });
        Assert.Equal(40, chart.Viewport.Padding); Assert.All(chart.Groups, group => Assert.Equal(320, group.Width));
        Assert.Equal("3.2e2", document.Configuration.Settings.Last(item => item.Path == "kanban.sectionWidth").Value);
        Assert.Equal(2, document.Configuration.Settings.Count(item => item.Path == "kanban.padding"));
        Assert.DoesNotContain(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
        chart.Viewport.Padding = 20; chart.Groups[0].Width = 400;
        var saved = chart.Prepare().ToInterchangeEnvelope(); Assert.True(saved.Groups[0].Width >= 400);
        Assert.Equal("320", saved.Groups[0].Extensions["mermaid.config.kanban.sectionWidth"]);
        Assert.Equal(200, new MermaidParser().ParseKanban(Source(24, 200)).Document!.ToTopologyChart().Groups[0].Width);
    }

    [Theory]
    [InlineData("padding", "-1")] [InlineData("padding", "'12'")]
    [InlineData("padding", "true")] [InlineData("padding", "1e999")]
    [InlineData("sectionWidth", "0")] [InlineData("sectionWidth", "-1")]
    [InlineData("sectionWidth", "'240'")] [InlineData("sectionWidth", "false")]
    [InlineData("sectionWidth", "null")] [InlineData("sectionWidth", "1e999")]
    public void InvalidDimensionsAreLocatedAndFailDirectAndFencedRendering(string field, string value) {
        var source = "---\nconfig:\n  kanban:\n    " + field + ": " + value + "\n---\nkanban\nqueue[Queue]\n  a[Task]\n";
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 4 && item.Span.Column > 0);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "```\n"); Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
    }

    [Theory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public void DenseConfiguredBoardRetainsEverySourceFactThroughNativeDirectFencePortableAndExports(bool dark, int width) {
        var options = Options(dark, width); var source = Fixture();
        Assert.Equal(source, File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "kanban-source-geometry.mmd")));
        var result = MermaidRenderer.Render(source, options); Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        var chart = Assert.IsType<TopologyChart>(artifact.Model); Assert.Equal(36, chart.Viewport.Padding);
        var prepared = chart.Prepare(); var facts = prepared.ToInterchangeEnvelope();
        Capture(prepared, "kanban-source-board-" + width + "-" + dark);
        Assert.Equal(new[] { "empty", "work", "done" }, facts.Groups.Select(group => group.Id)); Assert.Equal(13, facts.Nodes.Count); Assert.Empty(facts.Edges);
        Assert.All(facts.Groups, group => Assert.True(group.Width >= 240)); AssertContained(facts);
        Assert.Equal(Enumerable.Range(1, 12).Select(index => "task" + index), facts.Nodes.Where(node => node.GroupId == "work").Select(node => node.Id));
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var image = RasterImageDecoder.Decode(png);
        Assert.True(image.Width >= width); Assert.True(image.Height > 700);
        Assert.DoesNotContain(XDocument.Parse(svg).Descendants(), node => node.Name.LocalName is "script" or "foreignObject" or "image");
        foreach (var text in new[] { "Empty queue", "0 tasks", "Case 12", "Assigned: Avery Quinn", "Ticket: CFX-12", "Priority: Normal" }) Assert.Contains(text, svg);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "```\n"); Assert.False(fence.HasErrors);
        Assert.Equal(svg, Assert.Single(fence.Artifacts).ToSvg()); Assert.Equal(png, fence.Artifacts[0].ToPng());
        var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(restored.ToJson(), VisualArtifactInterchangeEnvelope.FromUtf8Json(restored.ToUtf8Json()).ToJson());
        var native = restored.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.Manual, native.LayoutMode);
        Assert.Equal(13, native.Nodes.Count); Assert.Equal(3, native.Groups.Count);
        Assert.Equal("240", native.Groups[0].Metadata["mermaid.config.kanban.sectionWidth"]);
        Assert.Equal("Avery Quinn", native.Nodes.Single(node => node.Id == "task12").Metadata["mermaid.meta.assigned"]);
        Assert.NotEmpty(native.ToPng());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OmittedDimensionsRetainTheExistingHostDefaultsAndDirectNativeExportBytes(bool dark) {
        const string source = "kanban\nqueue[Queue]\n  a[Task A]\ndone[Done]\n";
        var family = new MermaidTopologyRenderOptions { Id = "kanban-default-control", Width = 700, Height = 400, Padding = 19,
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family } };
        var document = new MermaidParser().ParseKanban(source).Document!; var native = document.ToTopologyChart(family);
        Assert.Equal(19, native.Viewport.Padding); Assert.All(native.Groups, group => Assert.Equal(0, group.Width)); Assert.Equal(180, native.Nodes[0].Width);
        Assert.All(native.Groups, group => Assert.DoesNotContain(group.Metadata.Keys, key => key.StartsWith("mermaid.config.kanban.", StringComparison.Ordinal)));
        var artifact = MermaidRenderer.Render(source, new MermaidRenderOptions { Kanban = family }).Artifact!;
        Assert.Equal(native.ToSvg(), artifact.ToSvg()); Assert.Equal(native.ToPng(), artifact.ToPng());
        Capture(native.Prepare(), "kanban-omitted-control-" + dark);
    }

    [Fact]
    public void AuthoredLargeColumnsRetainTheCapturedRasterAllocationLimitBeforePixelAllocation() {
        var limits = new MermaidResourceLimits { MaximumRasterPixels = 1024 };
        var result = MermaidRenderer.Render(Source(32, 1000), new MermaidRenderOptions {
            ParseOptions = new MermaidParseOptions { ResourceLimits = limits }, Kanban = new MermaidTopologyRenderOptions { Width = 390, Height = 400 }
        });
        Assert.False(result.HasErrors); Assert.NotNull(result.Artifact);
        limits.MaximumRasterPixels = 16_777_216;
        var error = Assert.Throws<MermaidResourceLimitException>(() => result.Artifact!.ToPng());
        Assert.Equal(nameof(limits.MaximumRasterPixels), error.LimitName); Assert.Contains("<svg", result.Artifact!.ToSvg());
    }

    [Theory]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    public void CompleteSourceProgramRetainsTheMaintainedFixtureAndOrdinaryMermaidPublicProducer(string variant, VisualThemeMode mode) {
        var program = ExampleRecipeSource.EmitKanban(variant, mode);
        Assert.Contains("var source = " + JsonSerializer.Serialize(Fixture()) + ";", program);
        Assert.Contains("MermaidRenderer.Render(source", program); Assert.Contains("VisualThemeMode." + mode, program);
        Assert.Contains("Width = " + (variant == "compact" ? 390 : 1100), program);
        foreach (var extension in new[] { "mmd", "svg", "png", "html", "json" }) Assert.Contains("board." + extension, program);
        Assert.DoesNotContain("{{", program); Assert.DoesNotContain("FontRegistry.Register", program);
        Assert.Throws<ArgumentOutOfRangeException>(() => ExampleRecipeSource.EmitKanban("unknown"));
    }

    internal static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "Mermaid", "kanban-source-geometry.mmd"));
    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family, FontSize = 12, TopologySizing = MermaidTopologySizing.Natural },
        Kanban = new MermaidTopologyRenderOptions { Width = width, Height = 700 }
    };
    private static string Source(double padding, double width) => "---\nconfig:\n  kanban:\n    padding: " + padding.ToString("R", CultureInfo.InvariantCulture)
        + "\n    sectionWidth: " + width.ToString("R", CultureInfo.InvariantCulture) + "\n---\nkanban\nqueue[Queue]\n  a[Task A]\nempty[Empty]\n";
    internal static void AssertContained(VisualArtifactInterchangeEnvelope facts) {
        foreach (var group in facts.Groups) {
            var tasks = facts.Nodes.Where(node => node.GroupId == group.Id).ToArray();
            for (var index = 0; index < tasks.Length; index++) {
                var task = tasks[index];
                Assert.True(task.X >= group.X + 23.999 && task.X + task.Width <= group.X + group.Width - 23.999);
                Assert.True(task.Y >= group.Y && task.Y + task.Height <= group.Y + group.Height);
                if (index > 0) Assert.True(task.Y >= tasks[index - 1].Y + tasks[index - 1].Height + 15.999);
            }
        }
        var panels = facts.Groups.ToArray();
        for (var index = 1; index < panels.Length; index++) Assert.True(panels[index].X >= panels[index - 1].X + panels[index - 1].Width + 23.999);
    }
    private static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name);
        File.WriteAllText(path + ".svg", prepared.ToSvg()); File.WriteAllBytes(path + ".png", prepared.ToPng()); File.WriteAllText(path + ".json", prepared.ToInterchangeEnvelope().ToJson());
    }
}
