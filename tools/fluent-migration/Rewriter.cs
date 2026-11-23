using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>A declaration cut from its type, waiting to be inserted into the type its move targets.</summary>
sealed record PendingMember(string TargetTypeName, string? TargetTypeId, string Text);

/// <summary>Rewrites one document: call sites, moved member references, object initializers, crefs and declarations.</summary>
sealed class Rewriter : CSharpSyntaxRewriter {
    const string ChainKind = "fluent-migration-chain";

    readonly Plan _plan;
    readonly SemanticModel _model;
    readonly bool _isLibrary;
    readonly List<PendingMember> _pending;
    readonly Dictionary<string, (string Receiver, string DotTrivia, List<RenderedCall> Calls)> _chains;
    internal List<string> Report { get; } = new();
    internal int Changes { get; private set; }
    // Namespaces whose generated extension methods the rewritten calls need.
    internal HashSet<string> RequiredNamespaces { get; } = new(StringComparer.Ordinal);

    void Require(IMethodSymbol symbol) {
        var definition = symbol.ReducedFrom ?? symbol;
        var receiver = definition.IsExtensionMethod ? definition.Parameters[0].Type : definition.ContainingType;
        if (receiver is ITypeParameterSymbol parameter) receiver = parameter.ConstraintTypes.FirstOrDefault() ?? receiver;
        var ns = receiver.ContainingNamespace;
        if (ns != null && !ns.IsGlobalNamespace) RequiredNamespaces.Add(ns.ToDisplayString());
    }

    // The configure lambdas of a step path receive group types that may live in other namespaces.
    void RequirePath(IMethodSymbol symbol, string[] path) {
        var definition = symbol.ReducedFrom ?? symbol;
        ITypeSymbol? current = definition.IsExtensionMethod ? definition.Parameters[0].Type : definition.ContainingType;
        if (current is ITypeParameterSymbol parameter) current = parameter.ConstraintTypes.FirstOrDefault();
        for (int i = 0; i < path.Length - 1 && current != null; i++) {
            var property = Members(current).OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == path[i]);
            if (property == null && current is INamedTypeSymbol named && _plan.InlineByType.TryGetValue(Plan.Id(named.OriginalDefinition), out string? inline)) {
                var inlined = Members(current).OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == inline)?.Type;
                property = inlined == null ? null : Members(inlined).OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == path[i]);
            }
            current = property?.Type;
            if (current?.ContainingNamespace is { IsGlobalNamespace: false } ns) RequiredNamespaces.Add(ns.ToDisplayString());
        }
    }

    static IEnumerable<ISymbol> Members(ITypeSymbol type) {
        for (ITypeSymbol? current = type; current != null; current = current.BaseType) foreach (var member in current.GetMembers()) yield return member;
    }

    // Adds missing using directives for RequiredNamespaces, unless the file's namespace or a global using covers them.
    internal SyntaxNode AddUsings(SyntaxNode root, ISet<string> globalUsings) {
        if (root is not CompilationUnitSyntax unit || RequiredNamespaces.Count == 0) return root;
        var declared = unit.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().Select(n => n.Name.ToString()).ToList();
        var existing = new HashSet<string>(unit.Usings.Where(u => u.Alias == null && u.StaticKeyword.RawKind == 0).Select(u => u.Name!.ToString()), StringComparer.Ordinal);
        var missing = RequiredNamespaces.Where(ns => !existing.Contains(ns) && !globalUsings.Contains(ns)
            && !declared.Any(d => d == ns || d.StartsWith(ns + ".", StringComparison.Ordinal))).OrderBy(n => n, StringComparer.Ordinal).ToList();
        if (missing.Count == 0) return root;
        var directives = missing.Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(" " + ns)).WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed.IsKind(SyntaxKind.EndOfLineTrivia) ? SyntaxFactory.EndOfLine("\n") : SyntaxFactory.EndOfLine("\n")));
        return unit.WithUsings(unit.Usings.AddRange(directives));
    }

    internal sealed record RenderedCall(string[] Path, string Arguments, string? Raw);

    internal Rewriter(Plan plan, SemanticModel model, bool isLibrary, List<PendingMember> pending, Dictionary<string, (string, string, List<RenderedCall>)> chains, List<ForwarderInfo> forwarders) : base(visitIntoStructuredTrivia: true) {
        _plan = plan; _model = model; _isLibrary = isLibrary; _pending = pending; _chains = chains; _forwarders = forwarders;
    }

    string Where(SyntaxNode node) => Path.GetFileName(node.SyntaxTree.FilePath) + ":" + (node.GetLocation().GetLineSpan().StartLinePosition.Line + 1);

    // ---------------------------------------------------------------- invocations

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax original) {
        var symbol = _model.GetSymbolInfo(original).Symbol as IMethodSymbol
            ?? _model.GetSymbolInfo(original).CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
        var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(original)!;
        if (symbol == null) return visited;
        var definition = symbol.ReducedFrom ?? symbol;
        string id = Plan.Id(definition);
        if (_plan.Keeps.TryGetValue(id, out string? newName)) {
            Changes++;
            if (_plan.Forwarded.ContainsKey(id)) Require(symbol);
            // Calls on the implicit receiver inside the declaring type reach the internal implementation.
            if (_plan.Forwarded.TryGetValue(id, out string? internalName) && (visited.Expression is IdentifierNameSyntax or GenericNameSyntax
                || visited.Expression is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax })) newName = internalName;
            return RenameInvocation(visited, newName);
        }
        if (!_plan.Calls.TryGetValue(id, out var call)) return visited;
        // Receiver text, the dot's trivia and an existing chain to merge into.
        string receiver;
        string dotTrivia = string.Empty;
        List<RenderedCall> calls = new();
        if (visited.Expression is MemberAccessExpressionSyntax access) {
            if (symbol.ReducedFrom == null && symbol.IsExtensionMethod) { Report.Add(Where(original) + ": static extension call left as is"); return visited; }
            var target = access.Expression;
            var annotation = target.GetAnnotations(ChainKind).FirstOrDefault();
            string? firstGroup = call.Steps.FirstOrDefault(s => s.Raw == null && s.Path.Length > 1)?.Path[0];
            if (annotation != null && _chains.TryGetValue(annotation.Data!, out var chain) && chain.Calls.Count > 0 && chain.Calls[^1].Raw == null
                && chain.Calls[^1].Path.Length > 1 && call.Steps.Count > 0 && call.Steps[0].Raw == null && call.Steps[0].Path.Length > 1 && call.Steps[0].Path[0] == chain.Calls[^1].Path[0]) {
                receiver = chain.Receiver; dotTrivia = chain.DotTrivia; calls.AddRange(chain.Calls);
                // The dot in front of this call keeps its own line break inside the merged chain only if the chain had none.
            } else {
                receiver = target.ToFullString();
                dotTrivia = access.OperatorToken.LeadingTrivia.ToFullString();
            }
        } else if (visited.Expression is MemberBindingExpressionSyntax) {
            Report.Add(Where(original) + ": conditional call left as is"); return visited;
        } else {
            if (symbol.IsStatic && !symbol.IsExtensionMethod) return visited;
            receiver = "this";
        }
        var arguments = BindArguments(original, visited, symbol);
        if (arguments == null) { Report.Add(Where(original) + ": could not bind arguments of " + symbol.Name); return visited; }
        foreach (var step in call.Steps) {
            var rendered = RenderStep(step, arguments, original);
            if (rendered != null) { calls.Add(rendered); RequirePath(symbol, step.Path); }
        }
        if (calls.Count == 0) { Report.Add(Where(original) + ": " + symbol.Name + " produced no calls"); return visited; }
        string text = receiver + dotTrivia + RenderCalls(calls, original, true);
        ExpressionSyntax replacement;
        try {
            replacement = SyntaxFactory.ParseExpression(text);
        } catch (Exception ex) {
            Report.Add(Where(original) + ": parse failed " + ex.Message); return visited;
        }
        if (replacement.ContainsDiagnostics) { Report.Add(Where(original) + ": generated invalid code: " + Squash(text)); return visited; }
        string key = Guid.NewGuid().ToString("N");
        _chains[key] = (receiver, dotTrivia, calls);
        Require(symbol);
        Changes++;
        return replacement.WithTriviaFrom(visited).WithAdditionalAnnotations(new SyntaxAnnotation(ChainKind, key));
    }

    static string Squash(string text) => Regex.Replace(text, @"\s+", " ");

    InvocationExpressionSyntax RenameInvocation(InvocationExpressionSyntax visited, string newName) {
        return visited.Expression switch {
            MemberAccessExpressionSyntax access => visited.WithExpression(access.WithName(Rename(access.Name, newName))),
            IdentifierNameSyntax identifier => visited.WithExpression(identifier.WithIdentifier(SyntaxFactory.Identifier(identifier.Identifier.LeadingTrivia, newName, identifier.Identifier.TrailingTrivia))),
            GenericNameSyntax generic => visited.WithExpression(generic.WithIdentifier(SyntaxFactory.Identifier(generic.Identifier.LeadingTrivia, newName, generic.Identifier.TrailingTrivia))),
            MemberBindingExpressionSyntax binding => visited.WithExpression(binding.WithName(Rename(binding.Name, newName))),
            _ => visited
        };
    }

    static SimpleNameSyntax Rename(SimpleNameSyntax name, string newName) => name switch {
        GenericNameSyntax generic => generic.WithIdentifier(SyntaxFactory.Identifier(generic.Identifier.LeadingTrivia, newName, generic.Identifier.TrailingTrivia)),
        _ => SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(name.Identifier.LeadingTrivia, newName, name.Identifier.TrailingTrivia))
    };

    // parameter name -> argument text (null when not supplied). Expanded params arrays become one array expression.
    Dictionary<string, string?>? BindArguments(InvocationExpressionSyntax original, InvocationExpressionSyntax visited, IMethodSymbol symbol) {
        var definition = symbol.ReducedFrom ?? symbol;
        var parameters = symbol.ReducedFrom != null ? definition.Parameters.Skip(1).ToList() : definition.Parameters.ToList();
        var result = parameters.ToDictionary(p => p.Name, p => (string?)null);
        var visitedArguments = visited.ArgumentList.Arguments;
        var paramsValues = new List<string>();
        IParameterSymbol? paramsParameter = null;
        for (int i = 0; i < visitedArguments.Count; i++) {
            var argument = visitedArguments[i];
            IParameterSymbol? parameter;
            if (argument.NameColon != null) parameter = parameters.FirstOrDefault(p => p.Name == argument.NameColon.Name.Identifier.Text);
            else if (i < parameters.Count) parameter = parameters[i];
            else parameter = parameters.LastOrDefault(p => p.IsParams);
            if (parameter == null) return null;
            string text = argument.Expression.ToFullString().Trim();
            if ((text == "null" || text == "null!") && (parameter.Type.TypeKind == TypeKind.Delegate || parameter.Type.TypeKind == TypeKind.Class && parameter.Type.SpecialType != SpecialType.System_String)) {
                var display = SymbolDisplayFormat.MinimallyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
                text = parameter.Type.NullableAnnotation == NullableAnnotation.Annotated ? "(" + parameter.Type.ToDisplayString(display) + ")null" : "(" + parameter.Type.ToDisplayString(display) + ")null!";
            }
            if (argument.RefKindKeyword.RawKind != 0) return null;
            _argumentTypes[parameter.Name] = _model.GetTypeInfo(original.ArgumentList.Arguments[i].Expression).Type;
            if (parameter.IsParams) {
                paramsParameter = parameter;
                paramsValues.Add(text);
            } else result[parameter.Name] = text;
        }
        var emptyParams = parameters.FirstOrDefault(p => p.IsParams && result[p.Name] == null && paramsParameter == null);
        if (emptyParams != null) result[emptyParams.Name] = "new " + ((IArrayTypeSymbol)emptyParams.Type).ElementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + "[0]";
        if (paramsParameter != null) {
            // A single argument that already is the array passes through.
            var originalArgument = original.ArgumentList.Arguments[^1];
            var type = _model.GetTypeInfo(originalArgument.Expression).ConvertedType;
            if (paramsValues.Count == 1 && type is IArrayTypeSymbol) result[paramsParameter.Name] = paramsValues[0];
            else result[paramsParameter.Name] = "new " + ((IArrayTypeSymbol)paramsParameter.Type).ElementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + "[] { " + string.Join(", ", paramsValues) + " }";
        }
        // Defaults for omitted parameters, used by placeholders outside optional segments.
        foreach (var parameter in parameters.Where(p => result[p.Name] == null && p.HasExplicitDefaultValue)) {
            _defaults[parameter.Name] = DefaultText(parameter);
        }
        return result;
    }

    readonly Dictionary<string, string> _defaults = new();
    readonly Dictionary<string, ITypeSymbol?> _argumentTypes = new();

    static string DefaultText(IParameterSymbol parameter) {
        var syntax = parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ParameterSyntax;
        if (syntax?.Default != null) return syntax.Default.Value.ToString();
        object? value = parameter.ExplicitDefaultValue;
        return value switch {
            null => "null",
            bool b => b ? "true" : "false",
            string s => SymbolDisplay.FormatLiteral(s, true),
            double d => d.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            _ => parameter.Type.TypeKind == TypeKind.Enum ? "(" + parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + ")" + value : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!
        };
    }

    RenderedCall? RenderStep(Step step, Dictionary<string, string?> arguments, SyntaxNode position) {
        string? Expand(string template) {
            // Optional segments first: dropped when any placeholder inside is missing.
            string text = template;
            while (true) {
                var match = Regex.Match(text, @"\[([^\[\]]*)\]");
                if (!match.Success) break;
                string inner = match.Groups[1].Value;
                bool missing = Regex.Matches(inner, @"\{(\w+)(:value)?\}").Any(m => !arguments.TryGetValue(m.Groups[1].Value, out var v) || v == null || (m.Groups[2].Success && v == "null"));
                text = text[..match.Index] + (missing ? string.Empty : inner) + text[(match.Index + match.Length)..];
            }
            bool failed = false;
            text = Regex.Replace(text, @"\{(\w+)(:value)?\}", m => {
                string name = m.Groups[1].Value;
                if (arguments.TryGetValue(name, out var value) && value != null) {
                    // {p:value} unwraps a nullable value-type argument; a non-nullable one passes as is.
                    if (m.Groups[2].Success && _argumentTypes.TryGetValue(name, out var argumentType) && argumentType?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                        return (Regex.IsMatch(value, @"^[\w\.]+$") ? value : "(" + value + ")") + ".Value";
                    return value;
                }
                if (_defaults.TryGetValue(name, out var fallback)) return fallback;
                failed = true; return m.Value;
            });
            // Tidy separators left by dropped segments.
            text = Regex.Replace(text, @"\(\s*,\s*", "(").Replace(", )", ")");
            text = Regex.Replace(text, @",\s*,", ",").Trim().TrimEnd(',').TrimStart(',').Trim();
            return failed ? null : text;
        }
        if (step.Optional) {
            string probe = step.Raw ?? step.Arguments;
            bool anyMissing = Regex.Matches(probe, @"\{(\w+)(:value)?\}").Any(m => !arguments.TryGetValue(m.Groups[1].Value, out var v) || v == null || (m.Groups[2].Success && v == "null"));
            if (anyMissing) return null;
        }
        // Type names written into a template need their namespace at the call site.
        foreach (Match identifier in Regex.Matches(step.Raw ?? step.Arguments, @"\b[A-Z]\w*\b")) {
            if (_plan.TypeNamespaces.TryGetValue(identifier.Value, out string? ns)) RequiredNamespaces.Add(ns);
        }
        if (step.Raw != null) {
            string? raw = Expand(step.Raw);
            return raw == null ? null : new RenderedCall(Array.Empty<string>(), string.Empty, raw);
        }
        string? args = Expand(step.Arguments);
        if (args == null) { Report.Add(Where(position) + ": missing argument in " + string.Join(".", step.Path)); return null; }
        if (step.BoolTarget && args == "true") args = string.Empty;
        if (args == "null" && step.NullCast != null) args = step.NullCast;
        return new RenderedCall(step.Path, args, null);
    }

    // Groups consecutive calls under configure lambdas: [Legend.Position(a), Legend.MaxRows(2)] -> .Legend(l => l.Position(a).MaxRows(2)).
    string RenderCalls(List<RenderedCall> calls, SyntaxNode position, bool leadingDot) {
        var text = new StringBuilder();
        int i = 0;
        bool first = true;
        while (i < calls.Count) {
            var call = calls[i];
            string dot = first && !leadingDot ? string.Empty : ".";
            if (call.Raw != null) { text.Append(dot).Append(call.Raw); i++; first = false; continue; }
            if (call.Path.Length == 1) { text.Append(dot).Append(call.Path[0]).Append('(').Append(call.Arguments).Append(')'); i++; first = false; continue; }
            string group = call.Path[0];
            var inner = new List<RenderedCall>();
            while (i < calls.Count && calls[i].Raw == null && calls[i].Path.Length > 1 && calls[i].Path[0] == group) {
                inner.Add(calls[i] with { Path = calls[i].Path[1..] });
                i++;
            }
            string variable = Variable(group, inner, position);
            text.Append(dot).Append(group).Append('(').Append(variable).Append(" => ").Append(variable).Append(RenderCalls(inner, position, true)).Append(')');
            first = false;
        }
        return text.ToString();
    }

    string Variable(string group, List<RenderedCall> inner, SyntaxNode position) {
        string camel = char.ToLowerInvariant(group[0]) + group[1..];
        var candidates = new[] { camel[..1], camel, camel + "Options", camel + "2", camel + "3" };
        string used = string.Join(" ", inner.Select(c => c.Arguments + " " + c.Raw));
        var identifiers = new HashSet<string>(Regex.Matches(used, @"[A-Za-z_]\w*").Select(m => m.Value));
        int start = position.SpanStart;
        foreach (string candidate in candidates) {
            if (identifiers.Contains(candidate) || SyntaxFacts.GetKeywordKind(candidate) != SyntaxKind.None) continue;
            bool clash = _model.LookupSymbols(start, name: candidate).Any(s => s is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol);
            // A lambda or local declared later in the enclosing member with the same name also clashes.
            var member = position.Ancestors().FirstOrDefault(a => a is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or PropertyDeclarationSyntax or FieldDeclarationSyntax or GlobalStatementSyntax);
            if (!clash && member != null) {
                clash = member.DescendantTokens().Any(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == candidate
                    && (t.Parent is ParameterSyntax or VariableDeclaratorSyntax or SingleVariableDesignationSyntax or ForEachStatementSyntax or CatchDeclarationSyntax));
            }
            if (!clash) return candidate;
        }
        return camel + "Value";
    }

    // ---------------------------------------------------------------- moved members

    MoveInfo? MoveOf(SyntaxNode node) {
        var symbol = _model.GetSymbolInfo(node).Symbol ?? _model.GetSymbolInfo(node).CandidateSymbols.FirstOrDefault();
        if (symbol == null) return null;
        if (symbol is IMethodSymbol method && method.ReducedFrom != null) return null;
        return _plan.Moves.TryGetValue(Plan.Id(symbol), out var move) ? move : null;
    }

    // Inside a member that moves into the same target type, a reference to another moved member is just its new name.
    bool InsideTarget(SyntaxNode node, MoveInfo move) {
        var declaration = node.Ancestors().FirstOrDefault(a => a is MemberDeclarationSyntax && a.Parent is TypeDeclarationSyntax);
        if (declaration == null) return false;
        var declared = _model.GetDeclaredSymbol(declaration) ?? (declaration is FieldDeclarationSyntax field ? _model.GetDeclaredSymbol(field.Declaration.Variables[0]) : null);
        if (declared == null) return false;
        if (_plan.Moves.TryGetValue(Plan.Id(declared), out var own) && !own.Delete && own.TargetTypeId == move.TargetTypeId) return true;
        // Already in the target type itself.
        return Plan.Id(declared.ContainingType) == move.TargetTypeId;
    }

    bool MovingOutOf(SyntaxNode node, INamedTypeSymbol type) {
        var declaration = node.AncestorsAndSelf().FirstOrDefault(a => a is MemberDeclarationSyntax && a.Parent is TypeDeclarationSyntax);
        if (declaration == null) return false;
        var declared = _model.GetDeclaredSymbol(declaration) ?? (declaration is FieldDeclarationSyntax field ? _model.GetDeclaredSymbol(field.Declaration.Variables[0]) : null);
        return declared != null && _plan.Moves.TryGetValue(Plan.Id(declared), out var own) && !own.Delete && own.Path.Length > 1 && own.DeclaringTypeId == Plan.Id(type);
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax original) {
        var move = MoveOf(original.Name);
        var visited = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(original)!;
        if (move == null) return visited;
        // Type.Member (nameof, static-looking access): name the new declaring type.
        if (_model.GetSymbolInfo(original.Expression).Symbol is INamedTypeSymbol) {
            Changes++;
            return SyntaxFactory.ParseExpression(move.TargetTypeName + "." + move.NewName).WithTriviaFrom(visited);
        }
        Changes++;
        string path = string.Join(".", move.Path);
        return visited.WithName(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(visited.Name.GetLeadingTrivia(), path, visited.Name.GetTrailingTrivia())));
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax original) {
        var visited = base.VisitIdentifierName(original);
        // Names reached through a member access are handled by VisitMemberAccessExpression.
        if (original.Parent is MemberAccessExpressionSyntax parentAccess && parentAccess.Name == original) return visited;
        if ((original.Parent is NameColonSyntax nameColon && nameColon.Parent is not SubpatternSyntax) || original.Parent is QualifiedNameSyntax) return visited;
        if (original.Parent is AssignmentExpressionSyntax assign && assign.Left == original && assign.Parent is InitializerExpressionSyntax) return visited;
        if (original.Ancestors().Any(a => a is CrefSyntax)) return visited;
        var move = MoveOf(original);
        if (move == null) {
            // A static helper of the source type, used inside a member that moves out of it, needs its type name.
            var helper = _model.GetSymbolInfo(original).Symbol;
            if (helper is { IsStatic: true } and (IMethodSymbol or IFieldSymbol or IPropertySymbol) && helper.ContainingType != null && MovingOutOf(original, helper.ContainingType)) {
                Changes++;
                return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(original.GetLeadingTrivia(), helper.ContainingType.Name + "." + original.Identifier.Text, original.GetTrailingTrivia()));
            }
            return visited;
        }
        Changes++;
        string text = InsideTarget(original, move) ? move.NewName : string.Join(".", move.Path);
        return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(original.GetLeadingTrivia(), text, original.GetTrailingTrivia()));
    }

    // new ChartOptions { LegendPosition = x } -> new ChartOptions { Legend = { Position = x } }
    public override SyntaxNode? VisitInitializerExpression(InitializerExpressionSyntax original) {
        var visited = (InitializerExpressionSyntax)base.VisitInitializerExpression(original)!;
        if (!original.IsKind(SyntaxKind.ObjectInitializerExpression)) return visited;
        var moves = original.Expressions.Select(e => e is AssignmentExpressionSyntax { Left: IdentifierNameSyntax name } ? MoveOf(name) : null).ToList();
        if (moves.All(m => m == null)) return visited;
        Changes++;
        // Build a tree of path -> value text, then print nested initializers.
        var root = new InitNode();
        for (int i = 0; i < original.Expressions.Count; i++) {
            var expression = visited.Expressions[i];
            if (moves[i] is { } move && expression is AssignmentExpressionSyntax assignment) {
                root.Add(move.Path, assignment.Right.ToFullString().Trim());
            } else root.Add(new[] { "\u0000" + i }, expression.ToFullString().Trim());
        }
        string text = "{ " + root.Print() + " }";
        var parsed = (InitializerExpressionSyntax)((ObjectCreationExpressionSyntax)SyntaxFactory.ParseExpression("new X " + text)).Initializer!;
        return parsed.WithTriviaFrom(visited);
    }

    sealed class InitNode {
        readonly List<(string Key, InitNode? Child, string? Value)> _items = new();
        internal void Add(string[] path, string value) {
            if (path.Length == 1) { _items.Add((path[0], null, value)); return; }
            var existing = _items.FirstOrDefault(i => i.Key == path[0] && i.Child != null);
            if (existing.Child == null) { existing = (path[0], new InitNode(), null); _items.Add(existing); }
            existing.Child!.Add(path[1..], value);
        }
        internal string Print() => string.Join(", ", _items.Select(i => i.Key.StartsWith('\u0000') ? i.Value! : i.Child != null ? i.Key + " = { " + i.Child.Print() + " }" : i.Key + " = " + i.Value));
    }

    // ---------------------------------------------------------------- crefs

    // An unqualified cref inside a member that moves out of its type keeps pointing at the old type.
    public override SyntaxNode? VisitXmlCrefAttribute(XmlCrefAttributeSyntax original) {
        var visited = (XmlCrefAttributeSyntax)base.VisitXmlCrefAttribute(original)!;
        if (original.Cref is not NameMemberCrefSyntax nameCref) return visited;
        var symbol = _model.GetSymbolInfo(original.Cref).Symbol ?? _model.GetSymbolInfo(original.Cref).CandidateSymbols.FirstOrDefault();
        if (symbol?.ContainingType == null || symbol is INamedTypeSymbol) return visited;
        if (_plan.Moves.ContainsKey(Plan.Id(symbol))) return visited;
        var trivia = original.Ancestors().OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();
        var host = trivia?.ParentTrivia.Token.Parent;
        if (host == null || !MovingOutOf(host, symbol.ContainingType)) return visited;
        Changes++;
        var parsed = SyntaxFactory.ParseLeadingTrivia("/// <see cref=\"" + symbol.ContainingType.Name + "." + nameCref.ToString() + "\"/>\n")
            .Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().SelectMany(d => d.DescendantNodes().OfType<XmlCrefAttributeSyntax>()).FirstOrDefault();
        return parsed == null ? visited : visited.WithCref(parsed.Cref);
    }

    public override SyntaxNode? VisitXmlEmptyElement(XmlEmptyElementSyntax original) {
        var visited = (XmlEmptyElementSyntax)base.VisitXmlEmptyElement(original)!;
        var cref = original.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault();
        if (cref == null) return visited;
        var symbol = _model.GetSymbolInfo(cref.Cref).Symbol ?? _model.GetSymbolInfo(cref.Cref).CandidateSymbols.FirstOrDefault();
        if (symbol == null) return visited;
        string id = Plan.Id(symbol);
        string? replacement = null;
        if (_plan.Moves.TryGetValue(id, out var move)) replacement = "<see cref=\"" + move.TargetTypeName + "." + move.NewName + "\"/>";
        else if (_plan.Calls.TryGetValue(id, out var call)) replacement = "<c>" + string.Join(", ", call.Steps.Select(s => s.Raw != null ? Regex.Replace(s.Raw, @"\(.*", "") : string.Join(".", s.Path))) + "</c>";
        else if (_plan.Keeps.TryGetValue(id, out var newName)) replacement = "<see cref=\"" + Regex.Replace(cref.Cref.ToString(), @"\b" + symbol.Name + @"\b", newName) + "\"/>";
        if (replacement == null) return visited;
        Changes++;
        var parsed = SyntaxFactory.ParseLeadingTrivia("/// " + replacement + "\n").Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();
        var node = parsed?.Content.OfType<XmlNodeSyntax>().FirstOrDefault(n => n is XmlEmptyElementSyntax or XmlElementSyntax);
        return node == null ? visited : node.WithTriviaFrom(visited);
    }

    // ---------------------------------------------------------------- declarations

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax original) {
        var symbol = _model.GetDeclaredSymbol(original);
        var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(original)!;
        if (symbol == null) return visited;
        string id = Plan.Id(symbol);
        if (_plan.Calls.ContainsKey(id) && !_plan.Stays.Contains(id)) { Changes++; return null; }
        if (_plan.Keeps.TryGetValue(id, out string? newName)) {
            Changes++;
            if (_plan.Forwarded.TryGetValue(id, out string? internalName)) {
                _forwarders.Add(Forwarder(original, symbol, newName, internalName));
                var modifiers = SyntaxFactory.TokenList(visited.Modifiers.Select(m => m.IsKind(SyntaxKind.PublicKeyword) ? SyntaxFactory.Token(m.LeadingTrivia, SyntaxKind.InternalKeyword, m.TrailingTrivia) : m));
                return visited.WithModifiers(modifiers).WithIdentifier(SyntaxFactory.Identifier(visited.Identifier.LeadingTrivia, internalName, visited.Identifier.TrailingTrivia));
            }
            return visited.WithIdentifier(SyntaxFactory.Identifier(visited.Identifier.LeadingTrivia, newName, visited.Identifier.TrailingTrivia));
        }
        if (_plan.Moves.TryGetValue(id, out var move)) return MoveDeclaration(visited.WithIdentifier(SyntaxFactory.Identifier(visited.Identifier.LeadingTrivia, move.NewName, visited.Identifier.TrailingTrivia)), move);
        return visited;
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax original) {
        var symbol = _model.GetDeclaredSymbol(original);
        var visited = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(original)!;
        if (symbol != null && _plan.Verbs.TryGetValue(Plan.Id(symbol), out string? verb)) {
            Changes++;
            var attribute = SyntaxFactory.ParseCompilationUnit("[PowerForge.Fluent.Fluent(\"" + verb + "\")] class X {}").Members[0].AttributeLists[0];
            var leading = visited.GetLeadingTrivia();
            string indent = leading.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToFullString();
            visited = visited.WithoutLeadingTrivia().AddAttributeLists(attribute.WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia("\n" + indent))).WithLeadingTrivia(leading);
            if (visited.AttributeLists.Count > 1) {
                // Keep the new attribute after existing ones.
            }
        }
        if (symbol == null || !_plan.Moves.TryGetValue(Plan.Id(symbol), out var move)) return visited;
        return MoveDeclaration(visited.WithIdentifier(SyntaxFactory.Identifier(visited.Identifier.LeadingTrivia, move.NewName, visited.Identifier.TrailingTrivia)), move);
    }

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax original) {
        var visited = (FieldDeclarationSyntax)base.VisitFieldDeclaration(original)!;
        if (original.Declaration.Variables.Count != 1) return visited;
        var symbol = _model.GetDeclaredSymbol(original.Declaration.Variables[0]);
        if (symbol == null || !_plan.Moves.TryGetValue(Plan.Id(symbol), out var move)) return visited;
        return MoveDeclaration(visited, move);
    }

    readonly List<ForwarderInfo> _forwarders;
    internal sealed record ForwarderInfo(string TypeName, string Namespace, string Directory, string Usings, string Text);

    // public static Ret Name(this Owner owner, params) => owner.SetName(args);
    ForwarderInfo Forwarder(MethodDeclarationSyntax original, IMethodSymbol symbol, string name, string internalName) {
        var type = symbol.ContainingType;
        string receiver = char.ToLowerInvariant(type.Name[0]) + type.Name[1..];
        if (SyntaxFacts.GetKeywordKind(receiver) != SyntaxKind.None || original.ParameterList.Parameters.Any(p => p.Identifier.Text == receiver)) receiver += "Instance";
        string receiverType = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeParameters = type.TypeParameters.Select(t => t.Name).Concat(original.TypeParameterList?.Parameters.Select(p => p.Identifier.Text) ?? Enumerable.Empty<string>()).ToList();
        string generic = typeParameters.Count == 0 ? string.Empty : "<" + string.Join(", ", typeParameters) + ">";
        var typeDeclaration = original.Ancestors().OfType<TypeDeclarationSyntax>().First();
        string constraints = string.Join(" ", typeDeclaration.ConstraintClauses.Select(c => c.ToString()).Concat(original.ConstraintClauses.Select(c => c.ToString())));
        // Fully qualified types: inside the Fluent class a type name can collide with a generated method (ThemeMode).
        var qualified = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
        string parameters = string.Join(", ", original.ParameterList.Parameters.Select((syntax, index) => {
            var parameter = symbol.Parameters[index];
            string prefix = string.Concat(syntax.AttributeLists.Select(a => a.ToString() + " ")) + (parameter.IsParams ? "params " : string.Empty)
                + (parameter.RefKind == RefKind.Ref ? "ref " : parameter.RefKind == RefKind.Out ? "out " : parameter.RefKind == RefKind.In ? "in " : string.Empty);
            // Types from another project's source generator can be unresolved here; the shared vocabulary lives in ChartForgeX.
            bool unresolved = parameter.Type is IErrorTypeSymbol || parameter.Type is INamedTypeSymbol { IsGenericType: true } g && g.TypeArguments.Any(a => a is IErrorTypeSymbol);
            string typeText = unresolved ? Regex.Replace(syntax.Type!.ToString(), @"\b(ThemeMode|Tone|Icon)\b", "global::ChartForgeX.$1") : parameter.Type.ToDisplayString(qualified);
            string text = prefix + typeText + " " + syntax.Identifier.Text;
            if (syntax.Default != null) text += " = " + (unresolved ? Regex.Replace(syntax.Default.Value.ToString(), @"^(ThemeMode|Tone|Icon)\.", "global::ChartForgeX.$1.") : QualifiedDefault(parameter, syntax.Default.Value, qualified));
            return text;
        }));
        string arguments = string.Join(", ", original.ParameterList.Parameters.Select(p => (p.Modifiers.Any(SyntaxKind.RefKeyword) ? "ref " : p.Modifiers.Any(SyntaxKind.OutKeyword) ? "out " : string.Empty) + p.Identifier.Text));
        string methodTypeArgs = original.TypeParameterList == null ? string.Empty : "<" + string.Join(", ", original.TypeParameterList.Parameters.Select(p => p.Identifier.Text)) + ">";
        // Documentation: the original comment plus the receiver parameter.
        var doc = original.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).Select(t => t.ToFullString()).FirstOrDefault() ?? string.Empty;
        var lines = doc.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).Select(l => l.StartsWith("///") ? l : "/// " + l).ToList();
        string receiverDoc = "/// <param name=\"" + receiver + "\">The instance to configure.</param>";
        // Unqualified crefs were relative to the declaring type.
        lines = lines.Select(l => Regex.Replace(l, "cref=\"([A-Za-z_]\\w*)(\\(|\")", m => type.GetMembers(m.Groups[1].Value).Any() ? "cref=\"" + type.Name + "." + m.Groups[1].Value + m.Groups[2].Value : m.Value)).ToList();
        bool documentsParameters = lines.Any(l => l.Contains("<param "));
        int insertAt = lines.FindIndex(l => l.Contains("<param") || l.Contains("<returns") || l.Contains("<example"));
        if (lines.Count == 0) lines.Add("/// <summary>Configures " + receiverType + ".</summary>");
        if (documentsParameters) { if (insertAt < 0) lines.Add(receiverDoc); else lines.Insert(insertAt, receiverDoc); }
        string returnType = symbol.ReturnType.ToDisplayString(qualified);
        string text = string.Concat(lines.Select(l => "    " + l + "\n"))
            + "    public static " + returnType + " " + name + generic + "(this " + receiverType + " " + receiver + (parameters.Length > 0 ? ", " + parameters : string.Empty) + ")"
            + (constraints.Length > 0 ? " " + constraints : string.Empty) + "\n"
            + "        => (" + receiver + " ?? throw new System.ArgumentNullException(nameof(" + receiver + "))).internalName" .Replace("internalName", internalName) + methodTypeArgs + "(" + arguments + ");\n";
        var unit = original.SyntaxTree.GetCompilationUnitRoot();
        string usings = string.Concat(unit.Usings.Select(u => u.ToString() + "\n"));
        return new ForwarderInfo(type.Name, type.ContainingNamespace.ToDisplayString(), Path.GetDirectoryName(original.SyntaxTree.FilePath)!, usings, text);
    }

    string QualifiedDefault(IParameterSymbol parameter, ExpressionSyntax value, SymbolDisplayFormat qualified) {
        // An enum member or other constant member reference gets its fully qualified name.
        var symbol = _model.GetSymbolInfo(value).Symbol;
        if (symbol is IFieldSymbol { IsConst: true } field) return field.ContainingType.ToDisplayString(qualified) + "." + field.Name;
        return value.ToString();
    }

    SyntaxNode? MoveDeclaration(MemberDeclarationSyntax visited, MoveInfo move) {
        Changes++;
        if (move.Delete) return null;
        if (move.Path.Length == 1) return visited; // a rename in place
        _pending.Add(new PendingMember(move.TargetTypeName, move.TargetTypeId, visited.ToFullString()));
        return null;
    }
}
