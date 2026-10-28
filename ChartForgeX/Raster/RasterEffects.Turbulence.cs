using System;
using System.Threading;
using ChartForgeX.Effects;

namespace ChartForgeX.Raster;

internal static partial class RasterEffects {
    /// <summary>Executes the common source-free preflight once; no canvas, fake SourceGraphic or second quantizer.</summary>
    internal static RasterEffectResult Generate(RasterEffectPreflight preflight, int index, CancellationToken cancellation) {
        if (preflight == null) throw new ArgumentNullException(nameof(preflight));
        cancellation.ThrowIfCancellationRequested();
        var plan = preflight.TakeSourceFree(index);
        var parameters = plan.TurbulenceData ?? throw new InvalidOperationException("Missing typed procedural source.");
        RasterTurbulenceLattice? lattice = parameters.IsConstant ? null : new RasterTurbulenceLattice(parameters.Effect.Seed, cancellation);
        cancellation.ThrowIfCancellationRequested();
        var region = parameters.Region;
        var pixels = new byte[checked(region.Width * region.Height * 4)];
        for (var y = 0; y < region.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < region.Width; x++) {
                var logical = parameters.SamplePoint(region.X + x + .5, region.Y + y + .5);
                var logicalX = logical.X;
                var logicalY = logical.Y;
                var r = SampleTurbulence(parameters, lattice, 0, logicalX, logicalY);
                var g = SampleTurbulence(parameters, lattice, 1, logicalX, logicalY);
                var b = SampleTurbulence(parameters, lattice, 2, logicalX, logicalY);
                var alpha = SampleTurbulence(parameters, lattice, 3, logicalX, logicalY);
                if (parameters.Effect.ColorSpace == VisualFilterColorSpace.LinearRgb) {
                    r = RasterColorTransfer.Encoded(r); g = RasterColorTransfer.Encoded(g); b = RasterColorTransfer.Encoded(b);
                }
                RasterEffectPixels.WriteGenerated(pixels, (y * region.Width + x) * 4, r, g, b, alpha);
            }
        }
        cancellation.ThrowIfCancellationRequested();
        return new RasterEffectResult(new RgbaImage(region.Width, region.Height, pixels), region);
    }

    /// <summary>Logical-coordinate sample shared by the generator and meaningful periodic-boundary contracts.</summary>
    internal static double SampleTurbulence(RasterTurbulenceParameters parameters, RasterTurbulenceLattice? lattice,
        int channel, double logicalX, double logicalY) {
        var fractal = parameters.Effect.Type == VisualTurbulenceType.FractalNoise;
        if (parameters.IsConstant) return fractal ? .5 : 0;
        var x = logicalX * parameters.FrequencyX; var y = logicalY * parameters.FrequencyY;
        double amplitude = 1, sum = 0;
        var periodX = parameters.StitchWidth; var periodY = parameters.StitchHeight;
        var wrapX = parameters.WrapX; var wrapY = parameters.WrapY;
        for (var octave = 0; octave < parameters.EffectiveOctaves; octave++) {
            var value = lattice!.Noise(channel, x, y, parameters.Effect.StitchTiles, periodX, periodY, wrapX, wrapY);
            sum += (fractal ? value : Math.Abs(value)) * amplitude;
            // The last unused octave is not doubled, avoiding overflow after the admitted final sample.
            if (octave + 1 == parameters.EffectiveOctaves) break;
            x *= 2; y *= 2; amplitude *= .5;
            if (parameters.Effect.StitchTiles) {
                periodX = checked(periodX * 2); periodY = checked(periodY * 2);
                wrapX = checked((int)((long)wrapX * 2 - 4096)); wrapY = checked((int)((long)wrapY * 2 - 4096));
            }
        }
        return Math.Max(0, Math.Min(1, fractal ? (sum + 1) * .5 : sum));
    }
}
