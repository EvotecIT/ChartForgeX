namespace ChartForgeX.Interactivity.Html;

/// <summary>Chooses the runtime contract included in a generated HTML page or fragment.</summary>
public enum HtmlChartRuntime {
    /// <summary>Includes the complete mutable controller and native host preparation APIs.</summary>
    Full,
    /// <summary>Includes a reader runtime selected from the declared interaction features, without native host connections.</summary>
    /// <remarks>All required code is embedded for offline use. Adaptive drawings retain the full runtime.
    /// Use Full for programmatic mounting, updates or native preparation.</remarks>
    Standalone
}
