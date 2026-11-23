using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>Compiles a website snippet against the loaded ChartForgeX assemblies, so a specialized snippet is only published when it builds.</summary>
internal static class SnippetCompiler {
    private static readonly Lazy<MetadataReference[]> References = new(() => {
        var platform = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var product = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && assembly.GetName().Name?.StartsWith("ChartForgeX", StringComparison.Ordinal) == true
                && assembly.GetName().Name != "ChartForgeX.Examples" && assembly.Location.Length > 0)
            .Select(assembly => assembly.Location);
        return platform.Concat(product).Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToArray();
    });

    private static readonly CSharpParseOptions Parse = new(LanguageVersion.Latest);
    private static readonly CSharpCompilationOptions Options = new(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Disable);

    /// <summary>Returns the first compile error, or null when the program builds.</summary>
    public static string? Errors(string program) {
        // The recipes compile with the product assemblies the examples load; make sure the ones a snippet may name are loaded.
        _ = typeof(ChartForgeX.Core.Chart).Assembly;
        var compilation = CSharpCompilation.Create("Snippet", new[] { CSharpSyntaxTree.ParseText(program, Parse) }, References.Value, Options);
        var error = compilation.GetDiagnostics().FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return error?.ToString();
    }
}
