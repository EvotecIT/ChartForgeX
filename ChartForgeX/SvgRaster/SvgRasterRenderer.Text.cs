using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private const long MaximumTextIntermediatePixels = 8_000_000;

    private static void RenderText(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, IReadOnlyList<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        var cursorX = HorizontalLength(element, "x", viewport) + HorizontalLength(element, "dx", viewport);
        var cursorY = VerticalLength(element, "y", viewport) + VerticalLength(element, "dy", viewport);
        var textAncestors = new List<SvgRasterElement>(ancestors) { element };
        var whitespace = new TextWhitespaceState { LineStartX = cursorX };
        var measureWhitespace = whitespace;
        var measureTransform = new SvgRasterTextTransformer();
        cursorX += TextAnchorOffset(style.TextAnchor, MeasureTextChunkFrom(element, 0, style, definitions.StyleSheet, textAncestors, viewport, ref measureWhitespace, ref measureTransform, includeFirstPositionedSpan: false));
        whitespace.LineStartX = cursorX;
        var paintBounds = new SvgRasterTextPaintBounds(matrix);
        var boundsCursorX = cursorX;
        var boundsCursorY = cursorY;
        var boundsWhitespace = whitespace;
        var boundsTransform = new SvgRasterTextTransformer();
        var paintTransform = new SvgRasterTextTransformer();
        var layout = NeedsWholeChunkShaping(element) ? new SvgTextLayout(style.TextAnchor) : null;
        RenderTextContent(null, element, style, matrix, definitions, width, height, textAncestors, viewport, paintBounds, true, ref boundsCursorX, ref boundsCursorY, ref boundsWhitespace, ref boundsTransform, layout);
        if (layout != null) {
            layout.Resolve();
            boundsCursorX = cursorX;
            boundsCursorY = cursorY;
            boundsWhitespace = whitespace;
            boundsTransform = new SvgRasterTextTransformer();
            RenderTextContent(null, element, style, matrix, definitions, width, height, textAncestors, viewport, paintBounds, true, ref boundsCursorX, ref boundsCursorY, ref boundsWhitespace, ref boundsTransform, layout);
            layout.Rewind();
        }
        RenderTextContent(canvas, element, style, matrix, definitions, width, height, textAncestors, viewport, paintBounds, false, ref cursorX, ref cursorY, ref whitespace, ref paintTransform, layout);
    }

    private static void RenderTextContent(RgbaCanvas? canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, List<SvgRasterElement> ancestors, SvgRasterViewport viewport, SvgRasterTextPaintBounds paintBounds, bool measureOnly, ref double cursorX, ref double cursorY, ref TextWhitespaceState whitespace, ref SvgRasterTextTransformer transform, SvgTextLayout? layout) {
        for (var contentIndex = 0; contentIndex < element.Content.Count; contentIndex++) {
            var content = element.Content[contentIndex];
            if (content.Text != null) {
                RenderTextValue(canvas, content.Text, style, matrix, definitions, viewport, paintBounds, measureOnly, ref cursorX, ref cursorY, ref whitespace, ref transform, layout);
                continue;
            }

            var span = content.Element;
            if (span == null) continue;
            if (!string.Equals(span.Name, "tspan", StringComparison.Ordinal)) {
                ReportUnsupportedElement(span, SvgRasterStyle.Resolve(style, span, definitions.StyleSheet, ancestors), definitions);
                continue;
            }
            var spanStyle = SvgRasterStyle.Resolve(style, span, definitions.StyleSheet, ancestors);
            ReportUnsupportedFilter(span, spanStyle, definitions);
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
                layout?.BeginChunk(spanStyle.TextAnchor, span.TryGet("x", out _));
                var measureWhitespace = whitespace;
                var measureTransform = transform;
                cursorX += TextAnchorOffset(spanStyle.TextAnchor, MeasureTextChunkFrom(element, contentIndex, style, definitions.StyleSheet, ancestors, viewport, ref measureWhitespace, ref measureTransform, includeFirstPositionedSpan: true));
            }
            var spanMatrix = matrix.Multiply(SvgRasterMatrix.ParseTransform(span.Get("transform")));
            ancestors.Add(span);
            RenderTextSpan(canvas, span, spanStyle, spanMatrix, definitions, width, height, ancestors, viewport, paintBounds, measureOnly, ref cursorX, ref cursorY, ref whitespace, ref transform, layout);
            ancestors.RemoveAt(ancestors.Count - 1);
        }
    }

    private static void RenderTextSpan(RgbaCanvas? canvas, SvgRasterElement span, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, List<SvgRasterElement> ancestors, SvgRasterViewport viewport, SvgRasterTextPaintBounds paintBounds, bool measureOnly, ref double cursorX, ref double cursorY, ref TextWhitespaceState whitespace, ref SvgRasterTextTransformer transform, SvgTextLayout? layout) {
        if (measureOnly) {
            RenderTextContent(null, span, style, matrix, definitions, width, height, ancestors, viewport, paintBounds, true, ref cursorX, ref cursorY, ref whitespace, ref transform, layout);
            return;
        }
        var hasClipPath = definitions.TryGetClipPath(ParseReference(style.ClipPath) ?? ReferenceId(span, "clip-path"), out var clipPath);
        var hasMask = definitions.TryGetMask(ReferenceId(span, "mask"), out var maskDefinition);
        var compositeOpacity = style.Opacity < 0.999 && (span.Children.Count > 0 || HasVisibleTextFillAndStroke(style));
        if (!hasClipPath && !hasMask && !compositeOpacity) {
            RenderTextContent(canvas, span, style, matrix, definitions, width, height, ancestors, viewport, paintBounds, false, ref cursorX, ref cursorY, ref whitespace, ref transform, layout);
            return;
        }

        var content = new RgbaCanvas(width, height, 1);
        var contentStyle = compositeOpacity ? style.Inherit() : style;
        RenderTextContent(content, span, contentStyle, matrix, definitions, width, height, ancestors, viewport, paintBounds, false, ref cursorX, ref cursorY, ref whitespace, ref transform, layout);
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

    private static void RenderTextValue(RgbaCanvas? canvas, string value, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport, SvgRasterTextPaintBounds paintBounds, bool measureOnly, ref double cursorX, ref double cursorY, ref TextWhitespaceState whitespace, ref SvgRasterTextTransformer transform, SvgTextLayout? layout) {
        var text = NormalizeTextWhitespace(value, style.WhiteSpace, ref whitespace);
        text = transform.Transform(text, style.TextTransform);
        var start = 0;
        while (start <= text.Length) {
            var newline = text.IndexOf('\n', start);
            var length = newline < 0 ? text.Length - start : newline - start;
            if (length > 0) {
                var valueRun = text.Substring(start, length);
                if (layout == null) cursorX += DrawTextRun(canvas, valueRun, cursorX, cursorY, style, matrix, definitions, viewport, paintBounds, measureOnly);
                else if (layout.Collecting) {
                    var advance = MeasureTextAdvance(valueRun, style);
                    layout.Add(valueRun, cursorX, cursorY, advance, style);
                    cursorX += advance;
                } else {
                    var run = layout.Next();
                    if (run.Pieces == null) cursorX += DrawTextRun(canvas, valueRun, cursorX, cursorY, style, matrix, definitions, viewport, paintBounds, measureOnly);
                    else {
                        foreach (var piece in run.Pieces) DrawTextRun(canvas, valueRun, piece.X, run.Y, style, matrix, definitions, viewport, paintBounds, measureOnly, piece.Glyphs);
                        cursorX += run.Advance;
                    }
                }
            }
            if (newline < 0) break;
            layout?.BeginChunk(style.TextAnchor, absoluteX: true);
            cursorX = whitespace.LineStartX;
            cursorY += style.FontSize * 1.2;
            start = newline + 1;
        }
    }

    private static double MeasureTextChunkFrom(SvgRasterElement element, int startIndex, SvgRasterStyle style, SvgRasterStyleSheet styleSheet, IReadOnlyList<SvgRasterElement> ancestors, SvgRasterViewport viewport, ref TextWhitespaceState whitespace, ref SvgRasterTextTransformer transform, bool includeFirstPositionedSpan) {
        var advance = 0.0;
        for (var contentIndex = startIndex; contentIndex < element.Content.Count; contentIndex++) {
            var content = element.Content[contentIndex];
            if (content.Text != null) {
                var text = NormalizeTextWhitespace(content.Text, style.WhiteSpace, ref whitespace);
                text = transform.Transform(text, style.TextTransform);
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
            advance += MeasureTextChunkFrom(span, 0, spanStyle, styleSheet, spanAncestors, viewport, ref whitespace, ref transform, includeFirstPositionedSpan: false);
        }
        return advance;
    }

    private static double MeasureTextAdvance(string text, SvgRasterStyle style) =>
        text.Length == 0 ? 0 : TextAdvanceWidth(text, style.FontSize, SvgTextFace(style));

    private static double MeasureTextPaintWidth(string text, SvgRasterStyle style) =>
        text.Length == 0 ? 0 : TextPaintWidth(text, style.FontSize, SvgTextFace(style), IsItalic(style.FontStyle));

    // The pen advance: what the next run starts after. A synthesized bold adds its offset, a slant does not.
    private static double TextAdvanceWidth(string text, double fontSize, ResolvedTypeface face) =>
        face.SynthesizeBold
            ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, face.Font, italic: false)
            : RgbaCanvas.MeasureTextWidth(text, fontSize, face.Font, italic: false);

    // The inked extent: a sheared face and a real italic face both lean past the last advance.
    private static double TextPaintWidth(string text, double fontSize, ResolvedTypeface face, bool italic) {
        var width = face.SynthesizeBold
            ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, face.Font, face.SynthesizeItalic)
            : RgbaCanvas.MeasureTextWidth(text, fontSize, face.Font, face.SynthesizeItalic);
        return italic && !face.SynthesizeItalic && text.Length > 0 ? width + TrueTypeFont.ItalicOverhang(fontSize) : width;
    }

    private static double TextAnchorOffset(string anchor, double width) {
        if (string.Equals(anchor, "middle", StringComparison.OrdinalIgnoreCase)) return -width / 2.0;
        if (string.Equals(anchor, "end", StringComparison.OrdinalIgnoreCase)) return -width;
        return 0;
    }

    private static double DrawTextRun(RgbaCanvas? canvas, string text, double x, double y, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport, SvgRasterTextPaintBounds paintBounds, bool measureOnly, IReadOnlyList<ShapedGlyph>? glyphs = null) {
        if (text.Length == 0) return 0;
        glyphs ??= SvgTextFace(style).Font is TrueTypeFont shapingFace ? TextShaper.Shape(shapingFace, text, style.FontSize) : null;
        if (measureOnly) {
            var measuredAdvance = PreparedAdvance(text, style.FontSize, SvgTextFace(style), glyphs);
            if (style.VisibilityVisible) paintBounds.Include(x, TextTop(y, style.FontSize, style.DominantBaseline, SvgTextFace(style).Font) + BaselineShiftOffset(style), PreparedPaintWidth(text, style.FontSize, SvgTextFace(style), IsItalic(style.FontStyle), glyphs), SvgTextPaintHeight(style, SvgTextFace(style).Font), matrix);
            var ink = glyphs == null ? null : SvgTextFace(style).Font?.MeasureGlyphInk(glyphs, style.FontSize, IsItalic(style.FontStyle));
            if (style.VisibilityVisible && ink.HasValue) {
                var box = ink.Value;
                paintBounds.Include(x + box.X, TextTop(y, style.FontSize, style.DominantBaseline, SvgTextFace(style).Font) + BaselineShiftOffset(style) + box.Y, box.Width, box.Height, matrix);
            }
            return measuredAdvance;
        }
        if (canvas == null) throw new InvalidOperationException("SVG text rendering requires a target canvas.");
        var renderScale = ResolveTextRenderScale(canvas, text, style, matrix.ScaleFactor, glyphs);
        var fontSize = Math.Max(1, style.FontSize * renderScale);
        var face = SvgTextFace(style);
        var font = face.Font;
        // A real bold or italic face draws as it is; only a missing one is synthesized on the nearest face.
        var emphasized = face.SynthesizeBold;
        var italic = face.SynthesizeItalic;
        var underline = HasUnderline(style.TextDecoration);
        var strikethrough = HasLineThrough(style.TextDecoration);
        var underlineStyle = DecorationStyle(style.UnderlineDecorationStyle);
        var strikethroughStyle = DecorationStyle(style.StrikethroughDecorationStyle);
        var width = PreparedPaintWidth(text, fontSize, face, IsItalic(style.FontStyle), glyphs);
        var advance = PreparedAdvance(text, fontSize, face, glyphs) / renderScale;
        if (!style.VisibilityVisible) return advance;
        var fillColor = style.FillColor();
        var strokeColor = style.StrokeWidth > 0 ? ResolveColor(style.Stroke, style.Opacity * style.StrokeOpacity, definitions) : ChartColor.Transparent;
        if (style.Fill.IsNone && strokeColor.A == 0) return advance;

        var drawX = x;
        var drawY = TextTop(y, style.FontSize, style.DominantBaseline, font) + BaselineShiftOffset(style);
        var strokeRadius = strokeColor.A == 0 ? 0 : Math.Max(1, (int)Math.Ceiling(style.StrokeWidth * renderScale / 2.0));
        var textHeight = Math.Max(1, RgbaCanvas.MeasureTextHeight(fontSize, font));
        var padding = (int)Math.Ceiling(TextInkPadding(font, glyphs, fontSize, italic, width, textHeight, Math.Max(2, Math.Ceiling(fontSize * 0.2) + strokeRadius)));
        var underlineThickness = Math.Max(1, fontSize / 13.0);
        // Decorations sit relative to the face's own baseline, which is its ascent below the buffer top.
        var ascent = TextAscent(fontSize, font);
        var underlineY = padding + ascent + fontSize * 0.1 + 2;
        var strikeY = padding + ascent - fontSize * 0.35;
        var contentHeight = underline ? Math.Max(textHeight, ascent + fontSize * 0.1 + 2 + TextDecorationMetrics.OuterExtent(underlineStyle, underlineThickness)) : textHeight;
        var localWidth = Math.Max(1, (int)Math.Ceiling(width + padding * 2.0));
        var localHeight = Math.Max(1, (int)Math.Ceiling(contentHeight + padding * 2.0));
        var buffer = new RgbaCanvas(localWidth, localHeight, 1, font) { TextHinting = canvas.TextHinting };
        RgbaCanvas? glyphMask = null;
        if (style.Fill.IsReference || strokeColor.A > 0) {
            glyphMask = new RgbaCanvas(localWidth, localHeight, 1, font) { TextHinting = canvas.TextHinting };
            DrawTextGlyphs(glyphMask, padding, padding, text, ChartColor.White, fontSize, emphasized, italic, font, glyphs);
            if (underline) RasterTextDecoration.Draw(glyphMask, padding, padding + width, underlineY, underlineStyle, ChartColor.White, underlineThickness);
            if (strikethrough) RasterTextDecoration.Draw(glyphMask, padding, padding + width, strikeY, strikethroughStyle, ChartColor.White, underlineThickness);
        }
        if (style.StrokeBeforeFill && strokeColor.A > 0)
            PaintDilatedTextStroke(buffer.Pixels, glyphMask!.Pixels, localWidth, localHeight, strokeRadius, strokeColor);
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
                var paintMask = glyphMask;
                if (HasColourGlyphs(glyphs)) {
                    paintMask = new RgbaCanvas(localWidth, localHeight, 1, font) { TextHinting = canvas.TextHinting, GlyphPaintMode = FontGlyphPaintMode.MonochromeOnly };
                    DrawTextGlyphs(paintMask, padding, padding, text, ChartColor.White, fontSize, emphasized, italic, font, glyphs);
                    if (underline) RasterTextDecoration.Draw(paintMask, padding, padding + width, underlineY, underlineStyle, ChartColor.White, underlineThickness);
                    if (strikethrough) RasterTextDecoration.Draw(paintMask, padding, padding + width, strikeY, strikethroughStyle, ChartColor.White, underlineThickness);
                }
                buffer.DrawImageMasked(0, 0, localWidth, localHeight, paintCanvas.Pixels, paintMask.Pixels, useAlphaMask: true);
                if (!ReferenceEquals(paintMask, glyphMask)) {
                    buffer.GlyphPaintMode = FontGlyphPaintMode.ColourOnly;
                    var foreground = new ChartColor(style.Color.R, style.Color.G, style.Color.B, (byte)Math.Round(style.Color.A * style.Opacity * style.FillOpacity));
                    DrawTextGlyphs(buffer, padding, padding, text, foreground, fontSize, emphasized, italic, font, glyphs);
                    buffer.GlyphPaintMode = FontGlyphPaintMode.All;
                }
            }
        } else if (fillColor.A > 0) {
            DrawTextGlyphs(buffer, padding, padding, text, fillColor, fontSize, emphasized, italic, font, glyphs);
            if (underline) RasterTextDecoration.Draw(buffer, padding, padding + width, underlineY, underlineStyle, fillColor, underlineThickness);
            if (strikethrough) RasterTextDecoration.Draw(buffer, padding, padding + width, strikeY, strikethroughStyle, fillColor, underlineThickness);
        }
        if (!style.StrokeBeforeFill && strokeColor.A > 0) {
            PaintDilatedTextStroke(buffer.Pixels, glyphMask!.Pixels, localWidth, localHeight, strokeRadius, strokeColor);
        }

        var textMatrix = matrix
            .Multiply(SvgRasterMatrix.Translate(drawX - padding / renderScale, drawY - padding / renderScale))
            .Multiply(SvgRasterMatrix.Scale(1 / renderScale, 1 / renderScale));
        textMatrix = AlignHintedRows(textMatrix, canvas, fontSize);
        canvas.DrawImageTransformed(localWidth, localHeight, buffer.Pixels, textMatrix.A, textMatrix.B, textMatrix.C, textMatrix.D, textMatrix.E, textMatrix.F);
        return advance;
    }

    // A hinted glyph buffer keeps its whole-pixel rows only when it lands on whole canvas rows, so an
    // unrotated, unscaled placement moves to the nearest row; horizontal positions keep their fractions.
    private static SvgRasterMatrix AlignHintedRows(SvgRasterMatrix matrix, RgbaCanvas canvas, double fontSize) {
        if (canvas.TextHinting == TextHinting.None || fontSize > GlyphGridFit.MaximumPixelSize) return matrix;
        if (Math.Abs(matrix.B) > 1e-9 || Math.Abs(matrix.C) > 1e-9 || Math.Abs(matrix.A - 1) > 1e-9 || Math.Abs(matrix.D - 1) > 1e-9) return matrix;
        return new SvgRasterMatrix(matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, Math.Round(matrix.F, MidpointRounding.AwayFromZero));
    }

    private static double ResolveTextRenderScale(RgbaCanvas canvas, string text, SvgRasterStyle style, double requestedScale, IReadOnlyList<ShapedGlyph>? glyphs) {
        const double minimumScale = 0.000000000001;
        var scale = Math.Max(minimumScale, requestedScale);
        var face = SvgTextFace(style);
        glyphs ??= face.Font is TrueTypeFont shapingFace ? TextShaper.Shape(shapingFace, text, style.FontSize) : null;
        var italic = IsItalic(style.FontStyle);
        for (var attempt = 0; attempt < 8; attempt++) {
            var fontSize = Math.Max(1, style.FontSize * scale);
            var width = Math.Max(1, PreparedPaintWidth(text, fontSize, face, italic, glyphs));
            var height = Math.Max(1, RgbaCanvas.MeasureTextHeight(fontSize, face.Font));
            if (HasUnderline(style.TextDecoration)) {
                var thickness = Math.Max(1, fontSize / 13.0);
                height = Math.Max(height, fontSize + 2 + TextDecorationMetrics.OuterExtent(DecorationStyle(style.UnderlineDecorationStyle), thickness));
            }
            var padding = TextInkPadding(face.Font, glyphs, fontSize, italic, width, height, Math.Max(2, Math.Ceiling(fontSize * 0.2 + style.StrokeWidth * scale / 2.0)));
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

    private static double TextInkPadding(TrueTypeFont? font, IReadOnlyList<ShapedGlyph>? glyphs, double size, bool italic, double width, double height, double padding) {
        if (font == null || glyphs == null) return padding;
        var positioned = false;
        foreach (var glyph in glyphs) if (glyph.Advance.HasValue) { positioned = true; break; }
        if (!positioned) return padding;
        var ink = font.MeasureGlyphInk(glyphs, size, italic);
        if (!ink.HasValue) return padding;
        var box = ink.Value;
        return Math.Max(padding, 2 + Math.Max(Math.Max(-box.X, box.X + box.Width - width), Math.Max(-box.Y, box.Y + box.Height - height)));
    }

    private static bool HasVisibleTextFillAndStroke(SvgRasterStyle style) =>
        !style.Fill.IsNone && style.FillOpacity > 0 && !style.Stroke.IsNone && style.StrokeOpacity > 0 && style.StrokeWidth > 0;

    // The same family, weight, and slant matching as FontSpec text, so SVG rasterized here and text
    // drawn through ImageComposition or VisualCanvas pick the same installed or registered face.
    // Without a font-family the stack is plain sans-serif, the face unstyled SVG text always used.
    private static ResolvedTypeface SvgTextFace(SvgRasterStyle style) =>
        TypographyFontResolver.WithLanguage(TypographyFontResolver.ResolveFace(style.FontFamily, style.FontWeight, IsItalic(style.FontStyle)), style.OpenTypeLanguageTag);

    private static bool IsItalic(string value) =>
        value.IndexOf("italic", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("oblique", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool HasUnderline(string value) =>
        value.IndexOf("underline", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool HasLineThrough(string value) =>
        value.IndexOf("line-through", StringComparison.OrdinalIgnoreCase) >= 0;

    private static TextDecorationStyle DecorationStyle(string value) {
        if (value.IndexOf("double", StringComparison.OrdinalIgnoreCase) >= 0) return TextDecorationStyle.Double;
        if (value.IndexOf("dotted", StringComparison.OrdinalIgnoreCase) >= 0) return TextDecorationStyle.Dotted;
        if (value.IndexOf("dashed", StringComparison.OrdinalIgnoreCase) >= 0) return TextDecorationStyle.Dashed;
        if (value.IndexOf("wavy", StringComparison.OrdinalIgnoreCase) >= 0) return TextDecorationStyle.Wavy;
        return TextDecorationStyle.Single;
    }

    private static double BaselineShiftOffset(SvgRasterStyle style) {
        if (string.Equals(style.BaselineShift, "super", StringComparison.OrdinalIgnoreCase)) return -style.FontSize * 0.45;
        if (string.Equals(style.BaselineShift, "sub", StringComparison.OrdinalIgnoreCase)) return style.FontSize * 0.25;
        return 0;
    }

    private static double SvgTextPaintHeight(SvgRasterStyle style, TrueTypeFont? font) {
        var height = RgbaCanvas.MeasureTextHeight(style.FontSize, font);
        if (!HasUnderline(style.TextDecoration)) return height;
        var thickness = Math.Max(1, style.FontSize / 13.0);
        return Math.Max(height, style.FontSize + 2 + TextDecorationMetrics.OuterExtent(DecorationStyle(style.UnderlineDecorationStyle), thickness));
    }

    private static void DrawTextGlyphs(RgbaCanvas canvas, double x, double y, string text, ChartColor color, double fontSize, bool emphasized, bool italic, TrueTypeFont? font, IReadOnlyList<ShapedGlyph>? glyphs) {
        if (glyphs != null && font != null) {
            font.DrawGlyphs(canvas, x, y, glyphs, color, fontSize, italic);
            if (emphasized) font.DrawGlyphs(canvas, x + TextEmphasisOffset(fontSize), y, glyphs, color, fontSize, italic, syntheticBoldCopyOnly: true);
        } else if (emphasized) canvas.DrawTextEmphasized(x, y, text, color, fontSize, italic);
        else canvas.DrawText(x, y, text, color, fontSize, italic);
    }

    private static bool HasColourGlyphs(IReadOnlyList<ShapedGlyph>? glyphs) {
        if (glyphs != null) foreach (var glyph in glyphs) if (glyph.Face.IsColorGlyph(glyph.Glyph)) return true;
        return false;
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

    // Glyphs are drawn from the top of the face's ascent, so the top is the alphabetic baseline minus
    // that ascent: faces with tall ascenders (Segoe UI) or short ones (Calibri) still sit on y.
    private static double TextTop(double y, double fontSize, string baseline, TrueTypeFont? font) =>
        TextBaseline(y, fontSize, baseline) - TextAscent(fontSize, font);

    private static double TextAscent(double fontSize, TrueTypeFont? font) => font?.Ascent(fontSize) ?? fontSize * 0.82;

    private static double TextBaseline(double y, double fontSize, string baseline) {
        if (string.Equals(baseline, "middle", StringComparison.OrdinalIgnoreCase) || string.Equals(baseline, "central", StringComparison.OrdinalIgnoreCase)) return y + fontSize * 0.32;
        if (string.Equals(baseline, "hanging", StringComparison.OrdinalIgnoreCase) || string.Equals(baseline, "text-before-edge", StringComparison.OrdinalIgnoreCase)) return y + fontSize * 0.82;
        if (string.Equals(baseline, "text-after-edge", StringComparison.OrdinalIgnoreCase) || string.Equals(baseline, "ideographic", StringComparison.OrdinalIgnoreCase)) return y - fontSize * 0.18;
        return y;
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
