using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartGridPanelThemeTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PanelPresentationIsAnExplicitChoiceIndependentOfTheOuterContext(bool usePanelThemes) {
        var firstTheme = Theme("serif", "#CA3100", "#F07530");
        var secondTheme = Theme("monospace", "#007B53", "#4CD0A5");
        var outerTheme = Theme("sans-serif", "#3344CC", "#8899FF");
        var first = ChartWithTheme("First panel", firstTheme, ThemeMode.Light);
        var second = ChartWithTheme("Second panel", secondTheme, ThemeMode.Dark);
        var grid = ChartGrid.Create().Title("Outer heading").UsePanelThemes(usePanelThemes).Add(first).Add(second);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 350)), outerTheme,
            ThemeMode.Dark, new VisualFrame(showCard: true), new FontSpec { Family = "Outer explicit font" });

        var prepared = grid.Prepare(context);
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => Text(node) == "Outer heading"
            && node.Text.Style.Font.Family == "Outer explicit font");
        AssertPanel(document, 0, "First panel", usePanelThemes ? "serif" : "Outer explicit font",
            (usePanelThemes ? firstTheme.Resolve(ThemeMode.Light) : outerTheme.Resolve(ThemeMode.Dark)).Palette[0]);
        AssertPanel(document, 1, "Second panel", usePanelThemes ? "monospace" : "Outer explicit font",
            (usePanelThemes ? secondTheme.Resolve(ThemeMode.Dark) : outerTheme.Resolve(ThemeMode.Dark)).Palette[0]);
        Assert.Same(firstTheme, first.Options.Theme);
        Assert.Equal(ThemeMode.Dark, second.Options.ThemeMode);

        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        first.Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark);
        second.FontFamily("serif");
        grid.UsePanelThemes(!usePanelThemes);
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void AuthoredPanelKeepsStandaloneNativePixelsAndFontRequest() {
        var chart = ChartWithTheme("Panel", Theme("serif", "#A34200", "#F09530"), ThemeMode.Dark)
            .ShowHeader(false).ShowCard(false).ShowPlotBackground(false).Legend(false).TransparentBackground();
        chart.Options.Padding = ChartPadding.All(8);
        var request = VisualExportRequest.ForChart(chart);
        var expected = chart.Prepare(request.Context);
        var grid = ChartGrid.Create().Columns(1).UsePanelThemes().Add(chart);
        var prepared = grid.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 260), 0),
            VisualTheme.Graphite(), ThemeMode.Light, new VisualFrame(showLegend: false, transparentBackground: true),
            new FontSpec { Family = "monospace" }));
        Assert.Equal(expected.ToRgbaImage().Pixels, prepared.ToRgbaImage().Pixels);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => Assert.Equal("serif", node.Text.Style.Font.Family));

        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(fontPath));
        chart.Raster(p => p.FontPath(fontPath));
        var authored = grid.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 260), 0),
            frame: new VisualFrame(showLegend: false, transparentBackground: true)));
        Assert.All(authored.Scene.Nodes.OfType<VisualSceneText>(), node => Assert.Equal(fontPath, node.Text.Style.Font.FilePath));
        Assert.Equal(chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToRgbaImage().Pixels, authored.ToRgbaImage().Pixels);
    }

    [Fact]
    public void PanelColorScopesKeepLiteralAndAuthoredVariablesSeparateFromTheGrid() {
        var color = ChartColor.FromHex("#345678");
        var theme = Theme("sans-serif", color.ToCss(), color.ToCss());
        var childVariables = new SvgColorVariables().Add("--panel-series", color, SvgColorRole.Series);
        var gridVariables = new SvgColorVariables().Add("--outer-series", color, SvgColorRole.Series);
        var grid = ChartGrid.Create().Theme(theme).UsePanelThemes().PanelSize(360, 260)
            .SvgColorVariables(gridVariables)
            .Add(ChartWithTheme("Literal", theme, ThemeMode.Light))
            .Add(ChartWithTheme("Mapped", theme, ThemeMode.Light).SvgColorVariables(childVariables));
        var prepared = grid.Prepare(VisualExportRequest.ForGrid(grid).Context);
        var svg = prepared.ToSvg();
        var document = XDocument.Parse(svg);
        Assert.DoesNotContain("--outer-series", Panel(document, 0).ToString());
        Assert.DoesNotContain("--outer-series", Panel(document, 1).ToString());
        Assert.DoesNotContain("var(", Panel(document, 0).ToString());
        Assert.Contains("var(--panel-series", Panel(document, 1).ToString());
        Assert.Contains(color.ToCss(), Panel(document, 0).ToString());
        Assert.Contains("var(--panel-series", grid.ToHtmlFragment());

        var pixels = prepared.ToPng();
        var artifact = prepared.ToArtifact("panel-themes");
        Assert.Contains("var(--panel-series", artifact.ToSvg());
        Assert.Contains("var(--panel-series", artifact.Clone().ToHtmlPage());
        Assert.Equal(pixels, artifact.Clone().ToPng());
        childVariables.Add("--later", ChartColor.Black);
        gridVariables.Add("--later", ChartColor.Black);
        grid.Charts[1].SvgColorVariables((SvgColorVariables?)null);
        Assert.Equal(svg, prepared.ToSvg());

        var explicitExport = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: gridVariables)));
        Assert.DoesNotContain("--outer-series", Panel(explicitExport, 0).ToString());
        Assert.Contains("--panel-series", Panel(explicitExport, 1).ToString());
        grid.UsePanelThemes(false);
        Assert.Contains("--outer-series", Panel(XDocument.Parse(grid.ToSvg()), 1).ToString());
    }

    [Fact]
    public void NestedColorScopesResolveGradientDefinitionsAndExportIdentityAtTheirOwnBoundary() {
        var color = ChartColor.FromHex("#123456");
        var variables = new SvgColorVariables().Add("--child", color, SvgColorRole.Series);
        var inner = new VisualSceneBuilder(new VisualSize(40, 40), new FontSpec());
        inner.RectGradient(new ChartRect(0, 0, 40, 40), new ChartPoint(0, 0), new ChartPoint(40, 0), new[] {
            new VisualGradientStop(0, color, SvgPaint.Of(color, SvgColorRole.Series)),
            new VisualGradientStop(1, ChartColor.White)
        });
        var parent = new VisualSceneBuilder(new VisualSize(100, 50), new FontSpec());
        parent.Append(inner.Build(), 0, 0, "literal", new VisualSceneColorScope(null));
        parent.Append(inner.Build(), 50, 0, "mapped", new VisualSceneColorScope(variables));
        var scene = parent.Build();
        var options = new VisualSvgOptions(colorVariables: new SvgColorVariables().Add("--outer", color, SvgColorRole.Series));
        var svg = VisualSceneSvgRenderer.Render(scene, options: options);
        var gradients = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "linearGradient").ToArray();
        Assert.Equal(2, gradients.Length);
        Assert.DoesNotContain("var(", gradients[0].ToString());
        Assert.Contains("var(--child", gradients[1].ToString());
        Assert.DoesNotContain("--outer", svg);
        Assert.NotEqual(gradients[0].Attribute("id")!.Value, gradients[1].Attribute("id")!.Value);

        var nested = new VisualSceneBuilder(new VisualSize(100, 50), new FontSpec());
        nested.Append(scene, 0, 0, "nested", new VisualSceneColorScope(null));
        Assert.Contains("var(--child", VisualSceneSvgRenderer.Render(nested.Build(), options: options));
        Assert.Equal(svg, VisualSceneSvgRenderer.Render(scene, options: options));
        var alternative = new VisualSceneBuilder(new VisualSize(100, 50), new FontSpec());
        alternative.Append(inner.Build(), 0, 0, "literal", new VisualSceneColorScope(null));
        alternative.Append(inner.Build(), 50, 0, "mapped", new VisualSceneColorScope(
            new SvgColorVariables().Add("--different-child", color, SvgColorRole.Series)));
        Assert.NotEqual(VisualSceneSvgRenderer.Identity(scene, null, null, null, false),
            VisualSceneSvgRenderer.Identity(alternative.Build(), null, null, null, false));
    }

    private static VisualTheme Theme(string family, string light, string dark) => VisualTheme.Graphite().Configure(builder => {
        builder.Typography = builder.Typography.WithFamily(family);
        builder.Light.Palette = new[] { ChartColor.FromHex(light) };
        builder.Dark.Palette = new[] { ChartColor.FromHex(dark) };
    });
    private static Chart ChartWithTheme(string title, VisualTheme theme, ThemeMode mode) => Chart.Create()
        .Title(title).Theme(theme).ThemeMode(mode).Size(360, 260).DataLabels(false)
        .AddBar("Values", new[] { new ChartPoint(0, 2), new ChartPoint(1, 4) });
    private static string Text(VisualSceneText node) => string.Join("\n", node.Text.Lines.Select(line => line.Text));
    private static XElement Panel(XDocument document, int index) => document.Descendants().Single(element =>
        (string?)element.Attribute("data-cfx-role") == "panel" && (string?)element.Attribute("data-cfx-source-id") == "panel-" + index);
    private static void AssertPanel(XDocument document, int index, string heading, string family, ChartColor color) {
        var panel = Panel(document, index);
        Assert.Contains(panel.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "frame-heading"
            && element.Value == heading && element.Descendants().Any(text => PrimaryFamily((string?)text.Attribute("font-family")) == family));
        Assert.Contains(panel.Descendants(), element => (string?)element.Attribute("fill") == color.ToCss());
    }
    // SVG font stacks end with a generic family, so the authored family is the first entry.
    private static string? PrimaryFamily(string? stack) => stack?.Split(',')[0].Trim();
}
