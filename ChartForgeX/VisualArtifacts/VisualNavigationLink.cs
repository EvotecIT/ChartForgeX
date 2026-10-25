using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;
namespace ChartForgeX.VisualArtifacts;
/// <summary>A detached literal navigation label and safe target; presentation belongs to a host adapter.</summary>
public sealed class VisualNavigationLink {
    /// <summary>Creates literal text and an HTTP, HTTPS, mail, telephone, relative or fragment target.</summary>
    /// <exception cref="ArgumentException">The label is empty or the target is unsafe.</exception>
    public VisualNavigationLink(string label,string href) {
        if(string.IsNullOrWhiteSpace(label))throw new ArgumentException("A navigation label is required.",nameof(label));
        if(label.Length>VisualArtifactInterchangeValidation.MaximumTextCharacters)throw new ArgumentOutOfRangeException(nameof(label));
        var safe=TopologyRenderPrimitives.SafeHref(href)??throw new ArgumentException("A safe navigation target is required.",nameof(href));
        if(safe.Length>VisualArtifactInterchangeValidation.MaximumTextCharacters)throw new ArgumentOutOfRangeException(nameof(href));
        Label=label;Href=safe;
    }
    /// <summary>Gets literal link text; HTML hosts escape it without interpreting markup.</summary>
    public string Label { get; }
    /// <summary>Gets the validated target without executing or fetching it.</summary>
    public string Href { get; }
    internal static IReadOnlyList<VisualNavigationLink> Snapshot(IReadOnlyList<VisualNavigationLink> links) {
        if(links==null)throw new ArgumentNullException(nameof(links));
        var copy=links.ToArray();if(copy.Any(item=>item==null))throw new ArgumentException("Navigation links cannot contain null entries.",nameof(links));
        return Array.AsReadOnly(copy);
    }
}
