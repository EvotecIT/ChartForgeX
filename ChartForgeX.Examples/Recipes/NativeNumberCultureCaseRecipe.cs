using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Native built-in numeric cultures and actual caption case contexts with caller-supplied typography.</summary>
internal static class NativeNumberCultureCaseRecipe {
    internal static Chart Create(string kind, string policy, double reading, int width, VisualThemeMode mode, string family) {
        ChartSeries series; var id = new ChartSeriesId("revenue");
        if (kind == "treemap") series = ChartSeries.CreateTreemap(id, "Revenue", new[] { new ChartHierarchyItem("root", "Revenue"), new ChartHierarchyItem("primary", "Subscriptions", "root", reading), new ChartHierarchyItem("second", "Support", "root", .1), new ChartHierarchyItem("third", "Services", "root", .2) });
        else if (kind is "pie" or "donut") series = new ChartSeries(id, "Revenue", kind == "pie" ? ChartSeriesKind.Pie : ChartSeriesKind.Donut, new[] { new ChartForgeX.Primitives.ChartPoint(1, reading), new ChartForgeX.Primitives.ChartPoint(2, .1), new ChartForgeX.Primitives.ChartPoint(3, .2) }).WithObservationIds(new[] { new ChartObservationId("primary"), new ChartObservationId("second"), new ChartObservationId("third") });
        else throw new ArgumentOutOfRangeException(nameof(kind));
        var chart = Chart.Create().WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(family), mode).WithHeader(false).WithLegend(false).WithDataLabels(true).WithDonutCenterLabel(false)
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndValue).WithXLabels("Subscriptions", "Support", "Services").AddSeries(series);
        var (format, textCase) = Policy(policy); return chart.WithValueFormat(format).ConfigureDataLabelStyle(style => style.TextCase = textCase);
    }
    internal static (double First, double Last) Readings(string policy) => policy == "ar-percent" ? (9,11)
        : policy == "scaled" ? (900000,1100000) : policy == "permille" ? (.009,.011) : (900,1100);
    internal static (ChartValueFormat Format, TextCaseTransform Case) Policy(string policy) => policy switch {
        "fr-number" => (ChartValueFormat.Number("N2", CultureInfo.GetCultureInfo("fr-FR")), TextCaseTransform.None),
        "sv-currency" => (ChartValueFormat.Number("C2", CultureInfo.GetCultureInfo("sv-SE")), TextCaseTransform.SentenceCase),
        "ar-percent" => (ChartValueFormat.Number("P1", CultureInfo.GetCultureInfo("ar-SA")), TextCaseTransform.None),
        "fr-compact" => (ChartValueFormat.Compact(CultureInfo.GetCultureInfo("fr-FR")), TextCaseTransform.None),
        "title" => (ChartValueFormat.Compact(), TextCaseTransform.TitleCase), "sentence" => (ChartValueFormat.Compact(), TextCaseTransform.SentenceCase), "toggle" => (ChartValueFormat.Compact(), TextCaseTransform.ToggleCase),
        "literal-ms" => (ChartValueFormat.Number("0.00 'ms'"), TextCaseTransform.ToggleCase),
        "sections" => (ChartValueFormat.Number("0.0;[debit](0.0);'zero'"), TextCaseTransform.SentenceCase),
        "scaled" => (ChartValueFormat.Number("#,##0,, 'M'"), TextCaseTransform.None),
        "permille" => (ChartValueFormat.Number("0.0‰", CultureInfo.GetCultureInfo("fr-FR")), TextCaseTransform.None),
        "scientific" => (ChartValueFormat.Number("0.00E+00"), TextCaseTransform.TitleCase),
        "escaped" => (ChartValueFormat.Number(@"0.00\m\s"), TextCaseTransform.ToggleCase),
        _ => throw new ArgumentOutOfRangeException(nameof(policy))
    };
}
