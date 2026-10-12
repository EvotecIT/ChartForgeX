using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFinalTypographyOwnerTests {
    [Theory]
    [InlineData(10.5, 360, true, false)]
    [InlineData(256, 360, true, false)]
    [InlineData(256, 800, true, true)]
    [InlineData(256, 360, false, true)]
    public async Task WardleyBoundaryAnnotationsRetainTheirCompleteCircle(double fontSize, int width, bool hostOwnsFrame, bool dark) {
        var theme = dark ? ChartTheme.Dark() : ChartTheme.Light();
        theme.FontFamily = "Arial"; theme.DataLabelFontSize = fontSize;
        var map = WardleyMapBlock.Create().WithSize(width, 320).WithTheme(theme);
        map.Options.HostOwnsFrame = hostOwnsFrame;
        map.AddNode("api", "API", .5, .5);
        map.AddAnnotation(1, "", 1, 1);
        map.AddAnnotation(2, "", 1, 0);
        map.AddAnnotation(3, "", 0, 1);
        map.AddAnnotation(4, "", 0, 0);
        var svg = map.ToSvg(); var png = map.ToPng();
        await Capture(svg, png, "wardley-boundary-" + fontSize + "-" + width + "-" + hostOwnsFrame + "-" + dark, width, 320);
        var circles = XDocument.Parse(svg).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "wardley-annotation").ToArray();
        Assert.Equal(4, circles.Length);
        foreach (var circle in circles) {
            var radius = (double)circle.Attribute("r")! + .5;
            Assert.InRange((double)circle.Attribute("cx")!, radius, width - radius);
            Assert.InRange((double)circle.Attribute("cy")!, radius, 320 - radius);
        }
        if (fontSize > 16) {
            var bottom = circles.Max(e => (double)e.Attribute("cy")! + (double)e.Attribute("r")! + .5);
            var builder = new VisualSceneBuilder(new VisualSize(width, 320), FontSpec.FromFamily("Arial"));
            var stages = XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == "text" &&
                new[] { "Genesis", "Custom", "Product", "Commodity" }.Contains(e.Value)).ToArray();
            Assert.Equal(4, stages.Length);
            Assert.All(stages, label => Assert.True((double)label.Attribute("y")! - builder.TextAscent((double)label.Attribute("font-size")!, 600) > bottom));
        }
    }

    [Theory]
    [InlineData(420, 1800, ChartLegendPosition.Right)]
    [InlineData(420, 1800, ChartLegendPosition.Bottom)]
    [InlineData(760, 420, ChartLegendPosition.Right)]
    public async Task OversizedLegendsRetainCaptionsWhenHeightAlreadyFits(int width, int height, ChartLegendPosition position) {
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\npie\n\"Saved\": 8\n\"Failed\": 2");
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<Chart>(result.Artifact!.Model).WithSize(width, height).WithHeader(false);
        chart.Options.LegendPosition = position;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var texts = XDocument.Parse(prepared.ToSvg()).Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value).ToArray();
        Assert.Contains("Saved", texts); Assert.Contains("Failed", texts);
        var sliceCaptions = XDocument.Parse(prepared.ToSvg()).Descendants().Where(e => e.Name.LocalName == "text" &&
            (string?)e.Parent?.Attribute("data-cfx-role") == "data-label").Select(e => e.Value).ToArray();
        Assert.Contains(sliceCaptions, text => text.StartsWith("Saved", StringComparison.Ordinal));
        Assert.Contains(sliceCaptions, text => text.StartsWith("Failed", StringComparison.Ordinal));
        await Capture(prepared.ToSvg(), prepared.ToPng(), "legend-" + width + "-" + height + "-" + position, width, height);
    }

    [Theory]
    [InlineData(360, 320, ChartDataLabelPlacement.Inside, false)]
    [InlineData(760, 420, ChartDataLabelPlacement.Inside, false)]
    [InlineData(360, 320, ChartDataLabelPlacement.Outside, false)]
    [InlineData(760, 420, ChartDataLabelPlacement.Above, false)]
    [InlineData(360, 320, ChartDataLabelPlacement.Inside, true)]
    [InlineData(760, 420, ChartDataLabelPlacement.Inside, true)]
    public async Task OversizedPieCaptionsFitTheirNativePlacement(int width, int height, ChartDataLabelPlacement placement, bool donut) {
        var chart = Chart.Create().WithSize(width, height).WithLegend(false).WithHeader(false)
            .WithXLabels("Saved", "Failed");
        var points = new[] { new ChartPoint(1, 8), new ChartPoint(2, 2) };
        if (donut) chart.AddDonut("Results", points); else chart.AddPie("Results", points);
        chart.Options.ShowDataLabels = true; chart.Options.DataLabelPlacement = placement;
        chart.Options.DataLabelStyle.WithFontFamily("Arial").WithFontSize(256);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = XDocument.Parse(prepared.ToSvg()).Descendants().Where(e => e.Name.LocalName == "text" &&
            (string?)e.Parent?.Attribute("data-cfx-role") == "data-label").ToArray();
        Assert.Equal(2, labels.Length);
        Assert.Contains(labels, e => e.Value == "80%"); Assert.Contains(labels, e => e.Value == "20%");
        Assert.All(labels, e => Assert.True((double)e.Attribute("font-size")! < 256));
        Assert.Equal(256, chart.Options.DataLabelStyle.FontSize);
        if (placement == ChartDataLabelPlacement.Inside) {
            var bounds = prepared.Regions.First(region => region.Role == (donut ? "donut-slice" : "pie-slice")).Bounds;
            var cx = bounds.Left + bounds.Width / 2; var cy = bounds.Top + bounds.Height / 2; var radius = bounds.Width / 2;
            var builder = new VisualSceneBuilder(new VisualSize(width, height), FontSpec.FromFamily("Arial"));
            foreach (var label in labels) {
                var size = (double)label.Attribute("font-size")!;
                var metrics = builder.MeasureText(label.Value, size);
                var left = (double)label.Attribute("x")!; var top = (double)label.Attribute("y")! - builder.TextAscent(size);
                var nearX = Math.Max(0, Math.Max(left - cx, cx - left - metrics.Width));
                var nearY = Math.Max(0, Math.Max(top - cy, cy - top - metrics.Height));
                if (donut) Assert.True(nearX * nearX + nearY * nearY >= Math.Pow(radius * chart.Options.DonutInnerRadiusRatio, 2) - .1);
                foreach (var x in new[] { left, left + metrics.Width })
                    foreach (var y in new[] { top, top + metrics.Height })
                        Assert.True(Math.Pow(x - cx, 2) + Math.Pow(y - cy, 2) <= radius * radius + .1);
            }
        }
        await Capture(prepared.ToSvg(), prepared.ToPng(), "pie-" + width + "-" + placement + "-" + donut, width, height);
    }

    [Theory]
    [InlineData(32, false, false)]
    [InlineData(256, false, false)]
    [InlineData(256, true, false)]
    [InlineData(32, false, true)]
    [InlineData(256, true, true)]
    public async Task ClassMultiplicitiesUseFittedEndpointMetrics(int size, bool fenced, bool padded) {
        var sourceLabel = padded ? " 1 " : "1";
        var targetLabel = padded ? " 0..* " : "0..*";
        var source = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':" + size + "}}}%%\nclassDiagram\nA \"" + sourceLabel + "\" --> \"" + targetLabel + "\" B";
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics);
        Assert.Equal(size, result.Document!.Configuration.FontSize);
        var artifact = result.Artifact!;
        if (fenced) {
            var parsed = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
            Assert.Empty(parsed.Diagnostics); artifact = Assert.Single(parsed.Artifacts);
        }
        var svg = artifact.ToSvg(); var xml = XDocument.Parse(svg);
        var labels = xml.Descendants().Where(e => e.Name.LocalName == "text" &&
            (string?)e.Parent?.Attribute("data-cfx-role") == "topology-endpoint-label").ToArray();
        Assert.Equal(2, labels.Length);
        Assert.Contains(labels, e => e.Value == sourceLabel); Assert.Contains(labels, e => e.Value == targetLabel);
        var width = (double)xml.Root!.Attribute("width")!; var height = (double)xml.Root.Attribute("height")!;
        var builder = new VisualSceneBuilder(new VisualSize(width, height), FontSpec.FromFamily("Arial"));
        foreach (var label in labels) {
            var fontSize = (double)label.Attribute("font-size")!;
            var metrics = builder.MeasureText(label.Value, fontSize, 600);
            Assert.True(fontSize <= 18);
            Assert.InRange((double)label.Attribute("x")!, 0, width - metrics.Width);
            Assert.InRange((double)label.Attribute("y")! - builder.TextAscent(fontSize, 600), 0, height - metrics.Height);
        }
        await Capture(svg, artifact.ToPng(), "class-endpoints-" + size + "-" + fenced + "-" + padded, (int)width, (int)height);
    }

    [Theory]
    [InlineData(TopologyEdgeRouting.Curved)]
    [InlineData(TopologyEdgeRouting.Straight)]
    public async Task EndpointCaptionsReserveTheirPaintedRouteBounds(TopologyEdgeRouting routing) {
        var chart = TopologyChart.Create().WithViewport(420, 220, 20).WithLegend(null)
            .AddNode("source", "Source", 40, 90, width: 60, height: 40)
            .AddNode("target", "Target", 320, 90, width: 60, height: 40)
            .AddNodePort("source", "top", TopologyEdgePort.Top)
            .AddNodePort("target", "top", TopologyEdgePort.Top)
            .AddEdge("route", "source", "target", routing: routing)
            .WithEdgeNamedPorts("route", "top", "top")
            .WithEdgeEndpointLabels("route", "outgoing records", "incoming records");
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false });
        var svg = prepared.ToSvg();
        var surfaces = XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == "rect" &&
            (string?)e.Attribute("data-cfx-role") == "topology-endpoint-label-surface").ToArray();
        Assert.Equal(2, surfaces.Length);
        foreach (var surface in surfaces) {
            var x = (double)surface.Attribute("x")!; var y = (double)surface.Attribute("y")!;
            var width = (double)surface.Attribute("width")!; var height = (double)surface.Attribute("height")!;
            Assert.InRange(x, 20 - .01, prepared.Width - 20 - width + .01);
            Assert.InRange(y, 20 - .01, prepared.Height - 20 - height + .01);
        }
        Assert.Equal(40, chart.Nodes[0].X); Assert.Equal(90, chart.Nodes[0].Y);
        await Capture(svg, prepared.ToPng(), "endpoint-route-bounds-" + routing, (int)prepared.Width, (int)prepared.Height);
    }

    private static async Task Capture(string svg, byte[] png, string name, int width, int height) {
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, height);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        File.WriteAllText(Path.Combine(captures, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(captures, name + ".native.png"), png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".browser.png"), FullPage = true });
    }
}
