using System;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterFilterPlan {
    /// <summary>Expands standard convenience operations to the same20-coefficient owner before any pixel capture.</summary>
    private static bool TryColorMatrix(SvgRasterElement node, out double[] values) {
        values = new double[] { 1,0,0,0,0, 0,1,0,0,0, 0,0,1,0,0, 0,0,0,1,0 };
        var text = node.Get("values");
        switch (node.Get("type") ?? "matrix") {
            case "matrix":
                if (text != null && (!ColorNumbers(text, 20, out values) || values.Length != 20)) return false;
                break;
            case "saturate":
                if (!ColorNumber(node, "values", 1, out var saturation) || saturation < 0 || saturation > 1) return false;
                values = new[] { .213+.787*saturation,.715-.715*saturation,.072-.072*saturation,0,0,
                    .213-.213*saturation,.715+.285*saturation,.072-.072*saturation,0,0,
                    .213-.213*saturation,.715-.715*saturation,.072+.928*saturation,0,0, 0,0,0,1,0 };
                break;
            case "hueRotate":
                if (!ColorNumber(node, "values", 0, out var degrees)) return false;
                var radians = (degrees % 360) * (Math.PI / 180); var c = Math.Cos(radians); var s = Math.Sin(radians);
                values = new[] { .213+.787*c-.213*s,.715-.715*c-.715*s,.072-.072*c+.928*s,0,0,
                    .213-.213*c+.143*s,.715+.285*c+.140*s,.072-.072*c-.283*s,0,0,
                    .213-.213*c-.787*s,.715-.715*c+.715*s,.072+.928*c+.072*s,0,0, 0,0,0,1,0 };
                break;
            case "luminanceToAlpha":
                values = new double[] { 0,0,0,0,0, 0,0,0,0,0, 0,0,0,0,0, .2125,.7154,.0721,0,0 }; break;
            default: return false;
        }
        for (var row = 0; row < 4; row++) {
            double magnitude = 0;
            for (var column = 0; column < 5; column++) magnitude += Math.Abs(values[row * 5 + column]);
            if (!Finite(magnitude)) return false;
        }
        return true;
    }
}
