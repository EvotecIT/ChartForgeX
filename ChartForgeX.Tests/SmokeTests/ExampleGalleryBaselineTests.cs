using System;
using System.IO;
using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ExampleGalleryBaselineFlagsHighDensityRegressions() {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-gallery-baseline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var chart = Chart.Create().WithSize(320, 180).WithTitle("Alpha").AddLine("Values", Points(1, 2, 3));
            File.WriteAllText(Path.Combine(output, "alpha.html"), chart.ToHtmlPage());
            File.WriteAllText(Path.Combine(output, "alpha.svg"), chart.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "alpha.png"), chart.ToPng());
            File.WriteAllText(Path.Combine(output, "alpha.csharp.txt"), "chart.SaveSvg(\"alpha.svg\");");
            File.WriteAllText(Path.Combine(output, "site-topology.html"), chart.ToHtmlPage());
            var topologySvg = chart.ToSvg()
                .Replace("viewBox=\"0 0 320 180\"", "viewBox=\"0 0 360 220\"", StringComparison.Ordinal)
                .Replace("</svg>", "<text x=\"340\" y=\"200\" font-size=\"10\">ViewBox coordinates</text></svg>", StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(output, "site-topology.svg"), topologySvg);
            File.WriteAllBytes(Path.Combine(output, "site-topology.png"), chart.ToPng());
            File.WriteAllText(Path.Combine(output, "visual-baseline.json"), "{\"version\":1,\"charts\":[{\"name\":\"alpha\",\"width\":320,\"height\":180,\"svg\":{\"minVisualNodes\":2,\"maxClippedTextNodes\":0,\"maxNearEdgeTextNodes\":999},\"png\":{\"outputScale\":2,\"minVisiblePixels\":64,\"minDistinctColors\":8,\"maxEdgeInkPixels\":0}},{\"name\":\"site-topology\",\"width\":320,\"height\":180,\"svg\":{\"minVisualNodes\":2,\"maxClippedTextNodes\":0,\"maxNearEdgeTextNodes\":999},\"png\":{\"outputScale\":1,\"minVisiblePixels\":64,\"minDistinctColors\":8,\"maxEdgeInkPixels\":0}}]}");

            GalleryWriter.Write(output);

            var manifest = File.ReadAllText(Path.Combine(output, "svg-png-comparison.json"));
            Assert(manifest.Contains("\"chartMatches\": 1", StringComparison.Ordinal), "Gallery manifest should retain the healthy topology baseline match while the alpha PNG density regresses.");
            Assert(manifest.Contains("\"warnings\": 1", StringComparison.Ordinal), "Gallery manifest should count high-DPI visual-baseline warnings.");
            Assert(manifest.Contains("\"clean\": false", StringComparison.Ordinal), "Gallery manifest should flag the visual baseline as not clean.");
            Assert(manifest.Contains("\"name\": \"site-topology\"", StringComparison.Ordinal), "Topology SVG/PNG pairs should use the same premium comparison and numeric baseline as charts.");
            using (var document = System.Text.Json.JsonDocument.Parse(manifest)) {
                var topology = document.RootElement.GetProperty("charts").EnumerateArray().Single(item => item.GetProperty("name").GetString() == "site-topology");
                Assert(topology.GetProperty("svg").GetProperty("clippedTextNodes").GetInt32() == 0, "SVG quality checks should evaluate text against viewBox coordinates rather than physical output dimensions.");
            }
            var dashboard = File.ReadAllText(Path.Combine(output, "quality-dashboard.html"));
            Assert(dashboard.Contains("Baseline warnings", StringComparison.Ordinal) && dashboard.Contains("<div class=\"value\">1</div>", StringComparison.Ordinal), "Quality dashboard should surface high-DPI visual-baseline warnings.");
            var index = File.ReadAllText(Path.Combine(output, "index.html"));
            var catalog = File.ReadAllText(Path.Combine(output, "catalog.html"));
            Assert(index.Contains("alpha.csharp.txt", StringComparison.Ordinal), "Generated gallery cards should link C# snippets when available.");
            Assert(catalog.Contains("alpha.csharp.txt", StringComparison.Ordinal), "Grouped catalog cards should link C# snippets when available.");
        } finally {
            Directory.Delete(output, true);
        }
    }
}
