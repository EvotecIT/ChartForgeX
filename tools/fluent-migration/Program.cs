using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

MSBuildLocator.RegisterDefaults();
return await Runner.Run(args);

static class Runner {
    internal static readonly string[] Libraries = {
        "ChartForgeX", "ChartForgeX.Visuals", "ChartForgeX.Stories", "ChartForgeX.Interactivity", "ChartForgeX.Interactivity.Html",
        "ChartForgeX.Markup", "ChartForgeX.Mermaid", "ChartForgeX.Markup.Mermaid"
    };

    internal static async Task<int> Run(string[] args) {
        string command = args[0];
        string solutionPath = args[1];
        var properties = new Dictionary<string, string> { ["TargetFramework"] = "net8.0", ["Configuration"] = "Debug" };
        // Extra MSBuild properties, e.g. the product DLL paths the benchmark fixtures compile against.
        foreach (string pair in (Environment.GetEnvironmentVariable("FLUENT_PROPS") ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries)) {
            int eq = pair.IndexOf('=');
            if (eq > 0) properties[pair[..eq]] = pair[(eq + 1)..];
        }
        using MSBuildWorkspace workspace = MSBuildWorkspace.Create(properties);
        workspace.RegisterWorkspaceFailedHandler(e => { if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure) Console.Error.WriteLine(e.Diagnostic.Message); });
        Solution solution = await workspace.OpenSolutionAsync(solutionPath);
        switch (command) {
            case "inventory":
                await Inventory.Run(solution, args[2]);
                return 0;
            case "plan":
            case "apply": {
                // args: command solution mapDir [extra.csproj ...]
                foreach (string extra in args.Skip(3)) {
                    if (solution.Projects.Any(p => string.Equals(p.FilePath, Path.GetFullPath(extra), StringComparison.OrdinalIgnoreCase))) continue;
                    try { await workspace.OpenProjectAsync(Path.GetFullPath(extra)); } catch (Exception ex) { Console.Error.WriteLine("skip " + extra + ": " + ex.Message); }
                    solution = workspace.CurrentSolution;
                }
                return await Migration.Run(solution, args[2], command == "apply");
            }
            default:
                Console.Error.WriteLine("unknown command");
                return 1;
        }
    }
}

static class Inventory {
    internal static async Task Run(Solution solution, string output) {
        var types = new List<object>();
        foreach (Project project in solution.Projects.Where(p => Runner.Libraries.Contains(p.Name))) {
            Compilation? compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;
            foreach (var tree in compilation.SyntaxTrees) {
                var model = compilation.GetSemanticModel(tree);
                foreach (var decl in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()) {
                    if (model.GetDeclaredSymbol(decl) is not INamedTypeSymbol type || type.DeclaredAccessibility != Accessibility.Public) continue;
                    var props = decl.Members.OfType<PropertyDeclarationSyntax>().Where(p => p.Modifiers.Any(SyntaxKind.PublicKeyword) && !p.Modifiers.Any(SyntaxKind.StaticKeyword))
                        .Select(p => {
                            var s = (IPropertySymbol)model.GetDeclaredSymbol(p)!;
                            return new {
                                name = s.Name, type = s.Type.ToDisplayString(),
                                settable = s.SetMethod != null && s.SetMethod.DeclaredAccessibility == Accessibility.Public && !s.SetMethod.IsInitOnly,
                                file = Path.GetFileName(tree.FilePath)
                            };
                        }).ToList();
                    var methods = decl.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword))
                        .Select(m => (syntax: m, symbol: model.GetDeclaredSymbol(m)!))
                        .Where(m => ReturnsSelf(m.symbol, type))
                        .Select(m => new {
                            name = m.symbol.Name,
                            ext = m.symbol.IsExtensionMethod,
                            receiver = m.symbol.IsExtensionMethod ? m.symbol.Parameters[0].Type.ToDisplayString() : type.ToDisplayString(),
                            parameters = string.Join(", ", m.symbol.Parameters.Skip(m.symbol.IsExtensionMethod ? 1 : 0).Select(p => p.Type.ToDisplayString() + " " + p.Name + (p.HasExplicitDefaultValue ? " = " + (p.ExplicitDefaultValue ?? "null") : ""))),
                            body = Compact((SyntaxNode?)m.syntax.Body ?? m.syntax.ExpressionBody),
                            file = Path.GetFileName(tree.FilePath)
                        }).ToList();
                    if (props.Count == 0 && methods.Count == 0) continue;
                    types.Add(new { project = project.Name, type = type.ToDisplayString(), kind = type.TypeKind.ToString(), isSealed = type.IsSealed, isStatic = type.IsStatic, properties = props, methods });
                }
            }
        }
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(types, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{types.Count} type declarations");
    }

    static bool ReturnsSelf(IMethodSymbol m, INamedTypeSymbol type) {
        if (m.IsExtensionMethod) return m.Parameters.Length > 0 && SymbolEqualityComparer.Default.Equals(m.ReturnType, m.Parameters[0].Type);
        if (m.IsStatic) return false;
        return SymbolEqualityComparer.Default.Equals(m.ReturnType.OriginalDefinition, type.OriginalDefinition) || m.ReturnType is ITypeParameterSymbol;
    }

    static string Compact(SyntaxNode? node) => node == null ? "" : System.Text.RegularExpressions.Regex.Replace(node.ToString(), @"\s+", " ");
}
