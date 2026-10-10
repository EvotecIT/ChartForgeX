using System.Collections.Generic;

namespace ChartForgeX.Rendering;

/// <summary>Declares the native mark described by separately layered caption ink, without duplicating its source identity.</summary>
internal static class VisualMarkLabel {
    /// <summary>Preserves the complete caption and, when supplied, references a stable scene mark ID rather than a scoped SVG element ID.</summary>
    internal static Dictionary<string, string> Metadata(string fullText, string? markId = null) {
        var facts = new Dictionary<string, string> { ["data-cfx-full-label"] = fullText };
        if (markId != null) {
            facts["data-cfx-label-for"] = markId;
            facts["data-cfx-label-decoration"] = "true";
        }
        return facts;
    }
}
