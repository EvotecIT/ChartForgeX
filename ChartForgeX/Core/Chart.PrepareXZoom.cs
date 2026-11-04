using System;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Prepares a detached finite X magnification bank with continuous contained pan.</summary>
    /// <param name="context">Logical size, frame, theme and native typography.</param>
    /// <param name="levels">Increasing magnifications beginning with one.</param>
    /// <returns>Native fixed-layout scenes that require no chart or formatter during navigation.</returns>
    /// <remarks>Supports numeric linear X Line and StepLine series with Linear or Step interpolation.
    /// Full-data Y domains, plot bounds, native paint and source identities remain fixed across the bank.
    /// Captions follow their full-track anchors and may be hidden when outside the plot or blocked;
    /// preparation records those decisions. This is a fixed-plot policy, not per-window axis reflow.
    /// Each window width rounds to the coarsest source-coordinate double spacing in the full domain.
    /// Preparation rejects a level when that changes its width by more than one double spacing of the full source span,
    /// or cannot form a positive finite width. Pan then rounds its bounds before positioning native paint.
    /// Preparation also rejects a native track when four double spacings at its largest absolute unshifted or
    /// translated X coordinate exceed 0.0005 logical units, half the SVG coordinate rounding step.
    /// This is a numeric rendering precision constraint, not a measured resource or performance limit.
    /// Static preparation remains script-free; an optional host provides interaction.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A level cannot retain the full source span's precision with a finite positive window width, or its native track cannot retain the required logical coordinate precision.</exception>
    public PreparedChartXZoom PrepareXZoom(VisualRenderContext context, ChartXZoomLevels levels) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (levels == null) throw new ArgumentNullException(nameof(levels));
        var view = PresentationView(context);
        foreach (var series in view.Series) series.ResolveFormatting(view);
        ChartTimeSpacingPreparation.Capture(view);
        ChartGuards.RenderCompatibility(view, preparing: true);
        VisualCartesianCompiler.ValidatePreparedXZoom(view);
        var prepared = view.BeginPreparation(context);
        return VisualCartesianCompiler.PrepareXZoom(view, prepared.Context, prepared.Builder.Build(), prepared.Content,
            prepared.AxisLabels!, prepared.ColorScales, levels, view.PreparationAccessibility(prepared.Context.Frame),
            new VisualSvgOptions(colorVariables: Options.SvgColorVariables), prepared.LegendHeight);
    }
}
