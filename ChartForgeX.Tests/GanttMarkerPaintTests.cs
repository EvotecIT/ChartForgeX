using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttMarkerPaintTests {
    [Theory]
    [InlineData(ChartSeriesState.Danger)]
    [InlineData(ChartSeriesState.Warning)]
    [InlineData(ChartSeriesState.Info)]
    [InlineData(ChartSeriesState.Neutral)]
    [InlineData(ChartSeriesState.Success)]
    [InlineData(ChartSeriesState.Quiet)]
    public void ExplicitStateUsesCanonicalStatusInk(ChartSeriesState state) {
        var chart = Chart.Create().WithLegend(false).AddGanttTask("Work", 1, 9).AddGanttMarker("Deadline", 4)
            .WithSeriesState("Deadline", state);
        var context = new VisualRenderContext();
        var prepared = chart.Prepare(context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-line");
        var status = context.Theme.Resolve(context.ThemeMode).Status;
        var expected = state switch {
            ChartSeriesState.Danger => status.Critical.Fill,
            ChartSeriesState.Warning => status.Medium.Fill,
            ChartSeriesState.Info => status.Info.Fill,
            ChartSeriesState.Neutral => status.Neutral.Fill,
            _ => status.Pass.Fill
        };
        Assert.Equal(expected, line.Stroke);
    }

    [Theory]
    [InlineData("point", "--series", "rgb(0, 128, 0)")]
    [InlineData("series", "--series", "rgb(0, 128, 0)")]
    [InlineData("state", "--status", "rgb(200, 0, 0)")]
    [InlineData("neutral", "--text", "rgb(0, 0, 200)")]
    public async Task MarkerLineAndCaptionRetainTheirPaintRoleWhenHostVariablesChange(string source, string variable, string expectedInk) {
        var shared = ChartColor.FromHex("#2864B4");
        var tokens = new VisualDesignTokens { MutedForeground = shared };
        tokens.Status.Critical = new VisualTokenColor(shared, ChartColor.White);
        var context = new VisualRenderContext(theme: new VisualTheme(tokens, tokens));
        var chart = Chart.Create().WithSize(640, 320).AddGanttTask("Work", 1, 9).AddGanttMarker("Deadline", 4)
            .AddVerticalLine(7, "Reference", shared);
        var marker = chart.Series[1];
        marker.WithLegendEntry();
        if (source != "neutral") marker.StateRole = ChartSeriesState.Danger;
        if (source is "series" or "point") marker.Color = source == "point" ? ChartColor.FromHex("#ff0000") : shared;
        if (source == "point") marker.WithPointColor(0, shared);
        var variables = new SvgColorVariables().Add("--axis", shared, SvgColorRole.Axis)
            .Add("--series", shared, SvgColorRole.Series).Add("--status", shared, SvgColorRole.Status).Add("--text", shared, SvgColorRole.Text);
        chart.Options.SvgColorVariables = variables;
        var prepared = chart.Prepare(context);
        var entry = Assert.Single(VisualScheduleCompiler.LegendEntries(chart, context.Theme.Resolve(context.ThemeMode)), item => item.Label == "Deadline");
        Assert.Equal(shared, entry.Color);
        var svg = prepared.ToSvg();
        var document = XDocument.Parse(svg);
        var group = Assert.Single(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "gantt-vertical-marker");
        var line = Assert.Single(group.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "annotation-line");
        var plate = Assert.Single(group.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "annotation-label-backplate");
        Assert.Contains("var(" + variable + ",", (string?)line.Attribute("stroke"));
        Assert.Contains("var(" + variable + ",", (string?)plate.Attribute("fill"));
        Assert.Contains("var(" + variable + ",", (string?)plate.Attribute("stroke"));
        var legend = Assert.Single(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "legend-entry"
            && (string?)node.Attribute("data-cfx-source-id") == "legend-series-1");
        Assert.Equal(marker.InteractionIdentityKey, (string?)legend.Attribute("data-cfx-series-key"));
        Assert.Equal(marker.StateRole.ToString(), (string?)legend.Attribute("data-cfx-state"));
        var swatch = Assert.Single(legend.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "legend-swatch");
        Assert.Contains("var(" + variable + ",", (string?)swatch.Attribute("fill"));
        var ordinary = Assert.Single(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "annotation");
        Assert.Contains("var(--axis,", (string?)ordinary.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "annotation-line").Attribute("stroke"));
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 640, 360);
        await session.Page.EvaluateAsync("() => { const svg = document.querySelector('svg'); for (const [name, value] of Object.entries({'--axis':'#aa00aa','--series':'#008000','--status':'#c80000','--text':'#0000c8'})) svg.style.setProperty(name, value); }");
        Assert.Equal(expectedInk, await session.Page.Locator("[data-cfx-role='gantt-vertical-marker'] [data-cfx-role='annotation-line']")
            .EvaluateAsync<string>("element => getComputedStyle(element).stroke"));
        Assert.Equal(expectedInk, await session.Page.Locator("[data-cfx-source-id='legend-series-1'] [data-cfx-role='legend-swatch']")
            .EvaluateAsync<string>("element => getComputedStyle(element).fill"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "gantt-marker-host-" + source + ".png"), FullPage = true });
        }
    }
}
