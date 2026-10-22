using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VennSetContainmentTests {
    [Theory]
    [InlineData(780, 460)]
    [InlineData(640, 240)]
    [InlineData(320, 180)]
    public void ThreeSetsKeepTheirPaintInsideThePaddedSurface(int width, int height) {
        var block = VennDiagramBlock.Create().WithSize(width, height).WithTitle("Overlap").WithCard(false)
            .AddSet("API", "API", 60).AddSet("UI", "UI", 55).AddSet("Ops", "Operations", 45);
        block.Options.Theme = block.Options.Theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: 21, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
        var svg = XDocument.Parse(block.ToSvg());
        var circles = svg.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == "venn-set").ToArray();
        Assert.Equal(3, circles.Length);
        foreach (var circle in circles) {
            double Number(string name) => double.Parse(circle.Attribute(name)!.Value, CultureInfo.InvariantCulture);
            Assert.True(Number("cy") + Number("r") + Number("stroke-width") / 2 <= height - block.Options.Padding.Bottom + 0.001);
            Assert.True(Number("cx") - Number("r") - Number("stroke-width") / 2 >= block.Options.Padding.Left - 0.001);
            Assert.True(Number("cx") + Number("r") + Number("stroke-width") / 2 <= width - block.Options.Padding.Right + 0.001);
        }
        Assert.NotEmpty(block.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredRegionFontsUseTheirMeasuredBaselineEnvelopeWithoutUnreadableLenses(bool dark) {
        const string source = "%%{init: {\"themeVariables\":{\"fontSize\":21}}}%%\nvenn-beta\ntitle Capability overlap\n"
            + "set API [\"API\"] : 60\nset UI [\"UI\"] : 55\nset Ops [\"Operations\"] : 45\n"
            + "union API,UI [\"Shared UX\"] : 18\nunion API,Ops [\"Automation\"] : 12\n"
            + "union UI,Ops [\"Runbooks\"] : 8\nunion API,UI,Ops [\"Platform\"] : 5\ntext API,UI note [\"Reviewed\"]\n";
        var options = new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions {
                FontFamily = BrowserProofFonts.Family,
                ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light
            }
        };
        var direct = MermaidRenderer.Render(source, options);
        Assert.False(direct.HasErrors);
        Assert.Equal(MermaidDiagnosticCodes.ApproximateMapping, Assert.Single(direct.Diagnostics).Code);
        var artifact = direct.Artifact!;
        Assert.IsType<VennDiagramBlock>(artifact.Model).WithCard(false);
        var svg = artifact.ToSvg();
        var document = XDocument.Parse(svg);
        var labels = document.Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        foreach (var required in new[] { "API", "UI", "Operations", "Shared UX", "Automation", "Runbooks", "Reviewed" })
            Assert.Contains(labels, element => element.Value == required);
        foreach (var label in labels)
            Assert.True((double)label.Attribute("font-size")! >= 8, label.Value + " must retain the gallery's readable font size.");

        // A real output line box is asymmetric around its baseline. Check both
        // opposing circle edges so a readable font cannot escape its membership.
        foreach (var (caption, ids) in new[] {
            ("Automation", new[] { "API", "Ops" }), ("Runbooks", new[] { "UI", "Ops" })
        }) {
            var label = Assert.Single(labels, element => element.Value == caption);
            var size = (double)label.Attribute("font-size")!;
            var style = new TextStyle {
                Font = new FontSpec { Family = (string)label.Attribute("font-family")!, Weight = (int)label.Attribute("font-weight")! },
                FontSize = size, LineHeight = 1
            };
            var metrics = TextLayoutEngine.Measure(caption, style);
            var face = TypographyFontResolver.ResolveFace(style.Font).Font;
            Assert.NotNull(face);
            var left = (double)label.Attribute("x")!;
            var top = (double)label.Attribute("y")! - face.Ascent(size);
            foreach (var id in ids) {
                var circle = Assert.Single(document.Descendants(), element => (string?)element.Attribute("data-set-id") == id);
                var cx = (double)circle.Attribute("cx")!; var cy = (double)circle.Attribute("cy")!; var radius = (double)circle.Attribute("r")!;
                foreach (var x in new[] { left, left + metrics.Width })
                    foreach (var y in new[] { top, top + metrics.Height })
                        Assert.True(Math.Pow(x - cx, 2) + Math.Pow(y - cy, 2) <= radius * radius + .1,
                            caption + " must remain inside " + id + " at its actual ascent and descent.");
            }
        }

        var png = artifact.ToPng();
        var image = RasterImageDecoder.Decode(png);
        Assert.Equal((780, 460), (image.Width, image.Height));
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        Assert.Equal(MermaidDiagnosticCodes.ApproximateMapping, Assert.Single(fence.Diagnostics).Code);
        var fenced = Assert.Single(fence.Artifacts);
        Assert.IsType<VennDiagramBlock>(fenced.Model).WithCard(false);
        Assert.Equal(svg, fenced.ToSvg());
        Assert.Equal(png, fenced.ToPng());
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "venn-readable-baseline-" + (dark ? "dark" : "light");
        File.WriteAllText(Path.Combine(captures, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(captures, name + ".png"), png);
    }
}
