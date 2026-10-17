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
    private string _size = "Size";
    private string _target = "Target";
    private string _authoredValue = "Provided value";
    private string _remainder = "Remainder";
    private string _allValues = "All values";
    private string _value = "value";
    private string _change = "Change";
    private string _category = "Category";
    private string _start = "Start";
    private string _end = "End";
    private string _minimum = "Minimum";
    private string _q1 = "Q1";
    private string _median = "Median";
    private string _q3 = "Q3";
    private string _maximum = "Maximum";
    private string _lowerWhisker = "Lower whisker";
    private string _upperWhisker = "Upper whisker";
    private string _sampleCount = "Sample count";
    private string _sourceMinimum = "Source minimum";
    private string _sourceMaximum = "Source maximum";
    private string _inlierMinimum = "Inlier minimum";
    private string _inlierMaximum = "Inlier maximum";
    private string _outlierCount = "Outlier count";
    private string _quartileEstimator = "Quartile estimator";
    private string _whiskerPolicy = "Whisker policy";
    private string _iqrMultiplier = "IQR multiplier";
    private string _noInliers = "None";
    private string _outlier = "Outlier";
    private string _openingBalance = "Opening balance";
    private string _subtotal = "Subtotal";
    private string _total = "Total";
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

    /// <summary>Gets or sets the interactive tooltip row name of a Bubble size measurement. Default <c>Size</c>.</summary>
    public string Size { get => _size; set => _size = Required(value, nameof(value)); }

    /// <summary>Gets or sets the tooltip row name of an unnamed gauge or bullet reference value. Default <c>Target</c>.</summary>
    public string Target { get => _target; set => _target = Required(value, nameof(value)); }

    /// <summary>Gets or sets the tooltip row name for a supplied group size that differs from its resolved size. Default <c>Provided value</c>.</summary>
    public string AuthoredValue { get => _authoredValue; set => _authoredValue = Required(value, nameof(value)); }

    /// <summary>Gets or sets the tooltip row name for the unallocated part of an inclusive Sunburst group total. Default <c>Remainder</c>.</summary>
    public string Remainder { get => _remainder; set => _remainder = Required(value, nameof(value)); }

    /// <summary>Gets or sets the caption for a discrete color scale with one unbounded band. Default <c>All values</c>.</summary>
    public string AllValues { get => _allValues; set => _allValues = Required(value, nameof(value)); }

    /// <summary>Gets or sets the word between the bounds of an interior discrete color band, for example <c>10 ≤ value &lt; 20</c>. Default <c>value</c>.</summary>
    public string Value { get => _value; set => _value = Required(value, nameof(value)); }

    /// <summary>Gets or sets the waterfall tooltip row name for a supplied change. Default <c>Change</c>.</summary>
    public string Change { get => _change; set => _change = Required(value, nameof(value)); }

    /// <summary>Gets or sets the Cartesian tooltip row name for the category. Default <c>Category</c>.</summary>
    public string Category { get => _category; set => _category = Required(value, nameof(value)); }

    /// <summary>Gets or sets the range-bar tooltip row name for the authored interval start. Default <c>Start</c>.</summary>
    public string Start { get => _start; set => _start = Required(value, nameof(value)); }

    /// <summary>Gets or sets the range-bar tooltip row name for the authored interval end. Default <c>End</c>.</summary>
    public string End { get => _end; set => _end = Required(value, nameof(value)); }

    /// <summary>Gets or sets the box plot tooltip row name for the minimum whisker. Default <c>Minimum</c>.</summary>
    public string Minimum { get => _minimum; set => _minimum = Required(value, nameof(value)); }

    /// <summary>Gets or sets the box plot tooltip row name for the first quartile. Default <c>Q1</c>.</summary>
    public string Q1 { get => _q1; set => _q1 = Required(value, nameof(value)); }

    /// <summary>Gets or sets the box plot tooltip row name for the median. Default <c>Median</c>.</summary>
    public string Median { get => _median; set => _median = Required(value, nameof(value)); }

    /// <summary>Gets or sets the box plot tooltip row name for the third quartile. Default <c>Q3</c>.</summary>
    public string Q3 { get => _q3; set => _q3 = Required(value, nameof(value)); }

    /// <summary>Gets or sets the box plot tooltip row name for the maximum whisker. Default <c>Maximum</c>.</summary>
    public string Maximum { get => _maximum; set => _maximum = Required(value, nameof(value)); }

    /// <summary>Gets or sets the default waterfall opening-balance axis caption and tooltip row name. Default <c>Opening balance</c>.</summary>
    public string OpeningBalance { get => _openingBalance; set => _openingBalance = Required(value, nameof(value)); }

    /// <summary>Gets or sets the default waterfall subtotal axis caption and tooltip row name. Default <c>Subtotal</c>.</summary>
    public string Subtotal { get => _subtotal; set => _subtotal = Required(value, nameof(value)); }

    /// <summary>Gets or sets the default waterfall total axis caption and tooltip row name. Default <c>Total</c>.</summary>
    public string Total { get => _total; set => _total = Required(value, nameof(value)); }

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
    /// set through <see cref="Chart.ConfigureAccessibility"/> still wins and the function is not called for it. Exceptions
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

    /// <summary>Gets or sets the raw box-plot tooltip row name for lower whisker.</summary>
    public string LowerWhisker { get => _lowerWhisker; set => _lowerWhisker = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for upper whisker.</summary>
    public string UpperWhisker { get => _upperWhisker; set => _upperWhisker = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for sample count.</summary>
    public string SampleCount { get => _sampleCount; set => _sampleCount = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for source minimum.</summary>
    public string SourceMinimum { get => _sourceMinimum; set => _sourceMinimum = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for source maximum.</summary>
    public string SourceMaximum { get => _sourceMaximum; set => _sourceMaximum = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for inlier minimum.</summary>
    public string InlierMinimum { get => _inlierMinimum; set => _inlierMinimum = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for inlier maximum.</summary>
    public string InlierMaximum { get => _inlierMaximum; set => _inlierMaximum = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for outlier count.</summary>
    public string OutlierCount { get => _outlierCount; set => _outlierCount = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for quartile estimator.</summary>
    public string QuartileEstimator { get => _quartileEstimator; set => _quartileEstimator = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for whisker policy.</summary>
    public string WhiskerPolicy { get => _whiskerPolicy; set => _whiskerPolicy = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot tooltip row name for iqr multiplier.</summary>
    public string IqrMultiplier { get => _iqrMultiplier; set => _iqrMultiplier = Required(value, nameof(value)); }

    /// <summary>Gets or sets the raw box-plot readout for an absent observed inlier.</summary>
    public string NoInliers { get => _noInliers; set => _noInliers = Required(value, nameof(value)); }

    /// <summary>Gets or sets the tooltip row name for an individual outlier measurement.</summary>
    public string Outlier { get => _outlier; set => _outlier = Required(value, nameof(value)); }

    internal ChartLabels Capture() => (ChartLabels)MemberwiseClone();

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Label text must not be empty.", parameterName) : value;
}
