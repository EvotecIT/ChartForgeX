using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Prepares alternate native drawings against one completed automatic domain envelope.</summary>
    /// <remarks>Each pass uses the ordinary layout and painted-extent owners. Automatic ends can only grow;
    /// authored ends and mutable chart policy remain unchanged. Failed convergence is explicit.</remarks>
    internal (PreparedVisual First, PreparedVisual Second, PreparedVisual FullFirst, PreparedVisual FullSecond) PrepareAligned(VisualRenderContext firstContext, VisualRenderContext secondContext,
        ChartCartesianViewport? viewport = null) {
        var scenes = new[] { PrepareCore(firstContext, null), PrepareCore(secondContext, null) };
        if (scenes[0].XProjection == null || scenes[1].XProjection == null) {
            if (viewport != null) throw new NotSupportedException("An aligned Cartesian viewport requires supported projections in both drawings.");
            return (scenes[0], scenes[1], scenes[0], scenes[1]);
        }
        var domain = ChartCartesianFullDomain.Enclose(scenes);
        const int maximumDomainPasses = 8;
        for (var pass = 0; pass <= maximumDomainPasses; pass++) {
            if (domain.Matches(scenes[0]) && domain.Matches(scenes[1])) {
                if (viewport == null) return (scenes[0], scenes[1], scenes[0], scenes[1]);
                return (PrepareCore(firstContext, viewport, domain), PrepareCore(secondContext, viewport, domain), scenes[0], scenes[1]);
            }
            if (pass == maximumDomainPasses)
                throw new InvalidOperationException("Alternate Cartesian drawings could not resolve one stable native full domain.");
            scenes[0] = PrepareCore(firstContext, null, domain); scenes[1] = PrepareCore(secondContext, null, domain);
            domain = ChartCartesianFullDomain.Enclose(scenes);
        }
        throw new InvalidOperationException("Unreachable aligned preparation state.");
    }
}
