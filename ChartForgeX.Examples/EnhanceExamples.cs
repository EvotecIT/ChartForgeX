using System.Globalization;
using System.Net;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Writes the enhanced-SVG example: three linked charts and a plain table standing in for a CanopyX grid.</summary>
internal static class EnhanceExamples {
    internal static void Write(string output) {
        var directory = Path.Combine(output, "enhance");
        Directory.CreateDirectory(directory);
        HtmlInteractiveAssetFiles.WriteTo(directory, HtmlEnhanceAssets.Files());
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "enhance-page.css"), Asset("enhance-page.css"));
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "enhance-page.js"), Asset("enhance-page.js"));
        var template = Asset("EnhanceExamples.html");
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "index.html"), Page(template, VisualThemeMode.Light, "auto"));
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "light.html"), Page(template, VisualThemeMode.Light, "light"));
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "dark.html"), Page(template, VisualThemeMode.Dark, "dark"));
    }

    internal static string Page(string template, VisualThemeMode mode, string theme) {
        var records = EnhanceRecipe.Records();
        return template.Replace("{{THEME}}", theme, StringComparison.Ordinal)
            .Replace("{{RUNTIME}}", HtmlEnhanceAssets.ReferenceMarkup(new HtmlAssetReferences("./")).TrimEnd(), StringComparison.Ordinal)
            .Replace("{{WEEK}}", Svg(EnhanceRecipe.Week(records, mode), "week", 360, 260, mode), StringComparison.Ordinal)
            .Replace("{{SUPPORT}}", Svg(EnhanceRecipe.Support(records, mode), "support", 360, 200, mode), StringComparison.Ordinal)
            .Replace("{{MONTH}}", Svg(EnhanceRecipe.Month(records, mode), "month", 360, 240, mode), StringComparison.Ordinal)
            .Replace("{{ROWS}}", Rows(records), StringComparison.Ordinal);
    }

    // Fluid charts keep text and tick density from their prepared width, so they are prepared for the narrowest page.
    private static string Svg(Chart chart, string id, int width, int height, VisualThemeMode mode) =>
        chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), 8),
                VisualTheme.Graphite(), mode, new VisualFrame("", "", showLegend: true, transparentBackground: true), FontSpec.FromFamily("system-ui, sans-serif")))
            .ToSvg(new VisualSvgOptions(id).WithFluidWidth().WithEnhanceHooks(includeData: true));

    private static string Rows(IReadOnlyList<EnhanceRecipe.Record> records) {
        var kinds = new Dictionary<string, string> { ["update"] = "Updates", ["note"] = "Publisher notes", ["retire"] = "Retirements" };
        var rows = new StringBuilder();
        foreach (var record in records.OrderByDescending(r => r.Day).ThenBy(r => r.Kind, StringComparer.Ordinal)) {
            string Text(string value) => WebUtility.HtmlEncode(value);
            var day = record.Day.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
            rows.Append("<tr data-key=\"").Append(Text(record.Id)).Append("\" data-day=\"").Append(day).Append("\" data-kind=\"").Append(Text(record.Kind))
                .Append("\" data-product=\"").Append(Text(record.Product)).Append("\"><td><input type=\"checkbox\" aria-label=\"Highlight ")
                .Append(Text(record.Title)).Append(", ").Append(record.Day.ToString("d MMM", CultureInfo.InvariantCulture)).Append("\"></td><td>")
                .Append(record.Day.ToString("ddd d MMM", CultureInfo.InvariantCulture)).Append("</td><td>").Append(Text(kinds[record.Kind]))
                .Append("</td><td>").Append(Text(record.Product)).Append("</td><td>").Append(Text(record.Title)).Append("</td></tr>");
        }
        return rows.ToString();
    }

    private static string Asset(string name) {
        using var stream = typeof(EnhanceExamples).Assembly.GetManifestResourceStream("ChartForgeX.Examples.Enhance." + name)
            ?? throw new InvalidOperationException("Missing enhance example asset: " + name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
