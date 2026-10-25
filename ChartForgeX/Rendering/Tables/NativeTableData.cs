using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Rendering;

// Materialized input shared by optional tables and embedded topology. No retained models or callbacks.
internal sealed class NativeTableData {
    internal string Id = "", ContentId = "", Title = "", Subtitle = "";
    internal ResolvedVisualTheme Theme = null!;
    internal FontSpec Font = null!;
    internal ChartRect Bounds;
    internal bool Dense, ShowHeader = true, RowStriping = true;
    internal int? StatusColumn, WeightOverride;
    internal double Scale = 1, Opacity = 1;
    internal ChartColor? Foreground;
    internal double? RowWindowBottom;
    // An authored node FontSize is an absolute current-output role size, independent of captured geometry scale.
    internal double? AbsoluteDataLabelSize;
    internal List<NativeTableColumn> Columns { get; } = new();
    internal List<NativeTableRow> Rows { get; } = new();
    internal static NativeTableData Capture(TableArtifact table, ChartRect bounds, ResolvedVisualTheme theme, FontSpec font, string id, double scale = 1) {
        var data = new NativeTableData { Id = id, ContentId = table.Id, Title = table.Title, Subtitle = table.Subtitle,
            Bounds = bounds, Theme = theme, Font = font.Clone(), Scale = scale * table.CapturedPresentationScale };
        for (var column = 0; column < table.Columns.Count; column++) {
            var item = table.Columns[column]; data.Columns.Add(new NativeTableColumn(item.Id, item.Label, item.Alignment, item.Width));
            if (!data.StatusColumn.HasValue && item.Type == TableArtifactColumnType.Status) data.StatusColumn = column;
        }
        foreach (var row in table.Rows) {
            var copy = new NativeTableRow { Key = row.Key };
            foreach (var cell in row.Cells) copy.Cells.Add(new NativeTableCell { Text = cell.DisplayText,
                Status = cell.Status == VisualStatus.None ? row.Status : cell.Status });
            data.Rows.Add(copy);
        }
        return data;
    }
}
internal sealed class NativeTableColumn {
    internal NativeTableColumn(string id, string label, TextAlignment alignment, double? width) { Id = id; Label = label; Alignment = alignment; Width = width; }
    internal string Id { get; }
    internal string Label { get; }
    internal TextAlignment Alignment { get; }
    internal double? Width { get; }
}
internal sealed class NativeTableRow {
    internal string Key = "";
    internal ChartColor? Background, Foreground;
    internal List<NativeTableCell> Cells { get; } = new();
}
internal enum NativeTableMicroVisual { None, MiniBars, Sparkline }
internal enum NativeTableBadgeTreatment { Soft, Solid, Outline }
internal sealed class NativeTableCell {
    internal string Text = "", BadgeText = "";
    internal TextAlignment? Alignment;
    internal ChartColor? Foreground, Background, MicroColor, BadgeColor;
    internal VisualStatus Status, BadgeStatus;
    internal NativeTableBadgeTreatment BadgeTreatment;
    internal NativeTableMicroVisual MicroVisual;
    internal double? Minimum, Maximum;
    internal List<double> Values { get; } = new();
    internal SparklineData? Sparkline;
}
