namespace ChartForgeX.Mermaid;

/// <summary>Host identity and presentation shared by every native Mermaid family.</summary>
public abstract class MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the stable artifact identifier.</summary>
    public string? Id { get; set; }
    /// <summary>Gets or sets a title override. Null retains the source title or family default.</summary>
    public string? Title { get; set; }
    /// <summary>Gets or sets the host subtitle.</summary>
    public string? Subtitle { get; set; }
    /// <summary>Gets or sets host presentation overrides; null values inherit the shared request defaults.</summary>
    public MermaidPresentationOptions? Presentation { get; set; }

    /// <summary>Copies mutable option fields and presentation so later host edits do not affect this configuration.</summary>
    public MermaidDiagramRenderOptions Clone() => Snapshot<MermaidDiagramRenderOptions>(null);

    internal T Snapshot<T>(MermaidPresentationOptions? defaults) where T : MermaidDiagramRenderOptions {
        var copy = (T)MemberwiseClone();
        copy.Presentation = MermaidPresentationOptions.Merge(defaults, Presentation);
        return copy;
    }
}
