using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Composition;
using ChartForgeX.SvgRaster;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Reference observation only; this class is excluded from ordinary/native acceptance and runs only in a selected browser lane.</summary>
[Trait("Category", "EffectReference")]
public sealed class TurbulencePrimaryReferenceObservationTests {
    [MermaidReferenceTheory]
    [InlineData(1, 1, false, false, false)]
    [InlineData(1, 1, true, true, true)]
    [InlineData(-7.9, 3, false, true, false)]
    [InlineData(13, 2, true, false, true)]
    public async Task PinnedBrowserSamplesDiscriminateGradientLawsWithoutSelectingAProductionVariant(
        double seed, int octaves, bool stitch, bool fractal, bool linearRgb) {
        var space = linearRgb ? "linearRGB" : "sRGB";
        var source = "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='48'><defs>"
            + "<filter id='noise' x='0' y='0' width='64' height='48' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='" + space + "'>"
            + "<feTurbulence type='" + (fractal ? "fractalNoise" : "turbulence") + "' baseFrequency='.071 .113' numOctaves='" + octaves
            + "' seed='" + seed.ToString(CultureInfo.InvariantCulture) + "' stitchTiles='" + (stitch ? "stitch" : "noStitch") + "'/></filter></defs>"
            + "<rect width='64' height='48' filter='url(#noise)'/></svg>";
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions()));
        var native = SvgRasterizer.Rasterize(source, strict: false, filters: new SvgRasterFilterOptions());
        await using var session = await BrowserProofFonts.OpenAsync("<main>" + source + "</main><canvas id='probe' width='64' height='48'></canvas>", 320, 200);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              const image=new Image();image.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(document.querySelector('svg').outerHTML)));
              await image.decode();const ctx=document.querySelector('#probe').getContext('2d',{willReadFrequently:true});ctx.drawImage(image,0,0);
              const locations=[[3,5],[11,7],[23,19],[41,29],[59,43]];
              return {userAgent:navigator.userAgent,colorSpace:document.querySelector('filter').getAttribute('color-interpolation-filters'),
                points:locations.map(([x,y])=>({x,y,pixel:[...ctx.getImageData(x,y,1,1).data]}))};
            }
            """);
        var square = new TurbulenceReferenceOracle(seed, false);
        var disk = new TurbulenceReferenceOracle(seed, true);
        var comparisons = observed.GetProperty("points").EnumerateArray().Select(point => {
            var x = point.GetProperty("x").GetInt32(); var y = point.GetProperty("y").GetInt32();
            return new { x, y, actual = point.GetProperty("pixel").EnumerateArray().Select(value => value.GetInt32()).ToArray(),
                squareCenter = square.Sample(x + .5, y + .5, .071, .113, octaves, fractal, stitch, linearRgb),
                diskCenter = disk.Sample(x + .5, y + .5, .071, .113, octaves, fractal, stitch, linearRgb),
                squareInteger = square.Sample(x, y, .071, .113, octaves, fractal, stitch, linearRgb),
                diskInteger = disk.Sample(x, y, .071, .113, octaves, fractal, stitch, linearRgb) };
        }).ToArray();
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, "turbulence-primary-" + seed.ToString(CultureInfo.InvariantCulture) + "-" + octaves + "-" + stitch + "-" + fractal + "-" + space);
            File.WriteAllText(stem + ".source.svg", source);
            File.WriteAllBytes(stem + ".native.png", ImageComposition.FromImage(native.Image).ToPng());
            File.WriteAllText(stem + ".native-diagnostics.json", JsonSerializer.Serialize(native.Diagnostics.Select(item => new { item.Code, item.Message })));
            File.WriteAllText(stem + ".reference.json", observed.GetRawText());
            File.WriteAllText(stem + ".gradient-comparisons.json", JsonSerializer.Serialize(comparisons));
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        // No expected pixel/generator choice is asserted before the actual engine/version receipts are inspected.
        Assert.Equal(space, observed.GetProperty("colorSpace").GetString());
        Assert.Equal(5, comparisons.Length);
        Assert.Contains(comparisons, point => !point.squareCenter.SequenceEqual(point.diskCenter));
        Assert.Contains(native.Diagnostics, diagnostic => diagnostic.Code == "SFR002");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
