using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// Status colours shared by the three reporting vocabularies: finding severity, outcome, and operational state.
/// Operational states reuse the severity and outcome hues but keep their own words. Status colours are never used
/// for data series. Defaults are the Graphite palette v1 light pairs; every ink is at least 4.5:1 on card surfaces.
/// </summary>
public sealed class VisualStatusTokens {
    /// <summary>Gets or sets the critical severity colours.</summary>
    public VisualTokenColor Critical { get; set; } = Pair("#D4302F", "#B8292A");

    /// <summary>Gets or sets the high severity colours.</summary>
    public VisualTokenColor High { get; set; } = Pair("#DD5A17", "#B64A13");

    /// <summary>Gets or sets the medium severity colours.</summary>
    public VisualTokenColor Medium { get; set; } = Pair("#C78404", "#8C5C05");

    /// <summary>Gets or sets the low severity colours.</summary>
    public VisualTokenColor Low { get; set; } = Pair("#0C8AA8", "#0A728B");

    /// <summary>Gets or sets the informational severity colours.</summary>
    public VisualTokenColor Info { get; set; } = Pair("#2F6BD9", "#2B5FC0");

    /// <summary>Gets or sets the passed outcome colours (also operational state <c>up</c>).</summary>
    public VisualTokenColor Pass { get; set; } = Pair("#1D8A52", "#177345");

    /// <summary>Gets or sets the neutral colours for not evaluated, could not evaluate, not observable, and unknown.</summary>
    public VisualTokenColor Neutral { get; set; } = Pair("#7C818A", "#5B6069");

    /// <summary>Gets or sets the maintenance state colours.</summary>
    public VisualTokenColor Maintenance { get; set; } = Pair("#6B5BD2", "#5A4BC0");

    /// <summary>
    /// Returns the five finding severities, most severe first, keyed <c>critical</c>, <c>high</c>, <c>medium</c>,
    /// <c>low</c>, and <c>info</c>.
    /// </summary>
    /// <param name="labels">Optional localized labels by key; missing keys keep the English label.</param>
    public IReadOnlyList<ChartStateCategory> SeverityCategories(IReadOnlyDictionary<string, string>? labels = null) => new[] {
        new ChartStateCategory("critical", Label(labels, "critical", "Critical"), Critical.Fill),
        new ChartStateCategory("high", Label(labels, "high", "High"), High.Fill),
        new ChartStateCategory("medium", Label(labels, "medium", "Medium"), Medium.Fill),
        new ChartStateCategory("low", Label(labels, "low", "Low"), Low.Fill),
        new ChartStateCategory("info", Label(labels, "info", "Informational"), Info.Fill)
    };

    /// <summary>
    /// Returns the outcomes that have their own colour, keyed <c>pass</c>, <c>notEvaluated</c> (hatched), and
    /// <c>couldNotEvaluate</c>. A failed result has no colour of its own; colour it by its severity.
    /// </summary>
    /// <param name="labels">Optional localized labels by key; missing keys keep the English label.</param>
    public IReadOnlyList<ChartStateCategory> OutcomeCategories(IReadOnlyDictionary<string, string>? labels = null) => new[] {
        new ChartStateCategory("pass", Label(labels, "pass", "Passed"), Pass.Fill),
        new ChartStateCategory("notEvaluated", Label(labels, "notEvaluated", "Not evaluated"), Neutral.Fill, hatched: true),
        new ChartStateCategory("couldNotEvaluate", Label(labels, "couldNotEvaluate", "Could not evaluate"), Neutral.Fill)
    };

    /// <summary>
    /// Returns the operational states keyed <c>up</c>, <c>degraded</c>, <c>down</c>, <c>recovering</c>,
    /// <c>maintenance</c>, <c>notObservable</c>, and <c>unknown</c>. Up uses the pass colour, degraded medium,
    /// down critical, recovering low; not observable and unknown are neutral and hatched.
    /// </summary>
    /// <param name="labels">Optional localized labels by key; missing keys keep the English label.</param>
    public IReadOnlyList<ChartStateCategory> OperationalStateCategories(IReadOnlyDictionary<string, string>? labels = null) => new[] {
        new ChartStateCategory("up", Label(labels, "up", "Up"), Pass.Fill),
        new ChartStateCategory("degraded", Label(labels, "degraded", "Degraded"), Medium.Fill),
        new ChartStateCategory("down", Label(labels, "down", "Down"), Critical.Fill),
        new ChartStateCategory("recovering", Label(labels, "recovering", "Recovering"), Low.Fill),
        new ChartStateCategory("maintenance", Label(labels, "maintenance", "Maintenance"), Maintenance.Fill),
        new ChartStateCategory("notObservable", Label(labels, "notObservable", "Not observable"), Neutral.Fill, hatched: true),
        new ChartStateCategory("unknown", Label(labels, "unknown", "Unknown"), Neutral.Fill, hatched: true)
    };

    /// <summary>Creates a copy of these status colours.</summary>
    public VisualStatusTokens Clone() => new() {
        Critical = Critical,
        High = High,
        Medium = Medium,
        Low = Low,
        Info = Info,
        Pass = Pass,
        Neutral = Neutral,
        Maintenance = Maintenance
    };

    internal static VisualTokenColor Pair(string fill, string ink) => new(ChartColor.FromHex(fill), ChartColor.FromHex(ink));

    private static string Label(IReadOnlyDictionary<string, string>? labels, string key, string fallback) =>
        labels != null && labels.TryGetValue(key, out var label) && !string.IsNullOrWhiteSpace(label) ? label : fallback;
}
