using System.Net;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Stories;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Writes script-free embedding pages and a source-and-output visual story.</summary>
internal static class ServerRenderedChartExamples {
    internal static void Write(string output) {
        var directory = Path.Combine(output, "embedding");
        Directory.CreateDirectory(directory);
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "embedding.css"), Asset("ServerRenderedChartExamples.css"));
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "ServerRenderedRecipe.cs.txt"), RecipeSource());
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "shared-strip.csharp.txt"),
            ExampleRecipeSource.EmitServerRenderedStrip("shared-strip", VisualThemeMode.Light, 720, 48));
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "compact-shared-strip.csharp.txt"),
            ExampleRecipeSource.EmitServerRenderedStrip("compact-shared-strip", VisualThemeMode.Light, 320, 48));
        var template = Asset("ServerRenderedChartExamples.html");
        var light = Page(template, VisualThemeMode.Light);
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "index.html"), light.Replace("{{THEME}}", "auto", StringComparison.Ordinal));
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "light.html"), light.Replace("{{THEME}}", "light", StringComparison.Ordinal));
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "dark.html"), Page(template, VisualThemeMode.Dark).Replace("{{THEME}}", "dark", StringComparison.Ordinal));
        WriteStory(directory);
    }

    private static string Page(string template, VisualThemeMode mode) {
        var columns = ServerRenderedRecipe.LinkedColumns(mode);
        var schedule = ServerRenderedRecipe.SupportSchedule(mode);
        var scale = ServerRenderedRecipe.SharedScale();
        var page = template.Replace("{{COLUMNS}}", columns.Prepare(Context(720, 252, mode)).ToSvg(Policy("activity", mode)), StringComparison.Ordinal)
            .Replace("{{GANTT}}", schedule.Prepare(Context(320, 232, mode)).ToSvg(Policy("support", mode)), StringComparison.Ordinal)
            .Replace("{{AXIS}}", scale.PrepareAxis(Context(720, 32, mode, padding: 0)).ToSvg(Policy("release-axis", mode)), StringComparison.Ordinal)
            .Replace("{{DAILY_ROWS}}", DailyRows(columns), StringComparison.Ordinal)
            .Replace("{{STRIP_SOURCE}}", WebUtility.HtmlEncode(Asset("shared-strip.csharp.txt")), StringComparison.Ordinal);
        for (var row = 0; row < 3; row++) {
            var prepared = ServerRenderedRecipe.CreateStrip(row, mode).PrepareStrip(Context(720, 48, mode, padding: 0), scale);
            page = page.Replace("{{STRIP_" + row + "}}", prepared.ToSvg(Policy("release-track-" + row, mode)), StringComparison.Ordinal);
        }
        return page;
    }

    internal static VisualRenderContext Context(int width, int height, VisualThemeMode mode, double padding = 8, string? family = null) =>
        new(new VisualLayoutOptions(new VisualSize(width, height), padding),
            VisualTheme.Graphite().WithTypography(new VisualTypography(axisSize: 14, legendSize: 14, dataLabelSize: 14)), mode,
            new VisualFrame("", "", showLegend: false, transparentBackground: true), FontSpec.FromFamily(family ?? "system-ui, sans-serif"));

    private static VisualSvgOptions Policy(string id, VisualThemeMode mode) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        var variables = new SvgColorVariables().Add("--page-ink", colors.Foreground, SvgColorRole.Text)
            .Add("--page-muted", colors.MutedForeground, SvgColorRole.Text)
            .Add("--page-line", colors.Grid, SvgColorRole.Grid).Add("--page-line", colors.Axis, SvgColorRole.Axis)
            .Add("--page-surface", colors.Background, SvgColorRole.Surface).Add("--page-surface", colors.Surface, SvgColorRole.Surface);
        return new VisualSvgOptions(id, variables).WithFluidWidth()
            .WithFontVariables(familyVariable: "--page-font", sizeVariable: "--chart-font-size");
    }

    private static string DailyRows(Chart chart) {
        var rows = new StringBuilder();
        for (var index = 0; index < chart.Series[0].Points.Count; index++) {
            var point = chart.Series[0].Points[index];
            var day = point.MarkOptions!.DataAttributes["data-day"];
            var label = DateTime.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("ddd d MMM", CultureInfo.InvariantCulture);
            rows.Append("<tr id=\"day-").Append(WebUtility.HtmlEncode(day)).Append("\"><th scope=\"row\">")
                .Append(WebUtility.HtmlEncode(label)).Append("</th><td>").Append(point.Y.ToString("0", CultureInfo.InvariantCulture))
                .Append("</td><td>").Append(chart.Series[1].Points[index].Y.ToString("0", CultureInfo.InvariantCulture)).Append("</td></tr>");
        }
        return rows.ToString();
    }

    private static void WriteStory(string directory) {
        var mode = VisualThemeMode.Light;
        var columns = ServerRenderedRecipe.LinkedColumns(mode).Prepare(Context(640, 300, mode));
        var gantt = ServerRenderedRecipe.SupportSchedule(mode).Prepare(Context(640, 300, mode));
        var strip = ServerRenderedRecipe.CreateStrip(1, mode).PrepareStrip(Context(640, 80, mode, padding: 0), ServerRenderedRecipe.SharedScale());
        var story = VisualStory.Create("Charts for a server-rendered page")
            .WithDescription("Mark links, an open support window and an HTML-friendly shared-domain strip, with their C# source.")
            .WithTheme(VisualTheme.Graphite(), mode).WithSize(1200, 720);
        Scene("links", "Give each stacked segment its own destination", columns, "linked-columns.csharp.txt",
            "Five stacked columns. Updates link to daily summaries; publisher notes open in a new tab.");
        Scene("schedule", "Mark the current row and let open support continue", gantt, "support-schedule.csharp.txt",
            "Three release rows. The 2026 row is current and its open support bar fades to the right.");
        Scene("strips", "Keep the label in HTML and share the scale", strip, "shared-strip.csharp.txt",
            "The 2026 release track against the shared January 2026 to January 2027 domain.");
        story.Outcome("page-charts", "The chart output and its embedding source are visible", "output-strips");
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "embedding-story.svg"), story.ToSvg("embedding-story"));
        File.WriteAllBytes(Path.Combine(directory, "embedding-story.png"), story.ToPng());
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "embedding-story.html"), story.ToHtmlPage());
        ExampleArtifactWriter.WriteText(Path.Combine(directory, "embedding-story.txt"), story.ToTranscript());

        void Scene(string id, string title, PreparedVisual visual, string sourceAsset, string description) {
            story.Scene(id, title, 3, VisualStorySceneLayout.Split)
                .Panel("source-" + id, new VisualStorySourceSurface(StorySourceText.Create(Asset(sourceAsset).TrimEnd(), "csharp"), "C# source"), weight: 1.1)
                .Panel("output-" + id, new VisualStoryMediaSurface(visual.ToPng(), description), "Static chart snapshot", weight: 1);
        }
    }

    private static string Asset(string name) => Read("ChartForgeX.Examples.Embedding." + name);
    private static string RecipeSource() => Read("ChartForgeX.Examples.Recipes.ServerRenderedRecipe.cs");
    private static string Read(string name) {
        using var stream = typeof(ServerRenderedChartExamples).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Missing embedding example asset: " + name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
