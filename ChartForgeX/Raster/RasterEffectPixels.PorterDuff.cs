using ChartForgeX.Effects;

namespace ChartForgeX.Raster;

internal static partial class RasterEffectPixels {
    /// <summary>Applies one explicit alpha operator through the existing premultiplied16 conversion and final quantizer.</summary>
    internal static void Composite(byte[] target, int output, byte sr, byte sg, byte sb, byte sa8,
        byte dr, byte dg, byte db, byte da8, VisualCompositeOperator operation, int x, int y, bool dither) {
        // Keep the incumbent explicit Normal operation bit-for-bit, including its single final quantization.
        if (operation == VisualCompositeOperator.SourceOver) {
            Composite(target, output, sr, sg, sb, sa8, dr, dg, db, da8, VisualBlendMode.Normal, x, y, dither);
            return;
        }
        if (operation == VisualCompositeOperator.Copy) {
            target[output] = sr; target[output + 1] = sg; target[output + 2] = sb; target[output + 3] = sa8;
            return;
        }
        if (operation == VisualCompositeOperator.Clear) return; // Every planned result starts transparent black.
        var sa = sa8 * 257;
        var da = da8 * 257;
        CompositePremultiplied(target, output, Premultiply(sr, sa), Premultiply(sg, sa), Premultiply(sb, sa), sa,
            Premultiply(dr, da), Premultiply(dg, da), Premultiply(db, da), da, operation, x, y, dither);
    }

    /// <summary>Shares the existing Porter-Duff factors with finite effects that retain 16-bit coverage until final output.</summary>
    internal static void CompositePremultiplied(byte[] target, int output, int sr, int sg, int sb, int sa,
        int dr, int dg, int db, int da, VisualCompositeOperator operation, int x, int y, bool dither) {
        int sourceFactor, destinationFactor;
        switch (operation) {
            case VisualCompositeOperator.SourceIn: sourceFactor = da; destinationFactor = 0; break;
            case VisualCompositeOperator.SourceOut: sourceFactor = 65535 - da; destinationFactor = 0; break;
            case VisualCompositeOperator.SourceAtop: sourceFactor = da; destinationFactor = 65535 - sa; break;
            case VisualCompositeOperator.DestinationOver: sourceFactor = 65535 - da; destinationFactor = 65535; break;
            case VisualCompositeOperator.DestinationIn: sourceFactor = 0; destinationFactor = sa; break;
            case VisualCompositeOperator.DestinationOut: sourceFactor = 0; destinationFactor = 65535 - sa; break;
            case VisualCompositeOperator.DestinationAtop: sourceFactor = 65535 - da; destinationFactor = sa; break;
            default: sourceFactor = 65535 - da; destinationFactor = 65535 - sa; break; // Validated Xor.
        }
        int Channel(int source, int destination) =>
            Multiply(source, sourceFactor) + Multiply(destination, destinationFactor);
        var alpha = Multiply(sa, sourceFactor) + Multiply(da, destinationFactor);
        Write(target, output, Channel(sr, dr), Channel(sg, dg), Channel(sb, db), alpha, x, y, dither);
    }
}
