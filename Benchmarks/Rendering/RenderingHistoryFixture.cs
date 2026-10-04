using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;

/// <summary>Fixed public rendering work using an original portable font rather than installed font discovery.</summary>
public static class RenderingHistoryFixture {
    private static readonly TextStyle Auto = TextStyle.Create(12, ChartColor.Black);
    private static readonly TextStyle Full = TextStyle.Create(10, ChartColor.Black);
    private const string Label = "Report HINO 12";

    /// <summary>Loads the fixture and warms font resolution outside measured operations.</summary>
    public static void Initialize(string fontPath) {
        FontRegistry.Register("CFX History", fontPath, 400);
        Auto.Font = FontSpec.FromFamily("CFX History"); Full.Font = FontSpec.FromFamily("CFX History");
        Full.Hinting = TextHinting.Full;
    }

    /// <summary>Renders twelve labels through composition or imported SVG. Optional delay is used only by gate calibration proof.</summary>
    public static RgbaImage Render(string mode, int proofDelayMilliseconds) {
        RgbaImage image;
        if (mode == "Svg") {
            var labels = string.Concat(Enumerable.Range(0, 12).Select(i => $"<text x='10' y='{15 + i * 13}' font-family='CFX History' font-size='12'>{Label}</text>"));
            image = SvgRasterizer.ToImage($"<svg xmlns='http://www.w3.org/2000/svg' width='500' height='180'>{labels}</svg>");
        } else {
            var style = mode == "Full" ? Full : mode == "Auto" ? Auto : throw new ArgumentOutOfRangeException(nameof(mode));
            var surface = ImageComposition.CreateTransparent(500, 180);
            for (var i = 0; i < 12; i++) surface.DrawText(10, 2 + i * 13, 450, Label, style, TextWrapMode.NoWrap, null, TextTrimming.None);
            image = surface.ToImage();
        }
        if (proofDelayMilliseconds > 0) Thread.Sleep(proofDelayMilliseconds);
        return image;
    }

    /// <summary>Checks dimensions and visible ink in every intended row outside timing.</summary>
    public static void Validate(RgbaImage image) {
        if (image.Width != 500 || image.Height != 180 || image.Pixels.Length != 360000) throw new InvalidOperationException("Rendering dimensions differ.");
        for (var row = 0; row < 12; row++) {
            var visible = false;
            // Probe the middle of each row so a neighbouring label cannot satisfy a missing row.
            for (var y = row * 13 + 7; y < row * 13 + 12; y++)
                for (var x = 10; x < 200; x++) visible |= image.Pixels[(y * image.Width + x) * 4 + 3] > 0;
            if (!visible) throw new InvalidOperationException($"Label row {row} is blank.");
        }
    }
}
