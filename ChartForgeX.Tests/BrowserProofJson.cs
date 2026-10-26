using System.Text.Json;
using Microsoft.Playwright;

namespace ChartForgeX.Tests;

/// <summary>Retains primitive browser facts before transport can attach object-reference metadata.</summary>
internal static class BrowserProofJson {
    internal static async Task<JsonElement> EvaluateReferenceJsonAsync(this IPage page, string expression, object? argument = null) {
        try {
            var json = await page.EvaluateAsync<string>("async argument => { try { return JSON.stringify(await (" + expression + ")(argument)); } catch(error) { throw new Error(String(error?.message ?? error?.str ?? error)); } }", argument);
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        } catch (PlaywrightException) {
            if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
                Directory.CreateDirectory(output);
                var name = Path.Combine(output, "reference-observer-error-" + Guid.NewGuid().ToString("N"));
                try {
                    File.WriteAllText(name + ".html", await page.ContentAsync());
                    File.WriteAllText(name + ".observer.js", expression);
                    await page.ScreenshotAsync(new PageScreenshotOptions { Path = name + ".png", FullPage = true });
                } catch (Exception captureError) {
                    File.WriteAllText(name + ".capture-error.txt", captureError.ToString());
                }
            }
            throw;
        }
    }
}
