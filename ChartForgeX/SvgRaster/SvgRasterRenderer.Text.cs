using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private const long MaximumTextIntermediatePixels = 8_000_000;

    private static void RenderText(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, IReadOnlyList<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        var cursorX = HorizontalLength(element, "x", viewport) + HorizontalLength(element, "dx", viewport);
        var cursorY = VerticalLength(element, "y", viewport) + VerticalLength(element, "dy", viewport);
        var textAncestors = new List<SvgRasterElement>(ancestors) { element };
        var whitespace = new TextWhitespaceState { LineStartX = cursorX };
        var measureWhitespace = whitespace;
        cursorX += TextAnchorOffset(style.TextAnchor, MeasureTextChunkFrom(element, 0, style, definitions.StyleSheet, textAncestors, viewport, ref measureWhitespace, includeFirstPositionedSpan: false));
        whitespace.LineStartX = cursorX;
        RenderTextContent(canvas, element, style, matrix, definitions, width, height, textAncestors, viewport, ref cursorX, ref cursorY, ref whitespace);
    }

    private static void RenderTextContent(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, List<SvgRasterElement> ancestors, SvgRasterViewport viewport, ref double cursorX, ref double cursorY, ref TextWhitespaceState whitespace) {
        for (var contentIndex = 0; contentIndex < element.Content.Count; contentIndex++) {
            var content = element.Content[contentIndex];
            if (content.Text != null) {
                RenderTextValue(canvas, content.Text, style, matrix, ref cursorX, ref cursorY, ref whitespace);
                continue;
            }

            var span = content.Element;
            if (span == null || !string.Equals(span.Name, "tspan", StringComparison.Ordinal)) continue;
            var spanStyle = SvgRasterStyle.Resolve(style, span, definitions.StyleSheet, ancestors);
            if (!spanStyle.Displayed) continue;
            var positioned = span.TryGet("x", out _) || span.TryGet("y", out _);
            if (span.TryGet("x", out _)) {
                cursorX = HorizontalLength(span, "x", viewport);
                whitespace.LineStartX = cursorX;
            }
            if (span.TryGet("y", out _)) cursorY = VerticalLength(span, "y", viewport);
            cursorX += HorizontalLength(span, "dx", viewport);
            cursorY += VerticalLength(span, "dy", viewport);
            if (positioned) {
                var measureWhitespace = whitespace;
                cursorX += TextAnchorOffset(spanStyle.TextAnchor, MeasureTextChunkFrom(element, contentIndex, style, definitions.StyleSheet, ancestors, viewport, ref measureWhitespace, includeFirstPositionedSpan: true));
            }
            var spanMatrix = matrix.Multiply(SvgRasterMatrix.ParseTransform(span.Get("transform")));
            ancestors.Add(span);
            RenderTextSpan(canvas, span, spanStyle, spanMatrix, definitions, width, height, ancestors, viewport, ref cursorX, ref cursorY, ref whitespace);
            ancestors.RemoveAt(ancestors.Count - 1);
        }
    }

    private static void RenderTextSpan(RgbaCanvas canvas, SvgRasterElement span, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, List<SvgRasterElement> ancestors, SvgRasterViewport viewport, ref double cursorX, ref double cursorY, ref TextWhitespaceState whitespace) {
        var hasClipPath = definitions.TryGetClipPath(ParseReference(style.ClipPath) ?? ReferenceId(span, "clip-path"), out var clipPath);
        var hasMask = definitions.TryGetMask(ReferenceId(span, "mask"), out var maskDefinition);
        var compositeOpacity = span.Children.Count > 0 && style.Opacity < 0.999;
        if (!hasClipPath && !hasMask && !compositeOpacity) {
            RenderTextContent(canvas, span, style, matrix, definitions, width, height, ancestors, viewport, ref cursorX, ref cursorY, ref whitespace);
            return;
        }

        var content = new RgbaCanvas(width, height, 1);
        var contentStyle = compositeOpacity ? style.Inherit() : style;
        RenderTextContent(content, span, contentStyle, matrix, definitions, width, height, ancestors, viewport, ref cursorX, ref cursorY, ref whitespace);
        if (hasClipPath) {
            var clipMask = new RgbaCanvas(width, height, 1);
            RenderClipPath(clipMask, clipPath, matrix, definitions, width, height, content.Pixels, viewport, span, style, ancestors);
            var clipped = new RgbaCanvas(width, height, 1);
            clipped.DrawImageMasked(0, 0, width, height, content.Pixels, clipMask.Pixels);
            content = clipped;
        }
        if (hasMask) {
            var mask = new RgbaCanvas(width, height, 1);
            RenderMask(mask, maskDefinition, matrix, definitions, width, height, content.Pixels, viewport, span, style, ancestors);
            var masked = new RgbaCanvas(width, height, 1);
            masked.DrawImageMasked(0, 0, width, height, content.Pixels, mask.Pixels, maskDefinition.UsesAlpha);
            content = masked;
        }
        canvas.DrawImage(0, 0, width, height, compositeOpacity ? ApplyOpacity(content.Pixels, style.Opacity) : content.Pixels);
    }

    private static void RenderTextValue(RgbaCanvas canvas, string value, SvgRasterStyle style, SvgRasterMatrix matrix, ref double cursorX, ref double cursorY, ref TextWhitespaceState whitespace) {
        var text = NormalizeTextWhitespace(value, style.WhiteSpace, ref whitespace);
        var start = 0;
        while (start <= text.Length) {
            var newline = text.IndexOf('\n', start);
            var length = newline < 0 ? text.Length - start : newline - start;
            if (length > 0) cursorX += DrawTextRun(canvas, text.Substring(start, length), cursorX, cursorY, style, matrix);
            if (newline < 0) break;
            cursorX = whitespace.LineStartX;
            cursorY += style.FontSize * 1.2;
            start = newline + 1;
        }
    }

    private static double MeasureTextChunkFrom(SvgRasterElement element, int startIndex, SvgRasterStyle style, SvgRasterStyleSheet styleSheet, IReadOnlyList<SvgRasterElement> ancestors, SvgRasterViewport viewport, ref TextWhitespaceState whitespace, bool includeFirstPositionedSpan) {
        var advance = 0.0;
        for (var contentIndex = startIndex; contentIndex < element.Content.Count; contentIndex++) {
            var content = element.Content[contentIndex];
            if (content.Text != null) {
                var text = NormalizeTextWhitespace(content.Text, style.WhiteSpace, ref whitespace);
                var newline = text.IndexOf('\n');
                if (newline >= 0) text = text.Substring(0, newline);
                advance += MeasureTextAdvance(text, style);
                if (newline >= 0) break;
                continue;
            }
            var span = content.Element;
            if (span == null || !string.Equals(span.Name, "tspan", StringComparison.Ordinal)) continue;
            var positioned = span.TryGet("x", out _) || span.TryGet("y", out _);
            if (positioned && !(includeFirstPositionedSpan && contentIndex == startIndex)) break;
            var spanStyle = SvgRasterStyle.Resolve(style, span, styleSheet, ancestors);
            if (!spanStyle.Displayed) continue;
            if (!(includeFirstPositionedSpan && contentIndex == startIndex)) advance += HorizontalLength(span, "dx", viewport);
            var spanAncestors = new List<SvgRasterElement>(ancestors) { span };
            advance += MeasureTextChunkFrom(span, 0, spanStyle, styleSheet, spanAncestors, viewport, ref whitespace, includeFirstPositionedSpan: false);
        }
        return advance;
    }

    private static double MeasureTextAdvance(string text, SvgRasterStyle style) {
        if (text.Length == 0) return 0;
        return IsBold(style.FontWeight) ? RgbaCanvas.MeasureTextEmphasizedWidth(text, style.FontSize, null) : RgbaCanvas.MeasureTextWidth(text, style.FontSize, null);
    }

    private static double TextAnchorOffset(string anchor, double width) {
        if (string.Equals(anchor, "middle", StringComparison.OrdinalIgnoreCase)) return -width / 2.0;
        if (string.Equals(anchor, "end", StringComparison.OrdinalIgnoreCase)) return -width;
        return 0;
    }

    private static double DrawTextRun(RgbaCanvas canvas, string text, double x, double y, SvgRasterStyle style, SvgRasterMatrix matrix) {
        if (text.Length == 0) return 0;
        var renderScale = ResolveTextRenderScale(canvas, text, style, matrix.ScaleFactor);
        var fontSize = Math.Max(1, style.FontSize * renderScale);
        var emphasized = IsBold(style.FontWeight);
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, null) : RgbaCanvas.MeasureTextWidth(text, fontSize, null);
        var advance = width / renderScale;
        if (!style.VisibilityVisible) return advance;
        var color = style.FillColor();
        if (color.A == 0) return advance;

        var drawX = x;
        var drawY = TextTop(y, style.FontSize, style.DominantBaseline);
        var padding = Math.Max(2, (int)Math.Ceiling(fontSize * 0.2));
        var textHeight = Math.Max(1, RgbaCanvas.MeasureTextHeight(fontSize, null));
        var localWidth = Math.Max(1, (int)Math.Ceiling(width + padding * 2.0));
        var localHeight = Math.Max(1, (int)Math.Ceiling(textHeight + padding * 2.0));
        var buffer = new RgbaCanvas(localWidth, localHeight, 1);
        if (emphasized) buffer.DrawTextEmphasized(padding, padding, text, color, fontSize);
        else buffer.DrawText(padding, padding, text, color, fontSize);

        var textMatrix = matrix
            .Multiply(SvgRasterMatrix.Translate(drawX - padding / renderScale, drawY - padding / renderScale))
            .Multiply(SvgRasterMatrix.Scale(1 / renderScale, 1 / renderScale));
        canvas.DrawImageTransformed(localWidth, localHeight, buffer.Pixels, textMatrix.A, textMatrix.B, textMatrix.C, textMatrix.D, textMatrix.E, textMatrix.F);
        return advance;
    }

    private static double ResolveTextRenderScale(RgbaCanvas canvas, string text, SvgRasterStyle style, double requestedScale) {
        var scale = Math.Max(0.000001, requestedScale);
        for (var attempt = 0; attempt < 2; attempt++) {
            var fontSize = Math.Max(1, style.FontSize * scale);
            var width = Math.Max(1, IsBold(style.FontWeight) ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, null) : RgbaCanvas.MeasureTextWidth(text, fontSize, null));
            var height = Math.Max(1, RgbaCanvas.MeasureTextHeight(fontSize, null));
            var padding = Math.Max(2, Math.Ceiling(fontSize * 0.2));
            var pixels = (width + padding * 2) * (height + padding * 2);
            var axisLimit = Math.Max(1024, Math.Min(32768, Math.Max(canvas.Width, canvas.Height) * 2));
            var reduction = Math.Min(1, Math.Min(axisLimit / (width + padding * 2), axisLimit / (height + padding * 2)));
            if (pixels > MaximumTextIntermediatePixels) reduction = Math.Min(reduction, Math.Sqrt(MaximumTextIntermediatePixels / pixels));
            if (reduction >= 0.999) break;
            scale = Math.Max(0.000001, scale * reduction);
        }
        return scale;
    }

    private static string NormalizeTextWhitespace(string value, string whiteSpace, ref TextWhitespaceState state) {
        if (value.Length == 0) return string.Empty;
        if (string.Equals(whiteSpace, "pre", StringComparison.OrdinalIgnoreCase) || string.Equals(whiteSpace, "pre-wrap", StringComparison.OrdinalIgnoreCase) || string.Equals(whiteSpace, "break-spaces", StringComparison.OrdinalIgnoreCase)) {
            if (value.Length > 0) {
                state.HasText = true;
                state.EndsWithSpace = char.IsWhiteSpace(value[value.Length - 1]) && value[value.Length - 1] != '\n';
            }
            return value.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\t', ' ');
        }
        if (string.Equals(whiteSpace, "pre-line", StringComparison.OrdinalIgnoreCase)) {
            var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split('\n');
            var preservedLines = new StringBuilder(normalized.Length);
            for (var index = 0; index < lines.Length; index++) {
                if (index > 0) {
                    preservedLines.Append('\n');
                    state.HasText = false;
                    state.EndsWithSpace = false;
                }
                preservedLines.Append(CollapseNormalWhitespace(lines[index], ref state));
            }
            return preservedLines.ToString();
        }
        return CollapseNormalWhitespace(value, ref state);
    }

    private static string CollapseNormalWhitespace(string value, ref TextWhitespaceState state) {
        var result = new StringBuilder(value.Length);
        var whitespace = false;
        foreach (var character in value) {
            if (char.IsWhiteSpace(character)) {
                whitespace = true;
                continue;
            }
            if (whitespace && state.HasText && !state.EndsWithSpace) result.Append(' ');
            result.Append(character);
            state.HasText = true;
            state.EndsWithSpace = false;
            whitespace = false;
        }
        if (whitespace && state.HasText && !state.EndsWithSpace) {
            result.Append(' ');
            state.EndsWithSpace = true;
        } else if (result.Length > 0) {
            state.EndsWithSpace = result[result.Length - 1] == ' ';
        }
        return result.ToString();
    }

    private static double TextTop(double y, double fontSize, string baseline) {
        if (string.Equals(baseline, "middle", StringComparison.OrdinalIgnoreCase) || string.Equals(baseline, "central", StringComparison.OrdinalIgnoreCase)) return y - fontSize * 0.5;
        if (string.Equals(baseline, "hanging", StringComparison.OrdinalIgnoreCase) || string.Equals(baseline, "text-before-edge", StringComparison.OrdinalIgnoreCase)) return y;
        if (string.Equals(baseline, "text-after-edge", StringComparison.OrdinalIgnoreCase) || string.Equals(baseline, "ideographic", StringComparison.OrdinalIgnoreCase)) return y - fontSize;
        return y - fontSize * 0.82;
    }

    private struct TextWhitespaceState {
        public bool HasText;
        public bool EndsWithSpace;
        public double LineStartX;
    }
}
