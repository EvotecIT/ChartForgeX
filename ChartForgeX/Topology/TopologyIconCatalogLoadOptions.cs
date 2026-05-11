namespace ChartForgeX.Topology;

/// <summary>
/// Defines how duplicate icon-pack ids are handled while loading manifest folders.
/// </summary>
public enum TopologyIconPackConflictBehavior {
    /// <summary>Report duplicate pack ids as load failures.</summary>
    ReportError,
    /// <summary>Skip duplicate pack ids and keep the existing catalog pack.</summary>
    Skip,
    /// <summary>Replace the existing catalog pack with the later loaded manifest pack.</summary>
    Replace
}

/// <summary>
/// Defines options for loading a topology icon catalog from manifest files.
/// </summary>
public sealed class TopologyIconCatalogLoadOptions {
    /// <summary>Gets or sets the file search pattern.</summary>
    public string SearchPattern { get; set; } = "*.json";

    /// <summary>Gets or sets whether child directories should be searched.</summary>
    public bool Recursive { get; set; }

    /// <summary>Gets or sets whether the returned catalog should start with ChartForgeX built-in packs.</summary>
    public bool IncludeBuiltInPacks { get; set; } = true;

    /// <summary>Gets or sets how duplicate pack ids should be handled.</summary>
    public TopologyIconPackConflictBehavior ConflictBehavior { get; set; }
}
