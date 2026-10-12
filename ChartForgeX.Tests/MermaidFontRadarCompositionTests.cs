using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFontRadarCompositionTests {
    [Theory]
    [InlineData(false, "/**/ 18px /**/", true, false)]
    [InlineData(true, "/**/ 18px /**/", true, false)]
    [InlineData(false, "/**/ calc(1rem + 2px) /**/", false, false)]
    [InlineData(true, "/**/ calc(1rem + 2px) /**/", false, false)]
    [InlineData(false, "calc(18px) /**/ trailing", false, true)]
    [InlineData(true, "calc(18px) /**/ trailing", false, true)]
    public void SourceFontClassificationAndRadarCurvePolicyComposeThroughDirectAndFencedRendering(bool directive, string size, bool applied, bool invalid) {
        var quoted = JsonSerializer.Serialize(size);
        var source = (directive
            ? "%%{init: {\"themeVariables\":{\"fontSize\":" + quoted + "},\"radar\":{\"curveTension\":0.25}}}%%\n"
            : "---\nconfig:\n  themeVariables:\n    fontSize: " + quoted + "\n  radar:\n    curveTension: 0.25\n---\n")
            + "radar-beta\naxis A, B, C\ncurve current{3, 5, 4}\ngraticule circle\n";
        var direct = MermaidRenderer.Render(source);
        Assert.Equal(invalid, direct.HasErrors);
        Assert.Equal(applied ? 18d : (double?)null, direct.Document!.Configuration.FontSize);
        Assert.Equal(size, direct.Document.Configuration.Settings.Single(setting => setting.Path == "themeVariables.fontSize").Value);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        if (applied) { Assert.Empty(direct.Diagnostics); Assert.Empty(fenced.Diagnostics); }
        else {
            var code = invalid ? MermaidDiagnosticCodes.InvalidConfiguration : MermaidDiagnosticCodes.UnsupportedConfiguration;
            Assert.Equal(code, Assert.Single(direct.Diagnostics).Code);
            Assert.Single(fenced.Diagnostics);
        }
        if (invalid) { Assert.Null(direct.Artifact); Assert.Empty(fenced.Artifacts); return; }
        var artifact = Assert.Single(fenced.Artifacts);
        Assert.NotNull(direct.Artifact);
        var expected = direct.Artifact!;
        Assert.Equal(expected.ToSvg(), artifact.ToSvg());
        Assert.Equal(expected.ToPng(), artifact.ToPng());
        var curve = XDocument.Parse(artifact.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "radar-outline");
        Assert.Contains("C", curve.Attribute("d")!.Value);
    }
}
