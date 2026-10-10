using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class CartesianLollipopLabelBrowserTests {
    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(true, -1, true)]
    public async Task ReversedEndpointCaptionAndPhysicalReadoutRetainTheirSignedSource(bool secondary, int sign, bool dark) {
        if (!Enabled) return;
        var chart = CartesianLollipopLabelTests.Create(secondary, true, sign)
            .WithSize(dark ? 360 : 640, 360).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), dark ? 390 : 700, 520);
        var page = session.Page; var point = page.Locator(Point(0, 0));
        var marker = await point.Locator("[data-cfx-role='lollipop-marker']").BoundingBoxAsync() ?? throw new InvalidOperationException("No native endpoint marker.");
        await page.Mouse.MoveAsync((float)(marker.X + marker.Width / 2), (float)(marker.Y + marker.Height / 2));
        var hover = await TooltipTextAsync(page);
        await page.Mouse.ClickAsync((float)(marker.X + marker.Width / 2), (float)(marker.Y + marker.Height / 2));
        var selected = await point.GetAttributeAsync("aria-selected");
        var direction = await point.EvaluateAsync<JsonElement>("""
            node=>{const stem=node.querySelector('[data-cfx-role="lollipop-stem"]'),label=node.ownerSVGElement.querySelector('[data-cfx-role="data-label"] text'),box=label.getBBox();
              return {baseline:Number(stem.getAttribute('y1')),endpoint:Number(stem.getAttribute('y2')),labelTop:box.y,labelBottom:box.y+box.height,source:node.dataset.cfxValue};}
            """);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var name = "lollipop-browser-" + (secondary ? "secondary-negative-compact-dark" : "primary-positive-wide-light");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new { direction, hover, selected }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".console.json"), JsonSerializer.Serialize(session.ConsoleLog, new JsonSerializerOptions { WriteIndented = true }));
        }
        Assert.Contains((40 * sign).ToString(), hover); Assert.Equal("true", selected);
        Assert.True(sign > 0 ? direction.GetProperty("labelTop").GetDouble() > direction.GetProperty("endpoint").GetDouble()
            : direction.GetProperty("labelBottom").GetDouble() < direction.GetProperty("endpoint").GetDouble());
        AssertNoConsoleErrors(session);
    }
}
