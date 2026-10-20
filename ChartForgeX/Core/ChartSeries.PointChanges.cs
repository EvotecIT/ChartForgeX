using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Applies a bounded, keyed numeric transaction while retaining surviving observations' identities and presentation.</summary>
    /// <remarks>
    /// Supported by the line, step-line, area, step-area, stacked-area, scatter, bar, horizontal-bar, radial-bar,
    /// radial-column and lollipop families. Derived and typed series use their own source APIs.
    /// Every existing observation must have an explicit ID. Each batch may name an ID once; append requires a new ID,
    /// and replace/remove require an existing ID. Enumeration, key, capacity and cancellation failures leave installed data unchanged.
    /// The optional rolling limit keeps the last observations in source order, rather than sorting by X.
    /// Collection cadence, asynchronous queues and retry/coalescing policy remain with the host. An oversized batch
    /// is rejected after at most <paramref name="maximumChanges"/> plus one entries, without queuing work.
    /// Like other chart authoring APIs, this method requires the caller to serialize access to its model.
    /// </remarks>
    /// <param name="changes">The finite sequence of uniquely keyed append, replace and remove operations.</param>
    /// <param name="maximumObservations">An optional positive rolling capacity. Oldest surviving observations are removed first.</param>
    /// <param name="maximumChanges">The positive maximum number of operations accepted in this call.</param>
    /// <param name="cancellationToken">Cancellation checked while capturing operations and before installation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ApplyPointChanges(IEnumerable<ChartPointChange> changes, int? maximumObservations = null,
        int maximumChanges = 4096, CancellationToken cancellationToken = default) {
        if (changes == null) throw new ArgumentNullException(nameof(changes));
        ValidateObservationChangeLimits(maximumObservations, maximumChanges);
        RequireMutableNumericObservations();
        if (!ChartSeriesKindTraits.SupportsPointSeriesMapping(Kind))
            throw new NotSupportedException("Keyed point changes require a point-mapped numeric series family.");
        var retained = PlanObservationChanges(_points, TypedPointChanges(changes), point => point,
            maximumObservations, maximumChanges, cancellationToken);
        var points = retained.Select(entry => entry.Value).ToArray();
        var presentation = CaptureObservationChangePresentation(retained);
        cancellationToken.ThrowIfCancellationRequested();

        _points.Clear(); _points.AddRange(points);
        InstallObservationChangePresentation(retained, presentation);
        return this;
    }

    private static IEnumerable<ChartObservationChange<ChartPoint>> TypedPointChanges(IEnumerable<ChartPointChange> changes) {
        foreach (var change in changes) {
            _ = change.Id.Value;
            yield return change.Kind switch {
                ChartPointChangeKind.Append => ChartObservationChange<ChartPoint>.Append(change.Id, change.Point!.Value),
                ChartPointChangeKind.Replace => ChartObservationChange<ChartPoint>.Replace(change.Id, change.Point!.Value),
                _ => ChartObservationChange<ChartPoint>.Remove(change.Id)
            };
        }
    }

    private static void InstallPointOverrides<T>(List<T> target, T[] values) { target.Clear(); target.AddRange(values); }

}
