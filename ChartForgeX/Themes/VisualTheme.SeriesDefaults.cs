using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    private static readonly IReadOnlyDictionary<ChartSeriesKind, VisualSeriesDefaults> NoSeriesDefaults = new Dictionary<ChartSeriesKind, VisualSeriesDefaults>();
    private IReadOnlyDictionary<ChartSeriesKind, VisualSeriesDefaults> _seriesDefaults = NoSeriesDefaults;

    /// <summary>Gets independent copies of the per-kind series defaults, keyed by series kind.</summary>
    /// <remarks>Edit them through <see cref="Configure"/> and <see cref="VisualThemeBuilder.ConfigureSeries"/>; changing a copy does not change this theme.</remarks>
    public IReadOnlyDictionary<ChartSeriesKind, VisualSeriesDefaults> SeriesDefaults =>
        new ReadOnlyDictionary<ChartSeriesKind, VisualSeriesDefaults>(CopySeriesDefaults().ToDictionary(pair => pair.Key, pair => pair.Value));

    internal bool HasSeriesDefaults(ChartSeriesKind kind) => _seriesDefaults.ContainsKey(kind);

    internal VisualSeriesDefaults? SeriesDefaultsSnapshot(ChartSeriesKind kind) => _seriesDefaults.TryGetValue(kind, out var defaults) ? defaults : null;

    internal IEnumerable<KeyValuePair<ChartSeriesKind, VisualSeriesDefaults>> CopySeriesDefaults() =>
        _seriesDefaults.Select(pair => new KeyValuePair<ChartSeriesKind, VisualSeriesDefaults>(pair.Key, pair.Value.Clone()));

    /// <summary>Validates and snapshots per-kind defaults onto a theme that has not been published yet.</summary>
    internal VisualTheme AttachSeriesDefaults(IEnumerable<KeyValuePair<ChartSeriesKind, VisualSeriesDefaults>> defaults) {
        var snapshot = new Dictionary<ChartSeriesKind, VisualSeriesDefaults>();
        foreach (var pair in defaults) {
            if (!Enum.IsDefined(typeof(ChartSeriesKind), pair.Key)) throw new ArgumentOutOfRangeException(nameof(defaults), pair.Key, "Unknown series kind.");
            if (pair.Value == null || pair.Value.IsEmpty) continue;
            pair.Value.Validate(pair.Key);
            snapshot[pair.Key] = pair.Value.Clone();
        }
        _seriesDefaults = snapshot.Count == 0 ? NoSeriesDefaults : snapshot;
        return this;
    }
}
