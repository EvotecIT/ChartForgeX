using System;
using ChartForgeX.Core;

namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    private static VisualTheme FromVisualContract(string json, VisualTypography? typography) {
        // The palette parser owns required colours and all input budgets before optional presentation is read.
        var light = VisualDesignTokens.FromJson(json, VisualThemeMode.Light);
        var dark = VisualDesignTokens.FromJson(json, VisualThemeMode.Dark);
        var root = GeoJsonValue.Parse(json, StringComparer.Ordinal, ThemeJsonLimits).AsObject("visual tokens");
        if (root.TryGetValue("contractVersion", out var version) && version.AsNumber("contractVersion") != 2)
            throw new ArgumentException("Only visual token contractVersion 2 is supported.", nameof(json));
        var radius = root.TryGetValue("radius", out var value) ? value.AsObject("radius") : null;
        return new VisualTheme(light, dark, typography ?? ReadTypography(root),
            barRadius: VisualDesignTokens.ReadRadius(radius, "sm", 4),
            cardRadius: VisualDesignTokens.ReadRadius(radius, "container", DefaultCardRadius),
            plotRadius: VisualDesignTokens.ReadRadius(radius, "control", 3));
    }

    private static VisualTypography? ReadTypography(System.Collections.Generic.Dictionary<string, GeoJsonValue> root) {
        if (!root.TryGetValue("typography", out var value)) return null;
        var type = value.AsObject("typography");
        var family = Required(type, "family", "typography").AsObject("typography.family");
        var ramp = Required(type, "ramp", "typography").AsObject("typography.ramp");
        var reference = OptionalNumber(type, "referencePixels", "typography", 16);
        double Size(string name) {
            var step = Required(ramp, name, "typography.ramp").AsObject(name);
            var size = Required(step, "size", name).AsString(name + ".size");
            var rem = size.EndsWith("rem", StringComparison.Ordinal);
            var suffix = rem ? 3 : size.EndsWith("px", StringComparison.Ordinal) ? 2 : 0;
            if (suffix == 0 || !double.TryParse(size.Substring(0, size.Length - suffix), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var number))
                throw new ArgumentException("Typography sizes must use rem or px units.");
            return number * (rem ? reference : 1);
        }
        return new VisualTypography(Required(family, "sans", "typography.family").AsString("sans"),
            titleSize: Size("subtitle"), subtitleSize: Size("small"), axisSize: Size("caption"),
            legendSize: Size("caption"), dataLabelSize: Size("caption"),
            centerValueSize: Size("title"), monospaceFamily: Required(family, "mono", "typography.family").AsString("mono"),
            tileLabelSize: Size("body"), tileDetailSize: Size("small"));
    }
}
