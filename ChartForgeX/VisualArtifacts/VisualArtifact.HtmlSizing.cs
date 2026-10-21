using System;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class VisualArtifact {
    private VisualArtifactHtmlSizing _htmlSizing;
    /// <summary>Gets or sets an optional producer/host policy for SVG, native pixels, PNG and standalone HTML exports.</summary>
    /// <remarks>Policy is host state, not source semantics. Clone retains the same policy reference; implementations
    /// should capture immutable settings and must not mutate the artifact during export.</remarks>
    public IVisualArtifactExportPolicy? ExportPolicy { get; set; }

    /// <summary>Gets or sets the producer's default standalone HTML sizing. Explicit export options override it.</summary>
    /// <remarks>This controls host scrolling or scaling of completed geometry; SVG and PNG dimensions are unchanged.</remarks>
    public VisualArtifactHtmlSizing HtmlSizing {
        get => _htmlSizing;
        set => _htmlSizing = Enum.IsDefined(typeof(VisualArtifactHtmlSizing), value)
            ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
}
