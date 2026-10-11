using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects source precedence, located validation and the direct/fenced layout contract.</summary>
public sealed class MermaidRadarLayoutTests {
    private const string Body = "radar-beta\naxis a[\"Coverage\"], b[\"Policy\"], c[\"Response\"], d[\"Recovery\"]\ncurve current[\"Current\"]{90,45,80,60}\nmax 100\n";
    private const string Source = "---\nconfig:\n  radar:\n    width: 460\n    height: 350\n    marginLeft: 12\n    marginTop: 14\n    marginRight: 16\n    marginBottom: 18\n    axisScaleFactor: 0.8\n    axisLabelFactor: 1.2\n    curveTension: 0.25\n---\n" + Body;

    [Fact]
    public void SourceLayoutReachesTheExistingNativeOwnersAndDirectivesWin() {
        var parsed = new MermaidParser().ParseRadar(Source);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart();
        Assert.Equal((460, 350), (chart.Options.Size.Width, chart.Options.Size.Height));
        Assert.Equal(new ChartPadding(12, 14, 16, 18), chart.Options.Padding);
        Assert.Equal(.8, chart.Options.PolarAxisScaleFactor); Assert.Equal(1.2, chart.Options.PolarAxisLabelFactor);
        Assert.Equal(.25, chart.Series[0].Radar.CurveTension);
        var later = new MermaidParser().ParseRadar(Source.Replace(Body, "%%{init: {'radar': {'width': 520, 'axisScaleFactor': 0}}}%%\n" + Body));
        Assert.Empty(later.Diagnostics);
        var overridden = later.Document!.ToChart();
        Assert.Equal(520, overridden.Options.Size.Width); Assert.Equal(350, overridden.Options.Size.Height);
        Assert.Equal(0, overridden.Options.PolarAxisScaleFactor); Assert.Equal(1.2, overridden.Options.PolarAxisLabelFactor);
        Assert.Equal(new[] { 90d, 45, 80, 60 }, overridden.Series[0].Points.Select(p => p.Y));
        Assert.Contains(parsed.Document!.Configuration.Settings, s => s.Path == "radar.axisScaleFactor" && s.Span.Line == 10);
    }

    [Theory]
    [InlineData("width", "0")]
    [InlineData("height", "1.5")]
    [InlineData("width", "2147483648")]
    [InlineData("marginLeft", "-1")]
    [InlineData("marginTop", "'12'")]
    [InlineData("marginRight", "true")]
    [InlineData("marginBottom", "null")]
    [InlineData("axisScaleFactor", "1e309")]
    [InlineData("axisLabelFactor", "{}")]
    [InlineData("axisLabelFactor", "\n      nested: 1")]
    public void InvalidLayoutProducesOneLocatedErrorWithoutDuplicateWarnings(string key, string value) {
        var parsed = new MermaidParser().ParseRadar("---\nconfig:\n  radar:\n    " + key + ": " + value + "\n---\n" + Body);
        var diagnostic = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, diagnostic.Code); Assert.Equal(4, diagnostic.Span.Line);
        Assert.True(parsed.HasErrors);
    }

    [Fact]
    public void PartialMarginsPreserveNativeDefaultsAndUnrelatedFamiliesRetainTheirWarnings() {
        var parsed = new MermaidParser().ParseRadar("%%{init: {'radar': {'marginLeft': 0}}}%%\n" + Body);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart();
        Assert.Equal((760, 620), (chart.Options.Size.Width, chart.Options.Size.Height));
        Assert.Equal(new ChartPadding(0, 24, 24, 24), chart.Options.Padding);
        Assert.Equal(1, chart.Options.PolarAxisScaleFactor); Assert.Equal(1.05, chart.Options.PolarAxisLabelFactor);
        var other = new MermaidParser().ParseFlowchart("%%{init: {'radar': {'width': 460}}}%%\nflowchart LR\nA --> B");
        Assert.False(other.HasErrors); Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, Assert.Single(other.Diagnostics).Code);
    }

    [Theory]
    [InlineData(320, false)]
    [InlineData(960, true)]
    public async Task FencesCaptureHostLayoutAndRemainResponsive(int viewport, bool overrideSource) {
        var host = overrideSource ? new MermaidRadarRenderOptions { Width = 520, AxisScaleFactor = 1.4, AxisLabelFactor = .9, Padding = ChartPadding.All(8) }
            : new MermaidRadarRenderOptions();
        var source = (overrideSource ? Source.Replace("config:", "config:\n  theme: dark") : Source);
        var expected = new MermaidParser().ParseRadar(source).Document!.ToVisualArtifact(host);
        var parser = new MermaidVisualMarkupParser(new MermaidRenderOptions { Radar = host });
        host.Width = 800; host.AxisScaleFactor = 0; host.AxisLabelFactor = 0; host.Padding = ChartPadding.All(0);
        var fenced = parser.Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var artifact = Assert.Single(fenced.Artifacts);
        Assert.Equal(expected.ToSvg(), artifact.ToSvg()); Assert.Equal(expected.ToPng(), artifact.ToPng());
        var model = Assert.IsType<Chart>(artifact.Model);
        Assert.Equal(overrideSource ? 520 : 460, model.Options.Size.Width); Assert.Equal(350, model.Options.Size.Height);
        Assert.Equal(overrideSource ? 1.4 : .8, model.Options.PolarAxisScaleFactor);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), viewport, 460);
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role=radar-axis-label]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "radar-layout-browser-" + viewport + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, "radar-layout-native-" + viewport + ".png"), artifact.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
