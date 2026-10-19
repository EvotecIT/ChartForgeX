using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>A detached authored annotation binding captured from the native scene, never inferred from its caption or ordinal.</summary>
internal sealed class VisualAnnotationIdentity {
    internal const string SourcePrefix = "annotation-id-";
    internal const string Attribute = "data-cfx-annotation-id";
    internal VisualAnnotationIdentity(string annotationId, string sourceId, string kind) {
        AnnotationId = annotationId; SourceId = sourceId; Kind = kind;
    }
    internal string AnnotationId { get; }
    internal string SourceId { get; }
    internal string Kind { get; }

    /// <summary>Formats the opaque native target in a namespace distinct from anonymous annotation ordinals.</summary>
    internal static string SourceIdFor(ChartAnnotationId id) => SourcePrefix
        + Convert.ToBase64String(new UTF8Encoding(false, true).GetBytes(id.Value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Copies bindings from immutable native annotation groups; composed panels may retain independent authored keys.</summary>
    internal static IReadOnlyList<VisualAnnotationIdentity> Capture(IReadOnlyList<VisualSceneNode> nodes) {
        var result = new List<VisualAnnotationIdentity>();
        foreach (var node in nodes) {
            if (node is VisualSceneGroup group && group.Role == "annotation" && group.Id != null
                && group.Metadata.TryGetValue(Attribute, out var id) && group.Metadata.TryGetValue("data-cfx-kind", out var kind))
                result.Add(new VisualAnnotationIdentity(id, group.Id, kind));
        }
        return result.AsReadOnly();
    }
}
