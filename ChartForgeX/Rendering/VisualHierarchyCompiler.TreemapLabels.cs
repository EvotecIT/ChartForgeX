using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    /// <summary>Preserves complete names in ordinary cells and tall strips without changing their areas or readable type.</summary>
    private static void TreemapLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, string name, string? value,
        ChartRect bounds, ChartColor fill, int index, SvgPaint fillPaint, bool header) {
        var style = LabelStyle(chart, context, fill, index);
        var preferredInset = Math.Min(header ? 4 : 8, Math.Min(bounds.Width, bounds.Height) / 5);
        style = VisualTextFitting.FitOversized(builder, style, Math.Max(0, bounds.Height - preferredInset * 2), fitted => {
            var measured = builder.MeasureText(name, fitted);
            return measured.Width <= bounds.Width - preferredInset * 2 && measured.Height <= bounds.Height - preferredInset * 2;
        });
        var lineHeight = builder.MeasureText("M", style).LineHeight;
        var role = header ? "treemap-group-label" : "treemap-label";
        foreach (var inset in new[] { preferredInset, Math.Min(2, preferredInset) }) {
            if (TryHorizontal(inset, includeValue: true) || value != null && TryHorizontal(inset, includeValue: false)) return;
        }
        if (!header && bounds.Height > bounds.Width) {
            var inset = Math.Min(2, preferredInset);
            if (TryRotated(inset, includeValue: true) || value != null && TryRotated(inset, includeValue: false)) return;
        }
        builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A treemap caption was omitted because its complete name does not fit; complete text remains in source semantics."));

        bool TryHorizontal(double inset, bool includeValue) {
            // A constrained group header may still contain a full text row; padding gives way before type.
            var verticalInset = Math.Min(inset, Math.Max(0, (bounds.Height - lineHeight) / 2));
            var width = Math.Max(0, bounds.Width - inset * 2);
            var height = Math.Max(0, bounds.Height - verticalInset * 2);
            var rows = Math.Min(header ? 1 : value == null ? 2 : 3, (int)Math.Floor((height + .000001) / lineHeight));
            if (rows < 1 || width < 6) return false;
            var paragraphs = name.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var lines = new List<string>();
            if (header && includeValue && value != null) paragraphs = new[] { name + " · " + value };
            var reserved = !header && includeValue && value != null ? 1 : 0;
            for (var paragraph = 0; paragraph < paragraphs.Length; paragraph++) {
                var spare = rows - lines.Count - reserved - (paragraphs.Length - paragraph - 1);
                if (spare < 1) return false;
                lines.AddRange(spare > 1 ? ChartLabelWrapping.BalancedTwoLine(paragraphs[paragraph], style.EffectiveFontSize, width,
                    (text, _) => builder.MeasureText(text, style).Width) : new[] { paragraphs[paragraph] });
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
            if (value != null && !includeValue) builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A treemap value was omitted to preserve its complete primary caption; complete text remains in source semantics."));
            style.Alignment = rotated ? TextAlignment.Center : TextAlignment.Left;
            var paint = VisualChartPaint.ExplicitDataLabelColor(chart, index) ? VisualChartPaint.Text(style) : SvgPaint.Contrast(fill, fillPaint);
            using (builder.PushClip(bounds))
            using (rotated ? builder.PushRotation(-90, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2) : null)
                builder.Text(displayed, x, top + builder.TextAscent(style), style, role, paint: paint);
        }
    }
}
