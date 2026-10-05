using System.Text.Json;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GalleryLabelQualityTests {
    [Fact]
    public void QualityChecksUseRenderedNestedCoordinatesAndIgnoreDroppedText() {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-nested-quality-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var chart = Chart.Create().WithSize(320, 180).AddLine("Values", Enumerable.Range(1, 3).Select(i => new ChartForgeX.Primitives.ChartPoint(i, i)));
            var svg = chart.ToSvg().Replace("</svg>", "<svg x='200' y='130' width='80' height='40' viewBox='0 0 800 400'>"
                + "<text x='700' y='200' font-size='12'>Nested</text></svg>"
                + "<g display='none'><text x='9999' y='9999' font-size='1'>Dropped</text></g>"
                + "<text transform='translate(340 0)' x='10' y='80' font-size='12'>Outside</text></svg>", StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(output, "chart.svg"), svg);
            File.WriteAllText(Path.Combine(output, "chart.html"), chart.ToHtmlPage());
            File.WriteAllBytes(Path.Combine(output, "chart.png"), chart.ToPng());
            GalleryWriter.Write(output);
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
            var quality = document.RootElement.GetProperty("charts")[0].GetProperty("svg");
            Assert.Equal(1, quality.GetProperty("clippedTextNodes").GetInt32());
            Assert.Equal(0, quality.GetProperty("tinyTextNodes").GetInt32());
        } finally { Directory.Delete(output, true); }
    }
}
