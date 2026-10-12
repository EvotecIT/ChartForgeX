using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFinalTypographyOwnerTests {
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
    [InlineData(32, false)]
    [InlineData(256, false)]
    [InlineData(256, true)]
    public async Task ClassMultiplicitiesUseFittedEndpointMetrics(int size, bool fenced) {
        var source = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':" + size + "}}}%%\nclassDiagram\nA \"1\" --> \"0..*\" B";
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
        Assert.Contains(labels, e => e.Value == "1"); Assert.Contains(labels, e => e.Value == "0..*");
        var width = (double)xml.Root!.Attribute("width")!; var height = (double)xml.Root.Attribute("height")!;
        var builder = new VisualSceneBuilder(new VisualSize(width, height), FontSpec.FromFamily("Arial"));
        foreach (var label in labels) {
            var fontSize = (double)label.Attribute("font-size")!;
            var metrics = builder.MeasureText(label.Value, fontSize, 600);
            Assert.True(fontSize <= 18);
            Assert.InRange((double)label.Attribute("x")!, 0, width - metrics.Width);
            Assert.InRange((double)label.Attribute("y")! - builder.TextAscent(fontSize, 600), 0, height - metrics.Height);
        }
        await Capture(svg, artifact.ToPng(), "class-endpoints-" + size + "-" + fenced, (int)width, (int)height);
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
