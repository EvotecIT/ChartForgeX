using System.Xml.Linq;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidQuadrantRegionTests {
    private const string Labels = "quadrantChart\ntitle Work priorities\nx-axis Low effort --> High effort\ny-axis Low impact --> High impact\n"
        + "quadrant-1 Plan\nquadrant-2 Prioritize\nquadrant-3 Consider\nquadrant-4 Reassess\n";

    [Theory]
    [InlineData("quadrant-basic")]
    [InlineData("quadrant-no-points")]
    public void SourceLabelsAndValuesMatchSharedReferenceExpectations(string name) {
        var root = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures");
        var parsed = new MermaidParser().ParseQuadrant(File.ReadAllText(Path.Combine(root, name + ".mmd")));
        Assert.False(parsed.HasErrors);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, name + ".expected.json")));
        var expected = json.RootElement.GetProperty("quadrant");
        Assert.Equal(expected.GetProperty("labels").EnumerateArray().Select(v => v.GetString()),
            Enumerable.Range(1, 4).Select(i => parsed.Document!.QuadrantLabels.TryGetValue(i, out var text) ? text : ""));
        var points = expected.GetProperty("points").EnumerateArray().ToArray();
        Assert.Equal(points.Length, parsed.Document!.Points.Count);
        for (var i = 0; i < points.Length; i++) {
            Assert.Equal(points[i][0].GetString(), parsed.Document.Points[i].Label);
            Assert.Equal(points[i][1].GetDouble(), parsed.Document.Points[i].X);
            Assert.Equal(points[i][2].GetDouble(), parsed.Document.Points[i].Y);
        }
    }

    [Theory]
    [InlineData(360, false, false)]
    [InlineData(360, true, true)]
    [InlineData(960, false, true)]
    [InlineData(960, true, false)]
    public void AuthoredQuadrantsOccupyTheirNormalizedRegionsWithAndWithoutPoints(int width, bool dark, bool points) {
        var source = (dark ? "---\nconfig:\n  theme: dark\n---\n" : "") + Labels
            + (points ? "Opportunity: [0.2, 0.8]\nInvestment: [0.8, 0.8]\n" : "");
        var parsed = new MermaidParser().ParseQuadrant(source);
        Assert.False(parsed.HasErrors, string.Join("; ", parsed.Diagnostics.Select(d => d.Message)));
        var chart = parsed.Document!.ToChart(new MermaidQuadrantRenderOptions { Width = width, Height = 440 });
        Assert.Equal(4, chart.PlotRegions.Count);
        Assert.Equal(new[] { "Plan", "Prioritize", "Consider", "Reassess" }, chart.PlotRegions.Select(r => r.Label));
        Assert.Equal(new[] { .5, 0, 0, .5 }, chart.PlotRegions.Select(r => r.XMinimum!.Value));
        Assert.Equal(new[] { 1, .5, .5, 1 }, chart.PlotRegions.Select(r => r.XMaximum!.Value));
        Assert.Equal(new[] { .5, .5, 0, 0 }, chart.PlotRegions.Select(r => r.YMinimum!.Value));
        Assert.Equal(new[] { 1, 1, .5, .5 }, chart.PlotRegions.Select(r => r.YMaximum!.Value));
        Assert.Equal(points ? 2 : 0, chart.Series[0].Points.Count);
        if (points) Assert.Equal(.2, chart.Series[0].Points[0].X);

        // Isolate native region paint: guide lines can cross the interior sample locations.
        chart.WithGrid(false);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var regions = prepared.Regions.Where(r => r.Role == "plot-region").ToArray();
        Assert.Equal(4, regions.Length);
        Assert.Equal(regions[0].Bounds.Width, regions[1].Bounds.Width, 6);
        Assert.Equal(regions[0].Bounds.Height, regions[2].Bounds.Height, 6);
        Assert.True(regions[0].Bounds.Left > regions[1].Bounds.Left);
        Assert.True(regions[0].Bounds.Top < regions[3].Bounds.Top);
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(n => n.Role == "plot-region-label").ToArray();
        Assert.Equal(4, captions.Length);
        Assert.DoesNotContain(prepared.Diagnostics, d => d.Code == "cartesian.plot-region-label-overflow");
        var svg = prepared.ToSvg();
        var groups = XDocument.Parse(svg).Descendants().Where(n => (string?)n.Attribute("data-cfx-role") == "plot-region").ToArray();
        Assert.Equal(new[] { "Plan", "Prioritize", "Consider", "Reassess" }, groups.Select(n => (string?)n.Attribute("data-cfx-label")));
        var png = prepared.ToPng();
        var image = RasterImageDecoder.Decode(png);
        Assert.Equal(width, image.Width);
        Assert.Equal(440, image.Height);
        // Interior background samples prove the regions are painted in the native export.
        var colors = regions.Select(r => {
            var offset = ((int)(r.Bounds.Top + r.Bounds.Height * .8) * image.Width + (int)(r.Bounds.Left + r.Bounds.Width * .7)) * 4;
            return (image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2]);
        }).ToArray();
        Assert.Equal(4, colors.Distinct().Count());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = $"quadrant-{width}-{dark}-{points}";
            File.WriteAllText(Path.Combine(capture, name + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(capture, name + ".png"), png);
        }
    }

    [Fact]
    public void UnlabelledQuadrantsKeepAllFourRegionsWithoutInventedCaptions() {
        var parsed = new MermaidParser().ParseQuadrant("quadrantChart\nquadrant-2 Prioritize\nObserved: [0.1, 0.9]");
        Assert.False(parsed.HasErrors);
        var chart = parsed.Document!.ToChart();
        Assert.Equal(4, chart.PlotRegions.Count);
        Assert.Equal(new[] { "", "Prioritize", "", "" }, chart.PlotRegions.Select(r => r.Label));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), n => n.Role == "plot-region-label");
    }

    [Fact]
    public void DirectAndMarkdownArtifactsRetainQuadrantGeometryAndSourceValues() {
        var source = Labels + "Opportunity: [0.2, 0.8]\n";
        var direct = MermaidRenderer.Render(source);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "```\n");
        Assert.False(direct.HasErrors);
        Assert.False(fence.HasErrors);
        foreach (var artifact in new[] { direct.Artifact!, Assert.Single(fence.Artifacts) }) {
            var chart = Assert.IsType<Chart>(artifact.Model);
            Assert.Equal(4, chart.PlotRegions.Count);
            Assert.Equal("4", artifact.Metadata["mermaid.quadrants"]);
            Assert.Equal("1", artifact.Metadata["mermaid.points"]);
            Assert.Equal(.2, Assert.Single(chart.Series[0].Points).X);
            Assert.Equal(.8, Assert.Single(chart.Series[0].Points).Y);
            Assert.Equal("Opportunity", chart.Series[0].PointLabels[0]);
        }
    }
}
