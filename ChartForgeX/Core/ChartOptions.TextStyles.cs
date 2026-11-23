using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>
    /// Gets the text style used for chart titles.
    /// </summary>
    public TextStyleOverride TitleStyle {
        get => _titleStyle;
        set => _titleStyle = value ?? throw new ArgumentNullException(nameof(value));
    }
    private TextStyleOverride _titleStyle = new();

    /// <summary>
    /// Gets the text style used for chart subtitles.
    /// </summary>
    public TextStyleOverride SubtitleStyle {
        get => _subtitleStyle;
        set => _subtitleStyle = value ?? throw new ArgumentNullException(nameof(value));
    }
    private TextStyleOverride _subtitleStyle = new();

    /// <summary>
    /// Gets the text style used for axis titles.
    /// </summary>
    public TextStyleOverride AxisTitleStyle {
        get => _axisTitleStyle;
        set => _axisTitleStyle = value ?? throw new ArgumentNullException(nameof(value));
    }
    private TextStyleOverride _axisTitleStyle = new();

    /// <summary>
    /// Gets the text style used for axis tick and category labels.
    /// </summary>
    public TextStyleOverride TickLabelStyle {
        get => _tickLabelStyle;
        set => _tickLabelStyle = value ?? throw new ArgumentNullException(nameof(value));
    }
    private TextStyleOverride _tickLabelStyle = new();

    /// <summary>
    /// Gets the text style used for legend labels.
    /// </summary>
    public TextStyleOverride LegendStyle {
        get => _legendStyle;
        set => _legendStyle = value ?? throw new ArgumentNullException(nameof(value));
    }
    private TextStyleOverride _legendStyle = new();

    /// <summary>
    /// Gets the text style used for data labels.
    /// </summary>
    public TextStyleOverride DataLabelStyle {
        get => _dataLabelStyle;
        set => _dataLabelStyle = value ?? throw new ArgumentNullException(nameof(value));
    }
    private TextStyleOverride _dataLabelStyle = new();

    /// <summary>
    /// Gets the text style for the requested role.
    /// </summary>
    public TextStyleOverride GetTextStyle(ChartTextRole role) {
        if (!Enum.IsDefined(typeof(ChartTextRole), role)) throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown text role.");
        if (role == ChartTextRole.Title) return TitleStyle;
        if (role == ChartTextRole.Subtitle) return SubtitleStyle;
        if (role == ChartTextRole.AxisTitle) return AxisTitleStyle;
        if (role == ChartTextRole.TickLabel) return TickLabelStyle;
        if (role == ChartTextRole.Legend) return LegendStyle;
        return DataLabelStyle;
    }

    internal void SetTextStyle(ChartTextRole role, TextStyleOverride style) {
        if (role == ChartTextRole.Title) TitleStyle = style;
        else if (role == ChartTextRole.Subtitle) SubtitleStyle = style;
        else if (role == ChartTextRole.AxisTitle) AxisTitleStyle = style;
        else if (role == ChartTextRole.TickLabel) TickLabelStyle = style;
        else if (role == ChartTextRole.Legend) LegendStyle = style;
        else if (role == ChartTextRole.DataLabel) DataLabelStyle = style;
        else throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown text role.");
    }
}
