using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Raster;

internal sealed partial class RasterEffectPlan {
    internal RasterTurbulenceParameters? TurbulenceData { get; }
    private RasterEffectPlan(RasterTurbulenceParameters procedural) {
        TurbulenceData = procedural;
        SourceRegion = OutputRegion = ResultRegion = MorphologyRegion = procedural.Region;
        HorizontalRadii = VerticalRadii = Array.AsReadOnly(Array.Empty<int>());
        WorkSamples = checked(procedural.SetupWorkSamples + Pixels *
            (16L + 4L * procedural.EffectiveOctaves * 96L + (procedural.SampleTransform.HasValue ? 16L : 0)));
    }
    internal static RasterEffectPlan CreateTurbulence(VisualTurbulence effect, VisualSize size, int density,
        CancellationToken cancellation) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        _ = new VisualRenderOptions(scale: density);
        var width = RasterEffectGrid.Coordinate(size.Width, density, nameof(size));
        var height = RasterEffectGrid.Coordinate(size.Height, density, nameof(size));
        return CreateTurbulence(effect, new ChartRect(0, 0, size.Width, size.Height),
            new RasterEffectRegion(0, 0, width, height), density, cancellation);
    }
    internal static RasterEffectPlan CreateTurbulence(RasterTurbulenceParameters parameters) => new(parameters);
    internal static RasterEffectPlan CreateTurbulence(VisualTurbulence effect, ChartRect tile,
        RasterEffectRegion localOutput, int density, CancellationToken cancellation,
        double? sampleStepX = null, double? sampleStepY = null, SvgRasterMatrix? sampleTransform = null) =>
        new(new RasterTurbulenceParameters(effect, tile, localOutput, density, cancellation,
            sampleStepX: sampleStepX, sampleStepY: sampleStepY, sampleTransform: sampleTransform));
}

/// <summary>Scalar setup admission and exact coordinate/stitch law; arrays allocate only after common preflight.</summary>
internal sealed class RasterTurbulenceParameters {
    internal RasterTurbulenceParameters(VisualTurbulence effect, ChartRect tile, RasterEffectRegion region,
        int density, CancellationToken cancellation, ChartPoint? sampleOrigin = null,
        double? sampleStepX = null, double? sampleStepY = null, SvgRasterMatrix? sampleTransform = null) {
        if (sampleTransform.HasValue && (sampleStepX.HasValue || sampleStepY.HasValue))
            throw new ArgumentException("An affine map and independent axis steps cannot be applied twice.");
        SampleTransform = sampleTransform;
        if (density <= 0) throw new ArgumentOutOfRangeException(nameof(density));
        Effect = effect; Tile = tile; Region = region; Density = density;
        SampleOrigin = sampleOrigin ?? new ChartPoint(tile.X, tile.Y);
        SampleStepX = ValidateStep(sampleStepX); SampleStepY = ValidateStep(sampleStepY);
        EffectiveOctaves = Math.Min(effect.NumberOfOctaves, 9);
        var fx = effect.BaseFrequencyX; var fy = effect.BaseFrequencyY;
        if (EffectiveOctaves == 0) { FrequencyX = FrequencyY = 0; return; }
        ValidateCoordinateRange(tile.X, tile.Right, fx, EffectiveOctaves);
        ValidateCoordinateRange(tile.Y, tile.Bottom, fy, EffectiveOctaves);
        FrequencyX = effect.StitchTiles ? StitchFrequency(fx, tile.Width) : fx;
        FrequencyY = effect.StitchTiles ? StitchFrequency(fy, tile.Height) : fy;
        // Stitch adjustment can choose the next integral period, so check the actual selected frequencies too.
        ValidateCoordinateRange(tile.X, tile.Right, FrequencyX, EffectiveOctaves);
        ValidateCoordinateRange(tile.Y, tile.Bottom, FrequencyY, EffectiveOctaves);
        StitchWidth = CheckedPeriod(tile.Width, FrequencyX);
        StitchHeight = CheckedPeriod(tile.Height, FrequencyY);
        WrapX = checked((int)(tile.X * FrequencyX + 4096) + StitchWidth);
        WrapY = checked((int)(tile.Y * FrequencyY + 4096) + StitchHeight);
        if (effect.StitchTiles) {
            var multiplier = 1L << (EffectiveOctaves - 1);
            if (StitchWidth * multiplier > int.MaxValue - 4098 || StitchHeight * multiplier > int.MaxValue - 4098 ||
                ((long)WrapX - 4096) * multiplier + 4096 > int.MaxValue - 2 ||
                ((long)WrapY - 4096) * multiplier + 4096 > int.MaxValue - 2)
                throw new ArgumentOutOfRangeException(nameof(effect), "Stitch wrapping exceeds the admitted lattice coordinate range.");
        }
        if (sampleTransform is { } transform) {
            var first = transform.Transform(new ChartPoint(region.X + .5, region.Y + .5));
            var second = transform.Transform(new ChartPoint((double)region.X + region.Width - .5, region.Y + .5));
            var third = transform.Transform(new ChartPoint(region.X + .5, (double)region.Y + region.Height - .5));
            var fourth = transform.Transform(new ChartPoint((double)region.X + region.Width - .5, (double)region.Y + region.Height - .5));
            ValidateCoordinateRange(Math.Min(Math.Min(first.X, second.X), Math.Min(third.X, fourth.X)),
                Math.Max(Math.Max(first.X, second.X), Math.Max(third.X, fourth.X)), FrequencyX, EffectiveOctaves);
            ValidateCoordinateRange(Math.Min(Math.Min(first.Y, second.Y), Math.Min(third.Y, fourth.Y)),
                Math.Max(Math.Max(first.Y, second.Y), Math.Max(third.Y, fourth.Y)), FrequencyY, EffectiveOctaves);
        } else {
            ValidateCoordinateRange(SampleOrigin.X, SampleOrigin.X + SampleSpan(region.Width, SampleStepX, density), FrequencyX, EffectiveOctaves);
            ValidateCoordinateRange(SampleOrigin.Y, SampleOrigin.Y + SampleSpan(region.Height, SampleStepY, density), FrequencyY, EffectiveOctaves);
        }
        if (!IsConstant) SetupWorkSamples = RasterTurbulenceLattice.SetupWork(effect.Seed, cancellation);
    }
    internal VisualTurbulence Effect { get; }
    internal ChartRect Tile { get; }
    internal RasterEffectRegion Region { get; }
    internal int Density { get; }
    internal ChartPoint SampleOrigin { get; }
    internal double? SampleStepX { get; }
    internal double? SampleStepY { get; }
    internal SvgRasterMatrix? SampleTransform { get; }
    internal ChartPoint SamplePoint(double x, double y) => SampleTransform is { } transform
        ? transform.Transform(new ChartPoint(x, y)) : new ChartPoint(SampleX(x), SampleY(y));
    // Null keeps the incumbent density division exactly, rather than changing it to reciprocal multiplication.
    internal double SampleX(double deviceCell) => SampleOrigin.X + (SampleStepX.HasValue ? deviceCell * SampleStepX.Value : deviceCell / Density);
    internal double SampleY(double deviceCell) => SampleOrigin.Y + (SampleStepY.HasValue ? deviceCell * SampleStepY.Value : deviceCell / Density);
    internal int EffectiveOctaves { get; }
    internal double FrequencyX { get; }
    internal double FrequencyY { get; }
    internal int StitchWidth { get; }
    internal int StitchHeight { get; }
    internal int WrapX { get; }
    internal int WrapY { get; }
    internal long SetupWorkSamples { get; }
    internal bool IsConstant => EffectiveOctaves == 0 || FrequencyX == 0 && FrequencyY == 0;
    internal long SetupArrayBytes => IsConstant ? 0 : 512L * sizeof(int) + 4L * 256 * 2 * sizeof(double);

    private static double? ValidateStep(double? step) {
        if (step.HasValue && (double.IsNaN(step.Value) || double.IsInfinity(step.Value) || step.Value <= 0))
            throw new ArgumentOutOfRangeException(nameof(step));
        return step;
    }
    private static double SampleSpan(int cells, double? step, int density) =>
        step.HasValue ? cells * step.Value : (double)cells / density;

    private static void ValidateCoordinateRange(double first, double last, double frequency, int octaves) {
        var multiplier = 1 << (octaves - 1);
        var low = first * frequency * multiplier + 4096;
        var high = last * frequency * multiplier + 4096;
        // These are the actual lattice conversion limits, not an arbitrary frequency cap.
        if (double.IsInfinity(low) || double.IsNaN(low) || double.IsInfinity(high) || double.IsNaN(high)
            || low < 0 || high > int.MaxValue - 2)
            throw new ArgumentOutOfRangeException(nameof(frequency), "Selected output coordinates exceed the finite native lattice range.");
    }
    private static double StitchFrequency(double frequency, double extent) {
        if (frequency == 0) return 0;
        var low = Math.Floor(extent * frequency) / extent;
        var high = Math.Ceiling(extent * frequency) / extent;
        return low > 0 && frequency / low < high / frequency ? low : high;
    }
    private static int CheckedPeriod(double extent, double frequency) {
        var period = extent * frequency + .5;
        if (period > int.MaxValue - 4098) throw new ArgumentOutOfRangeException(nameof(frequency));
        return (int)period;
    }
}
