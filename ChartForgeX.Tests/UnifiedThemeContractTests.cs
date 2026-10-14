using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class UnifiedThemeContractTests {
    [Fact]
    public void ReusedThemeAndRetainedBuilderCannotMutateAnotherChart() {
        var original = VisualTheme.Graphite();
        var originalJson = original.ToJson();
        var first = Chart.Create().WithTheme(original);
        var second = Chart.Create().WithTheme(original);
        VisualThemeBuilder? retained = null;
        var authored = ChartColor.FromHex("#12345678");

        first.ConfigureTheme(builder => {
            retained = builder;
            builder.Light.Palette = new[] { authored };
            builder.Dark.Status.Pass = new VisualTokenColor(authored, ChartColor.White);
        });
        var detached = Assert.IsType<VisualThemeBuilder>(retained);
        detached.Light.Palette = new[] { ChartColor.Black };
        detached.Dark.Status.Pass = new VisualTokenColor(ChartColor.Black, ChartColor.White);
        var returnedStatus = first.Options.Theme.Resolve(VisualThemeMode.Dark).Status;
        returnedStatus.Pass = new VisualTokenColor(ChartColor.Black, ChartColor.White);

        Assert.Equal(authored, Assert.Single(first.Options.Theme.Resolve(VisualThemeMode.Light).Palette));
        Assert.Equal(authored, first.Options.Theme.Resolve(VisualThemeMode.Dark).Status.Pass.Fill);
        Assert.Same(original, second.Options.Theme);
        Assert.Equal(originalJson, original.ToJson());
        var configured = first.Options.Theme;
        Assert.Throws<InvalidOperationException>(() => first.ConfigureTheme(builder => {
            builder.Light.Background = ChartColor.Black;
            throw new InvalidOperationException("Aborted configuration");
        }));
        Assert.Same(configured, first.Options.Theme);
    }

    [Fact]
    public void ThemeDocumentPreservesIndependentTypographySurfacesAndModeColors() {
        var original = VisualTheme.Graphite().Configure(builder => {
            builder.Light.PlotBorder = ChartColor.FromHex("#12345678");
            builder.Dark.PlotBorder = ChartColor.FromHex("#ABCDEF90");
            builder.Dark.Positive = ChartColor.FromHex("#34567890");
            builder.Dark.Status.Pass = new VisualTokenColor(ChartColor.FromHex("#10203040"), ChartColor.FromHex("#50607080"));
            builder.PlotRadius = 9;
            builder.BarRadius = 4;
            builder.Typography = new VisualTypography("Theme sans", axisSize: 11, axisTitleSize: 19, monospaceFamily: "Theme mono");
        });

        var imported = VisualTheme.FromJson(original.ToJson());

        Assert.Equal(original.ToJson(), imported.ToJson());
        Assert.Equal(11, imported.Typography.AxisSize);
        Assert.Equal(19, imported.Typography.AxisTitleSize);
        Assert.Equal("Theme sans", imported.Typography.ToFontSpec().Family);
        Assert.Equal("Theme mono", imported.Typography.ToFontSpec(monospace: true).Family);
        Assert.Equal(9, imported.PlotRadius);
        Assert.Equal(4, imported.BarRadius);
        Assert.Equal(ChartColor.FromHex("#12345678"), imported.Resolve(VisualThemeMode.Light).PlotBorder);
        Assert.Equal(ChartColor.FromHex("#ABCDEF90"), imported.Resolve(VisualThemeMode.Dark).PlotBorder);
        Assert.Equal(ChartColor.FromHex("#34567890"), imported.Resolve(VisualThemeMode.Dark).Positive);
        Assert.Equal(ChartColor.FromHex("#10203040"), imported.Resolve(VisualThemeMode.Dark).Status.Pass.Fill);
        Assert.Equal(ChartColor.FromHex("#50607080"), imported.Resolve(VisualThemeMode.Dark).Status.Pass.Ink);
        var chart = Chart.Create().WithSize(360, 240).WithTheme(imported)
            .AddBar("Observed", new[] { new ChartPoint(1, 12), new ChartPoint(2, 18) });
        chart.Options.ShowPlotBackground = true;
        var scene = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene;
        var surface = Assert.Single(scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "content-surface");
        Assert.Equal(9, surface.Radius);
        Assert.Equal(ChartColor.FromHex("#12345678"), surface.Stroke);

    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void ConvenienceAndExplicitPreparationUseTheSameThemeAndHostContextWins(VisualThemeMode mode) {
        var authored = ChartColor.FromHex("#7C3AED80");
        var theme = VisualTheme.Graphite().Configure(builder => {
            builder.Light.Background = ChartColor.FromHex("#F0E0D0");
            builder.Dark.Background = ChartColor.FromHex("#102030");
            builder.Light.Palette = new[] { ChartColor.FromHex("#CC3300") };
            builder.Dark.Palette = new[] { ChartColor.FromHex("#33CCFF") };
            builder.Typography = new VisualTypography("serif", axisSize: 12, axisTitleSize: 18);
        });
        var chart = Chart.Create().WithTitle("Traffic").WithSize(360, 240).WithTheme(theme, mode)
            .WithDataLabels(false).AddBar("Observed", new[] { new ChartPoint(1, 12), new ChartPoint(2, 18) }, authored);
        var request = VisualExportRequest.ForChart(chart);
        var prepared = chart.Prepare(request.Context);
        var svg = prepared.ToSvg();
        var png = prepared.ToPng(request.RasterOptions);

        Assert.Equal(mode, request.Context.ThemeMode);
        Assert.Same(theme, request.Context.Theme);
        Assert.Equal(svg, chart.ToSvg());
        Assert.Equal(png, chart.ToPng());
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneMark>(), mark => mark.Fill.HasValue && mark.Fill.Value.Equals(authored));

        chart.WithTheme(VisualTheme.Graphite(), mode == VisualThemeMode.Light ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithFontFamily("monospace");
        var overridden = chart.Prepare(request.Context);
        Assert.Equal(svg, overridden.ToSvg());
        Assert.Equal(png, overridden.ToPng(request.RasterOptions));
        Assert.NotEqual(svg, chart.ToSvg());
    }
}
