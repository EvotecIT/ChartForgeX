using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Explicit stroke colors stay literal; omitted colors retain the shared series token.</summary>
public sealed class RadarStrokeColorVariableTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlotAndLegendUseTheSameExplicitPaintUnderHostColorVariables(bool mixed) {
        var color = ChartColor.FromHex("#DC2626");
        var values = Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 65)).ToArray();
        var chart = Chart.Create().WithSize(520, 380).WithHeader(false).WithLegend(true).WithDataLabels(false).WithYAxisBounds(0, 100);
        if (mixed) chart.AddRadialColumn("Count", values);
        chart.AddRadarArea("Target", values, color);
        chart.WithSvgColorVariables(new SvgColorVariables().Add("--radar-outline", color, SvgColorRole.Series));
        chart.Series.Last().Radar.OutlineStroke = new ChartStrokeStyle(width: 4);
        Assert.All(Strokes(chart.ToSvg()), stroke => Assert.Contains("var(--radar-outline,", stroke, StringComparison.Ordinal));
        chart.Series.Last().Radar.OutlineStroke = new ChartStrokeStyle(color, 4);
        var svg = chart.ToSvg();
        Assert.All(Strokes(svg), stroke => Assert.Equal("#DC2626", stroke));
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><head><style>:root{--radar-outline:#22C55E}</style></head><body>" + svg + "</body></html>", 640, 460);
        var strokes = await session.Page.Locator("[data-cfx-role=radar-outline], [data-cfx-role=legend-line]")
            .EvaluateAllAsync<string[]>("nodes => nodes.map(node => getComputedStyle(node).stroke)");
        Assert.Equal(2, strokes.Length);
        Assert.All(strokes, stroke => Assert.Equal("rgb(220, 38, 38)", stroke));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(captures, "radar-outline-variable-" + (mixed ? "mixed" : "standalone") + ".png"), FullPage = true
            });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static string[] Strokes(string svg) {
        var strokes = XDocument.Parse(svg).Descendants().Where(element =>
            (string?)element.Attribute("data-cfx-role") is "radar-outline" or "legend-line")
            .Select(element => (string)element.Attribute("stroke")!).ToArray();
        Assert.Equal(2, strokes.Length);
        return strokes;
    }
}
