using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PolarGridShapeTests {
    [Theory]
    [InlineData("circle", false, 320)]
    [InlineData("polygon", true, 960)]
    public async Task MermaidGuidesRemainVisibleInStaticHosts(string shape, bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var document = new MermaidParser().ParseRadar("radar-beta\naxis A, B, C\ncurve c{17, 31, 73}\nmax 100\nticks 3\ngraticule " + shape).Document!;
        var chart = document.ToChart(new MermaidRadarRenderOptions { Width = 520, Height = 380 })
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style=\"margin:0\">" + chart.ToHtmlFragment() + "</body></html>", width, 460);
        var rings = session.Page.Locator("[data-cfx-role=\"radar-ring\"]");
        Assert.Equal(3, await rings.CountAsync());
        Assert.All(await rings.EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.tagName)"),
            tag => Assert.Equal(shape == "circle" ? "ellipse" : "path", tag));
        Assert.Equal(3, await session.Page.Locator("[data-cfx-role=\"radar-point\"]").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(captures, "radar-" + shape + "-" + (dark ? "dark-" : "light-") + width + ".png"), FullPage = true
            });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(true, ChartPolarGridShape.Automatic, false)]
    [InlineData(true, ChartPolarGridShape.Circle, true)]
    [InlineData(true, ChartPolarGridShape.Polygon, false)]
    [InlineData(false, ChartPolarGridShape.Automatic, true)]
    [InlineData(false, ChartPolarGridShape.Circle, true)]
    [InlineData(false, ChartPolarGridShape.Polygon, false)]
    public void SharedPolarGuidesSelectShapesWithoutChangingObservedPoints(bool radar, ChartPolarGridShape shape, bool circles) {
        var values = new[] { new ChartPoint(1, 15), new ChartPoint(2, 35), new ChartPoint(3, 25), new ChartPoint(4, 45) };
        var chart = Chart.Create().WithSize(520, 380).WithHeader(false).WithLegend(false).WithDataLabels(false).WithYAxisBounds(0, 50);
        if (radar) chart.AddRadarLine("Signal", values);
        else chart.AddPolar("Signal", values);
        var original = Prepare(chart);
        chart.WithPolarGridShape(shape);
        var prepared = Prepare(chart);
        var role = radar ? "radar-ring" : "polar-ring";
        var rings = prepared.Scene.Nodes.Where(node => node.Role == role).ToArray();
        Assert.NotEmpty(rings);
        Assert.All(rings, node => {
            if (circles) Assert.IsType<VisualSceneEllipse>(node);
            else Assert.True(Assert.IsType<VisualScenePath>(node).Close);
        });
        var pointRole = radar ? "radar-point" : "polar-point";
        Assert.Equal(original.Regions.Where(region => region.Role == pointRole).Select(region => region.Bounds),
            prepared.Regions.Where(region => region.Role == pointRole).Select(region => region.Bounds));
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.All(svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role),
            element => Assert.Equal(circles ? "ellipse" : "path", element.Name.LocalName));
        var png = prepared.ToPng();
        Assert.NotEmpty(png);
        if (circles == radar) Assert.False(original.ToPng().SequenceEqual(png), "Changing guide geometry must change native PNG ink.");
        var text = prepared.ToSvg();
        chart.Options.PolarGridShape = circles ? ChartPolarGridShape.Polygon : ChartPolarGridShape.Circle;
        Assert.Equal(text, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithPolarGridShape((ChartPolarGridShape)99));
    }

    [Theory]
    [InlineData("", ChartPolarGridShape.Circle)]
    [InlineData("graticule circle", ChartPolarGridShape.Circle)]
    [InlineData("graticule polygon", ChartPolarGridShape.Polygon)]
    public void MermaidGraticuleMapsToTheNativeOwner(string directive, ChartPolarGridShape expected) {
        var source = "radar-beta\naxis A, B, C\ncurve c{1, 2, 3}\n" + directive;
        var result = new MermaidParser().ParseRadar(source);
        Assert.False(result.HasErrors);
        Assert.Equal(expected, result.Document!.ToChart().Options.PolarGridShape);
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        Assert.NotNull(rendered.Artifact);
        Assert.NotEmpty(rendered.Artifact!.ToPng());
    }

    [Theory]
    [InlineData("square")]
    [InlineData("Circle")]
    [InlineData("")]
    public void InvalidAuthoredGraticulesProduceLocatedErrors(string value) {
        var result = MermaidRenderer.Render("radar-beta\naxis A, B, C\ncurve c{1, 2, 3}\ngraticule " + value);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Span.Line == 4);
    }

    [Fact]
    public void DirectDocumentConversionRejectsUnknownGridShapes() {
        var document = new MermaidParser().ParseRadar("radar-beta\naxis A, B, C\ncurve c{1, 2, 3}").Document!;
        document.Graticule = "square";
        Assert.Throws<ArgumentException>(() => document.ToChart());
    }

    [Theory]
    [InlineData(ChartScaleKind.Linear, false, 3)]
    [InlineData(ChartScaleKind.Linear, true, 3)]
    [InlineData(ChartScaleKind.Logarithmic, false, 3)]
    [InlineData(ChartScaleKind.Logarithmic, true, 3)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, false, 3)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, true, 3)]
    [InlineData(ChartScaleKind.Linear, false, 1)]
    public void ExactGuideCountsPreserveScaleAndSourceCoordinates(ChartScaleKind kind, bool reversed, int count) {
        var chart = Chart.Create().WithSize(520, 380).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithYAxisBounds(10, 1000).WithPolarGridShape(ChartPolarGridShape.Circle)
            .AddRadarLine("Signal", new[] { new ChartPoint(1, 10), new ChartPoint(2, 100), new ChartPoint(3, 1000) });
        chart.Options.YAxis.Scale = kind;
        chart.Options.YAxis.Reversed = reversed;
        var original = Prepare(chart);
        chart.Options.PolarGridRingCount = count;
        var prepared = Prepare(chart);
        var rings = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "radar-ring").ToArray();
        Assert.Equal(count, rings.Length);
        for (var index = 0; index < count; index++) Assert.Equal(rings[count - 1].Rx * (index + 1d) / count, rings[index].Rx, 7);
        Assert.Equal(original.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds),
            prepared.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.PolarGridRingCount = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.PolarGridRingCount = 101);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(32)]
    public void MermaidTickCountProducesTheRequestedConcentricGuides(int count) {
        var result = new MermaidParser().ParseRadar("radar-beta\naxis A, B, C\ncurve c{17, 31, 73}\nticks " + count);
        Assert.False(result.HasErrors);
        var chart = result.Document!.ToChart();
        Assert.Equal(73, chart.Options.YAxis.Maximum);
        Assert.Equal(count, Prepare(chart).Scene.Nodes.Count(node => node.Role == "radar-ring"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UnboundedTickCountsProduceLocatedErrors(int count) {
        var result = MermaidRenderer.Render("radar-beta\naxis A, B, C\ncurve c{1, 2, 3}\nticks " + count);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Span.Line == 4);
    }

    [Theory]
    [InlineData(33)]
    [InlineData(101)]
    public void MermaidCapsLargeGuideCountsWithALocatedWarningAndRetainedSourceValue(int count) {
        var result = new MermaidParser().ParseRadar("radar-beta\naxis A, B, C\ncurve c{17, 31, 73}\nticks " + count);
        Assert.False(result.HasErrors);
        Assert.Equal(count, result.Document!.Ticks);
        Assert.Contains(result.Diagnostics, item => item.Span.Line == 4 && item.Severity == MermaidDiagnosticSeverity.Warning);
        Assert.Equal(32, result.Document.ToChart().Options.PolarGridRingCount);
        Assert.Equal(32, Prepare(result.Document.ToChart()).Scene.Nodes.Count(node => node.Role == "radar-ring"));
        Assert.Equal(count.ToString(System.Globalization.CultureInfo.InvariantCulture), result.Document.ToVisualArtifact().Metadata["mermaid.ticks"]);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
