using System;
using System.Collections.Generic;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

public sealed partial class ChartGrid {
    /// <summary>Splits this grid into ordered pages with at most the specified number of charts.</summary>
    /// <param name="maximumChartsPerPage">The maximum chart count per page, independent of panel spans.</param>
    /// <returns>Read-only page metadata and independently configurable grids; an empty grid produces no pages.</returns>
    /// <remarks>
    /// Charts and themes retain their original references. Apply shared axis bounds before pagination
    /// to compare charts across all pages. Grid headings, styles, panel spans and export settings are copied.
    /// Empty columns are retained on composed pages. Automatic panel sizing is resolved separately per page; set PanelSize for uniform panel dimensions.
    /// This bounds chart count, not pixel dimensions or rows occupied by spanned panels.
    /// </remarks>
    public IReadOnlyList<ChartGridPage> Paginate(int maximumChartsPerPage = 6) {
        if (maximumChartsPerPage < 1) throw new ArgumentOutOfRangeException(nameof(maximumChartsPerPage), maximumChartsPerPage, "Page capacity must be greater than zero.");
        if (_charts.Count == 0) return Array.Empty<ChartGridPage>();

        var pageCount = (_charts.Count - 1) / maximumChartsPerPage + 1;
        var pages = new List<ChartGridPage>(pageCount);
        for (var first = 0; first < _charts.Count;) {
            var count = Math.Min(maximumChartsPerPage, _charts.Count - first);
            var grid = new ChartGrid {
                Title = Title,
                Subtitle = Subtitle,
                Columns = Columns,
                PreserveEmptyColumns = true,
                Gap = Gap,
                Padding = Padding,
                PngOutputScale = PngOutputScale,
                PanelSize = PanelSize,
                PanelFit = PanelFit,
                Theme = Theme ?? _charts[0].Options.Theme
            };
            CopyStyle(TitleStyle, grid.TitleStyle);
            CopyStyle(SubtitleStyle, grid.SubtitleStyle);
            for (var i = first; i < first + count; i++) {
                var span = _panelSpans[i];
                grid.Add(_charts[i], span.ColumnSpan, span.RowSpan);
            }
            pages.Add(new ChartGridPage(grid, pages.Count + 1, pageCount, first, _charts.Count));
            first += count;
        }
        return pages.AsReadOnly();
    }

    private static void CopyStyle(TextStyleOverride source, TextStyleOverride target) {
        target.Color = source.Color;
        target.FontFamily = source.FontFamily;
        target.FontWeight = source.FontWeight;
        target.FontSize = source.FontSize;
        target.Italic = source.Italic;
        target.Underline = source.Underline;
        target.UnderlineStyle = source.UnderlineStyle;
        target.Strikethrough = source.Strikethrough;
        target.StrikethroughStyle = source.StrikethroughStyle;
        target.Baseline = source.Baseline;
        target.TextCase = source.TextCase;
    }
}
