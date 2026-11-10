using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    private static readonly IReadOnlyDictionary<ChartSeriesKind, VisualSeriesDefaults> NoSeriesDefaults = new Dictionary<ChartSeriesKind, VisualSeriesDefaults>();
    private IReadOnlyDictionary<ChartSeriesKind, VisualSeriesDefaults> _seriesDefaults = NoSeriesDefaults;

    /// <summary>Gets the series kinds this theme carries defaults for, in kind order.</summary>
    public IReadOnlyList<ChartSeriesKind> SeriesDefaultKinds => _seriesDefaults.Keys.OrderBy(kind => kind).ToArray();

    /// <summary>Gets an independent copy of the defaults for one series kind, or null when the theme has none.</summary>
    /// <param name="kind">The series kind.</param>
    public VisualSeriesDefaults? GetSeriesDefaults(ChartSeriesKind kind) => SeriesDefaultsSnapshot(kind)?.Clone();

    /// <summary>Creates a theme whose defaults for one series kind are configured by <paramref name="configure"/>.</summary>
    /// <param name="kind">The series kind, such as <see cref="ChartSeriesKind.Line"/>.</param>
    /// <param name="configure">Edits a copy of the kind's current defaults.</param>
    /// <exception cref="ArgumentException">A setting does not apply to the kind, such as markers on bars.</exception>
    public VisualTheme WithSeriesDefaults(ChartSeriesKind kind, Action<VisualSeriesDefaults> configure) =>
        Configure(builder => builder.ConfigureSeries(kind, configure));

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
