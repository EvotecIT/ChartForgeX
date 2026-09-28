using System;

namespace ChartForgeX.Core;

/// <summary>
/// Holds the words ChartForgeX writes into rendered charts on its own (markers, tooltips, and default titles), so hosts
/// can localize them. Defaults are English; set values before rendering, or before calling builders that use them.
/// </summary>
public sealed class ChartLabels {
    private string _now = "Now";
    private string _ongoing = "ongoing";
    private string _soFar = "so far";
    private string _hourOfDay = "Hour of day";

    /// <summary>Gets or sets the label of the current-time line in Gantt lanes. Default <c>Now</c>.</summary>
    public string Now { get => _now; set => _now = Required(value, nameof(value)); }

    /// <summary>Gets or sets the tooltip word used in place of the end time of an open item. Default <c>ongoing</c>.</summary>
    public string Ongoing { get => _ongoing; set => _ongoing = Required(value, nameof(value)); }

    /// <summary>Gets or sets the tooltip suffix after the duration of an open item. Default <c>so far</c>.</summary>
    public string SoFar { get => _soFar; set => _soFar = Required(value, nameof(value)); }

    /// <summary>
    /// Gets or sets the default x-axis title of hour-by-weekday heatmaps, followed by the zone designator in parentheses.
    /// Set it before calling <see cref="Chart.AddHourWeekdayHeatmap"/>. Default <c>Hour of day</c>.
    /// </summary>
    public string HourOfDay { get => _hourOfDay; set => _hourOfDay = Required(value, nameof(value)); }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Label text must not be empty.", parameterName) : value;
}
