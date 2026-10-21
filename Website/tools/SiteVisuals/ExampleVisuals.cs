// Theme and export wrappers for the compiled recipes shown on the curated example pages.
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace SiteVisuals;

internal static class ExampleVisuals {
    public static Dictionary<string, object> Write(VisualOutput output) {
        var examples = new SortedDictionary<string, object>(StringComparer.Ordinal);

        examples["static-chart"] = Save(output, "static-chart", nameof(DeploymentHealthWebsiteRecipe), new[] { "chart", "save", "points" },
            mode => StaticChart(mode).ToSvg("cfx-ex-chart"),
            mode => StaticChart(mode).ToPng(),
            mode => StaticChart(mode).ToHtmlPage());

        examples["report-grid"] = Save(output, "report-grid", nameof(CpuBySiteWebsiteRecipe), new[] { "records", "grid", "save", "record" },
            mode => ReportGrid(mode).ToSvg("cfx-ex-grid"),
            mode => ReportGrid(mode).ToPng(),
            mode => ReportGrid(mode).ToHtmlPage());

        examples["organization-hierarchy"] = Save(output, "organization-hierarchy", nameof(PlatformTeamWebsiteRecipe), new[] { "people", "chart", "save", "helpers" },
            mode => TeamHierarchy(mode).ToSvg(),
            mode => TeamHierarchy(mode).ToPng(),
            mode => TeamHierarchy(mode).ToHtmlPage());

        // The interactive example's output is the explorer page itself; the static SVG is its card image.
        var estate = output.Pair("examples/interactive-topology", (_, dark) => Estate().ToGraphSvg(configure: options => {
            options.Width = 1180;
            options.Height = 640;
            options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
        }));
        output.Text("examples/interactive-topology.html", Estate().ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = HtmlGraphRenderBackend.Svg;
            options.Theme = HtmlGraphExplorerTheme.System;
            options.IncludeThemeToggle = true;
        }));
        estate["interactive"] = true;
        SiteRecipeSource.Attach(output, estate, "interactive-topology", nameof(EstateExplorerWebsiteRecipe), false, "estate", "sites", "layout", "save");
        examples["interactive-topology"] = estate;
        return new Dictionary<string, object>(examples, StringComparer.Ordinal);
    }

    private static Dictionary<string, object> Save(VisualOutput output, string slug, string recipe, string[] sections,
        Func<VisualThemeMode, string> svg, Func<VisualThemeMode, byte[]> png, Func<VisualThemeMode, string> html) {
        var entry = output.Pair("examples/" + slug, (mode, _) => svg(mode));
        long pngBytes = 0;
        foreach (var (name, mode, _) in output.Modes()) {
            var bytes = png(mode);
            pngBytes = Math.Max(pngBytes, bytes.Length);
            output.Binary($"examples/{slug}-{name}.png", bytes);
            output.Text($"examples/{slug}-{name}.html", html(mode));
        }

        entry["pngKb"] = Math.Round(pngBytes / 1024d);
        entry["interactive"] = false;
        SiteRecipeSource.Attach(output, entry, slug, recipe, true, sections);
        return entry;
    }

    // Documentation figures reuse these entry points; model source lives in the compiled recipes.
    public static Chart StaticChart(VisualThemeMode mode) => DeploymentHealthWebsiteRecipe.Create(mode);
    public static ChartGrid ReportGrid(VisualThemeMode mode) => CpuBySiteWebsiteRecipe.Create(mode);
    public static TopologyChart TeamHierarchy(VisualThemeMode mode) => PlatformTeamWebsiteRecipe.Create(mode);
    public static GraphScene Estate() => EstateExplorerWebsiteRecipe.Create();
}
