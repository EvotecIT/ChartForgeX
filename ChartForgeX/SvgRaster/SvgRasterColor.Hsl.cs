using System;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterColor {
    // CSS hue angles and percentage channels share one bounded scalar/grammar owner for native paint and precise derivation inputs.
    private static bool TryParseHsl(string value,out ChartColor color) {
        color=default;if(!TryReadHsl(value,out var hue,out var saturation,out var lightness,out var alpha))return false;
        color=ChartForgeX.Rendering.ChartColorMath.PreciseColor.FromNormalizedHsl(hue,saturation,lightness,alpha).ToColor();return true;
    }
    private static bool TryReadHsl(string value,out double hue,out double saturation,out double lightness,out double alpha) {
        hue=saturation=lightness=0;alpha=1;var open=value.IndexOf('(');if(open<=0||value[value.Length-1]!=')')return false;var function=value.Substring(0,open).Trim();
        if(!function.Equals("hsl",StringComparison.OrdinalIgnoreCase)&&!function.Equals("hsla",StringComparison.OrdinalIgnoreCase))return false;
        var parts=SplitFunctionArguments(value.Substring(open+1,value.Length-open-2));if(parts.Count<3||parts.Count>4||!Hue(parts[0],out hue)||!Percentage(parts[1],out saturation)||!Percentage(parts[2],out lightness))return false;
        if(parts.Count==4){var text=parts[3].Trim();var percent=text.EndsWith("%",StringComparison.Ordinal);if(!Finite(percent?text.Substring(0,text.Length-1):text,out var opacity))return false;alpha=Math.Max(0,Math.Min(1,percent?opacity/100:opacity));}
        return true;
    }

    private static bool Hue(string value, out double hue) {
        var text = value.Trim();
        double factor = 1;
        foreach (var suffix in new[] { "grad", "turn", "deg", "rad" }) {
            if (!text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
            text = text.Substring(0, text.Length - suffix.Length);
            factor = suffix == "grad" ? .9 : suffix == "turn" ? 360 : suffix == "rad" ? 180 / Math.PI : 1;
            break;
        }
        if (!Finite(text, out hue)) return false;
        // Reduce before multiplication so finite CSS angles cannot overflow.
        hue = (hue % (360 / factor) * factor % 360 + 360) % 360;
        return true;
    }

    private static bool Percentage(string value, out double number) {
        number = 0;
        var text = value.Trim();
        if (!text.EndsWith("%", StringComparison.Ordinal) || !Finite(text.Substring(0, text.Length - 1), out number)) return false;
        number = Math.Max(0, Math.Min(1, number / 100));
        return true;
    }

    private static bool Finite(string value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && !double.IsNaN(number) && !double.IsInfinity(number);
}
