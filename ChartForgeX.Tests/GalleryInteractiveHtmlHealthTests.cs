using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Qualifies generated interactive hosts without accepting arbitrary page clipping.</summary>
public sealed class GalleryInteractiveHtmlHealthTests {
    [Fact]
    public void GeneratedLifecyclePagesAndBoundedInteractiveHostsQualifyWhileBrokenHostContractsFail() {
        var chart = Chart.Create().WithSize(320, 220).WithTitle("Counts")
            .AddBar("Counts", new[] { new ChartPoint(1, 20), new ChartPoint(2, 50) });
        var fit = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        var readable = chart.ToInteractiveHtmlPage();
        var variants = new Dictionary<string, string> {
            ["graphite-light-chart-lifecycle"] = HtmlChartLifecycleExamples.CreatePage(false),
            ["graphite-dark-chart-lifecycle"] = HtmlChartLifecycleExamples.CreatePage(true),
            ["fit"] = fit,
            ["readable"] = readable,
            ["fake-host"] = fit.Replace("<section class=\"cfx-interactive-chart\"", "<section class=\"unrelated-chart\"", StringComparison.Ordinal),
            ["missing-native-size"] = fit.Replace("--cfx-native-width:", "--missing-native-width:", StringComparison.Ordinal),
            ["unbounded-native-svg"] = fit.Replace("viewBox=", "data-missing-viewbox=", StringComparison.Ordinal),
            ["nonfinite-native-svg"] = Regex.Replace(fit, "viewBox=\"[^\"]+\"", "viewBox=\"0 0 NaN 220\""),
            ["foreign-svg"] = fit.Replace("xmlns=\"http://www.w3.org/2000/svg\"", "xmlns=\"https://example.test/foreign\"", StringComparison.Ordinal),
            ["clipped-host"] = fit.Replace("</head>", "<style>.cfx-interactive-chart{overflow:hidden}</style></head>", StringComparison.Ordinal)
        };
        var directory = Path.Combine(Path.GetTempPath(), "ChartForgeX-interactive-html-health-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            var svg = chart.ToSvg();
            var png = chart.ToPng();
            foreach (var variant in variants) {
                File.WriteAllText(Path.Combine(directory, variant.Key + ".html"), variant.Value);
                File.WriteAllText(Path.Combine(directory, variant.Key + ".svg"), svg);
                File.WriteAllBytes(Path.Combine(directory, variant.Key + ".png"), png);
            }
            GalleryWriter.Write(directory);
            var manifestText = File.ReadAllText(Path.Combine(directory, "svg-png-comparison.json"));
            var retained = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(retained)) {
                Directory.CreateDirectory(retained);
                File.WriteAllText(Path.Combine(retained, "interactive-gallery-health.json"), manifestText);
            }
            using var manifest = JsonDocument.Parse(manifestText);
            var health = manifest.RootElement.GetProperty("charts").EnumerateArray()
                .ToDictionary(item => item.GetProperty("name").GetString()!, item => item.GetProperty("html").Clone());
            foreach (var name in new[] { "graphite-light-chart-lifecycle", "graphite-dark-chart-lifecycle", "fit", "readable" })
                Assert.True(health[name].GetProperty("healthy").GetBoolean(), name + ": " + health[name].GetRawText());
            foreach (var name in new[] { "fake-host", "missing-native-size", "unbounded-native-svg", "nonfinite-native-svg", "foreign-svg", "clipped-host" })
                Assert.False(health[name].GetProperty("hasExpectedOverflow").GetBoolean(), name + ": " + health[name].GetRawText());
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }
}
