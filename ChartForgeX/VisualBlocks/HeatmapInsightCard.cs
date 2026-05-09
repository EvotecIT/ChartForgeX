using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Dashboard heatmap card with an optional right-side insight rail and color key.
/// </summary>
public sealed class HeatmapInsightCard : VisualBlock<HeatmapInsightCard> {
    private readonly List<string> _columns = new();
    private readonly List<HeatmapInsightRow> _rows = new();
    private readonly List<HeatmapInsightItem> _insights = new();
    private string _leftControl = string.Empty;
    private string _selectedControl = string.Empty;
    private string _periodLabel = string.Empty;
    private string _insightTitle = "Busy times";
    private string _colorKeyLabel = "Color key";
    private double _minimum;
    private double _maximum = 1;
    private ChartColor _lowColor = ChartColor.FromHex("#EAF8FA");
    private ChartColor _highColor = ChartColor.FromHex("#0B8294");

    /// <summary>Gets column labels.</summary>
    public IReadOnlyList<string> Columns => _columns;

    /// <summary>Gets heatmap rows.</summary>
    public IReadOnlyList<HeatmapInsightRow> Rows => _rows;

    /// <summary>Gets side-rail insight items.</summary>
    public IReadOnlyList<HeatmapInsightItem> Insights => _insights;

    /// <summary>Gets or sets the unselected control label.</summary>
    public string LeftControl { get => _leftControl; set => _leftControl = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the selected control label.</summary>
    public string SelectedControl { get => _selectedControl; set => _selectedControl = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the period label.</summary>
    public string PeriodLabel { get => _periodLabel; set => _periodLabel = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the side insight title.</summary>
    public string InsightTitle { get => _insightTitle; set => _insightTitle = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the color key label.</summary>
    public string ColorKeyLabel { get => _colorKeyLabel; set => _colorKeyLabel = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the heatmap minimum.</summary>
    public double Minimum { get => _minimum; set => _minimum = value; }

    /// <summary>Gets or sets the heatmap maximum.</summary>
    public double Maximum { get => _maximum; set => _maximum = value; }

    /// <summary>Gets or sets the low-value heatmap color.</summary>
    public ChartColor LowColor { get => _lowColor; set => _lowColor = value; }

    /// <summary>Gets or sets the high-value heatmap color.</summary>
    public ChartColor HighColor { get => _highColor; set => _highColor = value; }

    /// <summary>Creates a new heatmap insight card.</summary>
    public static HeatmapInsightCard Create() => new();

    /// <summary>Sets the controls rendered above the matrix.</summary>
    public HeatmapInsightCard WithControls(string? left, string? selected, string? period) {
        LeftControl = left ?? string.Empty;
        SelectedControl = selected ?? string.Empty;
        PeriodLabel = period ?? string.Empty;
        return this;
    }

    /// <summary>Sets heatmap column labels.</summary>
    public HeatmapInsightCard WithColumns(params string[] columns) {
        if (columns == null) throw new ArgumentNullException(nameof(columns));
        _columns.Clear();
        foreach (var column in columns) _columns.Add(column ?? throw new ArgumentException("Column labels cannot contain null values.", nameof(columns)));
        return this;
    }

    /// <summary>Adds one heatmap row.</summary>
    public HeatmapInsightCard AddRow(string label, params double[] values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        _rows.Add(new HeatmapInsightRow(label, values));
        return this;
    }

    /// <summary>Adds one side-rail insight.</summary>
    public HeatmapInsightCard AddInsight(string label, string detail) {
        _insights.Add(new HeatmapInsightItem(label, detail));
        return this;
    }

    /// <summary>Sets the side insight title.</summary>
    public HeatmapInsightCard WithInsightTitle(string title) {
        InsightTitle = title ?? throw new ArgumentNullException(nameof(title));
        return this;
    }

    /// <summary>Sets the color key text and range.</summary>
    public HeatmapInsightCard WithColorKey(double minimum, double maximum, ChartColor? lowColor = null, ChartColor? highColor = null, string? label = null) {
        Minimum = minimum;
        Maximum = maximum;
        if (lowColor.HasValue) LowColor = lowColor.Value;
        if (highColor.HasValue) HighColor = highColor.Value;
        if (label != null) ColorKeyLabel = label;
        return this;
    }
}

/// <summary>
/// One row in a heatmap insight card.
/// </summary>
public sealed class HeatmapInsightRow {
    private string _label;
    private readonly List<double> _values;

    /// <summary>Initializes a heatmap row.</summary>
    public HeatmapInsightRow(string label, IEnumerable<double> values) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        if (values == null) throw new ArgumentNullException(nameof(values));
        _values = new List<double>(values);
    }

    /// <summary>Gets or sets the row label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets row values.</summary>
    public IReadOnlyList<double> Values => _values;
}

/// <summary>
/// One item in a heatmap insight rail.
/// </summary>
public sealed class HeatmapInsightItem {
    private string _label;
    private string _detail;

    /// <summary>Initializes a heatmap insight item.</summary>
    public HeatmapInsightItem(string label, string detail) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _detail = detail ?? throw new ArgumentNullException(nameof(detail));
    }

    /// <summary>Gets or sets the insight label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets supporting detail text.</summary>
    public string Detail { get => _detail; set => _detail = value ?? throw new ArgumentNullException(nameof(value)); }
}
