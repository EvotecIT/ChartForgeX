using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRailroadFrontendCompletionTests {
    private const string Abnf = "railroad-abnf-beta\nvalue = \"plain\" %i\"MiXeD\" %I\"UPPER\" %s\"Exact\" %S\"\" 2%s\"a\\b\" ;";
    private const string Peg = "railroad-peg-beta\nvalue <- ![0-9] &[a-z] ([a-zA-Z_] / \".\" / 'x')+ .? [] ;";

    [Fact]
    public void AbnfLiteralPrefixesPreserveCasePoliciesAndWholeSourceLocations() {
        var parsed = new MermaidParser().ParseRailroad(Abnf); Assert.Empty(parsed.Diagnostics);
        var sequence = Assert.Single(parsed.Document!.Rules).Definition.Children;
        var terminals = sequence.Take(5).Select(node => node.Expression).ToArray();
        Assert.Equal(new[] { "plain", "MiXeD", "UPPER", "Exact", "" }, terminals.Select(node => node.Value));
        Assert.Equal(new[] { false, false, false, true, true }, terminals.Select(node => node.CaseSensitive));
        Assert.All(terminals, node => Assert.Equal(RailroadExpressionKind.Terminal, node.Kind));
        var repeated = sequence[5]; Assert.Equal(RailroadExpressionKind.Repeat, repeated.Expression.Kind);
        Assert.Equal(2, repeated.Expression.MinimumOccurrences); Assert.Equal(2, repeated.Expression.MaximumOccurrences);
        var literal = Assert.Single(repeated.Children); Assert.Equal("a\\b", literal.Expression.Value); Assert.True(literal.Expression.CaseSensitive);
        Assert.Equal(2, sequence[3].Span.Line); Assert.Equal(Abnf.Split('\n')[1].IndexOf("%s", StringComparison.Ordinal) + 1, sequence[3].Span.Column);
        Assert.Equal("%s\"Exact\"".Length, sequence[3].Span.Length);
        Assert.Equal(RailroadRuleComparison.OrdinalIgnoreCase, parsed.Document.ToRailroadDiagramBlock().RuleComparison);
    }

    [Fact]
    public void PegClassesRemainSymbolicAndPreservePredicatesAnyCharacterAndOperatorPrecedence() {
        var parsed = new MermaidParser().ParseRailroad(Peg); Assert.Empty(parsed.Diagnostics);
        var sequence = Assert.Single(parsed.Document!.Rules).Definition.Children;
        Assert.Equal(new[] { RailroadExpressionKind.NegativeLookahead, RailroadExpressionKind.Lookahead, RailroadExpressionKind.Repeat, RailroadExpressionKind.Optional, RailroadExpressionKind.Except }, sequence.Select(node => node.Expression.Kind));
        var digit = Assert.Single(sequence[0].Children).Expression; Assert.Equal(48, digit.FirstCodePoint); Assert.Equal(57, digit.LastCodePoint);
        Assert.Equal(RailroadExpressionKind.CharacterRange, digit.Kind);
        var repeated = sequence[2]; Assert.Equal(1, repeated.Expression.MinimumOccurrences); Assert.Null(repeated.Expression.MaximumOccurrences);
        var ordered = Assert.Single(repeated.Children); Assert.True(ordered.Expression.OrderedChoice);
        var letters = ordered.Children[0]; Assert.True(letters.Expression.OrderedChoice);
        Assert.Equal(new[] { 97, 65, 95 }, letters.Children.Select(node => node.Expression.FirstCodePoint));
        Assert.Equal(new[] { 122, 90, 95 }, letters.Children.Select(node => node.Expression.LastCodePoint));
        Assert.Equal(RailroadExpressionKind.AnyCharacter, Assert.Single(sequence[3].Children).Expression.Kind);
        Assert.All(sequence[4].Children, node => Assert.Equal(RailroadExpressionKind.AnyCharacter, node.Expression.Kind));
        Assert.All(MermaidRailroadParserTests.Flatten(parsed.Document.Rules[0].Definition), node => Assert.True(node.Span.Line == 2 && node.Span.Column > 0 && node.Span.Length > 0));
    }

    [Fact]
    public void PegQuotesAndCharacterClassesShareEscapesWithoutTreatingRegexNegationAsPegSyntax() {
        const string source = "railroad-peg-beta\nvalue <- '\\141\\t\\'' \"\\101\\n\\\"\" [\\141-\\172\\]\\[\\-^\\u03A9😀] ;";
        var parsed = new MermaidParser().ParseRailroad(source); Assert.Empty(parsed.Diagnostics);
        var sequence = Assert.Single(parsed.Document!.Rules).Definition.Children;
        Assert.Equal("a\t'", sequence[0].Expression.Value); Assert.Equal("A\n\"", sequence[1].Expression.Value);
        var ranges = sequence[2].Children.Select(node => node.Expression).ToArray();
        Assert.Equal(new[] { 97, 93, 91, 45, 94, 937, 0x1f600 }, ranges.Select(node => node.FirstCodePoint));
        Assert.Equal(new[] { 122, 93, 91, 45, 94, 937, 0x1f600 }, ranges.Select(node => node.LastCodePoint));
        Assert.All(ranges, node => Assert.Equal(RailroadExpressionKind.CharacterRange, node.Kind));
        Assert.All(sequence.Take(2), node => Assert.True(node.Expression.CaseSensitive));
    }

    [Fact]
    public void PegEmptySequencesAndClassesHaveDifferentNativeSemantics() {
        const string source = "railroad-peg-beta\nempty <- ;\nchoice <- / 'x' / ;\ngroup <- () ;\nnone <- [] ;";
        var parsed = new MermaidParser().ParseRailroad(source); Assert.Empty(parsed.Diagnostics);
        var rules = parsed.Document!.Rules;
        Assert.Equal(RailroadExpressionKind.Empty, rules[0].Definition.Expression.Kind);
        Assert.Equal(new[] { RailroadExpressionKind.Empty, RailroadExpressionKind.Terminal, RailroadExpressionKind.Empty }, rules[1].Definition.Children.Select(node => node.Expression.Kind));
        Assert.True(rules[1].Definition.Expression.OrderedChoice); Assert.Equal(RailroadExpressionKind.Empty, rules[2].Definition.Expression.Kind);
        var none = rules[3].Definition; Assert.Equal(RailroadExpressionKind.Except, none.Expression.Kind);
        Assert.Equal(2, none.Children.Count); Assert.All(none.Children, node => Assert.Equal(RailroadExpressionKind.AnyCharacter, node.Expression.Kind));
        var svg = XDocument.Parse(MermaidRenderer.Render(source).Artifact!.ToSvg());
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-expression-kind") == "Except");
    }

    [Theory]
    [InlineData("railroad-abnf-beta\nvalue = %s 'x' ;")]
    [InlineData("railroad-abnf-beta\nvalue = %i \"x\" ;")]
    [InlineData("railroad-abnf-beta\nvalue = %s\"unterminated ;")]
    [InlineData("railroad-abnf-beta\nvalue = %i\"unterminated ;")]
    [InlineData("railroad-abnf-beta\nvalue = \"unterminated ;")]
    [InlineData("railroad-abnf-beta\nvalue = %i\"é\" ;")]
    [InlineData("railroad-abnf-beta\nvalue = \"a\tb\" ;")]
    [InlineData("railroad-peg-beta\nvalue <- [z-a] ;")]
    [InlineData("railroad-peg-beta\nvalue <- [\\q] ;")]
    [InlineData("railroad-peg-beta\nvalue <- [a-z ;")]
    [InlineData("railroad-peg-beta\nvalue <- [\\uD800] ;")]
    [InlineData("railroad-peg-beta\nvalue <- [\\uDC00] ;")]
    [InlineData("railroad-peg-beta\nvalue <- [\\uZZZZ] ;")]
    [InlineData("railroad-peg-beta\nvalue <- [a-z]** ;")]
    public void InvalidNewFormsRemainLocatedErrorsWithoutArtifacts(string source) {
        var parsed = new MermaidParser().ParseRailroad(source); Assert.True(parsed.HasErrors);
        var error = Assert.Single(parsed.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code); Assert.Equal(2, error.Span.Line); Assert.True(error.Span.Column > 0);
        Assert.Null(MermaidRenderer.Render(source).Artifact);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PegClassRangesChargeExistingElementAndEdgeBudgetsWithoutExpandingCodePoints(bool edges) {
        var limits = edges ? new MermaidResourceLimits { MaximumEdges = 2 } : new MermaidResourceLimits { MaximumElements = 3 };
        var parsed = new MermaidParser().ParseRailroad("railroad-peg-beta\nvalue <- [a-zA-Z0-9] ;", new MermaidParseOptions { ResourceLimits = limits });
        Assert.Null(parsed.Document); var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.ResourceLimitExceeded, error.Code);
        Assert.Contains(edges ? nameof(MermaidResourceLimits.MaximumEdges) : nameof(MermaidResourceLimits.MaximumElements), error.Message, StringComparison.Ordinal);
        var compact = new MermaidParser().ParseRailroad("railroad-peg-beta\nvalue <- [\\u0000-\\uFFFF] ;", new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 2 } });
        Assert.Empty(compact.Diagnostics); Assert.Equal(RailroadExpressionKind.CharacterRange, Assert.Single(compact.Document!.Rules).Definition.Expression.Kind);
    }

    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 1200)]
    public void CompletedFrontendsUseExistingDirectFencedNativeSvgPngAndCaseMetadata(bool dark, int width) {
        foreach (var source in new[] { Abnf, Peg }) {
            var options = new MermaidRenderOptions {
                WarningPolicy = MermaidWarningPolicy.Reject,
                Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family },
                Railroad = new MermaidRailroadRenderOptions { Width = width, Height = 220 }
            };
            var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
            var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Diagnostics);
            var native = Assert.IsType<VisualArtifact>(direct.Artifact);
            var artifact = Assert.Single(fenced.Artifacts); var svg = native.ToSvg(); var png = native.ToPng();
            Assert.Equal(svg, artifact.ToSvg()); Assert.Equal(png, artifact.ToPng());
            var xml = XDocument.Parse(svg); var image = RasterImageDecoder.Decode(png);
            var size = native.NaturalSize!.Value;
            Assert.Equal(size.Width, image.Width); Assert.Equal(size.Height, image.Height);
            Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
            if (source == Abnf) {
                Assert.Contains(xml.Descendants(), node => (string?)node.Attribute("data-case-sensitive") == "true");
                Assert.Contains(xml.Descendants(), node => (string?)node.Attribute("data-case-sensitive") == "false");
            } else Assert.Contains(xml.Descendants(), node => (string?)node.Attribute("data-first-code-point") == "48" && (string?)node.Attribute("data-last-code-point") == "57");
            var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(capture)) {
                Directory.CreateDirectory(capture); var path = Path.Combine(capture, "railroad-frontend-completion-" + (source == Abnf ? "abnf" : "peg") + "-" + width + "-" + (dark ? "dark" : "light"));
                File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png);
            }
        }
    }
}
