using System.Text;
using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void AppendTile(StringBuilder page, string output, ProofArtifact light, IReadOnlyList<ProofArtifact> artifacts) {
        var variants = artifacts.Where(artifact => artifact.Family == light.Family && artifact.Theme == "light").ToArray();
        var search = string.Join(" ", new[] { FamilyLabel(light.Family), light.Title, GroupFor(light.Family) }
            .Concat(variants.SelectMany(ArtifactKinds)).Concat(variants.Select(artifact => VariantLabel(artifact.Variant))));
        page.Append("<article class=\"tile\" data-gallery-family=\"").Append(light.Family).Append("\" data-group=\"")
            .Append(GroupFor(light.Family)).Append("\" data-search=\"").Append(Escape(search)).Append("\">");
        foreach (var artifact in PairedArtifacts(artifacts, light)) {
            page.Append("<a class=\"preview\" data-visual-theme=\"").Append(artifact.Theme).Append("\" href=\"").Append(artifact.Id)
                .Append(".html\"");
            AppendThemeLinkAttributes(page, artifacts, artifact, ".html", preferCompact: true);
            page.Append(" aria-label=\"Explore ").Append(Escape(FamilyLabel(artifact.Family))).Append(": ").Append(Escape(artifact.Title)).Append("\">")
                .Append(File.ReadAllText(Path.Combine(output, artifact.Id + ".thumbnail.svg"))).Append("</a>");
        }
        page.Append("<div class=\"caption\"><p class=\"tile-family\">").Append(Escape(FamilyLabel(light.Family))).Append("</p><h2>");
        AppendPairedLink(page, artifacts, light, light.Title, ".html", preferCompact: true);
        page.Append("</h2><p class=\"tile-detail\">").Append(variants.Length == 1 ? "SVG, PNG and C# source" : variants.Length + " examples · SVG, PNG and C# source")
            .Append("</p></div></article>");
    }

    private static void WritePage(string output, ProofArtifact selected, IReadOnlyList<ProofArtifact> artifacts) {
        var paired = PairedArtifacts(artifacts, selected).ToArray();
        var variants = artifacts.Where(artifact => artifact.Family == selected.Family && artifact.Theme == "light").ToArray();
        var page = new StringBuilder();
        StartPage(page, selected.Title + " · " + FamilyLabel(selected.Family), selected.Theme, "example");
        page.Append("<header class=\"example-header\"><div class=\"header-top\"><a class=\"back-link\" href=\"catalog.html\">← Gallery</a>");
        AppendThemeControl(page);
        page.Append("</div><p class=\"eyebrow\">").Append(Escape(FamilyLabel(selected.Family))).Append("</p><h1>").Append(Escape(selected.Title))
            .Append("</h1><p class=\"intro\">Explore the example, download the output or use the C# source in your own project.</p></header><main id=\"main-content\" class=\"example-main\">");
        page.Append("<nav class=\"variant-nav\" aria-label=\"Example variants\">");
        foreach (var variant in variants.OrderBy(artifact => artifact.Variant == "wide" || artifact.Variant == "expanded" ? 0 : 1).ThenBy(artifact => artifact.Variant, StringComparer.Ordinal)) {
            var current = ExampleKey(variant) == ExampleKey(selected);
            var label = VariantLabel(variant.Variant) + (variants.Count(item => item.Variant == variant.Variant) > 1 ? " · " + variant.Title : "");
            page.Append("<span").Append(current ? " class=\"current-variant\"" : "").Append('>');
            AppendPairedLink(page, artifacts, variant, label, ".html", current);
            page.Append("</span>");
        }
        page.Append("</nav><div class=\"output-control\" role=\"group\" aria-label=\"Preview format\"><span>Preview</span><button type=\"button\" data-set-output=\"svg\" aria-pressed=\"true\">SVG</button><button type=\"button\" data-set-output=\"png\" aria-pressed=\"false\">PNG</button></div><section class=\"example-surface\" aria-label=\"Rendered example\">");
        foreach (var artifact in paired) {
            page.Append("<div class=\"full\" style=\"--example-width:").Append(artifact.Width).Append("px\" data-visual-theme=\"").Append(artifact.Theme).Append("\" data-output-format=\"svg\">")
                .Append(File.ReadAllText(Path.Combine(output, artifact.Id + ".svg"))).Append("</div>");
            page.Append("<div class=\"full\" style=\"--example-width:").Append(artifact.Width).Append("px\" data-visual-theme=\"").Append(artifact.Theme).Append("\" data-output-format=\"png\"><img src=\"")
                .Append(artifact.Id).Append(".png\" width=\"").Append(artifact.Width).Append("\" height=\"").Append(artifact.Height)
                .Append("\" alt=\"").Append(Escape(artifact.Title)).Append("\" loading=\"lazy\" decoding=\"async\"></div>");
        }
        page.Append("</section><div class=\"export-row\"><span>").Append(selected.Width).Append(" × ").Append(selected.Height).Append(" pixels</span><nav aria-label=\"Download outputs\">");
        AppendPairedLink(page, artifacts, selected, "Download SVG", ".svg", download: true);
        AppendPairedLink(page, artifacts, selected, "Download PNG", ".png", download: true);
        AppendPairedLink(page, artifacts, selected, "Download C#", ".csharp.txt", download: true);
        page.Append("</nav></div>");
        foreach (var artifact in paired.Where(artifact => artifact.Diagnostics.Length > 0)) {
            page.Append("<aside class=\"layout-notes\" data-layout-notes data-visual-theme=\"").Append(artifact.Theme)
                .Append("\" aria-label=\"Layout notes for this size\"><h2>Layout notes for this size</h2><ul>");
            foreach (var message in (artifact.DiagnosticMessages ?? artifact.Diagnostics).Distinct(StringComparer.Ordinal))
                page.Append("<li>").Append(Escape(message)).Append("</li>");
            page.Append("</ul></aside>");
        }
        page.Append("<section class=\"source-section\" aria-labelledby=\"source-heading\"><div class=\"source-heading\"><h2 id=\"source-heading\">Create this example</h2><button type=\"button\" data-copy-source>Copy C#</button><span data-copy-status role=\"status\"></span></div><p>Register the included Carlito font files to reproduce the exported typography.</p>");
        foreach (var artifact in paired)
            page.Append("<pre data-visual-theme=\"").Append(artifact.Theme).Append("\"><code data-example-source>")
                .Append(Escape(File.ReadAllText(Path.Combine(output, artifact.Id + ".csharp.txt")))).Append("</code></pre>");
        page.Append("</section></main><footer><a href=\"catalog.html\">Explore more examples</a><a href=\"fonts/OFL.txt\">Font license</a></footer>");
        EndPage(page);
        ExampleArtifactWriter.WriteText(Path.Combine(output, selected.Id + ".html"), page.ToString());
    }

    private static void AppendPairedLink(StringBuilder page, IReadOnlyList<ProofArtifact> artifacts, ProofArtifact artifact, string text, string extension,
        bool current = false, bool download = false, bool preferCompact = false) {
        page.Append("<a href=\"").Append(artifact.Id).Append(extension).Append('"');
        AppendThemeLinkAttributes(page, artifacts, artifact, extension, preferCompact);
        if (current) page.Append(" aria-current=\"page\"");
        if (download) page.Append(" download");
        page.Append('>').Append(Escape(text)).Append("</a>");
    }

    private static void AppendThemeLinkAttributes(StringBuilder page, IReadOnlyList<ProofArtifact> artifacts, ProofArtifact artifact, string extension, bool preferCompact) {
        var light = PairedArtifacts(artifacts, artifact).FirstOrDefault(item => item.Theme == "light") ?? artifact;
        var dark = PairedArtifacts(artifacts, artifact).FirstOrDefault(item => item.Theme == "dark") ?? artifact;
        page.Append(" data-theme-link data-light-href=\"").Append(light.Id).Append(extension)
            .Append("\" data-dark-href=\"").Append(dark.Id).Append(extension).Append('"');
        if (!preferCompact) return;
        var compact = artifacts.Where(item => item.Family == artifact.Family && item.Variant == "compact" && item.Theme == "light")
            .OrderBy(item => item.Id.StartsWith("family-", StringComparison.Ordinal) ? 0 : 1).ThenBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault();
        if (compact is null) return;
        var darkCompact = PairedArtifacts(artifacts, compact).FirstOrDefault(item => item.Theme == "dark");
        if (darkCompact is null) return;
        page.Append(" data-light-compact-href=\"").Append(compact.Id).Append(extension)
            .Append("\" data-dark-compact-href=\"").Append(darkCompact.Id).Append(extension).Append('"');
    }

    private static void AppendThemeControl(StringBuilder page) => page.Append("<div class=\"theme-control\" role=\"group\" aria-label=\"Color theme\"><button type=\"button\" data-set-theme=\"light\" aria-pressed=\"false\">Light</button><button type=\"button\" data-set-theme=\"dark\" aria-pressed=\"false\">Dark</button><button type=\"button\" data-set-theme=\"system\" aria-pressed=\"false\">System</button></div>");

    private static void StartPage(StringBuilder page, string title, string theme, string kind) {
        page.Append("<!doctype html><html lang=\"en\" data-output-view=\"svg\" data-theme=\"").Append(theme).Append("\" data-catalog-page=\"").Append(kind)
            .Append("\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><link rel=\"icon\" href=\"data:,\"><title>")
            .Append(Escape(title)).Append("</title><style>");
        AppendThemeCss(page, ":root", VisualThemeMode.Light);
        AppendThemeCss(page, "html[data-theme=dark]", VisualThemeMode.Dark);
        page.Append("</style><style data-gallery-style>").Append(ReadCatalogAsset("gallery.css"))
            .Append("</style></head><body><a class=\"skip-link\" href=\"#main-content\">Skip to content</a>");
    }

    private static void EndPage(StringBuilder page) => page.Append("<script data-gallery-script>")
        .Append(ReadCatalogAsset("gallery.js")).Append("</script></body></html>");

    private static void AppendThemeCss(StringBuilder page, string selector, VisualThemeMode mode) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        page.Append(selector).Append("{color-scheme:").Append(mode == VisualThemeMode.Dark ? "dark" : "light")
            .Append(";--bg:").Append(colors.Background.ToHex()).Append(";--surface:").Append(colors.Surface.ToHex())
            .Append(";--ink:").Append(colors.Foreground.ToHex()).Append(";--muted:").Append(colors.MutedForeground.ToHex())
            .Append(";--line:").Append(colors.Border.ToHex()).Append(";--link:").Append(colors.Accent.ToHex()).Append('}');
    }

    private static void WriteCatalogAssets(string output) {
        foreach (var file in new[] { "gallery.css", "gallery.js" })
            ExampleArtifactWriter.WriteText(Path.Combine(output, file), ReadCatalogAsset(file));
    }

    private static string ReadCatalogAsset(string file) {
        using var stream = typeof(V2Examples).Assembly.GetManifestResourceStream("ChartForgeX.Examples.Gallery." + file)
            ?? throw new InvalidOperationException("Gallery asset is missing: " + file);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
