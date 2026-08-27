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
        var paintBounds = new SvgRasterTextPaintBounds(matrix);
        var boundsCursorX = cursorX;
        var boundsCursorY = cursorY;
        var boundsWhitespace = whitespace;
        RenderTextContent(null, element, style, matrix, definitions, width, height, textAncestors, viewport, paintBounds, measureOnly: true, ref boundsCursorX, ref boundsCursorY, ref boundsWhitespace);
        RenderTextContent(canvas, element, style, matrix, definitions, width, height, textAncestors, viewport, paintBounds, measureOnly: false, ref cursorX, ref cursorY, ref whitespace);
    }

    private static void RenderTextContent(RgbaCanvas? canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, List<SvgRasterElement> ancestors, SvgRasterViewport viewport, SvgRasterTextPaintBounds paintBounds, bool measureOnly, ref double cursorX, ref double cursorY, ref TextWhitespaceState whitespace) {
        for (var contentIndex = 0; contentIndex < element.Content.Count; contentIndex++) {
            var content = element.Content[contentIndex];
            if (content.Text != null) {
                RenderTextValue(canvas, content.Text, style, matrix, definitions, viewport, paintBounds, measureOnly, ref cursorX, ref cursorY, ref whitespace);
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
            RenderTextSpan(canvas, span, spanStyle, spanMatrix, definitions, width, height, ancestors, viewport, paintBounds, measureOnly, ref cursorX, ref cursorY, ref whitespace);
            ancestors.RemoveAt(ancestors.Count - 1);
        }
    }

    private static void RenderTextSpan(RgbaCanvas? canvas, SvgRasterElement span, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, List<SvgRasterElement> ancestors, SvgRasterViewport viewport, SvgRasterTextPaintBounds paintBounds, bool measureOnly, ref double cursorX, ref double cursorY, ref TextWhitespaceState whitespace) {
        if (measureOnly) {
            RenderTextContent(null, span, style, matrix, definitions, width, height, ancestors, viewport, paintBounds, measureOnly: true, ref cursorX, ref cursorY, ref whitespace);
            return;
        }
        var hasClipPath = definitions.TryGetClipPath(ParseReference(style.ClipPath) ?? ReferenceId(span, "clip-path"), out var clipPath);
        var hasMask = definitions.TryGetMask(ReferenceId(span, "mask"), out var maskDefinition);
        var compositeOpacity = style.Opacity < 0.999 && (span.Children.Count > 0 || HasVisibleTextFillAndStroke(style));
        if (!hasClipPath && !hasMask && !compositeOpacity) {
            RenderTextContent(canvas, span, style, matrix, definitions, width, height, ancestors, viewport, paintBounds, measureOnly: false, ref cursorX, ref cursorY, ref whitespace);
            return;
        }

        var content = new RgbaCanvas(width, height, 1);
        var contentStyle = compositeOpacity ? style.Inherit() : style;
        RenderTextContent(content, span, contentStyle, matrix, definitions, width, height, ancestors, viewport, paintBounds, measureOnly: false, ref cursorX, ref cursorY, ref whitespace);
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
        canvas!.DrawImage(0, 0, width, height, compositeOpacity ? ApplyOpacity(content.Pixels, style.Opacity) : content.Pixels);
    }

    private static void RenderTextValue(RgbaCanvas? canvas, string value, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport, SvgRasterTextPaintBounds paintBounds, bool measureOnly, ref double cursorX, ref double cursorY, ref TextWhitespaceState whitespace) {
        var text = NormalizeTextWhitespace(value, style.WhiteSpace, ref whitespace);
        var start = 0;
        while (start <= text.Length) {
            var newline = text.IndexOf('\n', start);
            var length = newline < 0 ? text.Length - start : newline - start;
            if (length > 0) cursorX += DrawTextRun(canvas, text.Substring(start, length), cursorX, cursorY, style, matrix, definitions, viewport, paintBounds, measureOnly);
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
        var font = SvgTextFont(style);
        return IsBold(style.FontWeight)
            ? RgbaCanvas.MeasureTextEmphasizedWidth(text, style.FontSize, font, italic: false)
            : RgbaCanvas.MeasureTextWidth(text, style.FontSize, font, italic: false);
    }

    private static double MeasureTextPaintWidth(string text, SvgRasterStyle style) {
        if (text.Length == 0) return 0;
        var font = SvgTextFont(style);
        var italic = IsItalic(style.FontStyle);
        return IsBold(style.FontWeight)
            ? RgbaCanvas.MeasureTextEmphasizedWidth(text, style.FontSize, font, italic)
            : RgbaCanvas.MeasureTextWidth(text, style.FontSize, font, italic);
    }

    private static double TextAnchorOffset(string anchor, double width) {
        if (string.Equals(anchor, "middle", StringComparison.OrdinalIgnoreCase)) return -width / 2.0;
        if (string.Equals(anchor, "end", StringComparison.OrdinalIgnoreCase)) return -width;
        return 0;
    }

    private static double DrawTextRun(RgbaCanvas? canvas, string text, double x, double y, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport, SvgRasterTextPaintBounds paintBounds, bool measureOnly) {
        if (text.Length == 0) return 0;
        if (measureOnly) {
            var measuredAdvance = MeasureTextAdvance(text, style);
            if (style.VisibilityVisible) paintBounds.Include(x, TextTop(y, style.FontSize, style.DominantBaseline), MeasureTextPaintWidth(text, style), SvgTextPaintHeight(style, SvgTextFont(style)), matrix);
            return measuredAdvance;
        }
        if (canvas == null) throw new InvalidOperationException("SVG text rendering requires a target canvas.");
        var renderScale = ResolveTextRenderScale(canvas, text, style, matrix.ScaleFactor);
        var fontSize = Math.Max(1, style.FontSize * renderScale);
        var font = SvgTextFont(style);
        var emphasized = IsBold(style.FontWeight);
        var italic = IsItalic(style.FontStyle);
        var underline = HasUnderline(style.TextDecoration);
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, font, italic) : RgbaCanvas.MeasureTextWidth(text, fontSize, font, italic);
        var advanceWidth = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, font, italic: false) : RgbaCanvas.MeasureTextWidth(text, fontSize, font, italic: false);
        var advance = advanceWidth / renderScale;
        if (!style.VisibilityVisible) return advance;
        var fillColor = style.FillColor();
        var strokeColor = style.StrokeWidth > 0 ? ResolveColor(style.Stroke, style.Opacity * style.StrokeOpacity, definitions) : ChartColor.Transparent;
        if (style.Fill.IsNone && strokeColor.A == 0) return advance;

        var drawX = x;
        var drawY = TextTop(y, style.FontSize, style.DominantBaseline);
        var strokeRadius = strokeColor.A == 0 ? 0 : Math.Max(1, (int)Math.Ceiling(style.StrokeWidth * renderScale / 2.0));
        var padding = Math.Max(2, (int)Math.Ceiling(fontSize * 0.2) + strokeRadius);
        var textHeight = Math.Max(1, RgbaCanvas.MeasureTextHeight(fontSize, font));
        var underlineThickness = Math.Max(1, fontSize / 13.0);
        var underlineY = padding + fontSize + 2;
        var contentHeight = underline ? Math.Max(textHeight, fontSize + 2 + underlineThickness / 2.0) : textHeight;
        var localWidth = Math.Max(1, (int)Math.Ceiling(width + padding * 2.0));
        var localHeight = Math.Max(1, (int)Math.Ceiling(contentHeight + padding * 2.0));
        var buffer = new RgbaCanvas(localWidth, localHeight, 1, font);
        RgbaCanvas? glyphMask = null;
        if (style.Fill.IsReference || strokeColor.A > 0) {
            glyphMask = new RgbaCanvas(localWidth, localHeight, 1, font);
            DrawTextGlyphs(glyphMask, padding, padding, text, ChartColor.White, fontSize, emphasized, italic);
            if (underline) glyphMask.DrawLine(padding, underlineY, padding + width, underlineY, ChartColor.White, underlineThickness);
        }
        if (style.Fill.IsReference && glyphMask != null) {
            var localToCanvas = matrix
                .Multiply(SvgRasterMatrix.Translate(drawX - padding / renderScale, drawY - padding / renderScale))
                .Multiply(SvgRasterMatrix.Scale(1 / renderScale, 1 / renderScale));
            if (localToCanvas.TryInvert(out var inverseTextMatrix)) {
                var paintCanvas = new RgbaCanvas(localWidth, localHeight, 1);
                SvgRasterObjectPaint? objectPaint = null;
                var localPaintBounds = paintBounds.HasBounds
                    ? TransformRing(RectRing(paintBounds.Left, paintBounds.Top, paintBounds.Width, paintBounds.Height), inverseTextMatrix.Multiply(paintBounds.RootMatrix))
                    : RectRing(padding, padding, width, contentHeight);
                if (paintBounds.HasBounds) {
                    objectPaint = new SvgRasterObjectPaint(
                        new SvgRasterGradientValues.GradientBounds(paintBounds.Left, paintBounds.Top, paintBounds.Width, paintBounds.Height),
                        inverseTextMatrix.Multiply(paintBounds.RootMatrix));
                }
                Fill(paintCanvas, new[] { localPaintBounds }, style, inverseTextMatrix.Multiply(matrix), definitions, viewport, objectPaint);
                buffer.DrawImageMasked(0, 0, localWidth, localHeight, paintCanvas.Pixels, glyphMask.Pixels, useAlphaMask: true);
            }
        } else if (fillColor.A > 0) {
            DrawTextGlyphs(buffer, padding, padding, text, fillColor, fontSize, emphasized, italic);
            if (underline) buffer.DrawLine(padding, underlineY, padding + width, underlineY, fillColor, underlineThickness);
        }
        if (strokeColor.A > 0) {
            PaintDilatedTextStroke(buffer.Pixels, glyphMask!.Pixels, localWidth, localHeight, strokeRadius, strokeColor);
        }

        var textMatrix = matrix
            .Multiply(SvgRasterMatrix.Translate(drawX - padding / renderScale, drawY - padding / renderScale))
            .Multiply(SvgRasterMatrix.Scale(1 / renderScale, 1 / renderScale));
        canvas.DrawImageTransformed(localWidth, localHeight, buffer.Pixels, textMatrix.A, textMatrix.B, textMatrix.C, textMatrix.D, textMatrix.E, textMatrix.F);
        return advance;
    }

    private static double ResolveTextRenderScale(RgbaCanvas canvas, string text, SvgRasterStyle style, double requestedScale) {
        const double minimumScale = 0.000000000001;
        var scale = Math.Max(minimumScale, requestedScale);
        var font = SvgTextFont(style);
        var italic = IsItalic(style.FontStyle);
        for (var attempt = 0; attempt < 8; attempt++) {
            var fontSize = Math.Max(1, style.FontSize * scale);
            var width = Math.Max(1, IsBold(style.FontWeight) ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, font, italic) : RgbaCanvas.MeasureTextWidth(text, fontSize, font, italic));
            var height = Math.Max(1, RgbaCanvas.MeasureTextHeight(fontSize, font));
            if (HasUnderline(style.TextDecoration)) height = Math.Max(height, fontSize + 2 + Math.Max(1, fontSize / 13.0) / 2.0);
            var padding = Math.Max(2, Math.Ceiling(fontSize * 0.2 + style.StrokeWidth * scale / 2.0));
            var pixels = (width + padding * 2) * (height + padding * 2);
            var axisLimit = Math.Max(1024, Math.Min(32768, Math.Max(canvas.Width, canvas.Height) * 2));
            var reduction = Math.Min(1, Math.Min(axisLimit / (width + padding * 2), axisLimit / (height + padding * 2)));
            if (pixels > MaximumTextIntermediatePixels) reduction = Math.Min(reduction, Math.Sqrt(MaximumTextIntermediatePixels / pixels));
            if (reduction >= 0.999 && !double.IsNaN(pixels) && !double.IsInfinity(pixels)) return scale;
            var reduced = scale * reduction * 0.98;
            if (double.IsNaN(reduced) || double.IsInfinity(reduced) || reduced < minimumScale) reduced = minimumScale;
            if (Math.Abs(reduced - scale) < minimumScale * 0.001) break;
            scale = reduced;
        }
        throw new InvalidOperationException("SVG text paint exceeds the supported intermediate raster budget.");
    }

    private static bool HasVisibleTextFillAndStroke(SvgRasterStyle style) =>
        !style.Fill.IsNone && style.FillOpacity > 0 && !style.Stroke.IsNone && style.StrokeOpacity > 0 && style.StrokeWidth > 0;

    private static TrueTypeFont? SvgTextFont(SvgRasterStyle style) =>
        string.IsNullOrWhiteSpace(style.FontFamily) ? null : TrueTypeFont.TryLoadForFamily(style.FontFamily, out _);

    private static bool IsItalic(string value) =>
        value.IndexOf("italic", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("oblique", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool HasUnderline(string value) =>
        value.IndexOf("underline", StringComparison.OrdinalIgnoreCase) >= 0;

    private static double SvgTextPaintHeight(SvgRasterStyle style, TrueTypeFont? font) {
        var height = RgbaCanvas.MeasureTextHeight(style.FontSize, font);
        return HasUnderline(style.TextDecoration) ? Math.Max(height, style.FontSize + 2 + Math.Max(1, style.FontSize / 13.0) / 2.0) : height;
    }

    private static void DrawTextGlyphs(RgbaCanvas canvas, double x, double y, string text, ChartColor color, double fontSize, bool emphasized, bool italic) {
        if (emphasized) canvas.DrawTextEmphasized(x, y, text, color, fontSize, italic);
        else canvas.DrawText(x, y, text, color, fontSize, italic);
    }

    private static void PaintDilatedTextStroke(byte[] destination, byte[] glyphPixels, int width, int height, int radius, ChartColor color) {
        var dilated = FilterTextAlpha(glyphPixels, width, height, radius, maximize: true);
        var eroded = FilterTextAlpha(glyphPixels, width, height, radius, maximize: false);
        for (var pixel = 0; pixel < dilated.Length; pixel++) {
            var coverage = Math.Max(0, dilated[pixel] - eroded[pixel]);
            if (coverage == 0) continue;
            var index = pixel * 4;
            BlendTextPixel(destination, index, color, (byte)Math.Round(color.A * coverage / 255.0));
        }
    }

    private static byte[] FilterTextAlpha(byte[] glyphPixels, int width, int height, int radius, bool maximize) {
        var pixelCount = checked(width * height);
        var horizontal = new byte[pixelCount];
        var filtered = new byte[pixelCount];
        var deque = new int[Math.Max(width, height)];
        for (var y = 0; y < height; y++) {
            var head = 0;
            var tail = 0;
            for (var x = 0; x < width + radius; x++) {
                if (x < width) {
                    var alpha = glyphPixels[(y * width + x) * 4 + 3];
                    while (tail > head && PreferTextAlpha(alpha, glyphPixels[(y * width + deque[tail - 1]) * 4 + 3], maximize)) tail--;
                    deque[tail++] = x;
                }
                var outputX = x - radius;
                if (outputX < 0) continue;
                while (tail > head && deque[head] < outputX - radius) head++;
                horizontal[y * width + outputX] = !maximize && (outputX < radius || outputX + radius >= width)
                    ? (byte)0
                    : glyphPixels[(y * width + deque[head]) * 4 + 3];
            }
        }

        for (var x = 0; x < width; x++) {
            var head = 0;
            var tail = 0;
            for (var y = 0; y < height + radius; y++) {
                if (y < height) {
                    var alpha = horizontal[y * width + x];
                    while (tail > head && PreferTextAlpha(alpha, horizontal[deque[tail - 1] * width + x], maximize)) tail--;
                    deque[tail++] = y;
                }
                var outputY = y - radius;
                if (outputY < 0) continue;
                while (tail > head && deque[head] < outputY - radius) head++;
                filtered[outputY * width + x] = !maximize && (outputY < radius || outputY + radius >= height)
                    ? (byte)0
                    : horizontal[deque[head] * width + x];
            }
        }
        return filtered;
    }

    private static bool PreferTextAlpha(byte candidate, byte existing, bool maximize) => maximize ? candidate >= existing : candidate <= existing;

    private static void BlendTextPixel(byte[] destination, int index, ChartColor color, byte alpha) {
        if (alpha == 0) return;
        if (alpha == 255) {
            destination[index] = color.R;
            destination[index + 1] = color.G;
            destination[index + 2] = color.B;
            destination[index + 3] = 255;
            return;
        }
        var sourceAlpha = alpha / 255.0;
        var destinationAlpha = destination[index + 3] / 255.0;
        var outputAlpha = sourceAlpha + destinationAlpha * (1 - sourceAlpha);
        destination[index] = (byte)((color.R * sourceAlpha + destination[index] * destinationAlpha * (1 - sourceAlpha)) / outputAlpha);
        destination[index + 1] = (byte)((color.G * sourceAlpha + destination[index + 1] * destinationAlpha * (1 - sourceAlpha)) / outputAlpha);
        destination[index + 2] = (byte)((color.B * sourceAlpha + destination[index + 2] * destinationAlpha * (1 - sourceAlpha)) / outputAlpha);
        destination[index + 3] = (byte)(outputAlpha * 255);
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

    private sealed class SvgRasterTextPaintBounds {
        private readonly SvgRasterMatrix _inverseRoot;
        private bool _hasInverse;
        private double _left = double.PositiveInfinity;
        private double _top = double.PositiveInfinity;
        private double _right = double.NegativeInfinity;
        private double _bottom = double.NegativeInfinity;

        public SvgRasterTextPaintBounds(SvgRasterMatrix rootMatrix) {
            RootMatrix = rootMatrix;
            _hasInverse = rootMatrix.TryInvert(out _inverseRoot);
        }

        public SvgRasterMatrix RootMatrix { get; }
        public bool HasBounds => _hasInverse && !double.IsInfinity(_left);
        public double Left => _left;
        public double Top => _top;
        public double Width => Math.Max(0, _right - _left);
        public double Height => Math.Max(0, _bottom - _top);

        public void Include(double x, double y, double width, double height, SvgRasterMatrix matrix) {
            if (!_hasInverse || width <= 0 || height <= 0) return;
            var relative = _inverseRoot.Multiply(matrix);
            Include(relative.Transform(new ChartPoint(x, y)));
            Include(relative.Transform(new ChartPoint(x + width, y)));
            Include(relative.Transform(new ChartPoint(x + width, y + height)));
            Include(relative.Transform(new ChartPoint(x, y + height)));
        }

        private void Include(ChartPoint point) {
            _left = Math.Min(_left, point.X);
            _top = Math.Min(_top, point.Y);
            _right = Math.Max(_right, point.X);
            _bottom = Math.Max(_bottom, point.Y);
        }
    }
}
