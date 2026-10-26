using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    private SvgRasterFilterPlan(RasterEffectRegion filter, VisualTurbulence turbulence, ChartRect tile,
        SvgRasterMatrix localSampling, ChartPoint[] contour, CancellationToken cancellation) {
        FilterRegion = CaptureRegion = filter;
        ProceduralClipContour = Array.AsReadOnly((ChartPoint[])contour.Clone());
        _effect = RasterEffectPlan.CreateTurbulence(turbulence, tile,
            new RasterEffectRegion(0, 0, filter.Width, filter.Height), 1, cancellation, sampleTransform: localSampling);
    }

    /// <summary>The existing inverse matrix maps actual device cell centers to one logical procedural field.</summary>
    private static SvgRasterFilterPlan CreateAffineTurbulence(VisualTurbulence effect, ChartRect tile,
        SvgRasterMatrix forward, SvgRasterMatrix inverse, CancellationToken cancellation) {
        var contour = new[] {
            forward.Transform(new ChartPoint(tile.Left, tile.Top)),
            forward.Transform(new ChartPoint(tile.Right, tile.Top)),
            forward.Transform(new ChartPoint(tile.Right, tile.Bottom)),
            forward.Transform(new ChartPoint(tile.Left, tile.Bottom))
        };
        var left = double.PositiveInfinity; var top = double.PositiveInfinity;
        var right = double.NegativeInfinity; var bottom = double.NegativeInfinity;
        foreach (var point in contour) {
            if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentOutOfRangeException(nameof(forward));
            left = Math.Min(left, point.X); right = Math.Max(right, point.X);
            top = Math.Min(top, point.Y); bottom = Math.Max(bottom, point.Y);
        }
        // This outward integer box owns storage only. The original transformed quad owns coverage.
        if (!Pixel(Math.Floor(left), out var x) || !Pixel(Math.Floor(top), out var y) ||
            !Pixel(Math.Ceiling(right), out var endX) || !Pixel(Math.Ceiling(bottom), out var endY))
            throw new ArgumentOutOfRangeException(nameof(forward));
        var region = new RasterEffectRegion(x, y, checked(endX - x), checked(endY - y));
        for (var index = 0; index < contour.Length; index++)
            contour[index] = new ChartPoint(contour[index].X - x, contour[index].Y - y);
        var sampling = inverse.Multiply(SvgRasterMatrix.Translate(x, y));
        if (!FiniteMatrix(sampling)) throw new ArgumentOutOfRangeException(nameof(forward));
        return new SvgRasterFilterPlan(region, effect, tile, sampling, contour, cancellation);
    }

    private static bool FiniteMatrix(SvgRasterMatrix matrix) => Finite(matrix.A) && Finite(matrix.B) &&
        Finite(matrix.C) && Finite(matrix.D) && Finite(matrix.E) && Finite(matrix.F);
}
