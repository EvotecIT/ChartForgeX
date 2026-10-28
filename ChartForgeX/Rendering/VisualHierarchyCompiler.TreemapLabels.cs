using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    private static string TreemapHeaderCaption(string name, string value) => name + " · " + value;

    /// <summary>Preserves complete names in ordinary cells and tall strips without changing their areas or readable type.</summary>
    private static void TreemapLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, string name, string? value,
        ChartRect bounds, ChartColor fill, int index, SvgPaint fillPaint, HierarchyCaptionSurface? surface, bool header) {
        var style = LabelStyle(chart, context, fill, index);
        if (fill.A != 255 && !VisualChartPaint.ExplicitDataLabelColor(chart, index)) style.Color = surface!.Ink;
        var preferredInset = Math.Min(header ? 4 : 8, Math.Min(bounds.Width, bounds.Height) / 5);
        style = VisualTextFitting.FitOversized(builder, style, Math.Max(0, bounds.Height - preferredInset * 2), fitted => {
            var measured = builder.MeasureText(name, fitted);
            return measured.Width <= bounds.Width - preferredInset * 2 && measured.Height <= bounds.Height - preferredInset * 2;
        });
        var lineHeight = builder.MeasureText("M", style).LineHeight;
        var role = header ? "treemap-group-label" : "treemap-label";
        var probing = false;
        if (TryComplete() || TrySmallerPrimary() || TryTrimmed()) return;
        builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A treemap caption was omitted because its complete name does not fit; complete text remains in source semantics."));

        bool TryComplete() {
            lineHeight = builder.MeasureText("M", style).LineHeight;
            foreach (var inset in new[] { preferredInset, Math.Min(2, preferredInset) }) {
                if (TryHorizontal(inset, includeValue: true) || value != null && TryHorizontal(inset, includeValue: false)) return true;
            }
            if (!header && bounds.Height > bounds.Width) {
                var inset = Math.Min(2, preferredInset);
                if (TryRotated(inset, includeValue: true) || value != null && TryRotated(inset, includeValue: false)) return true;
            }
            return false;
        }

        bool TrySmallerPrimary() {
            var minimum = chart.Options.Treemap.MinimumLabelFontSize;
            var preferred = style.FontSize;
            if (!minimum.HasValue || minimum.Value >= preferred) return false;
            probing = true;
            if (!AtSize(minimum.Value)) { probing = false; style.FontSize = preferred; return false; }
            var fitted = Math.Max(minimum.Value, ChartTextFitting.FitFontSize(minimum.Value, preferred, AtSize));
            if (!AtSize(fitted)) fitted = minimum.Value;
            probing = false;
            return AtSize(fitted);
        }

        bool AtSize(double size) { style.FontSize = size; return TryComplete(); }

        bool TryTrimmed() {
            if (chart.Options.Treemap.LabelTrimming != TextTrimming.Ellipsis) return false;
            if (chart.Options.Treemap.MinimumLabelFontSize is double minimum) style.FontSize = Math.Min(style.FontSize, minimum);
            lineHeight = builder.MeasureText("M", style).LineHeight;
            var inset = Math.Min(2, preferredInset);
            var verticalInset = Math.Min(inset, Math.Max(0, (bounds.Height - lineHeight) / 2));
            var width = Math.Max(0, bounds.Width - inset * 2);
            var height = Math.Max(0, bounds.Height - verticalInset * 2);
            var rows = (int)Math.Floor((height + .000001) / lineHeight);
            if (header) rows = Math.Min(1, rows);
            if (rows < 1 || width < 6) return false;
            var layout = builder.LayoutText(name, width, style, maximumLines: rows, trimming: TextTrimming.Ellipsis);
            var displayed = string.Join("\n", layout.Lines.Select(line => line.Text));
            if (string.IsNullOrWhiteSpace(displayed) || displayed == "…" || layout.Metrics.Width > width + .000001 || layout.Metrics.Height > height + .000001) return false;
            Paint(displayed, bounds.Left + inset, bounds.Top + verticalInset, rotated: false, includeValue: false);
            if (layout.Trimmed) builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A treemap primary caption was shortened at a whole text-element boundary; complete text remains in source semantics."));
            return true;
        }

        bool TryHorizontal(double inset, bool includeValue) {
            // A constrained group header may still contain a full text row; padding gives way before type.
            var verticalInset = Math.Min(inset, Math.Max(0, (bounds.Height - lineHeight) / 2));
            var width = Math.Max(0, bounds.Width - inset * 2);
            var height = Math.Max(0, bounds.Height - verticalInset * 2);
            var rows = (int)Math.Floor((height + .000001) / lineHeight);
            if (header) rows = Math.Min(1, rows);
            if (rows < 1 || width < 6) return false;
            var paragraphs = name.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var lines = new List<string>();
            if (header && includeValue && value != null) paragraphs = new[] { TreemapHeaderCaption(name, value) };
            var reserved = !header && includeValue && value != null ? 1 : 0;
            for (var paragraph = 0; paragraph < paragraphs.Length; paragraph++) {
                var spare = rows - lines.Count - reserved - (paragraphs.Length - paragraph - 1);
                if (spare < 1) return false;
                if (!ChartLabelWrapping.TryComplete(paragraphs[paragraph], style.EffectiveFontSize, width, spare,
                    (text, _) => builder.MeasureText(text, style).Width, out var complete)) return false;
                lines.AddRange(complete);
            }
            if (reserved > 0) lines.Add(value!);
            var displayed = string.Join("\n", lines);
            var measured = builder.MeasureText(displayed, style);
            if (lines.Count > rows || measured.Width > width + .000001 || measured.Height > height + .000001) return false;
            Paint(displayed, bounds.Left + inset, bounds.Top + verticalInset, rotated: false, includeValue);
            return true;
        }

        bool TryRotated(double inset, bool includeValue) {
            // A full word is easier to read along a tall strip than a stack of isolated characters.
            var displayed = name.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ')
                + (includeValue && value != null ? "\n" + value : "");
            var measured = builder.MeasureText(displayed, style);
            if (measured.Width > bounds.Height - inset * 2 + .000001 || measured.Height > bounds.Width - inset * 2 + .000001) return false;
            Paint(displayed, bounds.Left + bounds.Width / 2, bounds.Top + (bounds.Height - measured.Height) / 2,
                rotated: true, includeValue);
            return true;
        }

        void Paint(string displayed, double x, double top, bool rotated, bool includeValue) {
            if (probing) return;
            if (value != null && !includeValue) builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A treemap value was omitted to preserve its complete primary caption; complete text remains in source semantics."));
            style.Alignment = rotated ? TextAlignment.Center : TextAlignment.Left;
            var paint = VisualChartPaint.ExplicitDataLabelColor(chart, index) ? VisualChartPaint.Text(style) : surface?.Paint(fill, fillPaint, style.Color) ?? SvgPaint.Contrast(fill, fillPaint);
            using (builder.PushClip(bounds))
            using (rotated ? builder.PushRotation(-90, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2) : null)
                builder.Text(displayed, x, top + builder.TextAscent(style), style, role, paint: paint);
        }
    }
}
