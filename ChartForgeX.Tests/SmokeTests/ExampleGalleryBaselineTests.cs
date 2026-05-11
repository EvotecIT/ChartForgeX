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
            File.WriteAllText(Path.Combine(output, "visual-baseline.json"), "{\"version\":1,\"charts\":[{\"name\":\"alpha\",\"width\":320,\"height\":180,\"svg\":{\"minVisualNodes\":2,\"maxClippedTextNodes\":0,\"maxNearEdgeTextNodes\":999},\"png\":{\"outputScale\":2,\"minVisiblePixels\":64,\"minDistinctColors\":8,\"maxEdgeInkPixels\":0}}]}");

            GalleryWriter.Write(output);

            var manifest = File.ReadAllText(Path.Combine(output, "svg-png-comparison.json"));
            Assert(manifest.Contains("\"chartMatches\": 0", StringComparison.Ordinal), "Gallery manifest should fail baseline matches when PNG output density regresses.");
            Assert(manifest.Contains("\"warnings\": 1", StringComparison.Ordinal), "Gallery manifest should count high-DPI visual-baseline warnings.");
            Assert(manifest.Contains("\"clean\": false", StringComparison.Ordinal), "Gallery manifest should flag the visual baseline as not clean.");
            var dashboard = File.ReadAllText(Path.Combine(output, "quality-dashboard.html"));
            Assert(dashboard.Contains("Baseline warnings", StringComparison.Ordinal) && dashboard.Contains("<div class=\"value\">1</div>", StringComparison.Ordinal), "Quality dashboard should surface high-DPI visual-baseline warnings.");
        } finally {
            Directory.Delete(output, true);
        }
    }
}
