using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
[Collection(nameof(FontRegistryCollection))]
public sealed class SvgRasterOrderedFilterBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(760, false)] [InlineData(760, true)]
    public async Task NamedInputsAndActualNativeFilterRecipesKeepTheirOutputsAndCompleteDownloadVisible(int width, bool dark) {
        var cases = Cases(); var sources = new string[cases.Length]; var natives = new byte[cases.Length][];
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;font:14px sans-serif;background:" + (dark ? "#151a22;color:#f4f6fa" : "#f8fafc;color:#162030")
            + ";}main{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}figure{margin:0;min-width:0;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));align-content:start;gap:6px}svg,img{width:100%;max-width:96px;height:auto;aspect-ratio:4/3;image-rendering:pixelated}figcaption{grid-column:1/-1;min-height:36px}a{color:inherit}</style>"
            + "<h1>Ordered native filter sources</h1><p>Source SVG and native output. Small values retain native RGBA16 precision; bloom uses the native three-box approximation.</p><main>";
        for (var index = 0; index < cases.Length; index++) {
            var item = cases[index];
            var source = item.Source.Replace("id='f'", "id='f" + index + "'", StringComparison.Ordinal)
                .Replace("url(#f)", "url(#f" + index + ")", StringComparison.Ordinal);
            if (!string.IsNullOrWhiteSpace(directory)) {
                Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "ordered-filters-" + width + "-" + dark + "-" + index + ".source.svg"), source);
            }
            var result = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions()); Assert.Empty(result.Diagnostics);
            sources[index] = source; natives[index] = PngWriter.WriteRgba(result.Image);
            html += "<figure data-case='" + index + "'><figcaption>" + item.Name + "</figcaption>" + source
                + "<img alt='Native output' src='data:image/png;base64," + Convert.ToBase64String(natives[index]) + "'></figure>";
        }
        using var stream = typeof(SvgRasterOrderedFilterBrowserTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.RecipeOrderedFiltersProgram.cs")
            ?? throw new InvalidOperationException("Missing complete ordered-filter Core program.");
        using var reader = new StreamReader(stream); var program = reader.ReadToEnd();
        html += "</main><p><a download='ordered-filters.cs' href='data:text/plain;charset=utf-8," + Uri.EscapeDataString(program) + "'>Download complete C# program</a></p>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 720);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              const rows=[];
              for(const figure of document.querySelectorAll('figure')) {
                const source=new Image(); source.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(figure.querySelector('svg').outerHTML))); await source.decode();
                const native=figure.querySelector('img'); await native.decode();
                function read(image) { const canvas=document.createElement('canvas'); canvas.width=image.naturalWidth; canvas.height=image.naturalHeight;
                  const context=canvas.getContext('2d'); context.drawImage(image,0,0); return {width:canvas.width,height:canvas.height,rgba:[...context.getImageData(0,0,canvas.width,canvas.height).data]}; }
                rows.push({caption:figure.querySelector('figcaption').textContent,source:read(source),native:read(native)});
              }
              return {userAgent:navigator.userAgent,rows,overflow:document.documentElement.scrollWidth>innerWidth,scripts:document.querySelectorAll('svg script,svg foreignObject').length};
            }
            """);
        var download = await session.Page.RunAndWaitForDownloadAsync(() => session.Page.Locator("a[download]").ClickAsync());
        var downloaded = await download.PathAsync() ?? throw new InvalidOperationException("The complete source download produced no file.");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "ordered-filters-" + width + "-" + dark);
            File.WriteAllText(stem + ".observed.json", observed.GetRawText()); await download.SaveAsAsync(stem + ".download.cs");
            for (var index = 0; index < cases.Length; index++) {
                File.WriteAllText(stem + "-" + index + ".source.svg", sources[index]); File.WriteAllBytes(stem + "-" + index + ".native.png", natives[index]);
            }
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        Assert.Equal(program, File.ReadAllText(downloaded)); Assert.Equal("ordered-filters.cs", download.SuggestedFilename);
        var rows = observed.GetProperty("rows"); Assert.Equal(cases.Length, rows.GetArrayLength());
        for (var index = 0; index < cases.Length; index++) {
            var item = cases[index]; var row = rows[index];
            int[] Pixel(string kind) {
                var image = row.GetProperty(kind); var offset = (item.Y * image.GetProperty("width").GetInt32() + item.X) * 4;
                return image.GetProperty("rgba").EnumerateArray().Skip(offset).Take(4).Select(value => value.GetInt32()).ToArray();
            }
            var source = Pixel("source"); var native = Pixel("native");
            Assert.Equal(item.Native.Select(value => (int)value), native);
            if (item.Compare) for (var channel = 0; channel < 4; channel++) Assert.InRange(Math.Abs(source[channel] - native[channel]), 0, 1);
            else Assert.True(source[3] > 0); // Preserve actual precision/approximation observations without claiming browser/native identity.
        }
        Assert.False(observed.GetProperty("overflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static (string Name, string Source, int X, int Y, byte[] Native, bool Compare)[] Cases() {
        var outlineSource = RetainedAppearanceFixture.Source();
        var bloomSource = RetainedAppearanceFixture.Source(white: true);
        var options = new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters);
        return new[] {
            ("Three ordered merge inputs", OrderedFilterFixture.Source("<feFlood flood-color='blue' result='blue'/><feFlood flood-color='red' flood-opacity='.5' result='red'/>"
                + "<feFlood flood-color='lime' flood-opacity='.5' result='green'/><feMerge><feMergeNode in='blue'/><feMergeNode in='red'/><feMergeNode in='green'/></feMerge>"), 1, 1, new byte[] {64,128,64,255}, true),
            ("Arithmetic branches", OrderedFilterFixture.Source("<feFlood flood-color='red' flood-opacity='.5' result='red'/><feFlood flood-color='blue' flood-opacity='.5' result='blue'/>"
                + "<feComposite in='red' in2='blue' operator='arithmetic' k2='1' k3='1'/>"), 1, 1, new byte[] {128,0,128,255}, true),
            ("Small values retained in RGBA16", OrderedFilterFixture.Source("<feFlood flood-color='rgb(.2490234375,.2490234375,.2490234375)' result='low'/>"
                + OrderedFilterFixture.Scale("low","lift",8) + OrderedFilterFixture.Scale("lift",null,8)), 1, 1, new byte[] {16,16,16,255}, false),
            ("Outline from public NativeFilters", outlineSource.WithOutline(new VisualOutline(ChartColor.FromRgb(0,0,255),1,false)).ToSvg(options), 1, 1, new byte[] {0,0,255,128}, true),
            ("Bloom with native three-box blur", bloomSource.WithBloom(new VisualBloom(1,.1,.7,orderedDither:false)).ToSvg(options), 1, 1, new byte[] {255,255,255,20}, false),
            ("Linear RGB consumer", OrderedFilterFixture.Source("<feFlood flood-color='rgb(127.5,127.5,127.5)' result='a'/><feFlood flood-color='rgb(127.5,127.5,127.5)' result='b'/>"
                + "<feBlend in='a' in2='b' mode='multiply' color-interpolation-filters='linearRGB'/>"), 1, 1, new byte[] {60,60,60,255}, true)
        };
    }
}
