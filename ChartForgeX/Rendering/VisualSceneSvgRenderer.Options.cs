using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static string ResolvePaint(ChartColor? color, SvgPaint? paint, VisualSvgOptions? options) {
        if (!color.HasValue) return "none";
        // Unbound paints stay literal. A renderer must retain provenance explicitly instead of
        // guessing whether an equal RGB value is a theme token, derived contrast ink or highlight.
        if (options?.Variables == null || !paint.HasValue || paint.Value.Value == null) return color.Value.ToCss();
        return SvgPaint.Resolve(paint.Value.Value, options.Variables);
    }

    private static void PaintIdentity(BinaryWriter writer, VisualScenePaintBinding? paint) {
        Text(writer, paint?.Fill?.Value); Text(writer, paint?.Stroke?.Value);
    }

    private static string ExportIdentity(VisualScene scene, string identity, VisualSvgOptions? options) {
        if (options == null || options.Variables == null && options.LinkTarget == VisualSvgLinkTarget.SameContext) return identity;
        using var hash = SHA256.Create();
        using var stream = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true)) {
            writer.Write(identity); writer.Write((int)options.LinkTarget);
            foreach (var node in scene.Nodes) {
                if (node is VisualSceneMark mark) Mark(mark);
                else if (node is VisualSceneText text) writer.Write(ResolvePaint(text.Color, text.Paint, options));
                else if (node is VisualSceneGradient gradient) {
                    Mark(gradient.Shape);
                    foreach (var stop in gradient.Stops) writer.Write(ResolvePaint(stop.Color, stop.Paint, options));
                }
            }
            void Mark(VisualSceneMark mark) {
                writer.Write(ResolvePaint(mark.Fill, mark.Paint?.Fill, options));
                writer.Write(ResolvePaint(mark.Stroke, mark.Paint?.Stroke, options));
            }
        }
        stream.FlushFinalBlock();
        var prefix = new StringBuilder("cfx-v2-");
        foreach (var value in hash.Hash!) prefix.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return prefix.ToString();
    }
}
