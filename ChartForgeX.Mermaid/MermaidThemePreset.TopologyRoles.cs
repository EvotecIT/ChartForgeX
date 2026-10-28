using System.Collections.Generic;
using Precise = ChartForgeX.Rendering.ChartColorMath.PreciseColor;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidThemePreset {
    // Constructor literals that have a real topology color consumer. Common/Sequence
    // seeds stay unchanged; gradients/effects/font/radius/width policy are separate.
    private static void SeedTopologyRoles(string name, Dictionary<string, Precise> colors) {
        void Set(string key, string value) => colors[key] = Literal(value);
        switch (name) {
            case "default": Set("labelBackground", "rgba(232,232,232,0.8)"); break;
            case "dark": Set("border2", "rgba(255,255,255,0.25)"); Set("titleColor", "#F9FFFE"); Set("labelBackground", "#181818"); break;
            case "forest": Set("titleColor", "#333"); Set("edgeLabelBackground", "#e8e8e8"); break;
            case "neutral": Set("edgeLabelBackground", "white"); break;
            case "neo": Set("nodeBorder", "#000000"); break;
            case "neo-dark": Set("border2", "rgba(255,255,255,0.25)"); Set("labelBackground", "#181818"); break;
            case "redux": case "redux-color":
                Set("nodeBorder", "#28253D"); Set("clusterBkg", "#F9F9FB"); Set("clusterBorder", "#BDBCCC"); break;
            case "redux-dark": case "redux-dark-color":
                Set("border2", "rgba(255,255,255,0.25)"); Set("labelBackground", "#111113");
                Set("nodeBorder", "#FFFFFF"); Set("clusterBkg", "#1E1A2E"); Set("clusterBorder", "#BDBCCC"); break;
        }
    }

    // Exact finite role assignments from the pinned named constructors/updateColors.
    // Create replays authored literals after this owner, matching calculate.
    private static void DeriveTopologyRoles(string name, Dictionary<string, Precise> colors, bool darkMode) {
        Precise Get(string key) => colors[key];
        void Copy(string target, string source) => colors[target] = Get(source);
        void Fallback(string target, Precise value) { if (!colors.ContainsKey(target)) colors.Add(target, value); }
        if (name is "default" or "dark" or "forest" or "neutral") {
            Copy("nodeBorder", "border1"); Copy("clusterBkg", "secondBkg"); Copy("clusterBorder", "border2"); Copy("defaultLinkColor", "lineColor");
            if (name == "default") { Copy("titleColor", "textColor"); Copy("edgeLabelBackground", "labelBackground"); }
            else if (name == "dark") colors["edgeLabelBackground"] = Get("labelBackground").AdjustHsl(lightness: 25);
            else if (name == "neutral") Copy("titleColor", "text");
            // Legacy CSS falls back to textColor when no nodeTextColor was authored.
            return;
        }
        Fallback("nodeBorder", Get(name == "neo-dark" ? "border1" : "primaryBorderColor"));
        Fallback("clusterBkg", Get("tertiaryColor")); Fallback("clusterBorder", Get("tertiaryBorderColor"));
        Fallback("defaultLinkColor", Get("lineColor")); Fallback("titleColor", Get("tertiaryTextColor"));
        Fallback("edgeLabelBackground", darkMode ? Get("secondaryColor").AdjustHsl(lightness: -30) : Get("secondaryColor"));
        Fallback("nodeTextColor", Get("primaryTextColor"));
    }
}
