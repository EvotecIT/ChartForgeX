using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal static class TerminalPngTextPreserver {
    private const double ColumnWidthFactor = 0.61;
    private const int MaximumFallbackLabels = 4;
    internal const char EscapeStart = '\uFDD0';
    internal const char EscapeEnd = '\uFDD1';

    public static string Preserve(string value, TrueTypeFont? font) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        var output = new StringBuilder(value.Length + 16);
        var changed = false;
        foreach (var element in TerminalTextWidth.Elements(value)) {
            var requiresShaping = RequiresShaping(element) || ContainsContextualShapingScalar(element);
            for (var index = 0; index < element.Length;) {
                var scalarStart = index;
                if (TerminalTextWidth.TryPreservedScalar(element, index, out var preservedLength, out _)) {
                    output.Append(element, index, preservedLength);
                    index += preservedLength;
                    continue;
                }
                var codePoint = ReadCodePoint(element, ref index);
                if (!requiresShaping &&
                    !TerminalTextWidth.IsZeroWidthScalar(codePoint) &&
                    codePoint != EscapeStart &&
                    codePoint != EscapeEnd &&
                    CanRender(codePoint, font)) {
                    output.Append(element, scalarStart, index - scalarStart);
                    continue;
                }

                AppendPreservedScalar(output, codePoint);
                changed = true;
            }
        }

        return changed ? output.ToString() : value;
    }

    public static double Measure(string value, RgbaCanvas canvas, double fontSize) {
        return MeasureCore(value, canvas, fontSize, null);
    }

    public static double Measure(string value, RgbaCanvas canvas, double fontSize, TrueTypeFont? font) {
        return MeasureCore(value, canvas, fontSize, font);
    }

    public static double MeasureEmphasized(string value, RgbaCanvas canvas, double fontSize) {
        var width = MeasureCore(value, canvas, fontSize, null);
        if (value.Length == 0) return width;
        var emphasisOffset = canvas.MeasureTextEmphasizedWidth("M", fontSize) - canvas.MeasureTextWidth("M", fontSize);
        return width + Math.Max(0, emphasisOffset);
    }

    public static double MeasureEmphasized(string value, RgbaCanvas canvas, double fontSize, TrueTypeFont? font) {
        var width = MeasureCore(value, canvas, fontSize, font);
        if (value.Length == 0) return width;
        var emphasisOffset = RgbaCanvas.MeasureTextEmphasizedWidth("M", fontSize, font) - RgbaCanvas.MeasureTextWidthWithFont("M", fontSize, font);
        return width + Math.Max(0, emphasisOffset);
    }

    private static double MeasureCore(string value, RgbaCanvas canvas, double fontSize, TrueTypeFont? font) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var width = 0.0;
        foreach (var unit in RasterUnits(value)) {
            width += ContainsPreservedScalar(unit)
                ? TerminalTextWidth.Measure(unit) * fontSize * ColumnWidthFactor
                : font == null ? canvas.MeasureTextWidth(unit, fontSize) : RgbaCanvas.MeasureTextWidth(unit, fontSize, font);
        }
        return width;
    }

    public static void Draw(RgbaCanvas canvas, double x, double y, string value, ChartColor color, double fontSize) {
        Draw(canvas, x, y, value, color, fontSize, false, null, true);
    }

    public static void Draw(RgbaCanvas canvas, double x, double y, string value, ChartColor color, double fontSize, TrueTypeFont? font) {
        Draw(canvas, x, y, value, color, fontSize, false, font, false);
    }

    public static void DrawEmphasized(RgbaCanvas canvas, double x, double y, string value, ChartColor color, double fontSize) {
        Draw(canvas, x, y, value, color, fontSize, true, null, true);
    }

    public static void DrawEmphasized(RgbaCanvas canvas, double x, double y, string value, ChartColor color, double fontSize, TrueTypeFont? font) {
        Draw(canvas, x, y, value, color, fontSize, true, font, false);
    }

    private static void Draw(RgbaCanvas canvas, double x, double y, string value, ChartColor color, double fontSize, bool emphasized, TrueTypeFont? font, bool useCanvasFont) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (value == null) throw new ArgumentNullException(nameof(value));
        var cursor = x;
        foreach (var unit in RasterUnits(value)) {
            if (!ContainsPreservedScalar(unit)) {
                if (useCanvasFont) {
                    if (emphasized) canvas.DrawTextEmphasized(cursor, y, unit, color, fontSize);
                    else canvas.DrawText(cursor, y, unit, color, fontSize);
                    cursor += canvas.MeasureTextWidth(unit, fontSize);
                } else {
                    if (emphasized) canvas.DrawTextEmphasized(cursor, y, unit, color, fontSize, font);
                    else canvas.DrawText(cursor, y, unit, color, fontSize, font);
                    cursor += RgbaCanvas.MeasureTextWidthWithFont(unit, fontSize, font);
                }
                continue;
            }

            var width = TerminalTextWidth.Measure(unit) * fontSize * ColumnWidthFactor;
            var label = IsContextualFallbackUnit(unit)
                ? ContextualFallbackLabel(unit)
                : ClusterFallbackLabel(unit);
            if (width > 0 && label.Length > 0) {
                if (useCanvasFont) {
                    if (emphasized) canvas.DrawTextFittedEmphasized(cursor, y, label, color, fontSize, width);
                    else canvas.DrawTextFitted(cursor, y, label, color, fontSize, width);
                } else {
                    if (emphasized) canvas.DrawTextFittedEmphasized(cursor, y, label, color, fontSize, width, font);
                    else canvas.DrawTextFitted(cursor, y, label, color, fontSize, width, font);
                }
            }
            cursor += width;
        }
    }

    internal static IEnumerable<string> RasterUnits(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var elements = new List<string>(TerminalTextWidth.Elements(value));
        for (var index = 0; index < elements.Count;) {
            if (!IsContextualFallbackElement(elements[index]) &&
                !(TerminalStoryLayout.DisplayWidth(elements[index]) == 0 &&
                  index + 1 < elements.Count &&
                  IsContextualFallbackElement(elements[index + 1]))) {
                yield return elements[index++];
                continue;
            }

            var run = new StringBuilder();
            while (index < elements.Count &&
                   (IsContextualFallbackElement(elements[index]) ||
                    TerminalStoryLayout.DisplayWidth(elements[index]) == 0)) {
                run.Append(elements[index++]);
            }
            yield return run.ToString();
        }
    }

    private static bool CanRender(int codePoint, TrueTypeFont? font) {
        if (font != null) {
            return font.HasGlyph(codePoint);
        }
        return codePoint <= char.MaxValue && TinyFont.Supports((char)codePoint);
    }

    private static bool RequiresShaping(string element) {
        var scalarCount = 0;
        for (var index = 0; index < element.Length;) {
            if (TerminalTextWidth.TryPreservedScalar(element, index, out var preservedLength, out _)) {
                index += preservedLength;
            } else {
                ReadCodePoint(element, ref index);
            }
            scalarCount++;
            if (scalarCount > 1) {
                return true;
            }
        }
        return false;
    }

    private static bool ContainsContextualShapingScalar(string value) {
        for (var index = 0; index < value.Length;) {
            int codePoint;
            if (TerminalTextWidth.TryPreservedScalar(value, index, out var preservedLength, out codePoint)) {
                index += preservedLength;
            } else {
                codePoint = ReadCodePoint(value, ref index);
            }
            if (TerminalJoiningType.RequiresContextualShaping(codePoint)) {
                return true;
            }
        }
        return false;
    }

    private static bool IsContextualFallbackElement(string value) {
        return ContainsPreservedScalar(value) && ContainsContextualShapingScalar(value);
    }

    private static bool IsContextualFallbackUnit(string value) {
        foreach (var element in TerminalTextWidth.Elements(value)) {
            if (IsContextualFallbackElement(element)) {
                return true;
            }
        }
        return false;
    }

    private static string ContextualFallbackLabel(string value) {
        var firstVisible = -1;
        var visibleCount = 0;
        for (var index = 0; index < value.Length;) {
            int codePoint;
            if (TerminalTextWidth.TryPreservedScalar(value, index, out var preservedLength, out codePoint)) {
                index += preservedLength;
            } else {
                codePoint = ReadCodePoint(value, ref index);
            }
            if (TerminalTextWidth.IsZeroWidthScalar(codePoint)) {
                continue;
            }
            if (firstVisible < 0) {
                firstVisible = codePoint;
            }
            visibleCount++;
        }
        if (firstVisible < 0) {
            return string.Empty;
        }
        return CompactLabel(firstVisible) + (visibleCount > 1 ? "…" : string.Empty);
    }

    private static void AppendPreservedScalar(StringBuilder output, int codePoint) {
        output.Append(EscapeStart);
        output.Append("[U+");
        output.Append(codePoint.ToString(codePoint <= ushort.MaxValue ? "X4" : "X", CultureInfo.InvariantCulture));
        output.Append(']');
        output.Append(EscapeEnd);
    }

    private static bool ContainsPreservedScalar(string value) {
        for (var index = 0; index < value.Length;) {
            if (TerminalTextWidth.TryPreservedScalar(value, index, out _, out _)) {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static string ClusterFallbackLabel(string value) {
        var plain = new StringBuilder(value.Length);
        var labels = new StringBuilder();
        var hasVisibleFallback = false;
        var labelCount = 0;
        var truncated = false;
        for (var index = 0; index < value.Length;) {
            if (!TerminalTextWidth.TryPreservedScalar(value, index, out var length, out var codePoint)) {
                var scalarStart = index;
                codePoint = ReadCodePoint(value, ref index);
                plain.Append(value, scalarStart, index - scalarStart);
                if (!TerminalTextWidth.IsZeroWidthScalar(codePoint)) {
                    AppendLabel(labels, codePoint, ref labelCount, ref truncated);
                }
                continue;
            }

            if (!TerminalTextWidth.IsZeroWidthScalar(codePoint)) {
                AppendLabel(labels, codePoint, ref labelCount, ref truncated);
                hasVisibleFallback = true;
            }
            index += length;
        }
        if (truncated) labels.Append(" …");
        return hasVisibleFallback ? labels.ToString() : plain.ToString();
    }

    private static void AppendLabel(StringBuilder labels, int codePoint, ref int labelCount, ref bool truncated) {
        if (labelCount >= MaximumFallbackLabels) {
            truncated = true;
            return;
        }
        if (labels.Length > 0) {
            labels.Append(' ');
        }
        labels.Append(CompactLabel(codePoint));
        labelCount++;
    }

    private static string CompactLabel(int codePoint) {
        return "U+" + codePoint.ToString("X", CultureInfo.InvariantCulture);
    }

    private static int ReadCodePoint(string value, ref int index) {
        var first = value[index++];
        if (!char.IsHighSurrogate(first) || index >= value.Length || !char.IsLowSurrogate(value[index])) {
            return first;
        }
        return char.ConvertToUtf32(first, value[index++]);
    }
}
