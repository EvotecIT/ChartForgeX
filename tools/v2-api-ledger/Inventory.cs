using System.Diagnostics;
using System.Formats.Tar;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiLedger;

internal sealed record ApiSymbol(string Assembly, string Namespace, string Type, string Name,
    string Kind, string Accessibility, string DocId, string Signature, string Attributes,
    string Constant, string Source, int Line, bool Implicit, string Receiver, string References);

internal sealed record Snapshot(string Ref, string Sha, List<ApiSymbol> Symbols,
    string[] ConditionalSources, Dictionary<string, int> SourceCounts);

/// <summary>Reads immutable Git objects and resolves exported symbols without executing product code.</summary>
internal static class Inventory {
    internal static readonly string[] Packages = ["ChartForgeX", "ChartForgeX.Interactivity",
        "ChartForgeX.Interactivity.Html", "ChartForgeX.Markup", "ChartForgeX.Mermaid", "ChartForgeX.Markup.Mermaid"];

    private static readonly SymbolDisplayFormat Display = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers |
            SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters |
            SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeConstantValue |
            SymbolDisplayMemberOptions.IncludeExplicitInterface,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName |
            SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeParamsRefOut |
            SymbolDisplayParameterOptions.IncludeOptionalBrackets | SymbolDisplayParameterOptions.IncludeExtensionThis,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    internal static Snapshot Read(string repository, string reference) {
        string sha = Git(repository, "rev-parse", reference + "^{commit}").Trim();
        var files = ReadArchive(repository, sha);
        var runtime = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("Missing runtime references."))
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path)).Cast<MetadataReference>().ToArray();
        var compilations = new Dictionary<string, CSharpCompilation>(StringComparer.Ordinal);
        var projectClosures = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var symbols = new List<ApiSymbol>();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var conditions = new List<string>();
        foreach (string package in Packages) {
            var sources = files.Where(pair => pair.Key.StartsWith(package + "/", StringComparison.Ordinal) && pair.Key.EndsWith(".cs", StringComparison.Ordinal)).ToArray();
            counts[package] = sources.Length;
            var trees = sources.Select(pair => CSharpSyntaxTree.ParseText(pair.Value, new CSharpParseOptions(LanguageVersion.Latest), pair.Key)).ToArray();
            conditions.AddRange(trees.Where(tree => tree.GetRoot().DescendantTrivia(descendIntoTrivia: true)
                .Any(trivia => trivia.IsKind(SyntaxKind.IfDirectiveTrivia) || trivia.IsKind(SyntaxKind.ElifDirectiveTrivia))).Select(tree => tree.FilePath));
            var references = runtime.ToList();
            var closure = new HashSet<string>(StringComparer.Ordinal);
            var project = System.Xml.Linq.XDocument.Parse(files[package + "/" + package + ".csproj"]);
            foreach (var item in project.Descendants("ProjectReference")) {
                string dependency = Path.GetFileNameWithoutExtension(((string?)item.Attribute("Include") ?? "").Replace('\\', '/'));
                closure.Add(dependency);
                closure.UnionWith(projectClosures[dependency]);
            }
            foreach (string dependency in closure.Order(StringComparer.Ordinal)) references.Add(compilations[dependency].ToMetadataReference());
            projectClosures.Add(package, closure);
            var compilation = CSharpCompilation.Create(package, trees, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true));
            var errors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Length > 0) throw new InvalidOperationException(package + " symbol compilation failed:\n" + string.Join("\n", errors.Take(20).Select(item => item.ToString())));
            compilations.Add(package, compilation);
            symbols.AddRange(Extract(compilation));
        }
        return new(reference, sha, symbols.OrderBy(item => item.Assembly, StringComparer.Ordinal).ThenBy(item => item.DocId, StringComparer.Ordinal).ToList(), conditions.ToArray(), counts);
    }

    internal static List<ApiSymbol> Extract(CSharpCompilation compilation) {
        var output = new List<ApiSymbol>();
        VisitNamespace(compilation.Assembly.GlobalNamespace, compilation.AssemblyName!, output);
        return output;
    }

    private static void VisitNamespace(INamespaceSymbol space, string package, List<ApiSymbol> output) {
        foreach (var child in space.GetNamespaceMembers()) VisitNamespace(child, package, output);
        foreach (var type in space.GetTypeMembers()) VisitType(type, package, output);
    }

    private static void VisitType(INamedTypeSymbol type, string package, List<ApiSymbol> output) {
        if (!ExternallyVisible(type)) return;
        output.Add(Describe(type, type, package));
        foreach (var member in type.GetMembers()) {
            if (member is INamedTypeSymbol nested) { VisitType(nested, package, output); continue; }
            if (!ExternallyVisible(member) || member is IMethodSymbol { AssociatedSymbol: not null }) continue;
            if (member.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event) output.Add(Describe(member, type, package));
        }
    }

    private static bool ExternallyVisible(ISymbol symbol) {
        if (symbol.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal)) return false;
        return symbol.ContainingType == null || ExternallyVisible(symbol.ContainingType);
    }

    private static ApiSymbol Describe(ISymbol symbol, INamedTypeSymbol type, string package) {
        var location = symbol.Locations.FirstOrDefault(item => item.IsInSource) ?? type.Locations.FirstOrDefault(item => item.IsInSource);
        var span = location?.GetLineSpan();
        string signature = symbol.ToDisplayString(Display);
        if (symbol is INamedTypeSymbol named) {
            signature = named.DeclaredAccessibility.ToString().ToLowerInvariant() + " " +
                (named.IsStatic ? "static " : named.IsAbstract && named.TypeKind == TypeKind.Class ? "abstract " : named.IsSealed && named.TypeKind == TypeKind.Class ? "sealed " : "") +
                (named.IsReadOnly ? "readonly " : "") + (named.IsRefLikeType ? "ref " : "") +
                named.TypeKind.ToString().ToLowerInvariant() + " " + signature;
            var bases = new List<string>();
            if (named.TypeKind == TypeKind.Enum) bases.Add(named.EnumUnderlyingType!.ToDisplayString(Display));
            else if (named.BaseType is { SpecialType: not SpecialType.System_Object } baseType) bases.Add(baseType.ToDisplayString(Display));
            bases.AddRange(named.Interfaces.Select(item => item.ToDisplayString(Display)));
            signature += bases.Count == 0 ? "" : " : " + string.Join(", ", bases);
        }
        var references = new HashSet<string>(StringComparer.Ordinal);
        foreach (var referenced in Types(symbol)) Collect(referenced, references);
        return new(package, type.ContainingNamespace.ToDisplayString(), type.ToDisplayString(), symbol.Name,
            symbol is INamedTypeSymbol typed ? typed.TypeKind.ToString() : symbol is IMethodSymbol method ? method.MethodKind.ToString() : symbol.Kind.ToString(),
            symbol.DeclaredAccessibility.ToString(), symbol.GetDocumentationCommentId() ?? throw new InvalidOperationException("Missing stable symbol ID: " + signature),
            signature, Attributes(symbol),
            symbol is IFieldSymbol { HasConstantValue: true } field ? Convert.ToString(field.ConstantValue, System.Globalization.CultureInfo.InvariantCulture) ?? "null" : "",
            span?.Path ?? "", (span?.StartLinePosition.Line ?? -1) + 1, symbol.IsImplicitlyDeclared,
            symbol is IMethodSymbol { IsExtensionMethod: true } extension ? extension.Parameters[0].Type.Name : type.Name,
            string.Join(";", references.Order(StringComparer.Ordinal)));
    }

    private static string Attributes(ISymbol symbol) {
        var attributes = symbol.GetAttributes().Select(item => item.ToString()).ToList();
        if (symbol is IMethodSymbol method) {
            attributes.AddRange(method.GetReturnTypeAttributes().Select(item => "return:" + item));
            foreach (var parameter in method.Parameters)
                attributes.AddRange(parameter.GetAttributes().Select(item => "parameter:" + parameter.Name + ":" + item));
        }
        if (symbol is IPropertySymbol property)
            foreach (var parameter in property.Parameters)
                attributes.AddRange(parameter.GetAttributes().Select(item => "parameter:" + parameter.Name + ":" + item));
        return string.Join("; ", attributes.Order(StringComparer.Ordinal));
    }

    private static IEnumerable<ITypeSymbol> Types(ISymbol symbol) => symbol switch {
        IMethodSymbol method => method.Parameters.Select(item => item.Type).Append(method.ReturnType),
        IPropertySymbol property => property.Parameters.Select(item => item.Type).Append(property.Type),
        IFieldSymbol field => [field.Type], IEventSymbol eventSymbol => [eventSymbol.Type],
        INamedTypeSymbol type => type.Interfaces.Concat(type.BaseType == null ? [] : new[] { type.BaseType }), _ => []
    };

    private static void Collect(ITypeSymbol type, HashSet<string> result) {
        if (type is IArrayTypeSymbol array) { Collect(array.ElementType, result); return; }
        if (type is not INamedTypeSymbol named) return;
        if (named.ContainingNamespace.ToDisplayString().StartsWith("ChartForgeX", StringComparison.Ordinal)) result.Add(named.OriginalDefinition.ToDisplayString());
        foreach (var argument in named.TypeArguments) Collect(argument, result);
    }

    private static Dictionary<string, string> ReadArchive(string repository, string sha) {
        using var process = StartGit(repository, ["archive", "--format=tar", sha, .. Packages]);
        using var tar = new TarReader(process.StandardOutput.BaseStream);
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        TarEntry? entry;
        while ((entry = tar.GetNextEntry()) != null) {
            if (entry.DataStream == null || !(entry.Name.EndsWith(".cs", StringComparison.Ordinal) || entry.Name.EndsWith(".csproj", StringComparison.Ordinal))) continue;
            using var reader = new StreamReader(entry.DataStream, leaveOpen: true);
            files.Add(entry.Name, reader.ReadToEnd());
        }
        // Git pads the tar stream beyond the EOF entries. Drain it before waiting,
        // otherwise a full pipe can keep the archive process alive on Windows.
        process.StandardOutput.BaseStream.CopyTo(Stream.Null);
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException(process.StandardError.ReadToEnd());
        return files;
    }

    internal static string Git(string repository, params string[] arguments) {
        using var process = StartGit(repository, arguments);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException(error);
        return output;
    }

    private static Process StartGit(string repository, string[] arguments) {
        var start = new ProcessStartInfo("git") { WorkingDirectory = repository, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start) ?? throw new InvalidOperationException("Could not start Git.");
    }
}
