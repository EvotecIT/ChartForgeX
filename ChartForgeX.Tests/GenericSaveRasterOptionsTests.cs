using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GenericSaveRasterOptionsTests {
    [Theory]
    [InlineData("chart")]
    [InlineData("chart-grid")]
    [InlineData("block")]
    [InlineData("visual-grid")]
    [InlineData("canvas")]
    public void InferredPngSavePreservesDpiAndCompressionAcrossSupportedSurfaces(string surface) {
        var save = CreateSave(surface);
        var path = Path.Combine(Path.GetTempPath(), "chartforgex-save-options-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            save(path, new RasterImageOptions { Dpi = 300, PngCompressionLevel = 0 });
            var stored = File.ReadAllBytes(path);
            save(path, new RasterImageOptions { Dpi = 144, PngCompressionLevel = 9 });
            var compressed = File.ReadAllBytes(path);

            AssertPngOptions(stored, 11811, 0x01);
            AssertPngOptions(compressed, 5669, 0x9C);
            Assert.True(stored.Length > compressed.Length);
            Assert.Equal(RasterImageDecoder.Decode(stored).Pixels, RasterImageDecoder.Decode(compressed).Pixels);
        } finally {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("chart")]
    [InlineData("chart-grid")]
    [InlineData("block")]
    [InlineData("visual-grid")]
    [InlineData("canvas")]
    public void VectorSaveRoutesStillInferSvgAndHtml(string surface) {
        var save = CreateSave(surface);
        var path = Path.Combine(Path.GetTempPath(), "chartforgex-save-vector-" + Guid.NewGuid().ToString("N"));
        try {
            save(path + ".svg", new RasterImageOptions { Dpi = 300 });
            save(path + ".html", new RasterImageOptions { Dpi = 300 });
            Assert.Contains("<svg", File.ReadAllText(path + ".svg"));
            Assert.Contains("<!DOCTYPE html>", File.ReadAllText(path + ".html"), StringComparison.OrdinalIgnoreCase);
        } finally {
            File.Delete(path + ".svg");
            File.Delete(path + ".html");
        }
    }

    private static Action<string, RasterImageOptions> CreateSave(string surface) {
        var chart = Chart.Create().WithSize(320, 220).WithTitle("Ready")
            .AddLine("Results", new[] { new ChartPoint(0, 1), new ChartPoint(1, 3) });
        IVisualBlock block = MetricCard.Create().WithSize(180, 100).WithMetric("Ready", 42);
        return surface switch {
            "chart" => chart.Save,
            "chart-grid" => ChartGrid.Create().Add(chart).Save,
            "block" => block.Save,
            "visual-grid" => VisualGrid.Create().WithPanelSize(180, 100).Add(block).Save,
            "canvas" => VisualCanvas.Create(180, 100).Save,
            _ => throw new ArgumentOutOfRangeException(nameof(surface))
        };
    }

    private static void AssertPngOptions(byte[] bytes, int pixelsPerMetre, int zlibFlags) {
        var chunks = ExportContainerInfo.PngChunks(bytes).ToArray();
        var density = Assert.Single(chunks, chunk => chunk.Name == "pHYs").Data;
        Assert.Equal(pixelsPerMetre, ExportContainerInfo.Big32(density, 0));
        Assert.Equal(pixelsPerMetre, ExportContainerInfo.Big32(density, 4));
        Assert.Equal(1, density[8]);
        var data = Assert.Single(chunks, chunk => chunk.Name == "IDAT").Data;
        Assert.Equal(0x78, data[0]);
        Assert.Equal(zlibFlags, data[1]);
    }
}
