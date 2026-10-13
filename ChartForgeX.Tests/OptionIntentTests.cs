using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects authored defaults from disappearing behind theme inheritance or value comparison.</summary>
public sealed class OptionIntentTests {
    private static readonly ChartPoint[] Values = { new(0, 3), new(1, 7) };

    [Fact]
    public void CanonicalStyleGettersDescribeTheActualPreparedDefaultsAndApplyDirectAssignments() {
        var chart = Chart.Create().AddBar("Counts", Values);
        Assert.Equal(ChartBarStyle.Flat, chart.Options.BarVisualStyle.Kind);
        Assert.False(chart.Options.GridLineStyle.ShowVerticalLines);
        Assert.Equal(0, chart.Options.LineVisualStyle.HaloOpacity);
        Assert.Equal(chart.Options.BarVisualStyle.Kind, new ChartBarVisualStyle().Kind);
        Assert.Equal(chart.Options.GridLineStyle.ShowVerticalLines, new ChartGridLineStyle().ShowVerticalLines);
        Assert.Equal(chart.Options.LineVisualStyle.HaloOpacity, new ChartLineVisualStyle().HaloOpacity);
        chart.Options.BarVisualStyle.Kind = ChartBarStyle.Solid;
        chart.Options.GridLineStyle.ShowVerticalLines = true;
        var prepared = chart.Prepare(new VisualRenderContext());
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGradient>(), node => node.Role == "bar");
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "grid-x");
        var svg = prepared.ToSvg();
        Assert.Contains("linearGradient", svg, StringComparison.Ordinal);
        Assert.Contains("data-cfx-role=\"grid-x\"", svg, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitZeroRadiusAndUnitGridWidthOverrideThemeThroughDirectAndCallbackConfiguration(bool callback) {
        var chart = Chart.Create().AddBar("Counts", Values);
        var theme = VisualTheme.Graphite().Configure(builder => { builder.BarRadius = 7; builder.GridStrokeWidth = 3; });
        var context = new VisualRenderContext(theme: theme);
        var inherited = chart.Prepare(context);
        Assert.All(inherited.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar"), node => Assert.Equal(7, node.Radius));
        Assert.All(inherited.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "grid-y"), node => Assert.Equal(3, node.StrokeWidth));
        chart.ConfigureBarVisualStyle(_ => { }); chart.ConfigureGridStyle(_ => { });
        Assert.Equal(inherited.ToSvg(), chart.Prepare(context).ToSvg());
        if (callback) {
            chart.ConfigureBarVisualStyle(style => style.CornerRadius = 0);
            chart.ConfigureGridStyle(style => style.StrokeWidth = 1);
        } else {
            chart.Options.BarVisualStyle.CornerRadius = 0;
            chart.Options.GridLineStyle.StrokeWidth = 1;
        }
        var explicitDefaults = chart.Prepare(context);
        var bars = explicitDefaults.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").ToArray();
        var guides = explicitDefaults.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "grid-y").ToArray();
        Assert.NotEmpty(bars); Assert.NotEmpty(guides);
        Assert.All(bars, node => Assert.Equal(0, node.Radius));
        Assert.All(guides, node => Assert.Equal(1, node.StrokeWidth));
        Assert.NotEmpty(explicitDefaults.ToPng());
    }

    [Fact]
    public void ExplicitFalseTextOverridesDisableInheritedEmphasisAndClonesPreserveThatIntent() {
        var inherited = new TextStyle { UnderlineStyle = TextDecorationStyle.Double, StrikethroughStyle = TextDecorationStyle.Double };
        inherited.Font.Italic = true;
        var absent = new TextStyleOverride().Resolve(inherited);
        Assert.True(absent.Font.Italic);
        Assert.Equal(TextDecorationStyle.Double, absent.UnderlineStyle);
        Assert.Equal(TextDecorationStyle.Double, absent.StrikethroughStyle);
        var authored = new TextStyleOverride { Italic = false, Underline = false, Strikethrough = false };
        Assert.True(authored.HasOverrides);
        var resolved = authored.Clone().Resolve(inherited);
        Assert.False(resolved.Font.Italic);
        Assert.Equal(TextDecorationStyle.None, resolved.UnderlineStyle);
        Assert.Equal(TextDecorationStyle.None, resolved.StrikethroughStyle);
        Assert.True(inherited.Font.Italic);
        Assert.Equal(TextDecorationStyle.Double, inherited.UnderlineStyle);
    }

    [Fact]
    public void DefaultValuedFamilyAssignmentsRemainAuthoredForCompatibilityValidation() {
        var mapOptionsOnLine = Chart.Create().AddLine("Trend", Values);
        mapOptionsOnLine.ConfigureMapColorDimension(options => options.ShowLegend = true);
        Assert.Throws<InvalidOperationException>(() => mapOptionsOnLine.Prepare(new VisualRenderContext()));
        var colorOptionsOnLine = Chart.Create().AddLine("Trend", Values);
        colorOptionsOnLine.Series[0].ColorDimension.ValueFormat = null;
        Assert.Throws<InvalidOperationException>(() => colorOptionsOnLine.Prepare(new VisualRenderContext()));
        var financialOptionsOnLine = Chart.Create().AddLine("Trend", Values);
        financialOptionsOnLine.Series[0].ConfigureFinancial(options => options.Rising.Stroke = null);
        Assert.Throws<InvalidOperationException>(() => financialOptionsOnLine.Prepare(new VisualRenderContext()));
    }

    [Fact]
    public void PaginationPreservesUnspecifiedAndExplicitFalseTypography() {
        var grid = ChartGrid.Create().Add(Chart.Create().AddLine("Trend", Values));
        grid.ConfigureSubtitleStyle(style => style.Italic = false);
        var page = Assert.Single(grid.Paginate()).Grid;
        Assert.False(page.TitleStyle.HasOverrides);
        Assert.True(page.SubtitleStyle.HasOverrides);
        var inherited = new TextStyle(); inherited.Font.Italic = true;
        Assert.True(page.TitleStyle.Resolve(inherited).Font.Italic);
        Assert.False(page.SubtitleStyle.Resolve(inherited).Font.Italic);
        page.SubtitleStyle.Italic = true;
        Assert.False(grid.SubtitleStyle.Resolve(inherited).Font.Italic);
    }
}
