using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void RealImageInputsComposeAsVisualCanvasBackgrounds() {
        var sources = RealImageSources().Take(4).ToArray();
        if (sources.Length == 0) return;

        foreach (var source in sources) {
            var bytes = ReadRealImageBytes(source);
            var image = RasterImageDecoder.Decode(bytes);
            var width = Math.Min(360, image.Width);
            var height = Math.Max(1, (int)Math.Round(width * image.Height / (double)image.Width));
            var canvas = VisualCanvas.Create(width, height)
                .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
                .AddRasterImage(0, 0, width, height, image, VisualCanvasImageFit.Cover)
                .AddKeyValueBlock(
                    VisualCanvasPlacement.At(VisualCanvasAnchor.BottomLeft, 10, 10),
                    Math.Min(260, width - 20),
                    new[] {
                        VisualCanvasKeyValueItem.LabelRow("Real image input"),
                        VisualCanvasKeyValueItem.Pair("Source", Path.GetFileName(source)),
                        VisualCanvasKeyValueItem.Pair("Pixels", image.Width + "x" + image.Height)
                    },
                    labelFontSize: 10,
                    valueFontSize: 10,
                    columnGap: 12,
                    rowGap: 3,
                    labelColor: ChartColor.White,
                    valueColor: ChartColor.White);

            Assert(canvas.ToPng().Length > 64, "Real image inputs should decode and compose as visual canvas backgrounds: " + source);
        }
    }

    private static IEnumerable<string> RealImageSources() {
        var configured = Environment.GetEnvironmentVariable("CHARTFORGEX_REAL_IMAGE_SOURCES");
        if (!string.IsNullOrWhiteSpace(configured)) {
            foreach (var source in configured.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim())) {
                if (source.Length > 0) yield return source;
            }
        }

        var systemCandidates = new[] {
            @"C:\Windows\Web\touchkeyboard\TouchKeyboardThemeDark000.jpg",
            @"C:\Windows\Web\touchkeyboard\TouchKeyboardThemeLight000.jpg",
            @"C:\Windows\Web\4K\Wallpaper\Windows\img0_1920x1200.jpg",
            @"C:\Windows\Web\Wallpaper\Spotlight\img14.jpg"
        };

        foreach (var source in systemCandidates) {
            if (File.Exists(source)) yield return source;
        }
    }

    private static byte[] ReadRealImageBytes(string source) {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ChartForgeX smoke tests");
            return client.GetByteArrayAsync(uri).GetAwaiter().GetResult();
        }

        return File.ReadAllBytes(source);
    }
}
