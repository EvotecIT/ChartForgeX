using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Ordinary public exports retained before adding a new complete-child effect plan.</summary>
public sealed class NativeEffectExtensionBeforeControlsTests {
    [Theory]
    [InlineData(320, VisualThemeMode.Light, 1)] [InlineData(320, VisualThemeMode.Dark, 2)]
    [InlineData(800, VisualThemeMode.Light, 2)] [InlineData(800, VisualThemeMode.Dark, 1)]
    public void OrdinarySourceZeroBlurNonzeroBlurAndImperativeShadowProvideDetachedBeforeArtifacts(int width, VisualThemeMode mode, int density) {
        var series = new ChartSeries(new ChartSeriesId("completed"), "Completed", ChartSeriesKind.Line,
            new[] { new ChartPoint(1, 7), new ChartPoint(2, 19), new ChartPoint(3, 11), new ChartPoint(4, 24) })
            .WithObservationId(1, new ChartObservationId("cycle-2"));
        var theme = VisualTheme.Graphite();
        var chart = Chart.Create().WithSize(width, 260).WithTitle("Service completions").WithSubtitle("Four retained cycles")
            .WithTheme(theme, mode).WithXAxis("Cycle").WithYAxis("Finished").WithDataLabels(false).AddSeries(series);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 260), 24), theme, mode,
            new VisualFrame(showLegend: false, transparentBackground: true));
        var source = chart.Prepare(context); var options = new VisualRenderOptions(scale: density, supersampling: 1);
        var ordinarySvg = source.ToSvg(); var ordinaryPng = source.ToPng(options); var pixels = source.ToRgba(options);
        var zero = source.CaptureLayer(new VisualLayerBlur(0), options);
        var blur = source.CaptureLayer(new VisualLayerBlur(1.5, .75, orderedDither: false), options);
        var shadow = ImageComposition.FromImage(pixels).DrawShadow(ChartColor.FromRgba(0, 0, 0, 112), 2, 3, 1.5, .75);
        var stem = "native-effect-before-" + width + "-" + mode + "-density-" + density;
        // All retained exports precede assertions so a changed default still leaves its actual artifacts.
        NativeEffectBeforeCapture.Write(stem + "-source", ordinarySvg, ordinaryPng, pixels);
        NativeEffectBeforeCapture.Write(stem + "-zero", zero.Visual.ToSvg(), zero.Visual.ToPng(options), zero.ToImage());
        NativeEffectBeforeCapture.Write(stem + "-blur", blur.Visual.ToSvg(), blur.Visual.ToPng(options), blur.ToImage());
        NativeEffectBeforeCapture.Write(stem + "-composition-shadow", null, shadow.ToPng(), shadow.ToImage());
        NativeEffectBeforeCapture.Json(stem + "-facts", new {
            width, height = 260, density, source = SourceRegions(source), zero = Layer(zero), blur = Layer(blur),
            sourcePoints = series.Points.Select(point => new { point.X, point.Y }),
            ids = series.ObservationIds.Select(item => new { sourceIndex = item.Key, id = item.Value.Value })
        });
        Assert.Equal(width * density, pixels.Width); Assert.Equal(260 * density, pixels.Height);
        Assert.Equal(pixels.Pixels, zero.ToImage().Pixels); Assert.Equal(new ChartRect(0, 0, width, 260), zero.Bounds);
        Assert.Same(source, zero.Source); Assert.Same(source, blur.Source);
        Assert.True(blur.Bounds.X < 0 && blur.Bounds.Y < 0 && blur.Bounds.Width > width && blur.Bounds.Height > 260);
        var embedded = Assert.Single(XDocument.Parse(blur.Visual.ToSvg()).Descendants(), node => node.Name.LocalName == "image");
        var href = embedded.Attribute("href")!.Value; Assert.StartsWith("data:image/png;base64,", href);
        Assert.Equal(blur.ToImage().Pixels, RasterImageDecoder.Decode(Convert.FromBase64String(href[(href.IndexOf(',') + 1)..])).Pixels);
        Assert.Equal(ordinarySvg, source.ToSvg()); Assert.Equal(ordinaryPng, source.ToPng(options));
        Assert.Equal("cycle-2", series.ObservationIds[1].Value);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => source.CaptureLayer(new VisualLayerBlur(2), options, cancelled.Token));
        Assert.Equal(ordinaryPng, source.ToPng(options));
    }

    [Theory]
    [InlineData(0)] [InlineData(2)]
    public void ExistingOrdinarySvgTextStrokeProvidesByteBeforeControlForSharedExtremaExtraction(int strokeWidth) {
        var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='160' height='64'>"
            + "<text x='12' y='43' font-family='sans-serif' font-size='28' fill='#2563eb' stroke='#f97316' stroke-width='" + strokeWidth + "'>Ready</text></svg>";
        var result = SvgRasterizer.Rasterize(svg);
        NativeEffectBeforeCapture.Write("effect-extrema-before-text-stroke-" + strokeWidth, svg,
            ImageComposition.FromImage(result.Image).ToPng(), result.Image);
        NativeEffectBeforeCapture.Json("effect-extrema-before-text-stroke-" + strokeWidth + "-diagnostics",
            result.Diagnostics.Select(item => new { item.Code, item.Message, item.ElementName, item.ElementId }));
        Assert.Empty(result.Diagnostics); Assert.Contains(result.Image.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
    }

    private static object SourceRegions(PreparedVisual source) => source.Regions.Select(region => new {
        region.Id, region.Role, region.Label, bounds = new { region.Bounds.X, region.Bounds.Y, region.Bounds.Width, region.Bounds.Height }
    }).ToArray();
    private static object Layer(CapturedVisualLayer layer) => new {
        sourceBounds = new { layer.SourceBounds.X, layer.SourceBounds.Y, layer.SourceBounds.Width, layer.SourceBounds.Height },
        bounds = new { layer.Bounds.X, layer.Bounds.Y, layer.Bounds.Width, layer.Bounds.Height }, layer.PixelsPerUnit
    };
}

internal static class NativeEffectBeforeCapture {
    internal static void Write(string stem, string? svg, byte[] png, RgbaImage pixels) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var file = Path.Combine(directory, stem);
        if (svg != null) File.WriteAllText(file + ".svg", svg);
        File.WriteAllBytes(file + ".png", png); File.WriteAllBytes(file + ".rgba", pixels.Pixels);
    }
    internal static void Json(string stem, object value) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, stem + ".json"), JsonSerializer.Serialize(value));
    }
}
