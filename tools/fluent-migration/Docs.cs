using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Rewrites C# code blocks in Markdown files against the library compilations.</summary>
static class Docs {
    static readonly Regex Fence = new(@"(?ms)^(?<indent>[ \t]*)```(?<lang>csharp|cs|c#|C#)[ \t]*\r?\n(?<code>.*?)^\k<indent>```", RegexOptions.Compiled);

    internal static void Run(Plan plan, List<Compilation> libraries, IEnumerable<string> files, List<string> report) {
        var references = libraries.Select(c => c.ToMetadataReference()).Concat(libraries.SelectMany(c => c.References)).Distinct().ToList();
        var namespaces = libraries.SelectMany(c => Namespaces(c.Assembly.GlobalNamespace)).Where(n => n.Length > 0).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();
        string globalUsings = string.Concat(new[] { "System", "System.Collections.Generic", "System.Linq", "System.Globalization" }.Concat(namespaces).Select(n => "global using " + n + ";\n"));
        // Snippets often use a variable declared in the prose around them; a hidden class declares the usual ones.
        string preamble = @"
            public static class DocsSnippetVariables {
                public static ChartForgeX.Core.Chart chart = null!;
                public static ChartForgeX.Core.ChartAxis axis = null!;
                public static ChartForgeX.Core.ChartSeries series = null!, forecast = null!, demand = null!, sales = null!, readership = null!, completed = null!, tileSeries = null!, waterfall = null!, ranges = null!, prior = null!, current = null!, bubble = null!;
                public static ChartForgeX.Rendering.PreparedVisual prepared = null!;
                public static ChartForgeX.Composition.VisualCanvas canvas = null!;
                public static ChartForgeX.Core.ChartGrid grid = null!;
                public static ChartForgeX.Topology.TopologyChart topology = null!;
                public static ChartForgeX.Stories.VisualStory story = null!;
            }";
        globalUsings += "global using static DocsSnippetVariables;\n";
        var usingsTree = CSharpSyntaxTree.ParseText(globalUsings + preamble, new CSharpParseOptions(LanguageVersion.Latest), path: "GlobalUsings.cs");
        foreach (string file in files) {
            string text = File.ReadAllText(file);
            int changes = 0;
            string updated = Fence.Replace(text, match => {
                string code = match.Groups["code"].Value;
                string indent = match.Groups["indent"].Value;
                string body = indent.Length == 0 ? code : string.Join("\n", code.Split('\n').Select(l => l.StartsWith(indent) ? l[indent.Length..] : l));
                var tree = CSharpSyntaxTree.ParseText(body, new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Regular), path: Path.GetFileName(file));
                var compilation = CSharpCompilation.Create("DocSnippet", new[] { usingsTree, tree }, references, new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));
                var rewriter = new Rewriter(plan, compilation.GetSemanticModel(tree), false, new List<PendingMember>(), new Dictionary<string, (string, string, List<Rewriter.RenderedCall>)>(), new List<Rewriter.ForwarderInfo>());
                var root = rewriter.Visit(tree.GetRoot())!;
                report.AddRange(rewriter.Report.Select(r => file + ": " + r));
                if (rewriter.Changes == 0) return match.Value;
                changes += rewriter.Changes;
                string result = root.ToFullString();
                if (indent.Length > 0) result = string.Join("\n", result.Split('\n').Select(l => l.Length == 0 ? l : indent + l));
                return match.Value.Replace(code, result);
            });
            // Leftover With* calls the model couldn't bind are listed for a manual pass.
            foreach (Match left in Regex.Matches(updated, @"\.(With|Configure|Use)[A-Z]\w*\(")) report.Add(file + ": unbound " + left.Value);
            if (changes > 0) { File.WriteAllText(file, updated); report.Add(file + ": " + changes + " changes"); }
        }
    }

    static IEnumerable<string> Namespaces(INamespaceSymbol ns) {
        foreach (var child in ns.GetNamespaceMembers()) {
            if (child.GetTypeMembers().Any(t => t.DeclaredAccessibility == Accessibility.Public)) yield return child.ToDisplayString();
            foreach (var nested in Namespaces(child)) yield return nested;
        }
    }
}
