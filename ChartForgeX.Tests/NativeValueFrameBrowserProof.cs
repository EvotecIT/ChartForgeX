using System.Text.Json;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Observes actual layer bounds and painted browser pixels for both finite native value consumers.</summary>
internal static class NativeValueFrameBrowserProof {
    internal static async Task AssertAsync(IPage page, string markSelector, string name, double minimumInkFraction = .25) {
        var layout = await page.EvaluateAsync<JsonElement>("""
            ()=>{const root=document.querySelector('.cfx-interactive-chart'),svg=root.querySelector('.cfx-stage>svg'),layer=root.querySelector('.cfx-native-motion-frame');
                const box=node=>{const r=node.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height}};
                return {source:box(svg),layer:box(layer),frame:box(layer.querySelector('svg'))}}
            """);
        foreach (var key in new[] { "source", "layer", "frame" }) {
            var box = layout.GetProperty(key);
            foreach (var dimension in new[] { "x", "y", "width", "height" }) Assert.True(double.IsFinite(box.GetProperty(dimension).GetDouble()));
            Assert.True(box.GetProperty("width").GetDouble() > 0); Assert.True(box.GetProperty("height").GetDouble() > 0);
        }
        foreach (var key in new[] { "layer", "frame" }) foreach (var dimension in new[] { "x", "y", "width", "height" })
            Assert.InRange(Math.Abs(layout.GetProperty(key).GetProperty(dimension).GetDouble() - layout.GetProperty("source").GetProperty(dimension).GetDouble()), 0, .75);
        var paint = page.Locator(".cfx-native-motion-frame " + markSelector);
        var mark = await paint.BoundingBoxAsync()
            ?? throw new InvalidOperationException("The captured native mark has no visible bounds.");
        Assert.True(mark.Width > 4 && mark.Height > 4);
        // The marker perimeter is real paint too. A center-only crop can be entirely covered
        // by an unchanged uncertainty stem beneath a small circular marker.
        var clip = new Clip { X = mark.X, Y = mark.Y, Width = mark.Width, Height = mark.Height };
        var visible = await page.ScreenshotAsync(new PageScreenshotOptions { Clip = clip });
        byte[] hidden;
        await paint.EvaluateAsync("mark=>mark.style.visibility='hidden'");
        try { hidden = await page.ScreenshotAsync(new PageScreenshotOptions { Clip = clip }); }
        finally { await paint.EvaluateAsync("mark=>mark.style.removeProperty('visibility')"); }
        var on = PngReader.Decode(visible); var off = PngReader.Decode(hidden);
        Assert.Equal(on.Width, off.Width); Assert.Equal(on.Height, off.Height);
        var ink = Enumerable.Range(0, on.Width * on.Height).Count(pixel =>
            Math.Abs(on.Pixels[pixel * 4] - off.Pixels[pixel * 4]) + Math.Abs(on.Pixels[pixel * 4 + 1] - off.Pixels[pixel * 4 + 1])
                + Math.Abs(on.Pixels[pixel * 4 + 2] - off.Pixels[pixel * 4 + 2]) > 24);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); await File.WriteAllBytesAsync(Path.Combine(output, name + ".visible.png"), visible);
            await File.WriteAllBytesAsync(Path.Combine(output, name + ".hidden.png"), hidden);
            await File.WriteAllTextAsync(Path.Combine(output, name + ".bounds.json"), JsonSerializer.Serialize(new { layout, mark, ink, pixels = on.Width * on.Height, minimumInkFraction }));
        }
        Assert.True(ink >= Math.Max(1, (int)Math.Ceiling(on.Width * on.Height * minimumInkFraction)), "The displayed native frame must paint visible native mark ink.");
    }

}
