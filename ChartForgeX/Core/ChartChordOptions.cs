using System;

namespace ChartForgeX.Core;

/// <summary>Controls native weighted chord geometry. Nodes and endpoint slots retain authored order.</summary>
public sealed class ChartChordOptions {
    private double _startAngleDegrees = -90;
    private double _sweepAngleDegrees = 360;
    private double _nodeGapDegrees = 3;
    private double _nodeThicknessRatio = .06;
    private double _ribbonOpacity = .35;
    private ChartChordDirectionCue _directionCue = ChartChordDirectionCue.TargetChevron;
    private ChartChordLabelContent _labelContent = ChartChordLabelContent.LabelAndValue;

    /// <summary>Gets or sets the clockwise start in degrees; zero points right and -90 points up.</summary>
    public double StartAngleDegrees {
        get => _startAngleDegrees;
        set { ChartGuards.Finite(value, nameof(value)); _startAngleDegrees = value; }
    }

    /// <summary>Gets or sets the clockwise circular span, greater than zero and at most 360 degrees.</summary>
    public double SweepAngleDegrees {
        get => _sweepAngleDegrees;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0 || value > 360) throw new ArgumentOutOfRangeException(nameof(value));
            _sweepAngleDegrees = value;
        }
    }

    /// <summary>Gets or sets the gap in degrees after each positive node arc. Gaps must leave room inside the circular span.</summary>
    public double NodeGapDegrees {
        get => _nodeGapDegrees;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0 || value >= 360) throw new ArgumentOutOfRangeException(nameof(value));
            _nodeGapDegrees = value;
        }
    }

    /// <summary>Gets or sets node-arc thickness as a fraction strictly between zero and one of the outer radius.</summary>
    public double NodeThicknessRatio {
        get => _nodeThicknessRatio;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0 || value >= 1) throw new ArgumentOutOfRangeException(nameof(value));
            _nodeThicknessRatio = value;
        }
    }

    /// <summary>Gets or sets the opacity of filled ribbons, from zero to one.</summary>
    public double RibbonOpacity {
        get => _ribbonOpacity;
        set { ChartGuards.UnitInterval(value, nameof(value)); _ribbonOpacity = value; }
    }

    /// <summary>Gets or sets the target direction cue. Source and target metadata is retained in either mode.</summary>
    public ChartChordDirectionCue DirectionCue {
        get => _directionCue;
        set {
            if (!Enum.IsDefined(typeof(ChartChordDirectionCue), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _directionCue = value;
        }
    }

    /// <summary>Gets or sets measured external node-label content. Full facts remain in semantics when text is omitted.</summary>
    public ChartChordLabelContent LabelContent {
        get => _labelContent;
        set {
            if (!Enum.IsDefined(typeof(ChartChordLabelContent), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _labelContent = value;
        }
    }
}

/// <summary>Controls the native target cue on directed chord ribbons.</summary>
public enum ChartChordDirectionCue {
    /// <summary>Draws a ribbon without a direction cue.</summary>
    None,
    /// <summary>Draws a chevron pointing outward into the target endpoint slot.</summary>
    TargetChevron
}

/// <summary>Controls the contents of measured chord node labels.</summary>
public enum ChartChordLabelContent {
    /// <summary>Omits visible node labels while retaining complete semantics.</summary>
    None,
    /// <summary>Shows the authored display label.</summary>
    Label,
    /// <summary>Shows the display label and incoming plus outgoing endpoint value.</summary>
    LabelAndValue,
    /// <summary>Shows the display label and separate incoming and outgoing values.</summary>
    LabelAndTotals
}
