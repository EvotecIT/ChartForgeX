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
    private string _color = "Color";
    private string _allValues = "All values";
    private string _value = "value";
    private string _untitledChart = "ChartForgeX chart";
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

    /// <summary>Gets or sets the word for calendar days, map regions, and independent color measurements without a value, in scales and tooltips. Default <c>No data</c>.</summary>
    public string NoData { get => _noData; set => _noData = Required(value, nameof(value)); }

    /// <summary>Gets or sets the default title and tooltip row name of an independent numeric color measurement. Default <c>Color</c>.</summary>
    public string Color { get => _color; set => _color = Required(value, nameof(value)); }

    /// <summary>Gets or sets the caption for a discrete color scale with one unbounded band. Default <c>All values</c>.</summary>
    public string AllValues { get => _allValues; set => _allValues = Required(value, nameof(value)); }

    /// <summary>Gets or sets the word between the bounds of an interior discrete color band, for example <c>10 ≤ value &lt; 20</c>. Default <c>value</c>.</summary>
    public string Value { get => _value; set => _value = Required(value, nameof(value)); }

    /// <summary>
    /// Gets or sets the accessible name of a chart without a title (the SVG <c>title</c> element and the HTML page title).
    /// Default <c>ChartForgeX chart</c>.
    /// </summary>
    public string UntitledChart { get => _untitledChart; set => _untitledChart = Required(value, nameof(value)); }

    /// <summary>
    /// Gets or sets a function that writes the automatic accessible sentences from their <see cref="ChartDescriptionFacts"/>:
    /// the chart description (the SVG <c>desc</c> element) and the accessible names of calendar, dotted map, region map,
    /// and tile map groups; <see cref="ChartDescriptionFacts.Kind"/> says which. Null (the default), or a function
    /// returning null or white space for a sentence, writes <see cref="ChartDescriptionFacts.EnglishText"/>. A description
    /// set through <see cref="Chart.WithAccessibility"/> still wins and the function is not called for it. Exceptions
    /// thrown by the function are not caught; they surface from the render call.
    /// </summary>
    public Func<ChartDescriptionFacts, string?>? AccessibleTextFormatter { get; set; }

    /// <summary>
    /// Gets or sets a function that writes a day in the accessible name and hover title of a calendar heatmap cell, for
    /// example <c>day =&gt; day.ToString("D", culture)</c>. Null (the default), or a function returning null or white
    /// space, writes the ISO date (<c>yyyy-MM-dd</c>). The machine-readable <c>data-cfx-date</c> attribute always stays ISO.
    /// Exceptions thrown by the function are not caught; they surface from the render call.
    /// </summary>
    public Func<DateTime, string?>? DateFormatter { get; set; }

    internal string FormatDate(DateTime day) {
        var text = DateFormatter?.Invoke(day);
        return string.IsNullOrWhiteSpace(text) ? day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : text!;
    }

    internal string Describe(ChartDescriptionFacts description) {
        var text = AccessibleTextFormatter?.Invoke(description);
        return string.IsNullOrWhiteSpace(text) ? description.EnglishText : text!;
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Label text must not be empty.", parameterName) : value;
}
