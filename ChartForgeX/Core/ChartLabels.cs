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
    private string _less = "Less";
    private string _more = "More";
    private string _noData = "No data";
    private string _calendarSummary = "{0} calendar heatmap from {1} to {2} with {3} filled days and {4} empty days";
    private const string DefaultLevel = "Level";
    private string _level = DefaultLevel;

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

    /// <summary>
    /// Gets or sets the interactive tooltip row name for the intensity level of sequential heatmap cells. Default
    /// <c>Level</c>; a changed value is emitted as <c>data-cfx-label-level</c> on the heatmap for the HTML adapter.
    /// </summary>
    public string Level { get => _level; set => _level = Required(value, nameof(value)); }

    internal string? LevelOverride => _level == DefaultLevel ? null : _level;

    /// <summary>Gets or sets the word at the weak end of calendar and map colour scales. Default <c>Less</c>.</summary>
    public string Less { get => _less; set => _less = Required(value, nameof(value)); }

    /// <summary>Gets or sets the word at the strong end of calendar and map colour scales. Default <c>More</c>.</summary>
    public string More { get => _more; set => _more = Required(value, nameof(value)); }

    /// <summary>Gets or sets the word for calendar days and map regions without a value, in the scale and in tooltips. Default <c>No data</c>.</summary>
    public string NoData { get => _noData; set => _noData = Required(value, nameof(value)); }

    /// <summary>
    /// Gets or sets the accessible summary of a calendar heatmap: <c>{0}</c> the series name, <c>{1}</c> and <c>{2}</c>
    /// the first and last day, <c>{3}</c> the days with a value, and <c>{4}</c> the days without one. Default
    /// <c>{0} calendar heatmap from {1} to {2} with {3} filled days and {4} empty days</c>.
    /// </summary>
    public string CalendarSummary {
        get => _calendarSummary;
        set {
            var text = Required(value, nameof(value));
            try {
                _ = string.Format(System.Globalization.CultureInfo.InvariantCulture, text, "a", "b", "c", 1, 2);
            } catch (FormatException error) {
                throw new ArgumentException("The calendar summary must be a composite format using placeholders {0} to {4}.", nameof(value), error);
            }

            _calendarSummary = text;
        }
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Label text must not be empty.", parameterName) : value;
}
