using System;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Prepares alternate native drawings against one completed automatic domain envelope.</summary>
    /// <remarks>Each pass uses the ordinary layout and painted-extent owners. Automatic ends can only grow;
    /// authored ends and mutable chart policy remain unchanged. Failed convergence is explicit.</remarks>
    internal (PreparedVisual First, PreparedVisual Second, PreparedVisual? FullFirst, PreparedVisual? FullSecond) PrepareAligned(VisualRenderContext firstContext, VisualRenderContext secondContext,
        ChartCartesianViewport? viewport = null, bool retainFullDrawings = false) {
        // Both native domains are fixed before layout, so a section without an overview needs only its requested drawings.
        if (viewport != null && !retainFullDrawings && VisualCartesianCompiler.SupportsYViewport(this)
            && Options.XAxis.Minimum.HasValue && Options.XAxis.Maximum.HasValue
            && Options.YAxis.Minimum.HasValue && Options.YAxis.Maximum.HasValue
            && (!Series.Any(series => series.ValueAxis == ChartAxisSide.Secondary)
                || ChartCartesianOrientation.ValueAxis(this, ChartAxisSide.Secondary) is { Minimum: not null, Maximum: not null }))
            return (PrepareCore(firstContext, viewport), PrepareCore(secondContext, viewport), null, null);
        // Discover the common envelope before applying a request: a narrow padded edge may exceed the standalone wide domain.
        var scenes = new[] { PrepareCore(firstContext, null), PrepareCore(secondContext, null) };
        if (scenes[0].XProjection == null || scenes[1].XProjection == null) {
            if (viewport != null) throw new NotSupportedException("An aligned Cartesian viewport requires supported projections in both drawings.");
            return (scenes[0], scenes[1], retainFullDrawings ? scenes[0] : null, retainFullDrawings ? scenes[1] : null);
        }
        var domain = ChartCartesianFullDomain.Enclose(scenes);
        const int maximumDomainPasses = 8;
        for (var pass = 0; pass <= maximumDomainPasses; pass++) {
            if (domain.Matches(scenes[0]) && domain.Matches(scenes[1])) {
                if (viewport == null) return (scenes[0], scenes[1], retainFullDrawings ? scenes[0] : null, retainFullDrawings ? scenes[1] : null);
                return (PrepareCore(firstContext, viewport, domain.WithTickStepsFrom(scenes[0])),
                    PrepareCore(secondContext, viewport, domain.WithTickStepsFrom(scenes[1])),
                    retainFullDrawings ? scenes[0] : null, retainFullDrawings ? scenes[1] : null);
            }
            if (pass == maximumDomainPasses)
                throw new InvalidOperationException("Alternate Cartesian drawings could not resolve one stable native full domain.");
            if (!domain.Matches(scenes[0])) scenes[0] = PrepareCore(firstContext, null, domain);
            if (!domain.Matches(scenes[1])) scenes[1] = PrepareCore(secondContext, null, domain);
            domain = ChartCartesianFullDomain.Enclose(scenes);
        }
        throw new InvalidOperationException("Unreachable aligned preparation state.");
    }
}
