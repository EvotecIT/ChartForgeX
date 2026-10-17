using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RuntimeBrushScaleBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task NonlinearBrushCaptionsBoundsAndSelectedMarksShareTheEnclosingTicks(bool symmetric, bool reversed) {
        if (!Enabled) return;
        var minimum = symmetric ? -18d : 10;
        var maximum = symmetric ? 18d : 100;
        var chart = Chart.Create().WithSize(reversed ? 330 : 700, 360).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithTheme(VisualTheme.Graphite(), reversed ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXAxisBounds(minimum, maximum).WithYAxisBounds(0, 10)
            .WithXAxisScale(symmetric ? ChartScaleKind.SymmetricLogarithmic : ChartScaleKind.Logarithmic)
            .AddLine("Measurements", new[] {new ChartPoint(minimum, 2), new ChartPoint(symmetric ? 0 : 30, 5), new ChartPoint(maximum, 8)});
        chart.Options.XAxis.SymmetricLogarithmThreshold = 2;
        chart.Options.XAxis.Reversed = reversed;
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(minimum, "Lower"));
        if (symmetric) chart.Options.XAxis.Labels.Add(new ChartAxisLabel(0, "Zero"));
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(maximum, "Upper"));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Brush);
            options.Interaction.BrushAxis = ChartBrushAxis.X;
        }), reversed ? 390 : 900, 560);
        var page = session.Page;
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        var positions = await page.Locator(".cfx-interactive-chart").EvaluateAsync<double[]>("""
            (root,symmetric) => {
              window.scaleBrush=null;root.addEventListener('cfxbrush',event=>window.scaleBrush=event.detail,{once:true});
              const plot=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
              const matrix=root.querySelector('.cfx-stage svg').getScreenCTM();
              const a=new DOMPoint(plot.x+plot.width*(symmetric ? .22 : .45),plot.y+plot.height*.3).matrixTransform(matrix);
              const b=new DOMPoint(plot.x+plot.width*(symmetric ? .78 : .55),plot.y+plot.height*.7).matrixTransform(matrix);
              return [a.x,a.y,b.x,b.y];
            }
            """, symmetric);
        await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions[2], (float)positions[3], new MouseMoveOptions {Steps = 4});
        await page.Mouse.UpAsync();
        var facts = await page.Locator(".cfx-interactive-chart").EvaluateAsync<JsonElement>("""
            root => {
              const projection=JSON.parse(root.dataset.cfxPreparedChart).dataProjection,brush=root.querySelector('.cfx-brush-box');
              const rect=brush.getBoundingClientRect(),inverse=root.querySelector('.cfx-stage svg').getScreenCTM().inverse();
              const ends=[new DOMPoint(rect.left,rect.top),new DOMPoint(rect.right,rect.bottom)].map(point=>point.matrixTransform(inverse).x);
              return JSON.parse(JSON.stringify({detail:scaleBrush,ticks:projection.x.displayTicks,
                selected:Array.from(root.querySelectorAll('g[data-cfx-role="point"].cfx-selected')).map(node=>Number(node.dataset.cfxPoint)),
                boxRatios:ends.map(x=>(x-projection.plotBounds.x)/projection.plotBounds.width),shown:!brush.hidden}));
            }
            """);
        var detail = facts.GetProperty("detail");
        Assert.Equal("Lower – Upper", detail.GetProperty("rangeLabel").GetString());
        Assert.Equal(detail.GetProperty("rangeLabel").GetString(), detail.GetProperty("label").GetString());
        Assert.Equal(minimum, detail.GetProperty("displayRange").GetProperty("x")[0].GetDouble());
        Assert.Equal(maximum, detail.GetProperty("displayRange").GetProperty("x")[1].GetDouble());
        var raw = detail.GetProperty("range").GetProperty("x");
        Assert.Equal(raw.GetRawText(), detail.GetProperty("categoryRange").GetRawText());
        Assert.Equal(minimum, raw[0].GetDouble());
        Assert.Equal(maximum, raw[1].GetDouble());
        Assert.Equal(raw.GetRawText(), detail.GetProperty("displayRange").GetProperty("x").GetRawText());
        Assert.Equal(new[] { 0, 1, 2 }, facts.GetProperty("selected").EnumerateArray().Select(value => value.GetInt32()));
        Assert.InRange(facts.GetProperty("boxRatios")[0].GetDouble(), -.002, .002);
        Assert.InRange(facts.GetProperty("boxRatios")[1].GetDouble(), .998, 1.002);
        Assert.True(facts.GetProperty("shown").GetBoolean());
        var output = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var name = "brush-scale-" + (symmetric ? "symlog" : "log") + (reversed ? "-reversed-dark-390" : "-light-900");
            await page.ScreenshotAsync(new PageScreenshotOptions {Path = Path.Combine(output, name + ".png")});
            await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), facts.GetRawText());
        }
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);
    }
}
