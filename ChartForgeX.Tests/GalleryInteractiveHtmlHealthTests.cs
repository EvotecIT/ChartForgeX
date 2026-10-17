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
        var adaptive = chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 240; options.Adaptive.NarrowHeight = 260;
        });
        var second = chart.ToInteractiveHtmlFragmentWithoutAssets();
        var variants = new Dictionary<string, string> {
            ["graphite-light-chart-lifecycle"] = HtmlChartLifecycleExamples.CreatePage(false),
            ["graphite-dark-chart-lifecycle"] = HtmlChartLifecycleExamples.CreatePage(true),
            ["fit"] = fit,
            ["readable"] = readable,
            ["adaptive"] = adaptive,
            ["linked-light"] = LinkedChartViewsExamples.CreatePage(false),
            ["linked-dark"] = LinkedChartViewsExamples.CreatePage(true),
            ["two-hosts"] = fit.Replace("</main>", second + "</main>", StringComparison.Ordinal),
            ["fake-host"] = fit.Replace("<section class=\"cfx-interactive-chart\"", "<section class=\"unrelated-chart\"", StringComparison.Ordinal),
            ["missing-native-size"] = fit.Replace("--cfx-native-width:", "--missing-native-width:", StringComparison.Ordinal),
            ["unbounded-native-svg"] = fit.Replace("viewBox=", "data-missing-viewbox=", StringComparison.Ordinal),
            ["nonfinite-native-svg"] = Regex.Replace(fit, "viewBox=\"[^\"]+\"", "viewBox=\"0 0 NaN 220\""),
            ["foreign-svg"] = fit.Replace("xmlns=\"http://www.w3.org/2000/svg\"", "xmlns=\"https://example.test/foreign\"", StringComparison.Ordinal),
            ["clipped-host"] = fit.Replace("</head>", "<style>.cfx-interactive-chart{overflow:hidden}</style></head>", StringComparison.Ordinal),
            ["clipped-group-host"] = fit.Replace("</head>", "<style>.other,.cfx-interactive-chart{overflow:clip}</style></head>", StringComparison.Ordinal),
            ["missing-fit-sizing"] = fit.Replace(".cfx-interactive-chart[data-cfx-responsive-layout=\"fit\"] .cfx-stage svg", ".unrelated-fit-svg", StringComparison.Ordinal),
            ["missing-readable-scroll"] = readable.Replace("overflow-x: auto;", "overflow-x: visible;", StringComparison.Ordinal),
            ["missing-narrow-scene"] = Regex.Replace(adaptive, "<template data-cfx-adaptive-narrow=\"true\">[\\s\\S]*?</template>", string.Empty),
            ["missing-narrow-template"] = adaptive.Replace("data-cfx-adaptive-narrow=\"true\"", "data-missing-narrow-template=\"true\"", StringComparison.Ordinal),
            ["unclosed-narrow-template"] = adaptive.Replace("</template>", string.Empty, StringComparison.Ordinal),
            ["nonfinite-narrow-svg"] = AlterNarrow(adaptive, narrow => Regex.Replace(narrow, "viewBox=\"[^\"]+\"", "viewBox=\"0 0 NaN 260\"")),
            ["missing-narrow-size"] = AlterNarrow(adaptive, narrow => narrow.Replace("--cfx-native-width:", "--missing-native-width:", StringComparison.Ordinal)),
            ["mismatched-narrow-source"] = AlterNarrow(adaptive, narrow => Regex.Replace(narrow, "data-cfx-chart-id=\"[^\"]+\"", "data-cfx-chart-id=\"another-source\"")),
            ["broken-second-host"] = fit.Replace("</main>", second.Replace("--cfx-native-width:", "--missing-native-width:", StringComparison.Ordinal) + "</main>", StringComparison.Ordinal)
        };
        var healthyNames = new[] { "graphite-light-chart-lifecycle", "graphite-dark-chart-lifecycle", "fit", "readable", "adaptive", "linked-light", "linked-dark", "two-hosts" };
        foreach (var variant in variants.Where(variant => !healthyNames.Contains(variant.Key)))
            Assert.DoesNotContain(variant.Value, new[] { fit, readable, adaptive }, StringComparer.Ordinal);
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
            foreach (var name in healthyNames)
                Assert.True(health[name].GetProperty("healthy").GetBoolean(), name + ": " + health[name].GetRawText());
            foreach (var name in variants.Keys.Except(healthyNames)) {
                Assert.False(health[name].GetProperty("hasExpectedOverflow").GetBoolean(), name + ": " + health[name].GetRawText());
                Assert.False(health[name].GetProperty("healthy").GetBoolean(), name + ": " + health[name].GetRawText());
            }
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string AlterNarrow(string html, Func<string, string> change) =>
        Regex.Replace(html, "(?<open><template data-cfx-adaptive-narrow=\"true\">)(?<scene>[\\s\\S]*?)(?<close></template>)",
            match => match.Groups["open"].Value + change(match.Groups["scene"].Value) + match.Groups["close"].Value);
}
