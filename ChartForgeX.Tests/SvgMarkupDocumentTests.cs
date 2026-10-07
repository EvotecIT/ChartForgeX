using System.Xml;
using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// The label scene's markup tree replaces the XLinq round trip. For any markup the framework accepts it must hold the
/// same elements, attributes and text as <c>XDocument.Load</c> with preserved white space, and write exactly what
/// <c>ToString(SaveOptions.DisableFormatting)</c> writes, also after the edits the scene makes; markup the framework
/// rejects must be rejected the same way.
/// </summary>
public sealed class SvgMarkupDocumentTests {
    public static IEnumerable<object[]> Documents => new[] {
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><g/></svg>\r\n",
        "\r\n <svg/>",
        "<svg><g></g><text></text><text x=\"1\">a<tspan>b</tspan>c</text></svg>",
        "<svg a=\"x\ty\nz\r\nw\rq\"/>",
        "<svg>a\rb\r\nc\nd</svg>",
        "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"><use xlink:href=\"#a\"/></svg>",
        "<svg xmlns=\"http://www.w3.org/2000/svg\"><svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"/></svg>",
        "<svg xml:space=\"preserve\" a='single \"quoted\"'> x </svg>",
        "<svg a=\"1>2\">x>y</svg>",
        "<svg a=\"&#10;&#9;&#13;&quot;&apos;&lt;&gt;&amp;&#x41;&#0066;\">&#10;&#13;&lt;&gt;&amp;&apos;&quot;</svg>",
        "<svg a=\"﷐P1FF00FF00﷑\">﷐ \U0001F600 é− &#x1F600;</svg>",
        "<svg   a = \"1\"\n\tb\t=\t'2'   ><g  /></svg  >",
        "<svg><style>#a text{fill:#123456} .b>c{}</style></svg>",
        "<svg><!-- comment --><g/></svg>",
        "<!-- before --><svg/><!-- after -->",
        "<svg><!-- line\r\nbreak --></svg>",
        "<svg><style><![CDATA[a{fill:red}]]></style></svg>",
        "<svg><style><![CDATA[a{\r\nfill:red}]]></style></svg>",
        "<?xml version=\"1.0\"?><svg/>",
        "<svg><?pi data  here ?></svg>",
        "<svg:svg xmlns:svg=\"http://www.w3.org/2000/svg\"><svg:g/></svg:svg>",
        "<svg xmlns:p=\"urn:a\" xmlns:q=\"urn:a\"><q:g/></svg>",
        "<svg xmlns:p=\"urn:a\"><g xmlns:p=\"urn:b\"><p:x/></g></svg>",
        "<svg é=\"1\"><ét/></svg>",
        "<svg xml:space=\"preserve\"><g xml:space=\"default\"/></svg>",
        "<svg xml:lang=\"en\" xmlfoo=\"1\"/>",
        "<svg xmlns=\"\"><g xmlns=\"urn:a\"><h xmlns=\"\"/></g></svg>",
        "<svg><a\U00010000b c\U00010000=\"1\"/></svg>",
        "<svg><!-- a\nb --><a\U00010000b/></svg>",
        "<svg><!-- a\nb -->&#13;x&#13;&#10;y</svg>",
        // Rejected by the framework.
        "<svg xml:space=\"x\"/>",
        "<svg xmlns=\"http://www.w3.org/XML/1998/namespace\"/>",
        "<svg xmlns=\"http://www.w3.org/2000/xmlns/\"/>",
        "<svg xmlns:p=\"\"/>",
        // Rejected by the framework.
        "<!DOCTYPE svg><svg/>",
        "<svg>a]]>b</svg>",
        "<svg a=\"1\" a=\"2\"/>",
        "<svg a=\"1\"b=\"2\"/>",
        "<svg a=\"<\"/>",
        "<svg>&nbsp;</svg>",
        "<svg>&#1;</svg>",
        "<svg>\u0001</svg>",
        "<svg><g></svg>",
        "<svg/>x",
        "<svg p:a=\"1\"/>",
        "<svg>&#xD800;</svg>"
    }.Select(markup => new object[] { markup });

    [Theory]
    [MemberData(nameof(Documents))]
    public void Parse_AnyMarkup_HoldsAndWritesTheFrameworkTreeOrRejectsIt(string markup) {
        XDocument? framework = null;
        Exception? failure = null;
        try {
            framework = Load(markup);
        } catch (Exception exception) {
            failure = exception;
        }

        if (failure != null) {
            Assert.Throws(failure.GetType(), () => SvgMarkupParser.Parse(markup));
            Assert.Null(SvgMarkupParser.TryParseDirect(markup));
            return;
        }

        var document = SvgMarkupParser.Parse(markup);
        Assert.Equal(framework!.ToString(SaveOptions.DisableFormatting), document.ToString());
        var expected = framework.Descendants().ToList();
        var actual = document.Descendants().ToList();
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++) {
            Assert.Equal(expected[i].Name.LocalName, actual[i].LocalName);
            Assert.Equal(expected[i].Value, actual[i].Value);
            Assert.Equal(expected[i].IsEmpty, actual[i].IsEmpty);
            Assert.Equal(expected[i].HasElements, actual[i].HasElements);
            foreach (var attribute in expected[i].Attributes().Where(a => a.Name.Namespace == XNamespace.None)) {
                Assert.Equal(attribute.Value, actual[i].Attribute(attribute.Name.LocalName));
            }
        }
    }

    [Fact]
    public void TryParseDirect_RendererMarkup_IsTakenDirectly() {
        Assert.NotNull(SvgMarkupParser.TryParseDirect(Chart().ToSvg()));
        Assert.NotNull(SvgMarkupParser.TryParseDirect(DenseReplicationFixture.Sites60().Prepare(DenseReplicationFixture.Options()).ToSvg()));
    }

    [Fact]
    public void Parse_EveryCharacterInTextAndAttributes_MatchesTheFramework() {
        for (var code = 1; code < 0x10000; code++) {
            if (code is >= 0xD800 and <= 0xDFFF) continue;
            var reference = "&#" + code.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";";
            AssertSame("<x a=\"" + reference + "\">" + reference + "</x>");
            var literal = ((char)code).ToString();
            if (literal is "<" or "&" or "\"") continue;
            AssertSame("<x a=\"" + literal + "\">" + literal + "</x>");
        }
    }

    [Fact]
    public void Edits_AsTheLabelSceneMakesThem_WriteWhatXLinqWrites() {
        var svg = Chart().ToSvg();
        var framework = Load(svg);
        var document = SvgMarkupParser.Parse(svg);
        var expectedText = framework.Descendants().First(e => e.Name.LocalName == "text");
        var actualText = document.Descendants().First(e => e.LocalName == "text");
        expectedText.SetAttributeValue("data-cfx-label-status", "placed \"quoted\" <tag> & tab\there");
        actualText.SetAttributeValue("data-cfx-label-status", "placed \"quoted\" <tag> & tab\there");
        expectedText.SetAttributeValue("x", null);
        actualText.SetAttributeValue("x", null);
        expectedText.Value = "Ellipsis… & more";
        actualText.Value = "Ellipsis… & more";
        expectedText.AddBeforeSelf(new XElement(expectedText.Name.Namespace + "line", new XAttribute("x1", "1"), new XAttribute("stroke", "currentColor")));
        var line = new SvgMarkupElement(actualText.PrefixWithColon + "line");
        line.AddAttribute("x1", "1");
        line.AddAttribute("stroke", "currentColor");
        actualText.AddBeforeSelf(line);
        framework.Root!.SetAttributeValue("data-cfx-label-count", 12);
        document.Root.SetAttributeValue("data-cfx-label-count", 12);
        framework.Root.SetAttributeValue("data-x", 1.0 / 3);
        document.Root.SetAttributeValue("data-x", 1.0 / 3);
        var emptied = framework.Descendants().Last(e => e.Name.LocalName == "rect");
        emptied.Value = string.Empty;
        document.Descendants().Last(e => e.LocalName == "rect").Value = string.Empty;
        Assert.Equal(framework.ToString(SaveOptions.DisableFormatting), document.ToString());

        // A clone is a deep, parentless copy: editing it leaves the original as it was.
        var clone = document.Root.Clone();
        Assert.Null(clone.Parent);
        Assert.Equal(document.Root.Value, clone.Value);
        clone.DescendantsAndSelf().First(e => e.LocalName == "text").Value = "changed";
        Assert.Equal(framework.ToString(SaveOptions.DisableFormatting), document.ToString());
        Assert.Same(clone, clone.Elements().First().Parent);
    }

    [Fact]
    public void Parse_NestingDeeperThanTheSceneAllows_ReportsItAsTheSceneDoes() {
        var depth = ChartForgeX.SvgRaster.SvgRasterParser.MaximumElementDepth + 1;
        var markup = string.Concat(Enumerable.Repeat("<g>", depth)) + string.Concat(Enumerable.Repeat("</g>", depth));
        Assert.Null(SvgMarkupParser.TryParseDirect(markup));
        var exception = Assert.Throws<FormatException>(() => SvgMarkupParser.Parse(markup));
        Assert.Contains(depth - 1 + ".", exception.Message, StringComparison.Ordinal);
    }

    private static void AssertSame(string markup) {
        string? expected;
        try {
            expected = Load(markup).ToString(SaveOptions.DisableFormatting);
        } catch (Exception) {
            expected = null;
        }

        if (expected == null) {
            Assert.ThrowsAny<Exception>(() => SvgMarkupParser.Parse(markup).ToString());
            return;
        }

        var document = SvgMarkupParser.Parse(markup);
        Assert.Equal(expected, document.ToString());
        var framework = Load(markup).Root!;
        Assert.Equal(framework.Value, document.Root.Value);
        Assert.Equal(framework.Attribute("a")?.Value, document.Root.Attribute("a"));
    }

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><g><text x=\"1\">a</text></g></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><text xmlns=\"\" x=\"1\">a</text></svg>")]
    [InlineData("<svg><text xmlns=\"http://www.w3.org/2000/svg\" x=\"1\">a</text></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><text xmlns=\"http://www.w3.org/2000/svg\">a</text></svg>")]
    [InlineData("<s:svg xmlns:s=\"http://www.w3.org/2000/svg\"><s:text>a</s:text></s:svg>")]
    [InlineData("<svg><p:text xmlns:p=\"http://www.w3.org/2000/svg\">a</p:text></svg>")]
    [InlineData("<svg xmlns:q=\"urn:a\"><p:text xmlns:p=\"urn:a\">a</p:text></svg>")]
    [InlineData("<svg xmlns=\"urn:a\"><p:text xmlns:p=\"urn:a\">a</p:text></svg>")]
    [InlineData("<svg xmlns:q=\"urn:b\"><g xmlns:q=\"urn:a\"><p:text xmlns:p=\"urn:a\" xmlns:x=\"urn:x\">a</p:text></g></svg>")]
    [InlineData("<svg xmlns:p=\"urn:b\"><p:text xmlns:p=\"urn:a\">a</p:text></svg>")]
    public void CreateSibling_LeaderBeforeALabel_WritesWhatXLinqWrites(string markup) {
        var framework = Load(markup);
        var document = SvgMarkupParser.Parse(markup);
        var expectedText = framework.Descendants().First(e => e.Name.LocalName == "text");
        var actualText = document.Descendants().First(e => e.LocalName == "text");
        expectedText.AddBeforeSelf(new XElement(expectedText.Name.Namespace + "line", new XAttribute("x1", "1"), new XAttribute("stroke", "red")));
        var line = actualText.CreateSibling("line", 3, out var declaration);
        line.AddAttribute("x1", "1");
        line.AddAttribute("stroke", "red");
        if (declaration != null) line.AddAttribute("xmlns", declaration);
        actualText.AddBeforeSelf(line);
        Assert.Equal(framework.ToString(SaveOptions.DisableFormatting), document.ToString());
    }

    private static XDocument Load(string markup) {
        using var source = new StringReader(markup);
        using var reader = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    private static ChartForgeX.Core.Chart Chart() => ChartForgeX.Core.Chart.Create().WithSize(400, 240).WithTitle("Markup & plain <xml>")
        .WithLegend().AddBar("Series \"one\"", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3), new ChartPoint(3, 1) }, ChartColor.FromHex("#1E40AF"));
}
