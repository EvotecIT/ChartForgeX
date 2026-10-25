using System;
using System.IO;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Ordinary Core + Visuals consumer: a complete native chart and its text enter a report inset as one blurred unit.</summary>
internal static class CapturedNativeLayerExamples {
    internal static void Write(string output) {
        foreach (var dark in new[] { false, true }) foreach (var width in new[] { 400, 800 }) {
            var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
            var theme = VisualTheme.Report().WithFontFamily(ExampleTypography.FontFamily);
            var childWidth = width - 72; var childHeight = 210;
            var chart = Chart.Create().WithTitle("Completed work").WithSize(childWidth, childHeight).WithTheme(theme, mode);
            chart.AddBar("Completed", new[] { new ChartPoint(1, 12), new ChartPoint(2, 28), new ChartPoint(3, 19) });
            var source = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(childWidth, childHeight), 24),
                theme, mode, new VisualFrame(showLegend: false, transparentBackground: true), FontSpec.FromFamily(ExampleTypography.FontFamily)));
            var layer = source.CaptureLayer(new VisualLayerBlur(1.5), new VisualRenderOptions(supersampling: 2));
            var background = ChartColor.FromHex(dark ? "#111827" : "#F8FAFC");
            var color = ChartColor.FromHex(dark ? "#F8FAFC" : "#0F172A");
            var composition = ImageComposition.Create(width, 320, background).DrawLayer(layer, 36, 32)
                .DrawText(32, 272, width - 64, "One captured chart and caption", 16, color);
            var canvas = VisualCanvas.Create(width, 320).WithTitle("Captured native report inset").WithPngOutputScale(1)
                .WithBackdrop(VisualCanvasBackdropStyle.Transparent).AddImage(composition.ToImage(), 0, 0, width, 320);
            var name = "captured-native-layer-" + (dark ? "dark-" : "light-") + width;
            File.WriteAllText(Path.Combine(output, name + ".layer.svg"), layer.Visual.ToSvg());
            File.WriteAllBytes(Path.Combine(output, name + ".layer.png"), PngWriter.WriteRgba(layer.ToImage()));
            canvas.SaveSvg(Path.Combine(output, name + ".svg")); canvas.SavePng(Path.Combine(output, name + ".png"));
            canvas.SaveHtml(Path.Combine(output, name + ".html"));
        }
    }
}
