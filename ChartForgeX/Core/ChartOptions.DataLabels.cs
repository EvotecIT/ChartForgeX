using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartDataLabelPlacement _dataLabelPlacement = ChartDataLabelPlacement.Auto;
    private double _dataLabelConnectorOpacity = 0.55;
    private double _dataLabelConnectorStrokeWidth = 1.2;
    private ChartColor? _dataLabelConnectorColor;
    private ChartDataLabelConnectorStyle _dataLabelConnectorStyle = ChartDataLabelConnectorStyle.Elbow;
    internal bool HasDataLabelConnectorStyle { get; private set; }
    private ChartPieSliceLabelContent _pieSliceLabelContent = ChartPieSliceLabelContent.Percent;
    private double _pieOutsideLabelDistanceRatio = 1.14;

    /// <summary>
    /// Gets or sets the preferred data-label placement for capable renderers.
    /// </summary>
    public ChartDataLabelPlacement DataLabelPlacement {
        get => _dataLabelPlacement;
        set {
            if (!Enum.IsDefined(typeof(ChartDataLabelPlacement), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown data-label placement.");
            _dataLabelPlacement = value;
        }
    }

    /// <summary>
    /// Gets or sets an optional override color for data-label connector lines. Pie and donut callouts use the matching slice color when this is null.
    /// </summary>
    public ChartColor? DataLabelConnectorColor {
        get => _dataLabelConnectorColor;
        set => _dataLabelConnectorColor = value;
    }

    /// <summary>
    /// Gets or sets the opacity used by data-label connector lines.
    /// </summary>
    public double DataLabelConnectorOpacity {
        get => _dataLabelConnectorOpacity;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Data-label connector opacity must be between zero and one.");
            _dataLabelConnectorOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets the stroke width used by data-label connector lines.
    /// </summary>
    public double DataLabelConnectorStrokeWidth {
        get => _dataLabelConnectorStrokeWidth;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0 || value > 8) throw new ArgumentOutOfRangeException(nameof(value), value, "Data-label connector stroke width must be greater than zero and no more than eight.");
            _dataLabelConnectorStrokeWidth = value;
        }
    }

    /// <summary>
    /// Gets or sets the connector line shape used by outside and side data labels.
    /// </summary>
    public ChartDataLabelConnectorStyle DataLabelConnectorStyle {
        get => _dataLabelConnectorStyle;
        set {
            if (!Enum.IsDefined(typeof(ChartDataLabelConnectorStyle), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown data-label connector style.");
            _dataLabelConnectorStyle = value;
            HasDataLabelConnectorStyle = true;
        }
    }

    /// <summary>
    /// Gets or sets the text rendered for pie and donut slice data labels.
    /// </summary>
    public ChartPieSliceLabelContent PieSliceLabelContent {
        get => _pieSliceLabelContent;
        set {
            if (!Enum.IsDefined(typeof(ChartPieSliceLabelContent), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown pie slice label content.");
            _pieSliceLabelContent = value;
        }
    }

    /// <summary>
    /// Gets or sets a custom formatter for pie and donut slice data labels.
    /// </summary>
    public Func<ChartPieSliceLabelContext, string?>? PieSliceLabelFormatter { get; set; }

    /// <summary>
    /// Gets or sets the side-lane distance for outside pie and donut labels as a ratio of the outer radius.
    /// </summary>
    public double PieOutsideLabelDistanceRatio {
        get => _pieOutsideLabelDistanceRatio;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.9 || value > 1.8) throw new ArgumentOutOfRangeException(nameof(value), value, "Pie outside label distance ratio must be between 0.9 and 1.8.");
            _pieOutsideLabelDistanceRatio = value;
        }
    }
}
