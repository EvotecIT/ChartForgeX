using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidXYChartConfigurationTests {
    private const string Body = "xychart-beta\ntitle Work\nx-axis Month [Jan,Feb]\ny-axis Requests 0 --> 100\nbar \"Intake\" [20,60]\nline \"Complete\" [10 \"Ready\",50]";
    private static string With(string settings, string? theme = null) => "%%{init: {\"xyChart\":{" + settings + "}" + (theme == null ? "" : ",\"themeVariables\":{\"xyChart\":{" + theme + "}}") + "}}%%\n" + Body;

    [Fact]
    public void SourceSettingsUseLogicalAxesAndHostOverridesWithoutChangingRawDeclarations() {
        var source = With("\"width\":800,\"height\":460,\"chartOrientation\":\"horizontal\",\"showLegend\":false,\"showTitle\":false,\"showDataLabel\":true,\"xAxis\":{\"showTitle\":false,\"showAxisLine\":false,\"labelRotation\":25},\"yAxis\":{\"showAxisLine\":false}");
        var parsed = new MermaidParser().ParseXYChart(source); Assert.Empty(parsed.Diagnostics); var document = parsed.Document!; var chart = document.ToChart();
        Assert.Equal(800, chart.Options.Size.Width); Assert.Equal(460, chart.Options.Size.Height); Assert.False(chart.Options.ShowLegend); Assert.Empty(chart.Title);
        Assert.Equal(ChartOrientation.Horizontal, chart.Options.LineOrientation); Assert.Empty(chart.YAxisTitle); Assert.Equal("Requests", chart.XAxisTitle);
        Assert.False(chart.Options.YAxis.ShowLine); Assert.False(chart.Options.XAxis.ShowLine); Assert.Equal(25, chart.Options.YAxis.LabelAngle);
        Assert.True(chart.Series[0].ShowDataLabels); Assert.True(chart.Series[1].ShowDataLabels);
        var host = document.ToChart(new MermaidXYChartRenderOptions { Width = 720, ShowLegend = true, ShowDataLabels = false, Title = "Host" });
        Assert.Equal(720, host.Options.Size.Width); Assert.Equal(460, host.Options.Size.Height); Assert.True(host.Options.ShowLegend); Assert.Equal("Host", host.Title); Assert.False(host.Options.ShowDataLabels);
        Assert.Equal(source, document.SourceText); Assert.Contains(document.Configuration.Settings, setting => setting.Path == "xyChart.width" && setting.Value == "800");
        document.Orientation = ChartOrientation.Vertical; Assert.Equal(ChartOrientation.Vertical, document.ToChart().Options.LineOrientation);
        var header = new MermaidParser().ParseXYChart(source.Replace("xychart-beta\n", "xychart-beta vertical\n", StringComparison.Ordinal)).Document!;
        Assert.Equal(ChartOrientation.Vertical, header.ToChart().Options.LineOrientation);
    }

    [Fact]
    public void FenceOverridesOnlyItsSuppliedSourcePoliciesAndUseMaxWidthRemainsHtmlSizing() {
        var source = With("\"width\":800,\"height\":460,\"showLegend\":false,\"showDataLabel\":true,\"useMaxWidth\":false");
        var direct = MermaidRenderer.Render(source); Assert.Empty(direct.Diagnostics); Assert.Equal(VisualArtifactHtmlSizing.PreserveSize, direct.Artifact!.HtmlSizing);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid {width=\"390\" legend=\"true\" dataLabels=\"false\"}\n" + source + "\n```");
        Assert.Empty(fence.Diagnostics); var artifact = Assert.Single(fence.Artifacts); var chart = Assert.IsType<Chart>(artifact.Model);
        Assert.Equal(390, chart.Options.Size.Width); Assert.Equal(460, chart.Options.Size.Height); Assert.True(chart.Options.ShowLegend); Assert.False(chart.Options.ShowDataLabels);
    }

    [Fact]
    public void ExplicitThemeAndTypographyOwnNativePresentationOverSourceFamilyColors() {
        var source = With("\"titleFontSize\":28,\"legendFontSize\":19", "\"titleColor\":\"#193c72\",\"legendTextColor\":\"#265c3f\",\"dataLabelColor\":\"#6e244c\",\"backgroundColor\":\"#f4f1df\",\"plotColorPalette\":\"#2563eb,#dc2626\"");
        var parsed = new MermaidParser().ParseXYChart(source); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidXYChartDocument>(parsed.Document);
        var chart = document.ToChart(); Assert.Equal(28, chart.Options.TitleStyle.FontSize); Assert.Equal(19, chart.Options.LegendStyle.FontSize);
        Assert.Equal(ChartColor.FromHex("#193c72"), chart.Options.TitleStyle.Color); Assert.False(chart.Options.TransparentBackground);
        Assert.Equal(new[] { ChartColor.FromHex("#2563eb"), ChartColor.FromHex("#dc2626") }, chart.Options.Theme.Resolve(VisualThemeMode.Light).Palette);
        var theme = VisualTheme.Graphite(); var host = document.ToChart(new MermaidXYChartRenderOptions { Presentation = new MermaidPresentationOptions { Theme = theme, FontSize = 15 } });
        Assert.False(host.Options.TitleStyle.HasOverrides); Assert.False(host.Options.LegendStyle.HasOverrides);
        Assert.Equal(theme.Resolve(VisualThemeMode.Light).Background, host.Options.Theme.Resolve(VisualThemeMode.Light).Background);
        Assert.False(host.Options.TransparentBackground);
    }

    [Theory]
    [InlineData("width", "640.5")] [InlineData("height", "0")] [InlineData("showLegend", "\"false\"")]
    [InlineData("titleFontSize", "513")] [InlineData("legendFontSize", "false")] [InlineData("chartOrientation", "\"diagonal\"")]
    [InlineData("showDataLabel", "{}")] [InlineData("xAxis.showAxisLine", "\"false\"")]
    public void InvalidSupportedConfigurationIsLocatedAndCannotExport(string path, string value) {
        var parts = path.Split('.'); var setting = parts.Length == 1 ? "\"" + path + "\":" + value : "\"" + parts[0] + "\":{\"" + parts[1] + "\":" + value + "}";
        var direct = MermaidRenderer.Render(With(setting)); Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        var error = Assert.Single(direct.Diagnostics); Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code); Assert.Equal(1, error.Span.Line);
    }

    [Theory]
    [InlineData("\"titleColor\":false")] [InlineData("\"plotColorPalette\":\"#2563eb,url(x)\"")]
    [InlineData("\"plotColorPalette\":[]")]
    public void InvalidFamilyPaintDoesNotBecomeAnExportableFallback(string settings) {
        var result = MermaidRenderer.Render(With("", settings)); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 1);
    }

    [Fact]
    public void UnsupportedIndependentAxisRolesStayLocatedInsteadOfApplyingToBothAxes() {
        var source = With("\"xAxis\":{\"labelFontSize\":30,\"showTick\":false}", "\"yAxisLabelColor\":\"#193c72\"");
        var parsed = new MermaidParser().ParseXYChart(source); Assert.False(parsed.HasErrors); Assert.Equal(3, parsed.Diagnostics.Count);
        Assert.All(parsed.Diagnostics, item => { Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, item.Code); Assert.Equal(1, item.Span.Line); });
        var model = parsed.Document!.ToChart(); Assert.False(model.Options.TickLabelStyle.HasOverrides);
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.True(strict.HasErrors); Assert.Null(strict.Artifact);
    }

    [Fact]
    public void DirectivePrecedenceKeepsFrontmatterAndLastDirectiveDeclarationsAvailable() {
        var source = "---\nconfig:\n  xyChart:\n    showLegend: false\n    width: 800\n---\n%%{init: {\"xyChart\":{\"showLegend\":true}}}%%\n" + Body;
        var parsed = new MermaidParser().ParseXYChart(source); Assert.Empty(parsed.Diagnostics); var document = Assert.IsType<MermaidXYChartDocument>(parsed.Document); Assert.True(document.ToChart().Options.ShowLegend);
        Assert.Equal(new[] { "false", "true" }, document.Configuration.Settings.Where(setting => setting.Path == "xyChart.showLegend").Select(setting => setting.Value));
    }
}
