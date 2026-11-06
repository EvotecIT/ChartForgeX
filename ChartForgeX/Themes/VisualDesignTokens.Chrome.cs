using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Themes;

public sealed partial class VisualDesignTokens {
    private static VisualChromeTokens? ReadChrome(Dictionary<string, GeoJsonValue> root, Dictionary<string, GeoJsonValue> chrome, string path) {
        if (!chrome.ContainsKey("tooltipSurface")) return null;
        if (!chrome.TryGetValue("tooltipShadow", out var shadow)) throw new ArgumentException("Design tokens require '" + path + ".tooltipShadow'.");
        var radius = OptionalMember(root, "radius", "radius");
        double Radius(string key, string serializedKey, double fallback) => chrome.TryGetValue(serializedKey, out var value)
            ? value.AsNumber(path + "." + serializedKey) : ReadRadius(radius, key, fallback);
        return new VisualChromeTokens(Color(chrome, "tooltipSurface", path), Color(chrome, "tooltipText", path),
            Color(chrome, "tooltipMuted", path), Color(chrome, "tooltipLine", path), Color(chrome, "focusRing", path),
            shadow.AsString(path + ".tooltipShadow"), Radius("overlay", "tooltipRadius", 6), Radius("control", "controlRadius", 4));
    }

    internal static double ReadRadius(Dictionary<string, GeoJsonValue>? radius, string key, double fallback) {
        if (radius == null || !radius.TryGetValue(key, out var value)) return fallback;
        var text = value.AsString("radius." + key);
        if (text.EndsWith("px", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 2);
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            || double.IsNaN(number) || double.IsInfinity(number) || number < 0)
            throw new ArgumentException("Design token radius." + key + " requires a non-negative pixel length.");
        return number;
    }
}
