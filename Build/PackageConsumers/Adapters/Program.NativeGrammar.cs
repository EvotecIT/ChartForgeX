using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

internal static partial class Program {
    private static void NativeRailroadContracts() {
        var sources = new[] {
            ("ebnf", "railroad-ebnf-beta\nvalue = \"x\" [ part ] { \",\" part } ;\npart = \"p\" ;"),
            ("abnf", "railroad-abnf-beta\nvalue = \"x\" [ part ] *( \",\" part ) ;\npart = \"p\" ;"),
            ("peg", "railroad-peg-beta\nvalue <- \"x\" part? (\",\" part)* ;\npart <- \"p\" ;"),
            ("ir", "railroad-beta\nvalue = sequence(terminal(\"x\"), optional(nonterminal(\"part\")), zeroOrMore(sequence(terminal(\",\"), nonterminal(\"part\")))) ;\npart = terminal(\"p\") ;")
        };
        foreach (var item in sources) {
            var artifact = PackedNativeDiagram("railroad-" + item.Item1, item.Item2);
            var block = (RailroadDiagramBlock)artifact.Model!;
            PackageAssertions.Require(block.Rules.Select(rule => rule.Id).SequenceEqual(new[] { "value", "part" })
                && block.Rules[0].Expression.Kind == RailroadExpressionKind.Sequence,
                "Packed Railroad lost ordered rule identity or canonical expression semantics for " + item.Item1);
            var root = XDocument.Parse(artifact.ToSvg());
            PackageAssertions.Require(root.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "railroad-rule") == 2
                && root.Descendants().Any(node => (string?)node.Attribute("data-expression-kind") == "Reference")
                && root.Descendants().Any(node => (string?)node.Attribute("data-expression-kind") == "Repeat"),
                "Packed Railroad flattened references or repetition into label-only output for " + item.Item1);
        }
        var limits = new MermaidResourceLimits { MaximumOutputBytes = 100, MaximumRasterPixels = 100 };
        var bounded = RenderFixture(sources[0].Item2, new MermaidRenderOptions {
            ParseOptions = new MermaidParseOptions { ResourceLimits = limits }
        });
        PackageAssertions.Require(!bounded.HasErrors && bounded.Artifact != null, "Packed Railroad bounds rejected valid source before its captured export policy.");
        limits.MaximumOutputBytes = 1000000; limits.MaximumRasterPixels = 1000000;
        var svgRejected = false; var pngRejected = false;
        try { bounded.Artifact!.ToSvg(); } catch (MermaidResourceLimitException error) { svgRejected = error.LimitName == nameof(MermaidResourceLimits.MaximumOutputBytes); }
        try { bounded.Artifact!.ToPng(); } catch (MermaidResourceLimitException error) { pngRejected = error.LimitName == nameof(MermaidResourceLimits.MaximumRasterPixels); }
        PackageAssertions.Require(svgRejected && pngRejected, "Packed Railroad bypassed captured output or pre-allocation pixel limits.");
        using var cancellation = new CancellationTokenSource();
        var deferred = RenderFixture(sources[0].Item2, null, cancellation.Token); cancellation.Cancel();
        var cancelled = false;
        try { deferred.Artifact!.ToPng(); } catch (OperationCanceledException) { cancelled = true; }
        PackageAssertions.Require(cancelled, "Packed lazy Railroad export lost cooperative cancellation.");
        Console.WriteLine("Packed EBNF/ABNF/PEG/IR native grammar, captured export caps and deferred cancellation passed.");
    }
}
