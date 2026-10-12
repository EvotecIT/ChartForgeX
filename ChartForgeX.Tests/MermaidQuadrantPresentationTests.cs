using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidQuadrantPresentationTests {
    private const string Source = "quadrantChart\ntitle Priorities\nquadrant-1 Plan\nquadrant-2 Prioritize\nquadrant-3 Consider\nquadrant-4 Reassess\n";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void HostThemeControlsRegionFillsAndCaptionContrast(bool explicitContext, bool dark) {
        var chart = new MermaidParser().ParseQuadrant(Source).Document!.ToChart();
        var palette = new[] { ChartColor.FromHex("#E02030"), ChartColor.FromHex("#20B040"), ChartColor.FromHex("#3040D0"), ChartColor.FromHex("#D08010") };
        chart.WithTheme(ChartTheme.GraphiteLight().WithPalette(palette));
        var request = VisualExportRequest.ForChart(chart).Context;
        var tokens = new VisualDesignTokens { Palette = palette, Surface = dark ? ChartColor.Black : ChartColor.White };
        var context = explicitContext ? new VisualRenderContext(request.Layout, new VisualTheme(tokens, tokens),
            dark ? VisualThemeMode.Dark : VisualThemeMode.Light, request.Frame, request.Font) : request;
        var prepared = chart.Prepare(context);
        var svg = XDocument.Parse(prepared.ToSvg());
        var fills = Paints(svg, "plot-region-fill");
        Assert.Equal(palette.Select(color => color.WithOpacity(.14).ToCss()), fills);
        var plates = Paints(svg, "plot-region-label-backplate");
        var inks = Paints(svg, "plot-region-label");
        for (var i = 0; i < 4; i++) {
            var plate = ChartColorMath.Blend(context.Theme.Resolve(context.ThemeMode).Surface, palette[i], .12);
            Assert.Equal(plate.ToCss(), plates[i]);
            Assert.Equal(ChartColorMath.AccessibleTextOnBackground(plate).ToCss(), inks[i]);
        }
        var variables = new SvgColorVariables().Add("--host-quadrant", palette[0], SvgColorRole.Series);
        var mapped = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions("quadrant-host", variables)));
        Assert.Contains("var(--host-quadrant,", Paints(mapped, "plot-region-fill")[0]);
        Assert.Contains("var(--host-quadrant,", Paints(mapped, "plot-region-label-backplate")[0]);
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var regions = prepared.Regions.Where(region => region.Role == "plot-region").ToArray();
        chart.PlotRegions.Clear();
        var background = chart.Prepare(context).ToRgba(new VisualRenderOptions(supersampling: 1));
        for (var i = 0; i < regions.Length; i++) {
            var bounds = regions[i].Bounds;
            var offset = ((int)(bounds.Top + 8) * image.Width + (int)(bounds.Left + 8)) * 4;
            var alpha = palette[i].WithOpacity(.14).A;
            byte Composite(byte channel, byte background) => (byte)Math.Round((channel * alpha + background * (255 - alpha)) / 255d);
            Assert.InRange((int)image.Pixels[offset], Composite(palette[i].R, background.Pixels[offset]) - 1, Composite(palette[i].R, background.Pixels[offset]) + 1);
            Assert.InRange((int)image.Pixels[offset + 1], Composite(palette[i].G, background.Pixels[offset + 1]) - 1, Composite(palette[i].G, background.Pixels[offset + 1]) + 1);
            Assert.InRange((int)image.Pixels[offset + 2], Composite(palette[i].B, background.Pixels[offset + 2]) - 1, Composite(palette[i].B, background.Pixels[offset + 2]) + 1);
        }
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var path = Path.Combine(capture, $"quadrant-host-{explicitContext}-{dark}");
            File.WriteAllText(path + ".svg", prepared.ToSvg()); File.WriteAllBytes(path + ".png", prepared.ToPng());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RootDescriptionIncludesAuthoredRegionsWithAndWithoutPoints(bool points) {
        var chart = new MermaidParser().ParseQuadrant(Source + (points ? "Observed: [0.2, 0.8]\n" : "")).Document!.ToChart();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var description = XDocument.Parse(prepared.ToSvg()).Root!.Elements().Single(node => node.Name.LocalName == "desc").Value;
        foreach (var label in new[] { "Plan", "Prioritize", "Consider", "Reassess" }) Assert.Contains(label, description);
        chart.Options.Labels.AccessibleTextFormatter = facts => "Regions: " + string.Join("; ", facts.PlotRegionLabels);
        Assert.Equal("Regions: Plan; Prioritize; Consider; Reassess", chart.Prepare(VisualExportRequest.ForChart(chart).Context).Accessibility.Description);
        chart.Accessibility.Description = "Host-authored description";
        Assert.Equal("Host-authored description", chart.Prepare(VisualExportRequest.ForChart(chart).Context).Accessibility.Description);
    }

    [Fact]
    public void TranslucentHostPaletteRetainsCaptionPaintAndContrastBindings() {
        var color = ChartColor.FromRgba(224, 32, 48, 128);
        var chart = new MermaidParser().ParseQuadrant(Source).Document!.ToChart();
        chart.WithTheme(ChartTheme.GraphiteLight().WithPalette(new[] { color }));
        var context = VisualExportRequest.ForChart(chart).Context;
        var surface = context.Theme.Resolve(context.ThemeMode).Surface;
        var prepared = chart.Prepare(context);
        var variables = new SvgColorVariables().Add("--surface", surface, SvgColorRole.Surface)
            .Add("--region", color, SvgColorRole.Series);
        var svg = prepared.ToSvg(new VisualSvgOptions("translucent-quadrant", variables));
        Assert.Contains("var(--region,", Paints(XDocument.Parse(svg), "plot-region-fill")[0]);
        Assert.Contains("var(--region,", Paints(XDocument.Parse(svg), "plot-region-label-backplate")[0]);
        var otherColor = ChartColor.FromRgba(16, 160, 224, 64);
        var other = new SvgColorVariables().Add("--surface", ChartColor.Black, SvgColorRole.Surface)
            .Add("--region", otherColor, SvgColorRole.Series);
        var expectedPlate = ChartColorMath.Blend(ChartColor.Black,
            ChartColor.FromRgb(otherColor.R, otherColor.G, otherColor.B), .12);
        var inks = other.GetVariablesForSvg(svg).Where(variable => variable.Name.StartsWith("--cfx-derived-ink-", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(inks);
        Assert.All(inks, variable => Assert.Equal(ChartColorMath.AccessibleTextOnBackground(expectedPlate), variable.Color));
        var fallbackPlate = ChartColorMath.Blend(surface, ChartColor.FromRgb(color.R, color.G, color.B), .12);
        Assert.Equal(fallbackPlate.ToCss(), Paints(XDocument.Parse(prepared.ToSvg()), "plot-region-label-backplate")[0]);
    }

    [Fact]
    public void PaletteIndicesWrapAndRejectNegativeValues() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPlotRegion(-1));
        var chart = new MermaidParser().ParseQuadrant(Source).Document!.ToChart();
        var palette = new[] { ChartColor.FromHex("#E02030"), ChartColor.FromHex("#20B040") };
        chart.WithTheme(ChartTheme.GraphiteLight().WithPalette(palette));
        chart.PlotRegions.Clear();
        chart.AddPlotRegion(new ChartPlotRegion(5, label: "Wrapped"));
        Assert.Null(chart.PlotRegions[0].Color);
        Assert.Equal(5, chart.PlotRegions[0].PaletteIndex);
        var svg = XDocument.Parse(chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg());
        Assert.Equal(palette[1].WithOpacity(.14).ToCss(), Assert.Single(Paints(svg, "plot-region-fill")));
    }

    private static string[] Paints(XDocument svg, string role) => svg.Descendants()
        .Where(node => (string?)node.Attribute("data-cfx-role") == role).SelectMany(node => node.DescendantsAndSelf())
        .Where(node => node.Attribute("fill") != null).Select(node => node.Attribute("fill")!.Value).ToArray();
}
