using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Applies a bounded keyed bubble transaction, retaining surviving identities, independent color/size values and presentation.</summary>
    /// <param name="changes">Uniquely keyed append, replace and remove operations.</param>
    /// <param name="maximumObservations">Optional positive rolling capacity in authored source order.</param>
    /// <param name="maximumChanges">Positive maximum batch length; an overlong sequence is rejected after at most this limit plus one entries.</param>
    /// <param name="cancellationToken">Cancellation checked during capture and immediately before installation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ApplyBubbleChanges(IEnumerable<ChartObservationChange<ChartBubble>> changes, int? maximumObservations = null,
        int maximumChanges = 4096, CancellationToken cancellationToken = default) {
        RequireObservationKind(ChartSeriesKind.Bubble);
        ApplyTypedObservationChanges(ref _bubbles, changes, ValidateBubble, maximumObservations, maximumChanges, cancellationToken);
        return this;
    }

    /// <summary>Applies a bounded keyed financial transaction, retaining surviving prices, identities and per-observation styles.</summary>
    /// <param name="changes">Uniquely keyed append, replace and remove operations.</param>
    /// <param name="maximumObservations">Optional positive rolling capacity in authored source order.</param>
    /// <param name="maximumChanges">Positive maximum batch length.</param>
    /// <param name="cancellationToken">Cancellation checked during capture and immediately before installation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ApplyFinancialChanges(IEnumerable<ChartObservationChange<ChartCandlestick>> changes, int? maximumObservations = null,
        int maximumChanges = 4096, CancellationToken cancellationToken = default) {
        RequireObservationKind(ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc);
        ApplyTypedObservationChanges(ref _financialValues, changes, ValidateFinancialValue, maximumObservations, maximumChanges, cancellationToken);
        return this;
    }

    /// <summary>Applies a bounded keyed RangeBand or RangeArea transaction without losing lower/upper values, breaks or surviving presentation.</summary>
    /// <param name="changes">Uniquely keyed append, replace and remove operations.</param>
    /// <param name="maximumObservations">Optional positive rolling capacity in authored source order.</param>
    /// <param name="maximumChanges">Positive maximum batch length.</param>
    /// <param name="cancellationToken">Cancellation checked during capture and immediately before installation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ApplyRangeChanges(IEnumerable<ChartObservationChange<ChartRangeBand>> changes, int? maximumObservations = null,
        int maximumChanges = 4096, CancellationToken cancellationToken = default) {
        RequireObservationKind(ChartSeriesKind.RangeBand, ChartSeriesKind.RangeArea);
        ApplyTypedObservationChanges(ref _ranges, changes, ValidateRange, maximumObservations, maximumChanges, cancellationToken);
        return this;
    }

    /// <summary>Applies a bounded keyed difference-area transaction while retaining both authored values, breaks and surviving presentation.</summary>
    /// <param name="changes">Uniquely keyed append, replace and remove operations.</param>
    /// <param name="maximumObservations">Optional positive rolling capacity in authored source order.</param>
    /// <param name="maximumChanges">Positive maximum batch length.</param>
    /// <param name="cancellationToken">Cancellation checked during capture and immediately before installation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ApplyDifferenceChanges(IEnumerable<ChartObservationChange<ChartDifferencePoint>> changes, int? maximumObservations = null,
        int maximumChanges = 4096, CancellationToken cancellationToken = default) {
        RequireObservationKind(ChartSeriesKind.DifferenceArea);
        RequireDirectDifferencePoints();
        ApplyTypedObservationChanges(ref _differencePoints, changes, ValidateDifferencePoint, maximumObservations, maximumChanges, cancellationToken);
        return this;
    }

    private void ApplyTypedObservationChanges<T>(ref IReadOnlyList<T> target, IEnumerable<ChartObservationChange<T>> changes,
        Func<T, T> validate, int? maximumObservations, int maximumChanges, CancellationToken cancellationToken) where T : struct {
        if (changes == null) throw new ArgumentNullException(nameof(changes));
        ValidateObservationChangeLimits(maximumObservations, maximumChanges);
        var retained = PlanObservationChanges(target, changes, validate, maximumObservations, maximumChanges, cancellationToken);
        var snapshot = Array.AsReadOnly(retained.Select(entry => entry.Value).ToArray());
        var presentation = CaptureObservationChangePresentation(retained);
        cancellationToken.ThrowIfCancellationRequested();
        target = snapshot;
        InstallObservationChangePresentation(retained, presentation);
    }
}
