// Renders the visuals used by chartforgex.com with ChartForgeX itself.
// Usage: dotnet run --project Website/tools/SiteVisuals -c Release -- <Website directory>
// Writes static/site/*, data/hero.json and the inline hero SVG partial of the chartforgex theme.
using System.Globalization;
using System.Text;
using System.Text.Json;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Markup;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

var website = Path.GetFullPath(args.Length > 0 ? args[0] : "Website");
var output = Path.Combine(website, "static", "site");
var partials = Path.Combine(website, "themes", "chartforgex", "partials", "generated");
Directory.CreateDirectory(output);
Directory.CreateDirectory(partials);
var light = VisualDesignTokens.GraphiteLight();
var dark = VisualDesignTokens.GraphiteDark();

string[] HeroSeries = { "Passkey", "Authenticator app", "SMS code", "Password only" };
string[] HeroWeeks = Enumerable.Range(1, 12).Select(w => $"W{w}").ToArray();
double[][] HeroData = {
    new double[] { 800, 1200, 1700, 2300, 3000, 3800, 4700, 5500, 6400, 7200, 8100, 8900 },
    new double[] { 7200, 7400, 7500, 7600, 7500, 7400, 7300, 7100, 6900, 6700, 6400, 6200 },
    new double[] { 4200, 4000, 3800, 3500, 3200, 2900, 2600, 2300, 2000, 1800, 1500, 1300 },
    new double[] { 5600, 5300, 4900, 4500, 4100, 3700, 3300, 2900, 2500, 2100, 1800, 1500 }
};

// One chart definition, every surface. The C# shown on the home page mirrors this method.
Chart HeroChart(VisualDesignTokens tokens) => Chart.Create()
    .WithTitle("Sign-ins by method")
    .WithSubtitle("Weekly sign-ins across the tenant, last 12 weeks")
    .WithYAxis("Sign-ins")
    .WithDesignTokens(tokens)
    .ConfigureAccessibility(a => a.WithTextAlternative(
        "Sign-ins by method",
        "Passkey sign-ins grow from 800 to 8,900 a week while password-only and SMS sign-ins decline.",
        "en"))
    .WithSize(960, 540)
    .WithXLabels(HeroWeeks)
    .AddStackedArea("Passkey", Points(HeroData[0]))
    .AddStackedArea("Authenticator app", Points(HeroData[1]))
    .AddStackedArea("SMS code", Points(HeroData[2]))
    .AddStackedArea("Password only", Points(HeroData[3]));

var lightVariables = light.ToSvgColorVariables();
var darkVariables = dark.ToSvgColorVariables();

// SVG: one file for both themes; the page swaps the custom properties.
var themedHero = HeroChart(light).WithSvgColorVariables(lightVariables);
var heroSvg = themedHero.ToSvg("cfx-hero");
Write("hero.svg", heroSvg);
Write("hero-theme.css", ThemeCss(lightVariables, darkVariables));
File.WriteAllText(Path.Combine(partials, "hero-svg.html"), heroSvg, new UTF8Encoding(false));

foreach (var (mode, tokens) in new[] { ("light", light), ("dark", dark) }) {
    var chart = HeroChart(tokens);
    File.WriteAllBytes(Path.Combine(output, $"hero-{mode}.png"), chart.ToPng());
    Write($"hero-{mode}.html", chart.ToHtmlPage());
    Write($"hero-{mode}.interactive.html", chart.ToInteractiveHtmlPage());
}

// Touch icon and social card, rasterized by ChartForgeX from SVG.
var faviconSvg = File.ReadAllText(Path.Combine(website, "static", "favicon.svg"));
File.WriteAllBytes(Path.Combine(website, "static", "apple-touch-icon.png"), SvgRasterizer.ToPng(faviconSvg, 180, 180));
var heroLightSvg = HeroChart(light).ToSvg("cfx-social");
var heroInner = heroLightSvg.Substring(heroLightSvg.IndexOf('>') + 1);
heroInner = heroInner.Substring(0, heroInner.LastIndexOf("</svg>", StringComparison.Ordinal));
var logoInner = faviconSvg.Substring(faviconSvg.IndexOf('>') + 1);
logoInner = logoInner.Substring(0, logoInner.LastIndexOf("</svg>", StringComparison.Ordinal));
var socialSvg = $"""
<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
  <rect width="1200" height="630" fill="#f2f3f4"/>
  <svg x="64" y="60" width="56" height="56" viewBox="0 0 32 32">{logoInner}</svg>
  <text x="136" y="100" font-family="Segoe UI, Arial, sans-serif" font-size="34" font-weight="700" fill="#16181c">ChartForgeX</text>
  <text x="64" y="200" font-family="Segoe UI, Arial, sans-serif" font-size="60" font-weight="800" fill="#16181c">Render charts in .NET.</text>
  <text x="64" y="270" font-family="Segoe UI, Arial, sans-serif" font-size="60" font-weight="800" fill="#626770">Ship them anywhere.</text>
  <text x="64" y="340" font-family="Segoe UI, Arial, sans-serif" font-size="24" fill="#4d525b">SVG, PNG and HTML from one model. No browser, no dependencies.</text>
  <svg x="560" y="360" width="600" height="338" viewBox="0 0 960 540">{heroInner}</svg>
</svg>
""";
File.WriteAllBytes(Path.Combine(output, "social-card.png"), SvgRasterizer.ToPng(socialSvg, 1200, 630));

// Sizes shown next to each surface on the home page.
var sizes = new SortedDictionary<string, string>(StringComparer.Ordinal);
foreach (var name in new[] { "hero.svg", "hero-light.png", "hero-light.html", "hero-light.interactive.html" }) {
    sizes[name] = FormatSize(new FileInfo(Path.Combine(output, name)).Length);
}
File.WriteAllText(Path.Combine(website, "data", "hero.json"),
    JsonSerializer.Serialize(new { generatedBy = "Website/tools/SiteVisuals", width = 960, height = 540, sizes },
        new JsonSerializerOptions { WriteIndented = true }) + "\n",
    new UTF8Encoding(false));

// Markdown: the same chart authored as a fenced block.
const string fenceInfo = "chartforgex chart v1 {#sign-ins title=\"Sign-ins by method\" type=\"stackedArea\" width=960 height=540}";
var fenceBody = new StringBuilder("subtitle \"Weekly sign-ins across the tenant, last 12 weeks\"\n");
fenceBody.Append("| Week | ").Append(string.Join(" | ", HeroSeries)).Append(" |\n");
fenceBody.Append("| --- |").Append(string.Concat(HeroSeries.Select(_ => " --- |"))).Append('\n');
for (var week = 0; week < HeroWeeks.Length; week++) {
    fenceBody.Append("| ").Append(HeroWeeks[week]).Append(" | ")
        .Append(string.Join(" | ", HeroData.Select(series => series[week].ToString(CultureInfo.InvariantCulture))))
        .Append(" |\n");
}
fenceBody.Append("option yAxisTitle \"Sign-ins\"\n");
var fenceText = fenceBody.ToString();
Write("hero.chartforgex.md", $"```{fenceInfo}\n{fenceText}```\n");
File.WriteAllText(Path.Combine(partials, "hero-fence.html"),
    System.Net.WebUtility.HtmlEncode($"```{fenceInfo}\n{fenceText}```"), new UTF8Encoding(false));
var fenceLines = fenceText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
var fence = VisualMarkupScanner.ParseFenceBlock(fenceInfo, fenceText, fenceLine: 1, payloadStartLine: 2, payloadEndLine: 1 + fenceLines);
var markup = new VisualMarkupParser().Parse(fence);
Report("markdown fence", markup.Diagnostics.Select(d => d.ToString()));
if (markup.Artifacts.FirstOrDefault()?.Model is Chart fenced) {
    foreach (var (mode, tokens) in new[] { ("light", light), ("dark", dark) }) {
        Write($"hero-markdown-{mode}.svg", fenced.WithDesignTokens(tokens).ToSvg($"cfx-md-{mode}"));
    }
}

// Mermaid: source kept next to the native render.
var mermaid = new Dictionary<string, string> {
    ["xychart"] = """
        xychart-beta
          title "Tickets closed per sprint"
          x-axis [S1, S2, S3, S4, S5, S6, S7, S8]
          y-axis "Tickets" 0 --> 120
          bar [48, 61, 57, 72, 80, 77, 94, 103]
          line [48, 61, 57, 72, 80, 77, 94, 103]
        """,
    ["sankey"] = """
        sankey-beta
        Office network,Passkey,5200
        Office network,Authenticator app,3100
        Remote,Passkey,3700
        Remote,Authenticator app,3100
        Remote,SMS code,1300
        Remote,Password only,1500
        """,
    ["gantt"] = """
        gantt
          title Release train
          dateFormat YYYY-MM-DD
          section Engine
          Layout pass      :done,    l1, 2026-09-01, 12d
          Label placement  :active,  l2, after l1, 10d
          section Surfaces
          PNG parity       :         s1, 2026-09-10, 14d
          Email preview    :         s2, after s1, 7d
        """
};
foreach (var (name, source) in mermaid) {
    Write($"mermaid-{name}.mmd", source.Trim() + "\n");
    File.WriteAllText(Path.Combine(partials, $"mermaid-{name}.html"),
        System.Net.WebUtility.HtmlEncode(source.Trim()), new UTF8Encoding(false));
    foreach (var mode in new[] { "light", "dark" }) {
        var themed = mode != "dark" ? source
            : source.StartsWith("---", StringComparison.Ordinal) ? "---\nconfig:\n  theme: dark\n" + source.Substring(4)
            : "%%{init: {'theme':'dark'}}%%\n" + source;
        var result = MermaidRenderer.Render(themed);
        Report($"mermaid {name} ({mode})", result.Diagnostics.Select(d => $"{d.Severity} {d.Code}: {d.Message}"));
        if (!result.HasErrors && result.Artifact != null) {
            Write($"mermaid-{name}-{mode}.svg", result.Artifact.ToSvg());
        }
    }
}

Console.WriteLine($"Site visuals written to {output}");

void Write(string name, string content) => File.WriteAllText(Path.Combine(output, name), content, new UTF8Encoding(false));

static void Report(string label, IEnumerable<string?> diagnostics) {
    foreach (var diagnostic in diagnostics) {
        Console.WriteLine($"[{label}] {diagnostic}");
    }
}

static string FormatSize(long bytes) => bytes < 1024
    ? $"{bytes} B"
    : string.Format(CultureInfo.InvariantCulture, "{0:0} KB", Math.Ceiling(bytes / 1024d));

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) {
        yield return new ChartPoint(i + 1, y[i]);
    }
}

static string ThemeCss(SvgColorVariables lightVariables, SvgColorVariables darkVariables) {
    var darkByName = darkVariables.Variables.GroupBy(v => v.Name).ToDictionary(g => g.Key, g => g.First().Color);
    var css = new StringBuilder("/* Generated by Website/tools/SiteVisuals. Light and dark values for the hero SVG. */\n");
    css.Append(":root {\n");
    foreach (var variable in lightVariables.Variables) {
        css.Append(CultureInfo.InvariantCulture, $"  {variable.Name}: {variable.Color.ToCss()};\n");
    }
    css.Append("}\n:root[data-theme=\"dark\"] {\n");
    foreach (var variable in lightVariables.Variables) {
        if (darkByName.TryGetValue(variable.Name, out var color)) {
            css.Append(CultureInfo.InvariantCulture, $"  {variable.Name}: {color.ToCss()};\n");
        }
    }
    css.Append("}\n");
    return css.ToString();
}
