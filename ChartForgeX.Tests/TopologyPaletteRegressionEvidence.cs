using System.Text.Json;
using ChartForgeX.Topology;
using Microsoft.Playwright;

namespace ChartForgeX.Tests;

internal static class TopologyPaletteRegressionEvidence {
    internal static void Write(string name, PreparedTopology prepared, object receipt, byte[]? reference = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllText(path + ".svg", prepared.ToSvg());
        File.WriteAllBytes(path + ".native.png", prepared.ToPng());
        File.WriteAllText(path + ".json", JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
        if (reference != null) File.WriteAllBytes(path + ".reference.png", reference);
    }

    internal static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png"), FullPage = true });
    }
}
