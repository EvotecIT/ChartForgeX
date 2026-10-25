using System;
using System.Collections.Generic;
namespace ChartForgeX.VisualArtifacts;
public sealed partial class SequenceArtifactParticipant {
    private IReadOnlyList<VisualNavigationLink> _navigationLinks=Array.Empty<VisualNavigationLink>();
    /// <summary>Gets or replaces ordered host navigation links with a detached read-only snapshot.</summary>
    /// <remarks>Static SVG/PNG does not paint menus. The optional HTML adapter owns navigation presentation. Href remains the independent ordinary primary target.</remarks>
    public IReadOnlyList<VisualNavigationLink> NavigationLinks {get=>_navigationLinks;set=>_navigationLinks=VisualNavigationLink.Snapshot(value);}
}
