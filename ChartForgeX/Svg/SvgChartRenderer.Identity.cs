using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static string BuildProvisionalId(Chart chart, string idScope) {
        unchecked {
            uint hash = 2166136261;
            Add(ref hash, idScope ?? string.Empty);
            Add(ref hash, chart.Title);
            Add(ref hash, chart.Subtitle);
            Add(ref hash, chart.Options.Size.Width.ToString(CultureInfo.InvariantCulture));
            Add(ref hash, chart.Options.Size.Height.ToString(CultureInfo.InvariantCulture));
            AddVisualIdentity(ref hash, chart);
            foreach (var series in chart.Series) {
                Add(ref hash, series.Name);
                Add(ref hash, series.Kind.ToString());
                Add(ref hash, series.ShowInLegend.ToString(CultureInfo.InvariantCulture));
                Add(ref hash, series.SemanticRole ?? string.Empty);
                Add(ref hash, series.FillPattern.ToString());
                Add(ref hash, series.Color?.ToRgba().ToString("x8", CultureInfo.InvariantCulture) ?? string.Empty);
                foreach (var point in series.Points) {
                    Add(ref hash, point.X.ToString("R", CultureInfo.InvariantCulture));
                    Add(ref hash, point.Y.ToString("R", CultureInfo.InvariantCulture));
                }
                foreach (var color in series.PointColors) Add(ref hash, color?.ToRgba().ToString("x8", CultureInfo.InvariantCulture) ?? string.Empty);
                foreach (var label in series.PointLabels) Add(ref hash, label ?? string.Empty);
                foreach (var pattern in series.PointFillPatterns) Add(ref hash, pattern?.ToString() ?? string.Empty);
            }

            return "cfx" + hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    private static void AddVisualIdentity(ref uint hash, Chart chart) {
        var theme = chart.Options.Theme;
        Add(ref hash, theme.Background.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.CardBackground.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.PlotBackground.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.CardBorder.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.PlotBorder.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.Text.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.MutedText.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.Grid.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.Axis.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.Positive.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.Warning.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.Negative.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.ShadowColor.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        foreach (var color in theme.Palette) Add(ref hash, color.ToRgba().ToString("x8", CultureInfo.InvariantCulture));
        Add(ref hash, theme.UseCard.ToString(CultureInfo.InvariantCulture));
        Add(ref hash, theme.CornerRadius.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.PlotCornerRadius.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.StrokeWidth.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.ShadowOpacity.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.TitleFontSize.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.SubtitleFontSize.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.AxisTitleFontSize.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.TickLabelFontSize.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.LegendFontSize.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.DataLabelFontSize.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.MarkerRadius.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, theme.FontFamily);
        AddPictorialIdentity(ref hash, chart);
    }

    private static void AddPictorialIdentity(ref uint hash, Chart chart) {
        if (!IsPictorialChart(chart)) return;
        var options = chart.Options;
        Add(ref hash, options.PictorialShape.ToString());
        Add(ref hash, options.PictorialColumns.ToString(CultureInfo.InvariantCulture));
        Add(ref hash, options.PictorialMaximum?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty);
        Add(ref hash, options.PictorialValuePerSymbol?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty);
        Add(ref hash, options.ShowPictorialValues.ToString(CultureInfo.InvariantCulture));
        Add(ref hash, options.PictorialSymbolScale.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, options.PictorialEmptyOpacity.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, options.PictorialSvgPathData ?? string.Empty);
        Add(ref hash, options.PictorialSvgPathViewBox.X.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, options.PictorialSvgPathViewBox.Y.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, options.PictorialSvgPathViewBox.Width.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, options.PictorialSvgPathViewBox.Height.ToString("R", CultureInfo.InvariantCulture));
        Add(ref hash, options.PictorialPngFallbackShape.ToString());
    }

    private static void Add(ref uint hash, string value) {
        AddRaw(ref hash, value.Length.ToString(CultureInfo.InvariantCulture));
        AddRaw(ref hash, ":");
        AddRaw(ref hash, value);
        AddRaw(ref hash, "|");
    }

    private static void AddRaw(ref uint hash, string value) {
        foreach (var ch in value) {
            hash ^= ch;
            hash *= 16777619;
        }
    }
}
