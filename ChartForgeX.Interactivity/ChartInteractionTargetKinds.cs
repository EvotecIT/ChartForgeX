namespace ChartForgeX.Interactivity;

/// <summary>
/// Provides common adapter target-kind tokens for reusable interaction scenarios.
/// </summary>
public static class ChartInteractionTargetKinds {
    /// <summary>Targets a rendered chart series, usually by zero-based series index or adapter series id.</summary>
    public const string Series = "series";

    /// <summary>Targets a rendered chart point, usually by adapter point id.</summary>
    public const string Point = "point";

    /// <summary>Targets a rendered annotation, mark, or threshold element.</summary>
    public const string Annotation = "annotation";

    /// <summary>Targets a rendered geographic, matrix, or categorical region.</summary>
    public const string Region = "region";

    /// <summary>Targets a rendered hierarchy, flow, or relationship node.</summary>
    public const string Node = "node";

    /// <summary>Targets a rendered hierarchy, flow, route, or relationship link.</summary>
    public const string Link = "link";

    /// <summary>Targets a rendered legend entry.</summary>
    public const string Legend = "legend";

    /// <summary>Targets an element by rendered SVG id or adapter-specific data id.</summary>
    public const string Element = "element";

    /// <summary>Targets a rendered element by role plus value or label.</summary>
    public const string Role = "role";
}
