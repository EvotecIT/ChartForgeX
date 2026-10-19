using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Full source catalogs own retained highlight identities even when decimation omits their marks.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class HighlightObservationOwnershipBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OmittedSourceObservationsRetainTheirCatalogOwnerAcrossReplacement(bool descriptor) {
        if (!Enabled) return;
        var source = Enumerable.Range(0, 40).Select(index => new ChartPoint(index, 30 + Math.Sin(index / 3d) * 8)).ToArray();
        var sampling = ChartSeries.CreateDecimated(new ChartSeriesId("primary"), "Measured", ChartSeriesKind.Line, source, 6);
        var omittedIndex = Enumerable.Range(0, source.Length).First(index => !sampling.SourcePointIndices.Contains(index));
        var id = "reading-" + omittedIndex;
        foreach (var local in new[] { false, true }) {
            var initial = Model(full: false, local, referenceArrived: false);
            await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(Options), local ? 414 : 1000, 660);
            var page = session.Page;
            var catalog = await page.Locator(".cfx-interactive-chart").EvaluateAsync<JsonElement>("root=>JSON.parse(root.dataset.cfxPreparedChart).series[0]");
            Assert.Equal(local, catalog.GetProperty("local").GetBoolean());
            Assert.Contains(catalog.GetProperty("observationIds").EnumerateArray(), observation => observation.GetProperty("id").GetString() == id);
            Assert.Equal(0, await page.Locator("[data-cfx-observation-id='" + id + "']").CountAsync());
            await page.EvaluateAsync("""
                ({id,descriptor})=>{window.ownerController=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                    ownerController.highlight(descriptor?{targets:[{observationId:id,seriesScope:'shared'}]}:{observationIds:[id]},{dim:true});}
                """, new { id, descriptor });
            Assert.Equal(0, await Highlighted().CountAsync());

            await ReplaceAsync(full: false);
            Assert.Equal(1, await page.Locator("[data-cfx-series-key=reference][data-cfx-observation-id='" + id + "']").CountAsync());
            Assert.Equal(0, await Highlighted().CountAsync());
            Assert.Equal(0, await page.Locator(".cfx-linked-dim").CountAsync());
            await CaptureAsync("unrelated-arrival");

            await ReplaceAsync(full: true);
            Assert.Equal(local ? 0 : 1, await Highlighted().CountAsync());
            if (!local) Assert.Equal("primary", await Highlighted().GetAttributeAsync("data-cfx-series-key"));
            Assert.Equal(0, await page.Locator("[data-cfx-series-key=reference][data-cfx-highlighted=true]").CountAsync());
            Assert.Equal(0, await page.EvaluateAsync<int>("()=>ownerController.getSelection().length"));
            await CaptureAsync("source-arrival");
            AssertNoConsoleErrors(session);

            ILocator Highlighted() => page.Locator("[data-cfx-highlighted=true]");
            Task ReplaceAsync(bool full) => page.EvaluateAsync("html=>ownerController.update(html)", Model(full, local, referenceArrived: true).ToInteractiveHtmlFragmentWithoutAssets(Options));
            async Task CaptureAsync(string state) {
                var root = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
                if (string.IsNullOrWhiteSpace(root)) return;
                var folder = Path.Combine(root, "observation-ownership"); Directory.CreateDirectory(folder);
                await page.ScreenshotAsync(new PageScreenshotOptions {
                    Path = Path.Combine(folder, state + "-" + local + "-" + descriptor + ".png"), Animations = ScreenshotAnimations.Disabled
                });
            }
        }

        Chart Model(bool full, bool local, bool referenceArrived) {
            var chart = Chart.Create().WithSize(900, 390).WithLegend().WithDataLabels(false)
                .WithTitle("Source identity through decimation").WithTheme(VisualTheme.Graphite(), local ? VisualThemeMode.Dark : VisualThemeMode.Light);
            if (local) {
                if (full) chart.AddLine("Measured", source);
                else chart.AddDecimatedLine("Measured", source, 6);
            } else {
                chart.AddSeries(full ? ChartSeries.CreateLine(new ChartSeriesId("primary"), "Measured", source)
                    : ChartSeries.CreateDecimated(new ChartSeriesId("primary"), "Measured", ChartSeriesKind.Line, source, 6));
            }
            chart.Series[0].WithObservationIds(Enumerable.Range(0, source.Length).Select(index => new ChartObservationId("reading-" + index)));
            return chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("reference"), "Independent reference",
                new[] { new ChartPoint(omittedIndex, 55) }).WithObservationId(0, new ChartObservationId(referenceArrived ? id : "reference-only")));
        }
        static void Options(HtmlChartInteractionOptions options) { options.IdScope = "catalog-owner"; }
    }
}
