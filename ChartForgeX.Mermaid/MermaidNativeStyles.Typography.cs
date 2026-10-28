using System.Collections.Generic;
using ChartForgeX.Typography;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidNativeStyles {
    internal static bool SupportsText(string property, string value) => property switch {
        "font-size" => MermaidConfigurationTypography.TrySize(value, out _),
        "font-family" => MermaidConfigurationTypography.TryFamily(value, out _),
        "font-weight" => MermaidConfigurationTypography.TryWeight(value, out _),
        "font-style" => value is "normal" or "italic",
        "letter-spacing" => TypographyCss.TryLetterSpacing(value, 1, out _),
        _ => false
    };

    private static TextStyleOverride? ApplyTextStyle(TextStyleOverride? ordinary, Dictionary<string,string> styles) {
        TextStyleOverride? selected = null;
        foreach (var item in styles) {
            if (!SupportsText(item.Key, item.Value)) continue;
            selected ??= ordinary?.Clone() ?? new TextStyleOverride();
            switch (item.Key) {
                case "font-size": MermaidConfigurationTypography.TrySize(item.Value, out var size); selected.FontSize = size; break;
                case "font-family": selected.FontFamily = item.Value; break;
                case "font-weight": selected.FontWeight = item.Value; break;
                case "font-style": selected.Italic = item.Value == "italic"; break;
                case "letter-spacing": selected.LetterSpacing = 0; break; // Resolved after the actual presentation size is known.
            }
        }
        return selected ?? ordinary;
    }
}
