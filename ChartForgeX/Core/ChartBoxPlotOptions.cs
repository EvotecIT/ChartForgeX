using System;

namespace ChartForgeX.Core;

/// <summary>Controls box plot direction and captions while retaining category coordinates and five-number summaries.</summary>
public sealed class ChartBoxPlotOptions {
    private ChartOrientation _orientation;
    private ChartBoxPlotLabelContent _labelContent;

    /// <summary>Gets or sets whether labels show the legacy median heading or independently configured statistics. Default: Median.</summary>
    public ChartBoxPlotLabelContent LabelContent {
        get => _labelContent;
        set {
            if (!Enum.IsDefined(typeof(ChartBoxPlotLabelContent), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _labelContent = value;
        }
    }

    /// <summary>Gets or sets the optional formatter for Statistics captions. Null uses the chart's numeric value format.</summary>
    /// <remarks>During preparation, numeric text and formatter output are captured once in Minimum, Q1, Median, Q3, Maximum order for every summary, even if a caption is hidden. Null or blank output suppresses that caption. Custom point labels remain observation headings; detached exports never invoke the formatter.</remarks>
    public Func<ChartBoxPlotStatisticLabelContext, string?>? StatisticLabelFormatter { get; set; }

    /// <summary>Gets the rendered lower-whisker caption settings in Statistics mode.</summary>
    public ChartValueLabelOptions MinimumLabel { get; private set; } = new();
    /// <summary>Gets the first-quartile caption settings in Statistics mode.</summary>
    public ChartValueLabelOptions Q1Label { get; private set; } = new();
    /// <summary>Gets the median caption settings in Statistics mode.</summary>
    public ChartValueLabelOptions MedianLabel { get; private set; } = new();
    /// <summary>Gets the third-quartile caption settings in Statistics mode.</summary>
    public ChartValueLabelOptions Q3Label { get; private set; } = new();
    /// <summary>Gets the rendered upper-whisker caption settings in Statistics mode.</summary>
    public ChartValueLabelOptions MaximumLabel { get; private set; } = new();

    /// <summary>Gets or sets the value direction. Vertical is the default; horizontal uses X for values and Y for categories.</summary>
    public ChartOrientation Orientation {
        get => _orientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _orientation = value;
        }
    }

    /// <summary>Copies the configured state for isolated chart configuration.</summary>
    internal ChartBoxPlotOptions Clone() {
        var copy = (ChartBoxPlotOptions)MemberwiseClone();
        copy.MinimumLabel = MinimumLabel.Clone(); copy.Q1Label = Q1Label.Clone(); copy.MedianLabel = MedianLabel.Clone();
        copy.Q3Label = Q3Label.Clone(); copy.MaximumLabel = MaximumLabel.Clone();
        return copy;
    }
}
