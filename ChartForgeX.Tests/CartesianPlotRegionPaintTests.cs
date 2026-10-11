using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Authored region colors stay independent of equal-RGB theme tokens at static export.</summary>
public sealed class CartesianPlotRegionPaintTests {
    [Theory]
    [InlineData(1)]
    [InlineData(.35)]
    public void AuthoredAxisColorStaysLiteralWhileCaptionSurfaceRetainsHostThemePolicy(double opacity) {
        var authored = ChartColor.FromHex("#2864B4");
        var tokens = new VisualDesignTokens { Axis = authored };
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithAxes(true).WithGrid(false).WithDataLabels(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Observed", new[] { new ChartPoint(1, 1), new ChartPoint(9, 9) })
            .AddPlotRegion(new ChartPlotRegion(authored, label: "Authored", opacity: opacity));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(520, 300)), new VisualTheme(tokens, tokens),
            frame: new VisualFrame(showLegend: false, showSurface: true));
        var prepared = chart.Prepare(context);
        var literal = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions("authored-region")));
        var axisVariables = new SvgColorVariables().Add("--host-axis", authored, SvgColorRole.Axis);
        var surfaceVariables = new SvgColorVariables().Add("--host-surface", tokens.Surface, SvgColorRole.Surface);
        var allVariables = surfaceVariables.Clone().Add("--host-axis", authored, SvgColorRole.Axis);
        XDocument Export(SvgColorVariables variables) => XDocument.Parse(prepared.ToSvg(new VisualSvgOptions("authored-region", variables)));
        var axisMapped = Export(axisVariables); var surfaceMapped = Export(surfaceVariables); var allMapped = Export(allVariables);

        // Positive control: the same host variable still owns genuine axes.
        var axes = axisMapped.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "axis-x").ToArray();
        Assert.NotEmpty(axes);
        Assert.All(axes, axis => Assert.StartsWith("var(--host-axis,", (string?)axis.Attribute("stroke")));
        Assert.Equal(authored.WithOpacity(opacity).ToCss(), Paint(literal, "plot-region-fill"));
        foreach (var role in new[] { "plot-region-fill", "plot-region-label-backplate", "plot-region-label" }) {
            Assert.Equal(Paint(literal, role), Paint(axisMapped, role));
            Assert.Equal(Paint(surfaceMapped, role), Paint(allMapped, role));
        }
        Assert.Equal(Paint(literal, "plot-region-fill"), Paint(allMapped, "plot-region-fill"));
        Assert.Contains("var(--host-surface,", Paint(allMapped, "plot-region-label-backplate"));
        Assert.Contains(authored.ToHex(), Paint(allMapped, "plot-region-label-backplate"));
        var darkHost = new SvgColorVariables().Add("--host-surface", ChartColor.Black, SvgColorRole.Surface)
            .Add("--host-axis", ChartColor.White, SvgColorRole.Axis);
        var ink = Assert.Single(darkHost.GetVariablesForSvg(allMapped.ToString()), variable =>
            Paint(allMapped, "plot-region-label").StartsWith("var(" + variable.Name + ",", StringComparison.Ordinal));
        Assert.Equal(ChartColor.White, ink.Color);

        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var region = Assert.Single(prepared.Regions, item => item.Role == "plot-region").Bounds;
        var alpha = authored.WithOpacity(opacity).A;
        byte Composite(byte source, byte surface) => (byte)Math.Round((source * alpha + surface * (255 - alpha)) / 255d);
        AssertPixel(image, region.Left + 8, region.Top + 8, ChartColor.FromRgb(
            Composite(authored.R, tokens.Surface.R), Composite(authored.G, tokens.Surface.G), Composite(authored.B, tokens.Surface.B)));
        var plate = literal.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "plot-region-label-backplate");
        double Coordinate(string name) => double.Parse(plate.Attribute(name)!.Value, CultureInfo.InvariantCulture);
        AssertPixel(image, Coordinate("x") + 2, Coordinate("y") + Coordinate("height") / 2,
            ChartColor.FromHex(Paint(literal, "plot-region-label-backplate")));
        Capture(prepared, literal, allMapped, opacity);
    }

    private static string Paint(XDocument svg, string role) => svg.Descendants()
        .Single(element => (string?)element.Attribute("data-cfx-role") == role).Attribute("fill")!.Value;

    private static void AssertPixel(RgbaImage image, double x, double y, ChartColor expected) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        Assert.InRange((int)image.Pixels[offset], expected.R - 1, expected.R + 1);
        Assert.InRange((int)image.Pixels[offset + 1], expected.G - 1, expected.G + 1);
        Assert.InRange((int)image.Pixels[offset + 2], expected.B - 1, expected.B + 1);
        Assert.Equal(255, image.Pixels[offset + 3]);
    }

    private static void Capture(PreparedVisual prepared, XDocument literal, XDocument mapped, double opacity) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var name = Path.Combine(directory, "plot-region-authored-axis-" + opacity.ToString(CultureInfo.InvariantCulture));
        File.WriteAllText(name + ".svg", literal.ToString()); File.WriteAllText(name + "-mapped.svg", mapped.ToString());
        File.WriteAllBytes(name + ".png", prepared.ToPng());
    }
}
