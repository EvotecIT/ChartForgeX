using System.Collections.Generic;
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
        if (!paint.HasValue || paint.Value.Value == null) return color.Value.ToCss();
        if (!paint.Value.IsRaw) {
            if (SvgPaint.TryCssVariable(paint.Value.Value, color.Value, out var fallback, out var authored) && fallback.Equals(color.Value))
                return SvgPaint.Resolve(authored.Value!, options?.Variables);
            return options?.Variables != null && options.Variables.TryPaint(color.Value, SvgColorRole.Any, out var mapped) ? mapped : color.Value.ToCss();
        }
        if (options?.Variables == null && !paint.Value.HasCssVariable) return color.Value.ToCss();
        return SvgPaint.Resolve(paint.Value.Value, options?.Variables);
    }

    private static void PaintIdentity(BinaryWriter writer, VisualScenePaintBinding? paint) {
        Text(writer, paint?.Fill?.Value); Text(writer, paint?.Stroke?.Value);
    }

    private static string ExportIdentity(VisualScene scene, string identity, VisualSvgOptions? options) {
        var colorScopes = ColorScopeOptions(scene, options);
        var effectOptions = (scene.HasSurfaceEffects || scene.HasRetainedEffects || scene.HasProceduralSources) && options != null &&
            (options.EffectProfile != VisualSvgEffectProfile.Baked || options.EffectRasterOptions != null);
        var textRasterOptions = options != null && options.TextPresentation == VisualSvgTextPresentation.Automatic && options.EffectRasterOptions != null;
        if (colorScopes == null && (options == null || options.Variables == null &&
            options.LinkTarget == VisualSvgLinkTarget.SameContext && !options.Responsive &&
            !options.PatternTiles && !options.Explorable && !effectOptions && !textRasterOptions)) return identity;
        using var hash = SHA256.Create();
        using var stream = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true)) {
            writer.Write(identity);
            if (scene.HasSurfaceEffects || scene.HasRetainedEffects || scene.HasProceduralSources) {
                writer.Write((int)(options?.EffectProfile ?? VisualSvgEffectProfile.Baked));
                if (options?.EffectProfile != VisualSvgEffectProfile.NativeFilters && options?.EffectRasterOptions is { } raster) {
                    writer.Write(raster.Scale); writer.Write(raster.Supersampling);
                }
            }
            if (textRasterOptions) {
                var raster = options!.EffectRasterOptions!;
                writer.Write("native-text-raster"); writer.Write(raster.Scale); writer.Write(raster.Supersampling);
                writer.Write(raster.TextHinting.HasValue ? (int)raster.TextHinting.Value : -1);
            }
            writer.Write((int)(options?.LinkTarget ?? VisualSvgLinkTarget.SameContext));
            if (options?.Responsive == true) writer.Write("responsive");
            if (options?.PatternTiles == true) writer.Write("pattern-tiles");
            if (options?.Explorable == true) writer.Write("explorable");
            for (var i = 0; i < scene.Nodes.Count; i++) {
                var node = scene.Nodes[i];
                var paintOptions = colorScopes == null ? options : colorScopes[i];
                if (node is VisualSceneMark mark) Mark(mark);
                else if (node is VisualSceneText text) {
                    writer.Write(ResolvePaint(text.Color, text.Paint, paintOptions));
                    writer.Write(ResolvePaint(text.Stroke, text.StrokePaint, paintOptions));
                }
                else if (node is VisualSceneGradient gradient) {
                    Mark(gradient.Shape);
                    foreach (var stop in gradient.Stops) writer.Write(ResolvePaint(stop.Color, stop.Paint, paintOptions));
                }
                void Mark(VisualSceneMark mark) {
                    writer.Write(ResolvePaint(mark.Fill, mark.Paint?.Fill, paintOptions));
                    writer.Write(ResolvePaint(mark.Stroke, mark.Paint?.Stroke, paintOptions));
                }
            }
        }
        stream.FlushFinalBlock();
        var prefix = new StringBuilder("cfx-v2-");
        foreach (var value in hash.Hash!) prefix.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return prefix.ToString();
    }

    // Scope resolution is shared by paint attributes, gradient definitions and deterministic export identity.
    // Ordinary scenes keep the existing allocation-free path. Scope mappings were detached during preparation.
    private static VisualSvgOptions?[]? ColorScopeOptions(VisualScene scene, VisualSvgOptions? options) {
        var hasScope = false;
        foreach (var node in scene.Nodes) if (node is VisualSceneGroup group && group.ColorScope != null) { hasScope = true; break; }
        if (!hasScope) return null;
        var result = new VisualSvgOptions?[scene.Nodes.Count];
        var stack = new Stack<VisualSvgOptions?>();
        var current = options;
        for (var i = 0; i < scene.Nodes.Count; i++) {
            if (scene.Nodes[i] is VisualSceneGroup group) {
                stack.Push(current);
                if (group.ColorScope != null) current = new VisualSvgOptions(colorVariables: group.ColorScope.Variables,
                    linkTarget: options?.LinkTarget ?? VisualSvgLinkTarget.SameContext,
                    effectProfile: options?.EffectProfile ?? VisualSvgEffectProfile.Baked, effectRasterOptions: options?.EffectRasterOptions)
                    .WithTextPresentation(options?.TextPresentation ?? VisualSvgTextPresentation.Browser);
            }
            result[i] = current;
            if (scene.Nodes[i] is VisualSceneEndGroup) current = stack.Pop();
        }
        return result;
    }
}
