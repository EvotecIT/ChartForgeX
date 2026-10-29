using ChartForgeX.Core;
using ChartForgeX.Raster;

namespace ChartForgeX.Tests;

/// <summary>Compares rendered PNGs with committed baselines within a perceptual tolerance and writes a diff image on failure.</summary>
/// <remarks>
/// A pixel differs when its CIE76 colour distance (ΔE*ab, composited over white) exceeds <see cref="MaximumDeltaE"/>.
/// An image fails when more than <see cref="MaximumDifferingRatio"/> of its pixels differ or its size changed.
/// </remarks>
internal static class PixelBaseline {
    internal const double MaximumDeltaE = 2;
    internal const double MaximumDifferingRatio = .001;

    internal sealed record Result(string Name, int Width, int Height, long DifferingPixels, double MaximumObservedDeltaE, string? Failure) {
        internal double DifferingRatio => Width * (long)Height == 0 ? 1 : DifferingPixels / (double)(Width * (long)Height);
    }

    internal static Result Compare(string name, byte[] actualPng, byte[] baselinePng, string? diffDirectory = null) {
        var actual = PngReader.Decode(actualPng); var baseline = PngReader.Decode(baselinePng);
        if (actual.Width != baseline.Width || actual.Height != baseline.Height)
            return new Result(name, actual.Width, actual.Height, actual.Width * (long)actual.Height, double.PositiveInfinity,
                $"size changed from {baseline.Width}x{baseline.Height} to {actual.Width}x{actual.Height}");
        long differing = 0; var worst = 0d;
        var diff = diffDirectory == null ? null : new byte[actual.Pixels.Length];
        for (var offset = 0; offset < actual.Pixels.Length; offset += 4) {
            var delta = DeltaE(actual.Pixels, baseline.Pixels, offset);
            if (delta > worst) worst = delta;
            var over = delta > MaximumDeltaE;
            if (over) differing++;
            if (diff != null) {
                // Differences in red over a faded copy of the baseline.
                var grey = (byte)(255 - (255 - Luma(baseline.Pixels, offset)) / 4);
                diff[offset] = over ? (byte)220 : grey; diff[offset + 1] = over ? (byte)38 : grey; diff[offset + 2] = over ? (byte)38 : grey; diff[offset + 3] = 255;
            }
        }
        var result = new Result(name, actual.Width, actual.Height, differing, worst, null);
        if (result.DifferingRatio <= MaximumDifferingRatio) return result;
        if (diff != null) {
            Directory.CreateDirectory(diffDirectory!);
            File.WriteAllBytes(Path.Combine(diffDirectory!, name + ".diff.png"), RasterImageEncoder.Encode(new RgbaImage(actual.Width, actual.Height, diff), RasterImageFormat.Png));
            File.WriteAllBytes(Path.Combine(diffDirectory!, name + ".actual.png"), actualPng);
        }
        return result with { Failure = $"{differing} of {actual.Width * (long)actual.Height} pixels ({result.DifferingRatio:P3}) exceed ΔE {MaximumDeltaE}; worst ΔE {worst:0.0}" };
    }

    private static byte Luma(byte[] pixels, int offset) {
        var (r, g, b) = OverWhite(pixels, offset);
        return (byte)Math.Round(.2126 * r + .7152 * g + .0722 * b);
    }

    private static (double R, double G, double B) OverWhite(byte[] pixels, int offset) {
        var alpha = pixels[offset + 3] / 255d;
        return (pixels[offset] * alpha + 255 * (1 - alpha), pixels[offset + 1] * alpha + 255 * (1 - alpha), pixels[offset + 2] * alpha + 255 * (1 - alpha));
    }

    private static double DeltaE(byte[] first, byte[] second, int offset) {
        if (first[offset] == second[offset] && first[offset + 1] == second[offset + 1] && first[offset + 2] == second[offset + 2] && first[offset + 3] == second[offset + 3]) return 0;
        var (l1, a1, b1) = Lab(OverWhite(first, offset)); var (l2, a2, b2) = Lab(OverWhite(second, offset));
        return Math.Sqrt((l1 - l2) * (l1 - l2) + (a1 - a2) * (a1 - a2) + (b1 - b2) * (b1 - b2));
    }

    // sRGB (D65) to CIE L*a*b*.
    private static (double L, double A, double B) Lab((double R, double G, double B) rgb) {
        static double Linear(double channel) { var c = channel / 255; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
        double r = Linear(rgb.R), g = Linear(rgb.G), b = Linear(rgb.B);
        var x = (.4124 * r + .3576 * g + .1805 * b) / .95047;
        var y = .2126 * r + .7152 * g + .0722 * b;
        var z = (.0193 * r + .1192 * g + .9505 * b) / 1.08883;
        static double F(double t) => t > .008856 ? Math.Cbrt(t) : 7.787 * t + 16d / 116;
        return (116 * F(y) - 16, 500 * (F(x) - F(y)), 200 * (F(y) - F(z)));
    }
}
