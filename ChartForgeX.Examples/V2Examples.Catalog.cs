using System.Net;
using System.Text;
using System.Text.Json;
using ChartForgeX.Themes;

public static partial class V2Examples {
    private sealed record ProofArtifact(string Id, string Family, string Title, string Variant, string Theme,
        int Width, int Height, string[] Diagnostics, int SemanticRegions);

    private static string Escape(string value) => WebUtility.HtmlEncode(value);

    private static void WritePage(string output, string id, string title, VisualThemeMode mode) {
        var page = new StringBuilder();
        StartPage(page, title, mode.ToString().ToLowerInvariant());
        page.Append("<main><a href=\"index.html\">All examples</a><h1>").Append(Escape(title)).Append("</h1>");
        page.Append("<div class=\"full\">").Append(File.ReadAllText(Path.Combine(output, id + ".svg"))).Append("</div>");
        page.Append("<p><a href=\"").Append(id).Append(".svg\">SVG</a> · <a href=\"").Append(id).Append(".png\">PNG</a> · <a href=\"").Append(id).Append(".csharp.txt\">C# source</a></p>");
        page.Append("<h2>Source</h2><pre><code>").Append(Escape(File.ReadAllText(Path.Combine(output, id + ".csharp.txt")))).Append("</code></pre></main></body></html>");
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".html"), page.ToString());
    }

    private static void WriteCatalog(string output, IReadOnlyList<ProofArtifact> artifacts, bool curated) {
        var manifest = new {
            schemaVersion = 1,
            matrixScope = curated ? "selected" : "full",
            pipeline = "model-to-prepared-scene-to-svg-or-raster",
            fonts = new[] { "fonts/Carlito-Regular.ttf", "fonts/Carlito-Bold.ttf" }.Select(file => new {
                file, sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(output, file))))
            }),
            artifacts = artifacts.Select(artifact => new {
                id = artifact.Id, family = artifact.Family, title = artifact.Title, variant = artifact.Variant,
                theme = artifact.Theme, width = artifact.Width, height = artifact.Height,
                naturalAspect = (double)artifact.Width / artifact.Height,
                compact = artifact.Variant == "compact", source = artifact.Id + ".csharp.txt",
                svg = artifact.Id + ".svg", png = artifact.Id + ".png", html = artifact.Id + ".html",
                thumbnail = artifact.Id + ".thumbnail.svg", thumbnailWidth = 640, thumbnailHeight = 400,
                supportedOutputs = new[] { "svg", "png", "html" },
                diagnostics = artifact.Diagnostics, semanticRegions = artifact.SemanticRegions
            })
        };
        ExampleArtifactWriter.WriteText(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        var page = new StringBuilder();
        StartPage(page, "ChartForgeX direct-scene catalog", "light");
        page.Append("<header><h1>ChartForgeX direct-scene catalog</h1><p>Charts and diagram feasibility · light and dark · SVG and native PNG from one prepared scene</p><p><a href=\"manifest.json\">Artifact manifest</a></p></header><main>");
        foreach (var family in new[] { "cartesian", "donut", "pie", "topology", "sequence" }.Where(family => artifacts.Any(artifact => artifact.Family == family))) {
            page.Append("<h2>").Append(char.ToUpperInvariant(family[0]) + family.Substring(1)).Append("</h2><div class=\"grid\">");
            foreach (var artifact in artifacts.Where(artifact => artifact.Family == family)) {
                page.Append("<article class=\"tile\" data-theme=\"").Append(artifact.Theme).Append("\"><a class=\"preview\" href=\"").Append(artifact.Id).Append(".html\" aria-label=\"").Append(Escape(artifact.Title)).Append("\">").Append(File.ReadAllText(Path.Combine(output, artifact.Id + ".thumbnail.svg"))).Append("</a>");
                page.Append("<div class=\"caption\"><h3>").Append(Escape(artifact.Title)).Append("</h3><p>").Append(Escape(artifact.Variant.Replace('-', ' '))).Append(" · ").Append(artifact.Theme).Append(" · ").Append(artifact.Width).Append('×').Append(artifact.Height).Append("</p>");
                page.Append("<a href=\"").Append(artifact.Id).Append(".svg\">SVG</a> · <a href=\"").Append(artifact.Id).Append(".png\">PNG</a> · <a href=\"").Append(artifact.Id).Append(".csharp.txt\">C#</a></div></article>");
            }
            page.Append("</div>");
        }
        page.Append("</main></body></html>");
        ExampleArtifactWriter.WriteText(Path.Combine(output, "index.html"), page.ToString());
    }

    private static void StartPage(StringBuilder page, string title, string theme) {
        page.Append("<!doctype html><html lang=\"en\" data-theme=\"").Append(theme).Append("\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><link rel=\"icon\" href=\"data:,\"><title>").Append(Escape(title)).Append("</title><style>");
        AppendThemeCss(page, ":root", VisualThemeMode.Light);
        AppendThemeCss(page, "[data-theme=dark]", VisualThemeMode.Dark);
        page.Append("@font-face{font-family:'CFX Proof Carlito';src:url('fonts/Carlito-Regular.ttf') format('truetype');font-weight:400;font-display:block}@font-face{font-family:'CFX Proof Carlito';src:url('fonts/Carlito-Bold.ttf') format('truetype');font-weight:700;font-display:block}.preview svg,.full svg{display:block;width:100%;height:100%}.full svg{height:auto}");
        page.Append("*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font-size:15px;line-height:1.5;font-family:'").Append(ProofFont).Append("',").Append(VisualTheme.Graphite().Typography.Family).Append("}header,main{max-width:1500px;margin:auto;padding:24px}h1,h2,h3{line-height:1.2}h1{font-size:28px}h2{margin-top:32px}h3{font-size:16px;margin:0 0 8px}p{color:var(--muted)}a{color:var(--link)}a:focus-visible{outline:3px solid currentColor;outline-offset:3px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,350px),1fr));gap:20px}.tile{border:1px solid var(--line);background:var(--bg);color:var(--ink);min-width:0}.preview{height:260px;display:grid;place-items:center;padding:12px;border-bottom:1px solid var(--line)}.preview img{display:block;width:100%;height:100%;object-fit:contain}.caption{padding:16px;min-height:130px}.caption p{margin:0 0 10px}.full{display:block;width:100%;height:auto;max-width:1100px}pre{overflow:auto;border:1px solid var(--line);padding:16px;font-size:13px}@media(max-width:480px){header,main{padding:16px}.preview{height:240px}} ");
        page.Append("</style></head><body>");
    }

    private static void AppendThemeCss(StringBuilder page, string selector, VisualThemeMode mode) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        page.Append(selector).Append("{color-scheme:").Append(mode == VisualThemeMode.Dark ? "dark" : "light")
            .Append(";--bg:").Append(colors.Background.ToHex()).Append(";--ink:").Append(colors.Foreground.ToHex())
            .Append(";--muted:").Append(colors.MutedForeground.ToHex()).Append(";--line:").Append(colors.Border.ToHex())
            .Append(";--link:").Append(colors.Accent.ToHex()).Append('}');
    }
}
