using ChartForgeX.Primitives;
using ChartForgeX.Core;

namespace ChartForgeX.VisualBlocks;

/// <summary>Immutable Wardley paint-role overrides. Null fields keep the existing shared theme paint.</summary>
public sealed class WardleyMapStyle {
    /// <summary>Creates optional colors for roles that can be independently authored by Wardley source.</summary>
    public WardleyMapStyle(ChartColor? backgroundColor = null, ChartColor? axisColor = null, ChartColor? axisTextColor = null,
        ChartColor? gridColor = null, ChartColor? componentFill = null, ChartColor? componentStroke = null,
        ChartColor? componentLabelColor = null, ChartColor? linkStroke = null, ChartColor? evolutionStroke = null,
        ChartColor? annotationStroke = null, ChartColor? annotationTextColor = null, ChartColor? annotationFill = null) {
        BackgroundColor = backgroundColor; AxisColor = axisColor; AxisTextColor = axisTextColor; GridColor = gridColor;
        ComponentFill = componentFill; ComponentStroke = componentStroke; ComponentLabelColor = componentLabelColor;
        LinkStroke = linkStroke; EvolutionStroke = evolutionStroke; AnnotationStroke = annotationStroke;
        AnnotationTextColor = annotationTextColor; AnnotationFill = annotationFill;
    }
    /// <summary>Gets the plot background override.</summary>
    public ChartColor? BackgroundColor { get; }
    /// <summary>Gets the axes and pipeline outline color override.</summary>
    public ChartColor? AxisColor { get; }
    /// <summary>Gets the axes and evolution-stage caption color override.</summary>
    public ChartColor? AxisTextColor { get; }
    /// <summary>Gets the stage grid color override.</summary>
    public ChartColor? GridColor { get; }
    /// <summary>Gets the component fill override.</summary>
    public ChartColor? ComponentFill { get; }
    /// <summary>Gets the component outline override.</summary>
    public ChartColor? ComponentStroke { get; }
    /// <summary>Gets the component caption override.</summary>
    public ChartColor? ComponentLabelColor { get; }
    /// <summary>Gets the dependency stroke override.</summary>
    public ChartColor? LinkStroke { get; }
    /// <summary>Gets the evolution overlay color override.</summary>
    public ChartColor? EvolutionStroke { get; }
    /// <summary>Gets the annotation circle/box outline override.</summary>
    public ChartColor? AnnotationStroke { get; }
    /// <summary>Gets the annotation number/legend caption override.</summary>
    public ChartColor? AnnotationTextColor { get; }
    /// <summary>Gets the annotation circle/box fill override.</summary>
    public ChartColor? AnnotationFill { get; }
}

/// <summary>A normalized Wardley annotation legend origin; the measured legend is clamped within the plot.</summary>
public sealed class WardleyMapAnnotationBox {
    /// <summary>Creates an annotation legend origin using visibility first and evolution second.</summary>
    public WardleyMapAnnotationBox(double visibility, double evolution) {
        ChartGuards.UnitInterval(visibility, nameof(visibility)); ChartGuards.UnitInterval(evolution, nameof(evolution));
        Visibility = visibility; Evolution = evolution;
    }
    /// <summary>Gets the normalized vertical visibility coordinate.</summary>
    public double Visibility { get; }
    /// <summary>Gets the normalized horizontal evolution coordinate.</summary>
    public double Evolution { get; }
}
