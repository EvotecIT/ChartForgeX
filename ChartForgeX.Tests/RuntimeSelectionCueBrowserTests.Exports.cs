using System.Xml.Linq;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class RuntimeSelectionCueBrowserTests {
    private static async Task<string> DownloadSvg(IPage page) {
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").EvaluateAsync("button=>button.click()"));
        return await File.ReadAllTextAsync(await download.PathAsync() ?? throw new InvalidOperationException("No exported SVG."));
    }

    private static async Task AssertPersistentExports(IPage page, string name) {
        var svg = await DownloadSvg(page);
        var document = XDocument.Parse(svg);
        var cues = document.Descendants().Where(node => node.Attribute("data-cfx-state-outline") is not null).ToArray();
        Assert.Equal(2, cues.Length);
        foreach (var cue in cues) Assert.DoesNotContain(cue.DescendantsAndSelf(), node => node.Attribute("id") is not null
            || node.Attribute("tabindex") is not null || node.Attribute("data-cfx-target-id") is not null);
        Assert.DoesNotContain("data-cfx-keyboard-focus-outline", svg, StringComparison.Ordinal);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").EvaluateAsync("button=>button.click()"));
        var png = await File.ReadAllBytesAsync(await download.PathAsync() ?? throw new InvalidOperationException("No exported PNG."));
        var image = PngReader.Decode(png);
        var cyan = 0;
        for (var index = 0; index < image.Pixels.Length; index += 4)
            if (Math.Abs(image.Pixels[index] - 56) <= 3 && Math.Abs(image.Pixels[index + 1] - 189) <= 3
                && Math.Abs(image.Pixels[index + 2] - 248) <= 3 && image.Pixels[index + 3] > 240) cyan++;
        Assert.True(cyan > 4, "The PNG retains painted selected-observation cue pixels.");
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        await File.WriteAllTextAsync(Path.Combine(directory, "selection-cue-export-" + name + ".svg"), svg);
        await File.WriteAllBytesAsync(Path.Combine(directory, "selection-cue-export-" + name + ".png"), png);
    }
}
