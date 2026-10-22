using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RailroadDiagramBlockTests {
    [Fact]
    public void ImmutableExpressionsOwnTheirChildrenAndKeepSymbolicRecursionAndExactRepeatBounds() {
        var children = new[] { RailroadExpression.Terminal("first"), RailroadExpression.Reference("value") };
        var expression = RailroadExpression.Sequence(children);
        children[0] = RailroadExpression.Terminal("changed");
        Assert.Equal("first", expression.Children[0].Value);
        Assert.Throws<NotSupportedException>(() => ((IList<RailroadExpression>)expression.Children).Add(RailroadExpression.Empty()));
        var repeat = RailroadExpression.Repeat(expression, int.MaxValue, int.MaxValue);
        Assert.Same(expression, Assert.Single(repeat.Children)); Assert.Equal(int.MaxValue, repeat.MinimumOccurrences);
        var block = RailroadDiagramBlock.Create().AddRule("value", repeat).WithNaturalSize();
        Assert.InRange(block.Options.Size.Width, 1, 2000); Assert.InRange(block.Options.Size.Height, 1, 1000);
        var xml = XDocument.Parse(block.ToSvg());
        Assert.Single(Roles(xml, "railroad-repeat-loop"));
        Assert.Equal("exactly 2147483647", Assert.Single(Roles(xml, "railroad-repeat-constraint")).Value);
        Assert.Equal("value", Assert.Single(Roles(xml, "railroad-reference")).Attribute("data-reference-id")!.Value);
    }

    [Fact]
    public void RepeatRangeRuleIdentityAndNullConstraintsAreEnforcedBeforePaint() {
        Assert.Throws<ArgumentOutOfRangeException>(() => RailroadExpression.Repeat(RailroadExpression.Empty(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => RailroadExpression.Repeat(RailroadExpression.Empty(), 4, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => RailroadExpression.CharacterRange(20, 19));
        Assert.Throws<ArgumentOutOfRangeException>(() => RailroadExpression.CharacterRange(0, 0x110000));
        Assert.Throws<ArgumentException>(() => RailroadExpression.Choice(Array.Empty<RailroadExpression>()));
        Assert.Throws<ArgumentException>(() => RailroadExpression.Reference(" "));
        Assert.Throws<ArgumentException>(() => RailroadExpression.Sequence(new RailroadExpression[] { null! }));
        Assert.Throws<InvalidOperationException>(() => RailroadDiagramBlock.Create().WithNaturalSize());
        Assert.Throws<InvalidOperationException>(() => RailroadDiagramBlock.Create().AddRule("value", RailroadExpression.Empty()).AddRule("value", RailroadExpression.Empty()).ToSvg());
        var abnf = RailroadDiagramBlock.Create(); abnf.RuleComparison = RailroadRuleComparison.OrdinalIgnoreCase;
        Assert.Throws<InvalidOperationException>(() => abnf.AddRule("Packet", RailroadExpression.Empty()).AddRule("packet", RailroadExpression.Empty()).ToPng());
        Assert.Throws<ArgumentOutOfRangeException>(() => abnf.RuleComparison = (RailroadRuleComparison)900);
    }

    [Fact]
    public void RuleViewsStayStableAndReferenceLinksResolveOnlyExistingGrammarRules() {
        var block = RailroadDiagramBlock.Create(); var view = block.Rules;
        block.AddRule("entry", RailroadExpression.Sequence(RailroadExpression.Reference("value"), RailroadExpression.Reference("external"), RailroadExpression.Terminal("value")));
        block.AddRule("value", RailroadExpression.Terminal("x")); block.WithNaturalSize();
        Assert.Same(view, block.Rules); Assert.Equal(2, view.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<RailroadRule>)view).Clear());
        var xml = XDocument.Parse(block.ToSvg()); var references = Roles(xml, "railroad-reference");
        var target = Roles(xml, "railroad-rule")[1].Attribute("id")!.Value;
        Assert.Equal("#" + target, references[0].Attribute("href")!.Value);
        Assert.Equal("local", references[0].Attribute("data-reference-scope")!.Value);
        Assert.Equal("external", references[1].Attribute("data-reference-scope")!.Value); Assert.Null(references[1].Attribute("href"));
        Assert.Contains(Roles(xml, "railroad-terminal"), node => node.Value.Contains("\"value\"", StringComparison.Ordinal));
    }

    [Fact]
    public void ZeroRepeatAndNonConsumingPredicatesNeverPaintAFalseConsumingAlternative() {
        var expression = RailroadExpression.Sequence(
            RailroadExpression.Repeat(RailroadExpression.Terminal("never consumed"), 0, 0),
            RailroadExpression.Lookahead(RailroadExpression.Terminal("required next")),
            RailroadExpression.Lookahead(RailroadExpression.Terminal("forbidden next"), false),
            RailroadExpression.Except(RailroadExpression.Reference("name"), RailroadExpression.Reference("keyword")));
        var xml = XDocument.Parse(RailroadDiagramBlock.Create().AddRule("entry", expression).WithNaturalSize().ToSvg());
        Assert.DoesNotContain("never consumed", xml.ToString(), StringComparison.Ordinal);
        Assert.Equal("0 occurrences", Assert.Single(Roles(xml, "railroad-repeat-constraint")).Value);
        Assert.Equal(2, Roles(xml, "railroad-predicate-frame").Length);
        Assert.Equal(2, Roles(xml, "railroad-predicate-leader").Length);
        Assert.All(Roles(xml, "railroad-expression").Where(node => node.Attribute("data-consuming") != null), node => Assert.Equal("false", node.Attribute("data-consuming")!.Value));
        Assert.Single(Roles(xml, "railroad-exception-label")); Assert.Single(Roles(xml, "railroad-exception-leader"));
    }

    [Fact]
    public void MeasuredNativeSceneIsDeterministicAndControlTextCannotCreateInvalidSvg() {
        var block = RailroadDiagramBlock.Create().WithTitle("Native grammar").WithSubtitle("No reference expansion")
            .AddRule("entry\u0001", RailroadExpression.Sequence(RailroadExpression.Terminal("line\nnext\t\u0000"), RailroadExpression.Reference("entry\u0001")))
            .AddRule("scalar", RailroadExpression.CharacterRange(0x1f600, 0x1f64f)).WithNaturalSize();
        var size = block.MeasureNaturalSize(); Assert.Equal(size, block.MeasureNaturalSize());
        var svg = block.ToSvg(); var png = block.ToPng(); Assert.Equal(svg, block.ToSvg()); Assert.Equal(png, block.ToPng());
        var xml = XDocument.Parse(svg); var raster = RasterImageDecoder.Decode(png);
        Assert.Contains("\\u0000", svg, StringComparison.Ordinal); Assert.Contains("\\u0001", svg, StringComparison.Ordinal);
        Assert.Equal(size.Width, raster.Width); Assert.Equal(size.Height, raster.Height);
        Assert.Equal(size.Width, double.Parse(xml.Root!.Attribute("width")!.Value, CultureInfo.InvariantCulture));
        block.Options.Size = new ChartSize(1, 1);
        Assert.Throws<InvalidOperationException>(() => block.ToSvg()); Assert.Throws<InvalidOperationException>(() => block.ToPng());
    }

    [Fact]
    public void PublicNativeNestingAndFanOutHaveHardBoundsBeforeRecursiveLayout() {
        var nested = RailroadExpression.Terminal("x");
        for (var index = 0; index < 128; index++) nested = RailroadExpression.Optional(nested);
        Assert.Throws<InvalidOperationException>(() => RailroadDiagramBlock.Create().AddRule("entry", nested).WithNaturalSize());
        var wide = RailroadExpression.Sequence(Enumerable.Repeat(RailroadExpression.Empty(), 32768));
        Assert.Throws<InvalidOperationException>(() => RailroadDiagramBlock.Create().AddRule("entry", wide).WithNaturalSize());
    }

    private static XElement[] Roles(XDocument xml, string role) => xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
}
