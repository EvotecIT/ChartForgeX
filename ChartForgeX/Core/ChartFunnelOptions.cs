using System;

namespace ChartForgeX.Core;

/// <summary>Controls funnel anatomy and orientation without changing its ordered source stages.</summary>
public sealed class ChartFunnelOptions {
    private ChartFunnelForm _form;
    private ChartOrientation _orientation;

    /// <summary>Gets or sets the anatomy. The default is proportional stage bars.</summary>
    public ChartFunnelForm Form {
        get => _form;
        set {
            if (!Enum.IsDefined(typeof(ChartFunnelForm), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _form = value;
        }
    }

    /// <summary>Gets or sets the direction of the ordered stages. The default is top to bottom.</summary>
    public ChartOrientation Orientation {
        get => _orientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _orientation = value;
        }
    }

    private ChartStageLabelContent _labelContent;
    private double _outlineDashOffset;

    /// <summary>Gets or sets combined (default) or independently positioned component captions. Shared data labels must be enabled.</summary>
    public ChartStageLabelContent LabelContent {
        get => _labelContent;
        set {
            if (!Enum.IsDefined(typeof(ChartStageLabelContent), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _labelContent = value;
        }
    }
    /// <summary>Gets independent category visibility, physical placement and typography in Separate mode. Null inherits series/chart placement; Auto prefers the outside rail.</summary>
    public ChartValueLabelOptions CategoryLabel { get; private set; } = new();
    /// <summary>Gets independent value visibility, physical placement and typography in Separate mode. Null inherits series/chart placement; Auto prefers the mark interior.</summary>
    public ChartValueLabelOptions ValueLabel { get; private set; } = new();
    /// <summary>Gets or sets the optional shared stage outline policy. Point stroke colors take precedence over its color; null preserves the existing unoutlined partition or Cone line.</summary>
    public ChartStrokeStyle? StageOutline { get; set; }
    /// <summary>Gets or sets the finite stage outline dash phase in logical units.</summary>
    public double StageOutlineDashOffset {
        get => _outlineDashOffset;
        set { ChartGuards.Finite(value, nameof(value)); _outlineDashOffset = value; }
    }
    /// <summary>Gets independent retained-percentage visibility, placement and typography in Separate mode. Undefined baselines keep an explicit descriptive caption.</summary>
    public ChartValueLabelOptions RetentionLabel { get; private set; } = new();
    /// <summary>Gets independent previous-stage drop-off visibility, placement and typography in Separate mode. The first source has no previous baseline.</summary>
    public ChartValueLabelOptions DropOffLabel { get; private set; } = new();
    /// <summary>Gets or sets the immutable default for adjacent connections. Source-indexed series overrides take precedence; null preserves existing paint.</summary>
    public ChartFunnelConnectorStyle? ConnectorStyle { get; set; }

    /// <summary>Copies the configured state for isolated chart configuration.</summary>
    internal ChartFunnelOptions Clone() {
        var copy = (ChartFunnelOptions)MemberwiseClone();
        copy.CategoryLabel = CategoryLabel.Clone(); copy.ValueLabel = ValueLabel.Clone();
        copy.RetentionLabel = RetentionLabel.Clone(); copy.DropOffLabel = DropOffLabel.Clone();
        return copy;
    }
}
