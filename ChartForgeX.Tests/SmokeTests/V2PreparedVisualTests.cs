using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the public compile-once boundary and shared frame behavior.</summary>
public sealed class V2PreparedVisualTests {
    [Fact]
    public void HidingFrameHeadingRetainsModelNameAndExplicitAccessibleOverride() {
        var chart = Chart.Create().WithTitle("Revenue").AddLine("Samples", Points(1, 2));
        var context = new VisualRenderContext(frame: new VisualFrame(title: "", subtitle: ""));
        var prepared = chart.Prepare(context);
        Assert.Equal("Revenue", prepared.Accessibility.Name);
        Assert.Equal("Revenue", (string?)XDocument.Parse(prepared.ToSvg()).Root!.Attribute("aria-label"));
        Assert.DoesNotContain("data-cfx-role=\"frame-heading\"", prepared.ToSvg());
        chart.Accessibility.Name = "Explicit alternative";
        Assert.Equal("Explicit alternative", chart.Prepare(context).Accessibility.Name);
    }
    [Fact]
    public void PreparedExportsDetachFromModelThemeFontAndAccessibilityMutation() {
        var chart = Chart.Create().WithTitle("Stable title").AddLine("Samples", Points(1, 3, 2));
        var light = VisualDesignTokens.FromJson(CanonicalJson(), VisualThemeMode.Light);
        var dark = VisualDesignTokens.FromJson(CanonicalJson(), VisualThemeMode.Dark);
        var theme = new VisualTheme(light, dark);
        var context = new VisualRenderContext(theme: theme);
        var prepared = chart.Prepare(context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Title = "Changed"; chart.Series.Clear(); chart.Accessibility.Name = "Changed";
        light.Background = ChartColor.Black; light.Palette = new[] { ChartColor.Black };
        context.Font.Family = "Changed"; prepared.Accessibility.Name = "Changed";
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal("Stable title", prepared.Accessibility.Name);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void ChartAndDonutShareResolvedFrameAndExactLogicalDimensions(VisualThemeMode mode) {
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(480, 320)), themeMode: mode,
            frame: new VisualFrame("Shared heading", "Shared subtitle", showLegend: false, showSurface: true));
        var cartesian = Chart.Create().AddLine("Samples", Points(1, 3, 2)).Prepare(context);
        var donut = Chart.Create().AddDonut("Samples", Points(1, 3, 2)).Prepare(context);
        var first = XDocument.Parse(cartesian.ToSvg()); var second = XDocument.Parse(donut.ToSvg());
        var headings1 = first.Descendants().Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("frame-heading", StringComparison.Ordinal) == true).Select(element => element.ToString()).ToArray();
        var headings2 = second.Descendants().Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("frame-heading", StringComparison.Ordinal) == true).Select(element => element.ToString()).ToArray();
        Assert.Equal(headings1, headings2); Assert.NotEmpty(headings1);
        Assert.Equal("0 0 480 320", (string?)first.Root!.Attribute("viewBox"));
        var image = donut.ToRgba(new VisualRenderOptions(scale: 2, supersampling: 1));
        Assert.Equal(960, image.Width); Assert.Equal(640, image.Height);
    }

    [Fact]
    public void TransparentEmbeddingKeepsOuterPixelsClearAndExplicitDataColor() {
        var color = ChartColor.FromHex("#9742AB");
        var chart = Chart.Create().AddBar("Samples", Points(2, 4), color);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        var prepared = chart.Prepare(new VisualRenderContext(frame: new VisualFrame("", "", false, transparentBackground: true)));
        var svg = prepared.ToSvg();
        Assert.Contains(color.ToHex(), svg, StringComparison.OrdinalIgnoreCase);
        var image = prepared.ToRgba();
        Assert.Equal(0, image.Pixels[3]);
        Assert.Contains(image.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
    }

    [Fact]
    public void EmptyDonutHasObservableNoDataAndMultilineHeadingIsBounded() {
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 260)),
            frame: new VisualFrame("First line\nSecond line\nThird line", showLegend: false));
        var prepared = Chart.Create().AddDonut("Empty", Array.Empty<ChartPoint>()).Prepare(context);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.no-data");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "frame.heading-overflow");
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(2, xml.Descendants().Count(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("frame-heading", StringComparison.Ordinal) == true));
        Assert.DoesNotContain("Third line", string.Concat(xml.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value)));
    }

    [Fact]
    public void InvalidViewportsAndRasterBudgetsFailBeforeAllocation() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualSize(double.NaN, 40));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualLayoutOptions(default));
        var prepared = Chart.Create().AddDonut("Samples", Points(1, 2)).Prepare(new VisualRenderContext());
        Assert.Throws<ArgumentOutOfRangeException>(() => prepared.ToPng(new VisualRenderOptions(pixelBudget: 1)));
    }

    private static IEnumerable<ChartPoint> Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value));
    private static string CanonicalJson() {
        using var stream = typeof(VisualTheme).Assembly.GetManifestResourceStream("ChartForgeX.Themes.Tokens.evotec.chartforgex.tokens.json")!;
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
}
