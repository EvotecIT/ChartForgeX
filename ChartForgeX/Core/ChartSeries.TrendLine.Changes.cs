using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Applies a bounded keyed raw-source transaction and refits the same trend line once.</summary>
    /// <remarks>Every existing raw source observation needs an explicit ID. The shared append/replace/remove and rolling
    /// capacity rules apply; a remaining source that cannot fit the configured model is rejected atomically. Surviving
    /// raw IDs are retained. Derived endpoint labels and paint remain endpoint presentation, never raw-source ordinals.
    /// Hosts still own collection cadence, queues, coalescing and serialized access.</remarks>
    public ChartSeries ApplyTrendLineSourceChanges(IEnumerable<ChartObservationChange<ChartPoint>> changes,
        int? maximumObservations = null, int maximumChanges = 4096, CancellationToken cancellationToken = default) {
        RequireTrendLineSource();
        if (changes == null) throw new ArgumentNullException(nameof(changes));
        ValidateObservationChangeLimits(maximumObservations, maximumChanges);
        var retained = PlanObservationChanges(TrendLineSourcePoints, changes, point => point,
            maximumObservations, maximumChanges, cancellationToken);
        var source = retained.Select(entry => entry.Value).ToArray();
        var fit = ChartTrendLineFit.Create(source, _trendLineOptions, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        InstallTrendLine(source, _trendLineOptions, fit);
        _observationIds.Clear();
        for (var index = 0; index < retained.Count; index++) _observationIds.Add(index, retained[index].Id);
        return this;
    }
}
