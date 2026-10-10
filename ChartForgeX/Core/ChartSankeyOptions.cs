using System;

namespace ChartForgeX.Core;

/// <summary>Controls the native layout and paint of weighted Sankey flows without changing their authored facts.</summary>
public sealed class ChartSankeyOptions {
    private ChartSankeyAlignment _alignment = ChartSankeyAlignment.Justify;
    private ChartSankeyVerticalAlignment _verticalAlignment = ChartSankeyVerticalAlignment.Center;
    private ChartSankeyNodeOrder _nodeOrder = ChartSankeyNodeOrder.Auto;
    private double _nodeWidth = 10;
    private double? _nodeGap, _nodeCornerRadius;
    private double _ribbonOpacity = .35;

    /// <summary>Gets or sets horizontal layer placement. The default is Justify; every flow remains directed to a later layer.</summary>
    public ChartSankeyAlignment Alignment {
        get => _alignment;
        set { Defined(value); _alignment = value; }
    }

    /// <summary>Gets or sets where each column's nodes occupy its unused vertical space. The default is Center.</summary>
    public ChartSankeyVerticalAlignment VerticalAlignment {
        get => _verticalAlignment;
        set { Defined(value); _verticalAlignment = value; }
    }

    /// <summary>Gets or sets ordering within each layer. The default is Auto; authored arrays and styling indexes stay unchanged.</summary>
    public ChartSankeyNodeOrder NodeOrder {
        get => _nodeOrder;
        set { Defined(value); _nodeOrder = value; }
    }

    /// <summary>Gets or sets the finite positive node-bar width in logical units. The default is ten; an unfittable width is rejected during preparation.</summary>
    public double NodeWidth {
        get => _nodeWidth;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Sankey node width must be positive.");
            _nodeWidth = value;
        }
    }

    /// <summary>Gets or sets the non-negative gap between nodes in each column. Null retains the theme gap; an unfittable gap is rejected during preparation.</summary>
    public double? NodeGap {
        get => _nodeGap;
        set { NonNegative(value); _nodeGap = value; }
    }

    /// <summary>Gets or sets a non-negative node-bar corner radius. Null retains the theme radius; geometry bounds it to half the bar's smaller dimension.</summary>
    public double? NodeCornerRadius {
        get => _nodeCornerRadius;
        set { NonNegative(value); _nodeCornerRadius = value; }
    }

    /// <summary>Gets or sets the node fill. Null retains series/state/theme paint; explicit point colors take precedence.</summary>
    public ChartColor? NodeFill { get; set; }

    /// <summary>Gets or sets the ribbon fill. Null retains source-node paint; explicit source point colors take precedence.</summary>
    public ChartColor? RibbonFill { get; set; }

    /// <summary>Gets or sets filled ribbon opacity from zero to one. The default is .35; the separate authored pattern overlay retains its own opacity.</summary>
    public double RibbonOpacity {
        get => _ribbonOpacity;
        set { ChartGuards.UnitInterval(value, nameof(value)); _ribbonOpacity = value; }
    }

    private static void NonNegative(double? value) {
        if (!value.HasValue) return;
        ChartGuards.Finite(value.Value, nameof(value));
        if (value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Sankey dimensions must be non-negative.");
    }

    private static void Defined<T>(T value) where T : struct {
        if (!Enum.IsDefined(typeof(T), value)) throw new ArgumentOutOfRangeException(nameof(value));
    }
}

/// <summary>Controls the horizontal layers of a directed acyclic Sankey graph.</summary>
public enum ChartSankeyAlignment {
    /// <summary>Uses each node's longest path depth from a source.</summary>
    Left,
    /// <summary>Uses the final layer minus each node's longest remaining path to a sink.</summary>
    Right,
    /// <summary>Uses the floor of the midpoint between the earliest and latest feasible layers.</summary>
    /// <remarks>The bounds are longest source depth and final layer minus longest remaining sink distance. This policy keeps every flow directed to a later layer.</remarks>
    Center,
    /// <summary>Uses longest source depths and moves terminal nodes to the final layer.</summary>
    Justify
}

/// <summary>Controls vertical placement of the nodes and gaps within each Sankey column.</summary>
public enum ChartSankeyVerticalAlignment {
    /// <summary>Places each column at the top of the available content.</summary>
    Top,
    /// <summary>Splits each column's unused space equally above and below its nodes.</summary>
    Center,
    /// <summary>Places each column at the bottom of the available content.</summary>
    Bottom
}

/// <summary>Controls deterministic node order within each Sankey layer.</summary>
public enum ChartSankeyNodeOrder {
    /// <summary>Uses bounded weighted ordering passes to reduce link crossings.</summary>
    Auto,
    /// <summary>Retains authored input order within each layer.</summary>
    Input,
    /// <summary>Orders display labels using ordinal ascending comparison, retaining input order for ties.</summary>
    LabelAscending,
    /// <summary>Orders display labels using ordinal descending comparison, retaining input order for ties.</summary>
    LabelDescending
}
