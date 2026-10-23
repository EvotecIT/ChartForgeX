using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;

/// <summary>Complete opt-in baked blur/shadow example; both exporters consume one captured image rather than SVG filters.</summary>
internal static class CompositionEffectsExamples {
    internal static void Write(string output) {
        foreach (var dark in new[] { false, true }) foreach (var width in new[] { 400, 800 }) {
            var height = width == 400 ? 280 : 360;
            var background = ChartColor.FromHex(dark ? "#111827" : "#F1F5F9");
            var foreground = ChartColor.FromHex(dark ? "#F8FAFC" : "#0F172A");
            var accent = ChartColor.FromHex(dark ? "#38BDF8" : "#0369A1");
            // Both the mark and lower text are softened together; the foreground panel keeps its own sharp text.
            var glow = ImageComposition.CreateTransparent(width, height)
                .FillRoundedRectangle(width * .2, height * .2, width * .55, height * .55, 28, accent.WithAlpha(96))
                .DrawText(40, height - 54, width - 80, "CFX", width == 400 ? 28 : 36, accent.WithAlpha(224), emphasized: true).Blur(12);
            var panel = ImageComposition.CreateTransparent(width - 64, height - 56)
                .FillRoundedRectangle(12, 8, width - 96, height - 96, 14, ChartColor.FromHex(dark ? "#1E293B" : "#FFFFFF"))
                .DrawText(32, 28, width - 144, "Service overview", width == 400 ? 20 : 26, foreground, emphasized: true)
                .DrawText(32, 76, width - 144, "Ready for the next request", width == 400 ? 14 : 18, accent)
                .DrawShadow(ChartColor.FromRgba(0, 0, 0, 144), 0, 8, 4);
            var composition = ImageComposition.Create(width, height, background).DrawImage(glow.ToImage(), 0, 0, width, height)
                .DrawImage(panel.ToImage(), 32, 28, panel.Width, panel.Height);
            var image = composition.ToImage();
            var canvas = VisualCanvas.Create(width, height).WithTitle("Blurred mark and text with panel shadow")
                .WithPngOutputScale(1).WithBackdrop(VisualCanvasBackdropStyle.Transparent).AddImage(image, 0, 0, width, height);
            var name = "composition-effects-" + (dark ? "dark-" : "light-") + width;
            composition.Save(Path.Combine(output, name + ".source.png"));
            canvas.SaveSvg(Path.Combine(output, name + ".svg")); canvas.SavePng(Path.Combine(output, name + ".png"));
            canvas.SaveHtml(Path.Combine(output, name + ".html"));
        }
    }
}
