using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Data label visibility, placement and leader-line connectors. Configure it through <see cref="ChartOptions.DataLabels"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartDataLabelOptions {
    internal ChartDataLabelOptions Clone() => (ChartDataLabelOptions)MemberwiseClone();
    /// <summary>
    /// Gets or sets chart-level data-label visibility. Series without an explicit override use this setting.
    /// When unset, the renderer uses its family's default visibility.
    /// </summary>
    public bool? Visible { get => _showDataLabels; set => _showDataLabels = value; }

    private bool? _showDataLabels;

    internal bool? Visibility => _showDataLabels;

    private ChartDataLabelPlacement _dataLabelPlacement = ChartDataLabelPlacement.Auto;

    private double _dataLabelConnectorOpacity = 0.55;

    private double _dataLabelConnectorStrokeWidth = 1.2;

    private ChartColor? _dataLabelConnectorColor;

    private ChartDataLabelConnectorStyle _dataLabelConnectorStyle = ChartDataLabelConnectorStyle.Elbow;

    internal bool HasConnectorStyle { get; private set; }


    /// <summary>
    /// Gets or sets the preferred data-label placement for capable renderers.
    /// </summary>
    public ChartDataLabelPlacement Placement {
        get => _dataLabelPlacement;
        set {
            if (!Enum.IsDefined(typeof(ChartDataLabelPlacement), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown data-label placement.");
            _dataLabelPlacement = value;
        }
    }


    /// <summary>
    /// Gets or sets an optional override color for data-label connector lines. Pie and donut callouts use the matching slice color when this is null.
    /// </summary>
    public ChartColor? ConnectorColor {
        get => _dataLabelConnectorColor;
        set => _dataLabelConnectorColor = value;
    }


    /// <summary>
    /// Gets or sets the opacity used by data-label connector lines.
    /// </summary>
    public double ConnectorOpacity {
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
    public double ConnectorStrokeWidth {
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
    public ChartDataLabelConnectorStyle ConnectorStyle {
        get => _dataLabelConnectorStyle;
        set {
            if (!Enum.IsDefined(typeof(ChartDataLabelConnectorStyle), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown data-label connector style.");
            _dataLabelConnectorStyle = value;
            HasConnectorStyle = true;
        }
    }
}
