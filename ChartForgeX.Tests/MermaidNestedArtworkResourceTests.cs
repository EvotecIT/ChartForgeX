using System.Text;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidNestedArtworkResourceTests {
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void DirectAndFencedSourceExportKeepsTheOuterImagePropertySpanForNestedResourceFailure(bool fenced, bool dataSvg) {
        const string source = "flowchart LR\nimage@{img: 'key'}";
        var artifact = fenced
            ? Assert.Single(new MermaidVisualMarkupParser().Parse("Intro\n```mermaid\n" + source + "\n```").Artifacts)
            : Assert.IsType<VisualArtifact>(MermaidRenderer.Render(source).Artifact);
        var header = "data:image/x-portable-pixmap;base64," + Convert.ToBase64String(Encoding.ASCII.GetBytes("P6\n6000 6000\n255\n"));
        var body = "<rect width='32' height='32' fill='#24A060'/><image width='32' height='32' href='" + header + "'/>";
        var artwork = dataSvg ? TopologyIconArtwork.Image("data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 32 32'>" + body + "</svg>"))) : TopologyIconArtwork.InlineSvg(body, "0 0 32 32");
        var chart = TopologyChart.Create().WithTitle("").WithViewport(320, 240, 12).WithLegend(null); chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.Nodes.Add(new TopologyNode { Id = "image", Label = "Image", X = 40, Y = 40, Width = 32, Height = 32,
            DisplayMode = TopologyNodeDisplayMode.Artwork, Artwork = artwork, ShowLabel = false, ShowStatusBadge = false, PreserveDisplayModeSize = true });
        chart.WithRenderOptions(new TopologyRenderOptions { FitContentToViewport = false, IncludeLegend = false, IncludeTitle = false });
        // This supported host model replacement retains the actual source export policy and image property span.
        artifact.Model = chart; artifact.NaturalSize = new VisualArtifactSize(320, 240); artifact.PreserveNaturalSize = true;
        foreach (var export in new Action[] { () => artifact.ToPng(), () => artifact.ToSvg(), () => artifact.ToHtmlPage() }) {
            var failure = Assert.Throws<MermaidResourceLimitException>(export);
            Assert.Equal("NativeRasterResourceBytes", failure.LimitName);
            Assert.Equal(2, failure.Span.Line); Assert.Equal(source.Split('\n')[1].IndexOf("'key'", StringComparison.Ordinal) + 1, failure.Span.Column);
            var native = Assert.IsType<VisualResourceLimitException>(failure.InnerException); Assert.Equal("image", native.ItemId);
        }
    }
}
