using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    internal bool IsStrip { get; private set; }

    /// <summary>Prepares only the plot, with no axes, legend, frame or outer padding, against a shared horizontal scale.</summary>
    /// <remarks>Use HTML for row labels and <see cref="ChartStripScale.PrepareAxis"/> for a matching axis.
    /// Cartesian and schedule families are supported. The chart and scale remain unchanged; ordinary vertical-axis
    /// settings still control Cartesian values. Explicit horizontal endpoints clip marks rather than moving the domain.</remarks>
    public PreparedVisual PrepareStrip(VisualRenderContext context, ChartStripScale scale) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (scale == null) throw new ArgumentNullException(nameof(scale));
        var family = VisualChartCompiler.Family(this);
        if (family is not (VisualChartFamily.Cartesian or VisualChartFamily.Schedule))
            throw new NotSupportedException("Plot strips support Cartesian and schedule charts.");
        var stripContext = new VisualRenderContext(new VisualLayoutOptions(context.Layout.Size, 0),
            context.Theme, context.ThemeMode,
            new VisualFrame(title: "", subtitle: "", showLegend: false, transparentBackground: true), context.Font);
        var view = (Chart)MemberwiseClone();
        view.Options = Options.PresentationView(stripContext);
        view.Options.XAxis = scale.Axis;
        view.Options.ShowAxes = false;
        view.Options.ShowGrid = false;
        view.IsStrip = true;
        ChartTimeSpacingPreparation.Capture(view);
        return view.PrepareScene(stripContext, null, null);
    }
}
