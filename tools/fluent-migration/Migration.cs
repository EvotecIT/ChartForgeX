using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class Migration {
    internal static async Task<int> Run(Solution solution, string mapDirectory, bool apply) {
        var map = MigrationMap.Load(Directory.GetFiles(mapDirectory, "*.map").OrderBy(f => f, StringComparer.Ordinal));
        var libraries = new List<Compilation>();
        var compilations = new Dictionary<ProjectId, Compilation>();
        foreach (var project in solution.Projects) {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;
            compilations[project.Id] = compilation;
            if (Runner.Libraries.Contains(project.Name)) libraries.Add(compilation);
        }
        var plan = Plan.Build(map, libraries);
        string outDir = Path.Combine(mapDirectory, "..", "out");
        Directory.CreateDirectory(outDir);
        File.WriteAllLines(Path.Combine(outDir, "errors.txt"), plan.Errors);
        File.WriteAllLines(Path.Combine(outDir, "nonpublic-setters.txt"), plan.NonPublicSetters);
        File.WriteAllLines(Path.Combine(outDir, "keeps.txt"), plan.KeepReasons.OrderBy(x => x, StringComparer.Ordinal));
        File.WriteAllLines(Path.Combine(outDir, "unmapped.txt"), plan.Unmapped.OrderBy(x => x, StringComparer.Ordinal));
        File.WriteAllLines(Path.Combine(outDir, "auto.txt"), plan.Calls.Where(c => c.Value.Auto).Select(c => c.Key + "\t" + string.Join(" ; ", c.Value.Steps.Select(s => string.Join(".", s.Path) + "(" + s.Arguments + ")"))).OrderBy(x => x, StringComparer.Ordinal));
        File.WriteAllLines(Path.Combine(outDir, "types.txt"), plan.Calls.Keys.Concat(plan.Keeps.Keys).Where(k => !plan.Stays.Contains(k))
            .Select(k => System.Text.RegularExpressions.Regex.Replace(k[2..], @"\(.*$", string.Empty)).Select(k => k[..k.LastIndexOf('.')]).Distinct().OrderBy(x => x, StringComparer.Ordinal));
        Console.WriteLine($"moves {plan.Moves.Count}, calls {plan.Calls.Count} (auto {plan.Calls.Values.Count(c => c.Auto)}), keeps {plan.Keeps.Count}, unmapped {plan.Unmapped.Count}, errors {plan.Errors.Count}");
        if (plan.Errors.Count > 0) { foreach (var e in plan.Errors.Take(40)) Console.WriteLine("  " + e); }
        if (Environment.GetEnvironmentVariable("FLUENT_DOCS") is string docsList && docsList.Length > 0) {
            var docsReport = new List<string>();
            Docs.Run(plan, libraries, File.ReadAllLines(docsList).Where(l => l.Trim().Length > 0), docsReport);
            File.WriteAllLines(Path.Combine(outDir, "docs-report.txt"), docsReport);
            Console.WriteLine("docs: " + docsReport.Count + " report lines");
            return 0;
        }
        if (!apply) return plan.Errors.Count == 0 ? 0 : 2;
        if (plan.Errors.Count > 0) return 2;

        var pending = new List<PendingMember>();
        var chains = new Dictionary<string, (string, string, List<Rewriter.RenderedCall>)>();
        var report = new List<string>();
        var forwarders = new List<Rewriter.ForwarderInfo>();
        var results = new Dictionary<string, SyntaxNode>(StringComparer.Ordinal);
        int changed = 0;
        foreach (var project in solution.Projects) {
            if (!compilations.TryGetValue(project.Id, out var compilation)) continue;
            bool isLibrary = Runner.Libraries.Contains(project.Name);
            foreach (var tree in compilation.SyntaxTrees) {
                string path = tree.FilePath;
                if (string.IsNullOrEmpty(path) || path.Contains("/obj/") || path.Contains("/bin/") || results.ContainsKey(path) || !File.Exists(path)) continue;
                var model = compilation.GetSemanticModel(tree);
                var rewriter = new Rewriter(plan, model, isLibrary, pending, chains, forwarders);
                var root = rewriter.Visit(tree.GetRoot())!;
                var globalUsings = new HashSet<string>(compilation.SyntaxTrees.SelectMany(t => t.GetCompilationUnitRoot().Usings).Where(u => u.GlobalKeyword.RawKind != 0 && u.Name != null).Select(u => u.Name!.ToString()), StringComparer.Ordinal);
                root = rewriter.AddUsings(root, globalUsings);
                report.AddRange(rewriter.Report);
                if (rewriter.Changes > 0) { results[path] = root; changed++; }
            }
        }
        // Insert moved declarations into their target types.
        foreach (var group in pending.GroupBy(p => p.TargetTypeName)) {
            string? file = FindTypeFile(group.Key, results, solution);
            if (file == null) { report.Add("no file for target type " + group.Key); continue; }
            SyntaxNode root = results.TryGetValue(file, out var r) ? r : CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(file), path: file).GetRoot();
            var type = root.DescendantNodes().OfType<TypeDeclarationSyntax>().First(t => t.Identifier.Text == group.Key);
            string text = root.ToFullString();
            int insert = type.CloseBraceToken.SpanStart;
            string members = string.Concat(group.Select(m => "\n" + m.Text.TrimEnd() + "\n"));
            text = text[..insert] + members.TrimStart('\n') + text[insert..];
            results[file] = CSharpSyntaxTree.ParseText(text, path: file).GetRoot();
        }
        foreach (var (path, root) in results) await File.WriteAllTextAsync(path, root.ToFullString());
        // Extension forwarders for kept methods whose name a property already uses.
        foreach (var group in forwarders.GroupBy(f => (f.TypeName, f.Namespace))) {
            var first = group.First();
            string file = Path.Combine(first.Directory, group.Key.TypeName + ".Fluent.cs");
            var usings = new SortedSet<string>(group.SelectMany(f => f.Usings.Split('\n', StringSplitOptions.RemoveEmptyEntries)), StringComparer.Ordinal);
            string text = string.Concat(usings.Select(u => u + "\n")) + (usings.Count > 0 ? "\n" : string.Empty)
                + "namespace " + group.Key.Namespace + ";\n\n"
                + "/// <summary>Hand-written fluent methods of <see cref=\"" + group.Key.TypeName + "\"/> that do more than set one property.</summary>\n"
                + "public static partial class " + group.Key.TypeName + "Fluent {\n"
                + string.Join("\n", group.Select(f => f.Text)) + "}\n";
            await File.WriteAllTextAsync(file, text);
            report.Add("forwarders: " + file + " (" + group.Count() + ")");
        }
        File.WriteAllLines(Path.Combine(outDir, "report.txt"), report);
        Console.WriteLine($"changed {results.Count} files; report {report.Count} lines");
        return 0;
    }

    static string? FindTypeFile(string typeName, Dictionary<string, SyntaxNode> results, Solution solution) {
        var files = solution.Projects.SelectMany(p => p.Documents).Select(d => d.FilePath!).Where(f => f != null && !f.Contains("/obj/")).Distinct().ToList();
        string? preferred = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == typeName);
        if (preferred != null) return preferred;
        foreach (var f in files) {
            string text = results.TryGetValue(f, out var r) ? r.ToFullString() : File.ReadAllText(f);
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\b(class|struct|record)\s+" + typeName + @"\b")) return f;
        }
        return null;
    }
}
