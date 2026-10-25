using System;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateCaptionContent(TextContent? content, string? plain, string context) {
        if (content == null) return;
        if (!content.IsStructured) throw new ArgumentException(context + " must carry explicitly structured content.");
        Text(content.PlainText, context);
        if (!string.Equals(plain, content.PlainText, StringComparison.Ordinal))
            throw new ArgumentException(context + " must equal its plain semantic projection.");
    }
}
