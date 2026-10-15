namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    /// <summary>Gets or sets whether a plain card or enclosed diagram label may grow its node using the prepared rendering font.</summary>
    /// <remarks>The default is false, so authored dimensions remain fixed. When enabled, <see cref="Width"/> and
    /// <see cref="Height"/> are minimum dimensions. Preparation grows a detached node before layout and routing;
    /// the source node is unchanged. Applies to label-only card nodes without icons, subtitles, details or badges.
    /// Explicit label character and line limits still apply. Actor captions and compact display modes retain their own sizing policy.</remarks>
    public bool FitLabelToContent { get; set; }
}
