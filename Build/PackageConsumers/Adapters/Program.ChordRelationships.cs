using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void TypedChordMarkupContracts() {
        const string source = "type chord\nnode a Support\nnode b Support\nflow first a b 3 colorValue=-10\nflow self a a 1\nflow zero b a 0\noption chordNodeOrder b a\noption chordRibbonTension .55";
        var parsed = new VisualMarkupParser().Parse("```chartforgex chart v1\n" + source + "\n```");
        PackageAssertions.Require(!parsed.HasErrors && parsed.Artifacts.Count == 1 && parsed.Artifacts[0].Model is Chart,
            "Packed chart fence did not lower typed Chord relationships into the existing Chart model.");
        var chart = (Chart)parsed.Artifacts[0].Model!;
        PackageAssertions.Require(chart.Series[0].FlowLinks.Select(flow => flow.Id).SequenceEqual(new[] { "first", "self", "zero" })
            && chart.Series[0].FlowLinks[0].ColorValue == -10 && chart.Series[0].Points.Count == 0,
            "Packed typed Chord markup fabricated numeric slots or changed source facts.");
        PackageAssertions.Png(chart.ToPng());
        var rejected = new MarkupChartParser().Parse("type chord\nnode a A\nflow bad a missing 1");
        PackageAssertions.Require(rejected.HasErrors && rejected.Document == null && rejected.Diagnostics.Any(item => item.Line == 3),
            "Packed malformed Chord endpoint input was accepted or lost its source location.");
        Console.WriteLine("Packed typed Chord chart-fence contracts passed.");
    }
}
