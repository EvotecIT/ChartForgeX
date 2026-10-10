using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private readonly Dictionary<string, ChartFlowStyle> _flowStyles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChartSeriesState> _flowStates = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, ChartFlowStyle>? _flowStylesView;
    private IReadOnlyDictionary<string, ChartSeriesState>? _flowStatesView;

    /// <summary>Gets a read-only view of paint overrides keyed by case-sensitive authored flow ID, scoped to this Sankey or Chord series.</summary>
    public IReadOnlyDictionary<string, ChartFlowStyle> FlowStyles => _flowStylesView ??= new ReadOnlyDictionary<string, ChartFlowStyle>(_flowStyles);

    /// <summary>Gets a read-only view of semantic overrides keyed by case-sensitive authored flow ID, scoped to this Sankey or Chord series.</summary>
    public IReadOnlyDictionary<string, ChartSeriesState> FlowStates => _flowStatesView ??= new ReadOnlyDictionary<string, ChartSeriesState>(_flowStates);

    /// <summary>Assigns paint to an existing authored Sankey or Chord flow. Null removes the override and restores inheritance.</summary>
    /// <param name="id">The exact authored flow ID, including the distinct ID of a parallel flow.</param>
    /// <param name="style">Immutable paint overrides, or null to remove them.</param>
    /// <returns>This mutable series builder.</returns>
    public ChartSeries WithFlowStyle(string id, ChartFlowStyle? style) {
        RequireFlow(id);
        if (style.HasValue) _flowStyles[id] = style.Value;
        else _flowStyles.Remove(id);
        return this;
    }

    /// <summary>Assigns semantic state to an existing authored Sankey or Chord flow. Null restores source-node/series inheritance; None explicitly suppresses it.</summary>
    /// <param name="id">The exact authored flow ID.</param>
    /// <param name="state">An explicit semantic state, or null to restore inheritance. Explicit colors still take precedence over semantic colors.</param>
    /// <returns>This mutable series builder.</returns>
    public ChartSeries WithFlowState(string id, ChartSeriesState? state) {
        RequireFlow(id);
        if (state.HasValue && !Enum.IsDefined(typeof(ChartSeriesState), state.Value)) throw new ArgumentOutOfRangeException(nameof(state));
        if (state.HasValue) _flowStates[id] = state.Value;
        else _flowStates.Remove(id);
        return this;
    }

    private void RequireFlow(string id) {
        if (Kind != ChartSeriesKind.Sankey && Kind != ChartSeriesKind.Chord) throw new InvalidOperationException("Flow styling requires a Sankey or Chord series.");
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Flow ID must not be empty.", nameof(id));
        if (Relationships?.ContainsFlow(id) != true) throw new ArgumentException("The series has no authored flow with this ID.", nameof(id));
    }
}
