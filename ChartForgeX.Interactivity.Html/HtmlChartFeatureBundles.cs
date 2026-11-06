using System;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Optional runtime bundles a page loads beside its chart runtime.</summary>
[Flags]
internal enum HtmlChartFeatureBundles {
    None = 0,
    /// <summary>Load animation and eased window transitions.</summary>
    Motion = 1,
    /// <summary>Context menu, CSV/JSON download and copy, and host menu items.</summary>
    Data = 2,
    /// <summary>Double-click and double-tap series isolation.</summary>
    Isolate = 4,
    /// <summary>Saved-page Treemap and Sunburst drill views.</summary>
    Hierarchy = 8
}
