using System;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Detaches preparation-time callback results before geometry or retained-source capture reads them.</summary>
    private Chart PresentationView(VisualRenderContext context) {
        var view = (Chart)MemberwiseClone();
        view.Options = Options.PresentationView(context);
        // Theme defaults per series kind resolve here, once, so every renderer reads ordinary series settings.
        var theme = context.Theme;
        if (Series.Any(series => series.HasPresentationCallbacks || theme.HasSeriesDefaults(series.Kind)))
            view._preparationSeries = Array.AsReadOnly(Series.Select(series => series.PrepareItemStyles().WithThemeDefaults(theme, view.Options)).ToArray());
        return view;
    }

    internal Chart CaptureCallbackPresentation(VisualRenderContext context) {
        if (!Series.Any(series => series.HasPresentationCallbacks)) return this;
        var view = PresentationView(context);
        view.HierarchyPreparation = ChartHierarchyPreparation.Resolve(view, view.NativeHierarchyVisibility?.HierarchyView, false, view.NativeHierarchyVisibility);
        foreach (var series in view.Series) series.ResolveFormatting(view);
        return view;
    }
}
