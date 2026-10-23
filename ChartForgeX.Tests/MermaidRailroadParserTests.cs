using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRailroadParserTests {
    public static IEnumerable<object[]> CommonSources() => new[] {
        new object[] { "ebnf", "railroad-ebnf-beta\nvalue = \"x\" [ part ] { \",\" part } ;\npart = \"p\" ;" },
        new object[] { "abnf", "railroad-abnf-beta\nvalue = \"x\" [ part ] *( \",\" part ) ;\npart = \"p\" ;" },
        new object[] { "peg", "railroad-peg-beta\nvalue <- \"x\" part? (\",\" part)* ;\npart <- \"p\" ;" },
        new object[] { "ir", "railroad-beta\nvalue = sequence(terminal(\"x\"), optional(nonterminal(\"part\")), zeroOrMore(sequence(terminal(\",\"), nonterminal(\"part\")))) ;\npart = terminal(\"p\") ;" },
    };

    [Theory]
    [MemberData(nameof(CommonSources))]
    public void FourFrontendsLowerToOneSemanticTreeWithoutTurningReferencesIntoLiterals(string dialect, string source) {
        var result = new MermaidParser().ParseRailroad(source); Assert.Empty(result.Diagnostics);
        var document = result.Document!; Assert.False(document.IsDiagnosticOnly);
        Assert.Equal(new[] { "value", "part" }, document.Rules.Select(rule => rule.Id));
        var value = document.Rules[0].Definition;
        Assert.Equal(RailroadExpressionKind.Sequence, value.Expression.Kind);
        Assert.Equal(new[] { RailroadExpressionKind.Terminal, RailroadExpressionKind.Optional, RailroadExpressionKind.Repeat }, value.Children.Select(child => child.Expression.Kind));
        Assert.Equal("x", value.Children[0].Expression.Value);
        Assert.Equal(dialect != "abnf", value.Children[0].Expression.CaseSensitive);
        Assert.Equal("part", Assert.Single(value.Children[1].Children).Expression.Value);
        Assert.Equal(RailroadExpressionKind.Reference, Assert.Single(value.Children[1].Children).Expression.Kind);
        Assert.Equal(0, value.Children[2].Expression.MinimumOccurrences); Assert.Null(value.Children[2].Expression.MaximumOccurrences);
        Assert.Equal(2, document.Rules[0].Span.Line);
        Assert.All(Flatten(value), node => Assert.True(node.Span.Line > 0 && node.Span.Column > 0 && node.Span.Length > 0));
    }

    [Fact]
    public void EbnfPreservesIsoCommentsSpecialSequencesPostfixesAndLanguageExceptions() {
        const string source = "railroad-ebnf-beta\n/* W3C comment */\nvalue ::= (letter , [ \"_\" ] , { digit }) - keyword ;\nletter = \"a\" | 'b' ; (* ISO comment *)\ndigit = ? decimal digit ? ;\nkeyword = \"if\" ;\npostfix = letter? letter* letter+ ;";
        var parsed = new MermaidParser().ParseRailroad(source); Assert.Empty(parsed.Diagnostics);
        Assert.Equal(RailroadExpressionKind.Except, parsed.Document!.Rules[0].Definition.Expression.Kind);
        Assert.Equal("decimal digit", parsed.Document.Rules[2].Definition.Expression.Value);
        var postfix = parsed.Document.Rules[4].Definition.Children;
        Assert.Equal(RailroadExpressionKind.Optional, postfix[0].Expression.Kind);
        Assert.Equal(0, postfix[1].Expression.MinimumOccurrences); Assert.Equal(1, postfix[2].Expression.MinimumOccurrences);
    }

    [Fact]
    public void AbnfKeepsRepeatBoundsNumericRadicesCaseRulesAndSemicolonCommentBoundaries() {
        const string source = "railroad-abnf-beta\n; comment before the first rule\nPacket = 2*4(%x41.42 / %d48-57) 3DIGIT *WSP 1*ALPHA ; trailing comment\nnext = packet %b1000001 ;\n";
        var parsed = new MermaidParser().ParseRailroad(source); Assert.Empty(parsed.Diagnostics); var document = parsed.Document!;
        Assert.Equal(new[] { "Packet", "next" }, document.Rules.Select(rule => rule.Id));
        Assert.Equal(3, document.Rules[0].Span.Line); Assert.Equal(4, document.Rules[1].Span.Line);
        var sequence = document.Rules[0].Definition.Children;
        Assert.Equal(2, sequence[0].Expression.MinimumOccurrences); Assert.Equal(4, sequence[0].Expression.MaximumOccurrences);
        Assert.Equal(3, sequence[1].Expression.MinimumOccurrences); Assert.Equal(3, sequence[1].Expression.MaximumOccurrences);
        Assert.Equal(0, sequence[2].Expression.MinimumOccurrences); Assert.Null(sequence[2].Expression.MaximumOccurrences);
        Assert.Equal(1, sequence[3].Expression.MinimumOccurrences);
        var numeric = Flatten(document.Rules[0].Definition).Where(node => node.Expression.Kind == RailroadExpressionKind.CharacterRange).Select(node => node.Expression).ToArray();
        Assert.Contains(numeric, node => node.FirstCodePoint == 65 && node.LastCodePoint == 65);
        Assert.Contains(numeric, node => node.FirstCodePoint == 66 && node.LastCodePoint == 66);
        Assert.Contains(numeric, node => node.FirstCodePoint == 48 && node.LastCodePoint == 57);
        Assert.Equal(65, document.Rules[1].Definition.Children[1].Expression.FirstCodePoint);
        Assert.Equal(RailroadRuleComparison.OrdinalIgnoreCase, document.ToRailroadDiagramBlock().RuleComparison);
    }

    [Fact]
    public void PegKeepsOrderedChoiceAndBothZeroWidthPredicates() {
        const string source = "railroad-peg-beta\nvalue <- !keyword &letter letter+ / . ; # comment\nkeyword <- \"if\" / \"else\" ;\nletter <- 'a' ;";
        var parsed = new MermaidParser().ParseRailroad(source); Assert.Empty(parsed.Diagnostics);
        var expression = parsed.Document!.Rules[0].Definition;
        Assert.True(expression.Expression.OrderedChoice);
        Assert.Contains(Flatten(expression), node => node.Expression.Kind == RailroadExpressionKind.NegativeLookahead);
        Assert.Contains(Flatten(expression), node => node.Expression.Kind == RailroadExpressionKind.Lookahead);
        Assert.Contains(Flatten(expression), node => node.Expression.Kind == RailroadExpressionKind.AnyCharacter);
    }

    [Theory]
    [InlineData("railroad-ebnf-beta\na = [ \"x\" ;")]
    [InlineData("railroad-ebnf-beta\na = \"x\"")]
    [InlineData("railroad-ebnf-beta\na = \"unclosed;")]
    [InlineData("railroad-ebnf-beta\na = \"x\" | ;")]
    [InlineData("railroad-ebnf-beta\na = \"x\";\na = \"y\";")]
    [InlineData("railroad-abnf-beta\na = 4*2DIGIT ;")]
    [InlineData("railroad-abnf-beta\na = %b2 ;")]
    [InlineData("railroad-abnf-beta\na = %x110000 ;")]
    [InlineData("railroad-abnf-beta\na = %x41-40 ;")]
    [InlineData("railroad-abnf-beta\na = %s\"x\" ;")]
    [InlineData("railroad-abnf-beta\na = %i\"x\" ;")]
    [InlineData("railroad-abnf-beta\na = 'x' ;")]
    [InlineData("railroad-abnf-beta\nA = \"x\" ;\na = \"y\" ;")]
    [InlineData("railroad-peg-beta\na <- !!b ;")]
    [InlineData("railroad-peg-beta\na <- b?? ;")]
    [InlineData("railroad-beta\na = terminal() ;")]
    [InlineData("railroad-beta\na = sequence() ;")]
    [InlineData("railroad-beta\na = evaluate(\"code\") ;")]
    public void MalformedOrUnsupportedGrammarHasLocatedErrorsAndNeverProducesAnArtifact(string source) {
        var parsed = new MermaidParser().ParseRailroad(source); Assert.True(parsed.HasErrors);
        var error = Assert.Single(parsed.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code); Assert.True(error.Span.Line >= 2 && error.Span.Column >= 1);
        Assert.Null(MermaidRenderer.Render(source).Artifact);
    }

    [Fact]
    public void OriginalFrontMatterAndMarkdownLocationsSurviveTheSharedParser() {
        const string source = "---\ntitle: Grammar\n---\nrailroad-ebnf-beta\nvalue = [ \"x\" ;";
        var direct = new MermaidParser().ParseRailroad(source); var error = Assert.Single(direct.Diagnostics);
        Assert.Equal(5, error.Span.Line);
        var fenced = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Artifacts); var mapped = Assert.Single(fenced.Diagnostics);
        Assert.Equal(error.Span.Line + 3, mapped.Line); Assert.Equal(error.Span.Column, mapped.Column);
    }

    [Theory]
    [MemberData(nameof(CommonSources))]
    public void EveryFrontendUsesTheCapturedElementBudget(string dialect, string source) {
        var parsed = new MermaidParser().ParseRailroad(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 1 } });
        Assert.True(parsed.HasErrors, $"The {dialect} frontend must reject a model over its captured element budget.");
        Assert.Null(parsed.Document); Assert.Equal(MermaidDiagnosticCodes.ResourceLimitExceeded, Assert.Single(parsed.Diagnostics).Code);
    }

    [Fact]
    public void NestedAndWideExpressionsUseDepthAndFanOutBudgetsAndCancellation() {
        var parser = new MermaidParser();
        var nested = parser.ParseRailroad("railroad-beta\na = optional(optional(optional(terminal(\"x\")))) ;", new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumNestingDepth = 2 } });
        Assert.Null(nested.Document); Assert.Contains(nameof(MermaidResourceLimits.MaximumNestingDepth), Assert.Single(nested.Diagnostics).Message, StringComparison.Ordinal);
        var wide = parser.ParseRailroad("railroad-ebnf-beta\na = \"a\" \"b\" \"c\" ;", new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumEdges = 1 } });
        Assert.Null(wide.Document); Assert.Contains(nameof(MermaidResourceLimits.MaximumEdges), Assert.Single(wide.Diagnostics).Message, StringComparison.Ordinal);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => parser.ParseRailroad("railroad-ebnf-beta\na = \"x\";", null, cancellation.Token));
    }

    internal static IEnumerable<MermaidRailroadExpression> Flatten(MermaidRailroadExpression expression) {
        yield return expression; foreach (var child in expression.Children) foreach (var descendant in Flatten(child)) yield return descendant;
    }
}
