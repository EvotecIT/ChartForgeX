using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterStageDebitContracts {
    [Fact]
    public void OneApplicationChargesThreeRealRgba16ResultsAndTheFinalRgba8CanvasWithoutRefunds() {
        var context = new SvgRasterFilterContext(new SvgRasterFilterOptions(maximumApplications: 1, maximumLiveBytes: 28));
        var frame = new RasterEffectRegion(0, 0, 1, 1); context.BeginStages(0, 0, 1, 1);
        var first = context.ReserveSurface(RasterEffectPlan.SurfaceFlood(frame, new RasterEffectPixel16(65535, 0, 0, 65535), false), null).ExecuteSurface();
        var second = context.ReserveSurface(RasterEffectPlan.SurfaceOffset(frame, 0, 0, false), first).ExecuteSurface();
        var third = context.ReserveSurface(RasterEffectPlan.SurfaceOffset(frame, 0, 0, false), second).ExecuteSurface();
        Assert.Equal(28L, context.Budget.Bytes); Assert.NotSame(first.Pixels, second.Pixels); Assert.NotSame(second.Pixels, third.Pixels);
        Assert.Throws<NotSupportedException>(() => context.ReserveSurface(RasterEffectPlan.SurfaceOffset(frame, 0, 0, false), third));
        Assert.Equal(28L, context.Budget.Bytes); // Clearing a slot or collecting an earlier array is not admission for a fourth allocation.
        Assert.Throws<NotSupportedException>(() => context.BeginStages(0, 0, 1, 1));
    }

    [Fact]
    public void TheSameBorrowedOperandIsAddedBackOnceWhileTheSharedCumulativeOwnerRetainsPriorArrays() {
        var context = new SvgRasterFilterContext(new SvgRasterFilterOptions(maximumLiveBytes: 20));
        var frame = new RasterEffectRegion(0, 0, 1, 1); context.BeginStages(0, 0, 1, 1);
        var source = context.ReserveSurface(RasterEffectPlan.SurfaceFlood(frame, new RasterEffectPixel16(32768, 0, 0, 32768), false), null).ExecuteSurface();
        var permit = context.ReserveSurface(RasterEffectPlan.SurfaceComposite(frame, source, VisualCompositeOperator.SourceOver, false), source);
        Assert.Equal(20L, context.Budget.Bytes);
        var result = permit.ExecuteSurface(); Assert.Equal(new ushort[] { 49152, 0, 0, 49152 }, result.Pixels);
        Assert.Throws<InvalidOperationException>(() => permit.ExecuteSurface()); Assert.Equal(20L, context.Budget.Bytes);
    }

    [Fact]
    public void CancellationBeforeAdmissionLeavesTheCanonicalCountersUntouched() {
        var context = new SvgRasterFilterContext(new SvgRasterFilterOptions(cancellationToken: new CancellationToken(true)));
        Assert.Throws<OperationCanceledException>(() => context.BeginStages(0, 0, 1, 1));
        Assert.Equal(0L, context.Budget.Bytes); Assert.Equal(0L, context.Budget.WorkSamples);
    }
}
