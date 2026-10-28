using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartDifferenceAlignment? _differenceAlignment;
    /// <summary>Gets complete raw independent sources and matched provenance, or null for ordinary authored tuples.</summary>
    public ChartDifferenceAlignment? DifferenceAlignment => _differenceAlignment;

    /// <summary>Creates a comparison from an immutable exact-X alignment, without assigning fabricated destination observation IDs.</summary>
    /// <param name="id">The explicit destination series identity.</param>
    /// <param name="name">The destination display name.</param>
    /// <param name="alignment">Complete immutable source captures and matched pairs.</param>
    /// <param name="color">Optional existing boundary/fill paint.</param>
    /// <returns>An independently configurable derived difference series.</returns>
    public static ChartSeries CreateAlignedDifferenceArea(ChartSeriesId id, string name, ChartDifferenceAlignment alignment, ChartColor? color = null) {
        if (alignment == null) throw new ArgumentNullException(nameof(alignment));
        var series = new ChartSeries(id, name, ChartSeriesKind.DifferenceArea, Array.Empty<ChartPoint>()) { Color = color };
        return series.ReplaceDifferenceAlignment(alignment);
    }

    /// <summary>Replaces captured independent sources atomically and clears destination observation presentation/IDs under the ordinary typed replacement policy.</summary>
    /// <param name="alignment">An already detached alignment. Re-align changed original sources before calling this method.</param>
    /// <returns>The current series.</returns>
    /// <remarks>Retains paint options; replaces readout names and boundary policies from the captured sources. Empty raw names use First/Second readouts while the exact names remain in the alignment.</remarks>
    public ChartSeries ReplaceDifferenceAlignment(ChartDifferenceAlignment alignment) {
        RequireObservationKind(ChartSeriesKind.DifferenceArea);
        if (alignment == null) throw new ArgumentNullException(nameof(alignment));
        var options = DifferenceArea.Clone();
        options.FirstName = string.IsNullOrWhiteSpace(alignment.FirstName) ? "First" : alignment.FirstName;
        options.SecondName = string.IsNullOrWhiteSpace(alignment.SecondName) ? "Second" : alignment.SecondName;
        ReplaceDifferencePoints(alignment.Points);
        if (alignment.FirstBoundary.Interpolation == alignment.SecondBoundary.Interpolation
            && alignment.FirstBoundary.StepPosition == alignment.SecondBoundary.StepPosition) {
            WithInterpolation(alignment.FirstBoundary.Interpolation, alignment.FirstBoundary.StepPosition);
            FirstBoundaryInterpolation = null; SecondBoundaryInterpolation = null;
        } else WithBoundaryInterpolation(alignment.FirstBoundary, alignment.SecondBoundary);
        DifferenceArea = options; _differenceAlignment = alignment;
        return this;
    }

    internal void RequireDifferenceAlignmentSource() {
        if (Kind is not (ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.Area or ChartSeriesKind.StepArea) || HasSourceIndexedObservationIds)
            throw new NotSupportedException("Independent difference alignment accepts ordinary owned Line, StepLine, Area or StepArea sources; derived/decimated sources must be rebuilt from their complete inputs.");
    }

    private void RequireDirectDifferencePoints() {
        if (_differenceAlignment != null) throw new NotSupportedException("Aligned difference observations are derived. Re-align their original sources and replace the alignment, or explicitly replace ordinary tuples to leave derived mode.");
    }

    /// <summary>Adds only matched source references to a native observation; complete snapshots stay on the immutable alignment owner.</summary>
    internal void AddDifferenceSourceFacts(int item, Dictionary<string, string> facts) {
        var alignment = _differenceAlignment; if (alignment == null) return;
        var first = alignment.FirstSourceIndices[item]; var second = alignment.SecondSourceIndices[item];
        facts["data-cfx-first-source-series-id"] = alignment.FirstSeriesId.Value;
        facts["data-cfx-second-source-series-id"] = alignment.SecondSeriesId.Value;
        facts["data-cfx-first-source-index"] = first.ToString(System.Globalization.CultureInfo.InvariantCulture);
        facts["data-cfx-second-source-index"] = second.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (alignment.FirstObservationIds.TryGetValue(first, out var firstId)) facts["data-cfx-first-source-observation-id"] = firstId.Value;
        if (alignment.SecondObservationIds.TryGetValue(second, out var secondId)) facts["data-cfx-second-source-observation-id"] = secondId.Value;
    }
}
