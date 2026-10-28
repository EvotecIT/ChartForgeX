using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRadarThemeTests {
    private const string Body = "radar-beta\ntitle Styled radar\naxis Coverage, Policy, Response, Recovery\ncurve current[\"Current\"]{90,45,80,60}\ncurve target[\"Target\"]{95,85,90,80}\nmax 100\ngraticule circle\n";
    private const string Source = "---\nconfig:\n  themeVariables:\n    fontSize: 18\n    titleColor: '#7C3AED'\n    cScale0: '#2563EB'\n    cScale1: '#EA580C'\n    radar:\n      axisColor: '#16A34A'\n      axisStrokeWidth: 3\n      axisLabelFontSize: 15\n      graticuleColor: '#64748B'\n      graticuleOpacity: 0.12\n      graticuleStrokeWidth: 1.5\n      curveOpacity: 0.25\n      curveStrokeWidth: 4\n      legendFontSize: 16\n---\n" + Body;

    [Fact]
    public void AuthoredThemeVariablesReachNativeOwnersAndSurviveRepeatedPresentation() {
        var parsed = new MermaidParser().ParseRadar(Source);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart();
        Assert.Equal(ChartColor.FromHex("#7C3AED"), chart.Options.TitleStyle.Color);
        Assert.Equal(ChartColor.FromHex("#2563EB"), chart.Series[0].Color);
        Assert.Equal(ChartColor.FromHex("#EA580C"), chart.Series[1].Color);
        Assert.Equal(3, chart.Options.PolarAxisStroke!.Width);
        Assert.Equal(1.5, chart.Options.PolarGridStroke!.Width);
        Assert.Equal(.12, chart.Options.PolarGridFillOpacity);
        Assert.Equal(15, chart.Options.TickLabelStyle.FontSize); Assert.Equal(16, chart.Options.LegendStyle.FontSize);
        Assert.All(chart.Series, series => { Assert.Equal(.25, series.Radar.FillOpacity); Assert.Equal(4, series.Radar.OutlineStroke!.Width); });
        var artifact = parsed.Document!.ToVisualArtifact();
        Assert.Equal(chart.ToPng(), artifact.ToPng());
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + Source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        Assert.Equal(artifact.ToPng(), Assert.Single(fenced.Artifacts).ToPng());
    }

    [Fact]
    public void LaterDeclarationsAndZeroValuesWinWithoutDroppingObservations() {
        var source = Source.Replace(Body, "%%{init: {'themeVariables': {'cScale0': 'red', 'radar': {'curveStrokeWidth': 0, 'curveOpacity': 0, 'graticuleOpacity': 0, 'axisStrokeWidth': 0}}}}%%\n" + Body);
        var parsed = new MermaidParser().ParseRadar(source); Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart();
        Assert.Equal(ChartColor.FromHex("#FF0000"), chart.Series[0].Color);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "radar-outline" || node.Role == "legend-line");
        Assert.Equal(8, prepared.Regions.Count(region => region.Role == "radar-point"));
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role == "radar-area"), node => Assert.Equal(0, node.Fill!.Value.A));
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role == "radar-ring"), node => Assert.Null(node.Fill));
    }

    [Theory]
    [InlineData("axisColor", "false")]
    [InlineData("axisColor", "'url(https://example.com/paint)'")]
    [InlineData("axisStrokeWidth", "-1")]
    [InlineData("curveStrokeWidth", "1025")]
    [InlineData("curveOpacity", "1.1")]
    [InlineData("graticuleOpacity", "null")]
    [InlineData("graticuleStrokeWidth", "'2'")]
    [InlineData("axisLabelFontSize", "0")]
    [InlineData("legendFontSize", "'16px'")]
    [InlineData("curveOpacity", "\n        nested: 1")]
    public void InvalidKnownStylesProduceOneLocatedError(string key, string value) {
        var source = "---\nconfig:\n  themeVariables:\n    radar:\n      " + key + ": " + value + "\n---\n" + Body;
        var parsed = new MermaidParser().ParseRadar(source);
        var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code); Assert.Equal(5, error.Span.Line);
        Assert.True(parsed.HasErrors);
    }

    [Theory]
    [InlineData("titleColor", "null")]
    [InlineData("cScale0", "false")]
    [InlineData("cScale1", "'url(https://example.com/paint)'")]
    [InlineData("cScale2", "\n      nested: red")]
    public void InvalidIndexedAndTitleColorsProduceOneLocatedError(string key, string value) {
        var source = "---\nconfig:\n  themeVariables:\n    " + key + ": " + value + "\n---\n" + Body;
        var parsed = new MermaidParser().ParseRadar(source);
        var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code);
        Assert.Equal(4, error.Span.Line); Assert.True(parsed.HasErrors);
    }

    [Fact]
    public void SparseIndexedColorsRepeatAfterTwelveAndUnsupportedSettingsRemainVisible() {
        var curves = string.Join("\n", Enumerable.Range(0, 13).Select(index => "curve c" + index + "{10,20,30}"));
        var parsed = new MermaidParser().ParseRadar("%%{init: {'themeVariables': {'cScale0': 'red'}}}%%\nradar-beta\naxis A,B,C\n" + curves);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart();
        Assert.Equal(ChartColor.FromHex("#FF0000"), chart.Series[0].Color); Assert.Equal(chart.Series[0].Color, chart.Series[12].Color);
        Assert.Null(chart.Series[1].Color);
        var unknown = new MermaidParser().ParseRadar("%%{init: {'themeVariables': {'radar': {'legendBoxSize': 18}}}}%%\n" + Body);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, Assert.Single(unknown.Diagnostics).Code);
        var other = new MermaidParser().ParseFlowchart("%%{init: {'themeVariables': {'radar': {'axisColor': 'red'}}}}%%\nflowchart LR\nA --> B");
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, Assert.Single(other.Diagnostics).Code);
        Assert.Contains("themeVariables.radar.axisColor", Assert.Single(other.Diagnostics).Message);
    }

    [BrowserTheory]
    [InlineData(320, false)]
    [InlineData(320, true)]
    [InlineData(960, false)]
    [InlineData(960, true)]
    public async Task StyledStaticFencesRenderInBothThemesWithoutScripts(int viewport, bool dark) {
        var source = dark ? Source.Replace("config:", "config:\n  theme: dark") : Source;
        var rendered = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Radar = new MermaidRadarRenderOptions { Width = viewport, Height = 520 }
        }); Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), viewport, 520);
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role=radar-outline]").CountAsync());
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role=radar-axis-label]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var suffix = viewport + (dark ? "-dark" : "-light");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "radar-theme-browser-" + suffix + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, "radar-theme-native-" + suffix + ".png"), artifact.ToPng());
            File.WriteAllText(Path.Combine(captures, "radar-theme-native-" + suffix + ".svg"), artifact.ToSvg());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
