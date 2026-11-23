using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

/// <summary>One fluent step in a call template: a path of group configure methods ending in a method call, or raw text.</summary>
sealed record Step(string[] Path, string Arguments, bool Optional, string? Raw, bool BoolTarget = false, string? NullCast = null);

/// <summary>A resolved member move.</summary>
sealed record MoveInfo(string[] Path, bool Delete, string DeclaringTypeId, string? TargetTypeId, string TargetTypeName, string NewName, string Source);

/// <summary>A resolved call rewrite for one method overload.</summary>
sealed record CallInfo(List<Step> Steps, bool Auto, string Source);

/// <summary>Resolves the map against the library compilations and derives the plain-setter rewrites.</summary>
sealed class Plan {
    internal Dictionary<string, MoveInfo> Moves { get; } = new(StringComparer.Ordinal);       // member doc id -> move
    internal Dictionary<string, CallInfo> Calls { get; } = new(StringComparer.Ordinal);       // method doc id -> call rewrite
    internal Dictionary<string, string> Keeps { get; } = new(StringComparer.Ordinal);         // method doc id -> new name
    internal HashSet<string> Stays { get; } = new(StringComparer.Ordinal);                     // method doc id kept as is
    internal Dictionary<string, string> Forwarded { get; } = new(StringComparer.Ordinal);      // kept method doc id -> internal name (new name collides with a member)
    internal List<string> KeepReasons { get; } = new();
    internal HashSet<string> InlineProperties { get; } = new(StringComparer.Ordinal);         // property doc id
    internal Dictionary<string, string> InlineByType { get; } = new(StringComparer.Ordinal);  // type doc id -> inline property name
    internal Dictionary<string, string> Verbs { get; } = new(StringComparer.Ordinal);         // property doc id -> verb
    internal List<string> Unmapped { get; } = new();
    internal Dictionary<string, string> TypeNamespaces { get; } = new(StringComparer.Ordinal);   // unique public type name -> namespace
    internal SortedSet<string> NonPublicSetters { get; } = new(StringComparer.Ordinal);
    internal List<string> Errors { get; } = new();

    readonly List<Compilation> _libraries;

    Plan(List<Compilation> libraries) { _libraries = libraries; }

    internal static Plan Build(MigrationMap map, List<Compilation> libraries) {
        var plan = new Plan(libraries);
        foreach (var group in plan.LibraryTypes().Where(t => t.DeclaredAccessibility == Accessibility.Public && t.ContainingType == null).GroupBy(t => t.Name)) {
            var namespaces = group.Select(t => t.ContainingNamespace.ToDisplayString()).Distinct().ToList();
            if (namespaces.Count == 1 && namespaces[0].Length > 0) plan.TypeNamespaces[group.Key] = namespaces[0];
        }
        plan.Resolve(map);
        plan.MoveBackingFields();
        plan.Derive();
        return plan;
    }

    internal static string Id(ISymbol symbol) => (symbol.OriginalDefinition ?? symbol).GetDocumentationCommentId() ?? symbol.ToDisplayString();

    IEnumerable<INamedTypeSymbol> LibraryTypes() => _libraries.SelectMany(c => AllTypes(c.Assembly.GlobalNamespace));

    static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol ns) {
        foreach (var member in ns.GetMembers()) {
            if (member is INamespaceSymbol child) { foreach (var t in AllTypes(child)) yield return t; }
            else if (member is INamedTypeSymbol type) {
                yield return type;
                foreach (var nested in type.GetTypeMembers()) yield return nested;
            }
        }
    }

    INamedTypeSymbol? FindType(string name, string source) {
        var matches = LibraryTypes().Where(t => t.Name == name || t.MetadataName == name).Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToList();
        if (matches.Count == 1) return matches[0];
        Errors.Add(source + ": type '" + name + "' matched " + matches.Count + " types.");
        return null;
    }

    static string Simple(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat).Replace(" ", string.Empty);

    List<IMethodSymbol> FindMethods(INamedTypeSymbol type, string name, string[]? parameters, string source) {
        // Instance methods of the type, plus extension methods in the library whose receiver is the type.
        var candidates = type.GetMembers(name).OfType<IMethodSymbol>().ToList();
        candidates.AddRange(LibraryTypes().Where(t => t.IsStatic && t.MightContainExtensionMethods)
            .SelectMany(t => t.GetMembers(name).OfType<IMethodSymbol>())
            .Where(m => m.IsExtensionMethod && m.Parameters.Length > 0 && SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type.OriginalDefinition, type.OriginalDefinition)));
        if (parameters != null) {
            candidates = candidates.Where(m => {
                var ps = m.Parameters.Skip(m.IsExtensionMethod ? 1 : 0).Select(p => Simple(p.Type)).ToArray();
                return ps.Length == parameters.Length && ps.Zip(parameters).All(pair => pair.First == pair.Second);
            }).ToList();
        }
        if (candidates.Count == 0) Errors.Add(source + ": no method " + type.Name + "." + name + (parameters == null ? "" : "(" + string.Join(",", parameters) + ")"));
        return candidates;
    }

    void Resolve(MigrationMap map) {
        foreach (var (typeName, property) in map.Inlines) {
            var type = FindType(typeName, "inline");
            var symbol = type?.GetMembers(property).OfType<IPropertySymbol>().FirstOrDefault();
            if (type == null || symbol == null) { Errors.Add("inline " + typeName + "." + property + " not found"); continue; }
            InlineProperties.Add(Id(symbol));
            InlineByType[Id(type)] = property;
        }
        foreach (var (key, verb) in map.Verbs) {
            int dot = key.LastIndexOf('.');
            var type = FindType(key[..dot], "verb " + key);
            var symbol = type?.GetMembers(key[(dot + 1)..]).FirstOrDefault();
            if (symbol == null) { Errors.Add("verb " + key + " not found"); continue; }
            Verbs[Id(symbol)] = verb;
        }
        foreach (var move in map.Moves) {
            var type = FindType(move.Type, move.Source);
            if (type == null) continue;
            var members = type.GetMembers(move.Member).Where(m => m is IPropertySymbol or IMethodSymbol or IFieldSymbol).ToList();
            if (members.Count == 0) { Errors.Add(move.Source + ": member " + move.Type + "." + move.Member + " not found"); continue; }
            string[] path = move.Path.Split('.');
            // Walk the path's parents to find the type the member lands in.
            ITypeSymbol current = type;
            bool ok = true;
            for (int i = 0; i < path.Length - 1; i++) {
                var next = current.GetMembers(path[i]).OfType<IPropertySymbol>().FirstOrDefault()
                    ?? (current as INamedTypeSymbol)?.BaseType?.GetMembers(path[i]).OfType<IPropertySymbol>().FirstOrDefault();
                if (next == null) { Errors.Add(move.Source + ": path segment '" + path[i] + "' not found on " + current.Name); ok = false; break; }
                current = next.Type;
            }
            if (!ok) continue;
            foreach (var member in members) {
                Moves[Id(member)] = new MoveInfo(path, move.Delete, Id(type), path.Length > 1 ? Id(current) : Id(type), current.Name, path[^1], move.Source);
            }
        }
        foreach (var call in map.Calls) {
            var type = FindType(call.Type, call.Source);
            if (type == null) continue;
            foreach (var method in FindMethods(type, call.Method, call.Parameters, call.Source)) {
                Calls[Id(method)] = new CallInfo(ParseTemplate(call.Template, call.Source), false, call.Source);
            }
        }
        foreach (var keep in map.Keeps) {
            var type = FindType(keep.Type, keep.Source);
            if (type == null) continue;
            foreach (var method in FindMethods(type, keep.Method, keep.Parameters, keep.Source)) {
                Keeps[Id(method)] = keep.NewName;
                // An instance method can't share a name with a property, field or event of its type: it becomes an
                // internal Set{Name} method plus a public extension forwarder in the type's Fluent class.
                var declaring = method.ContainingType;
                bool collides = !method.IsExtensionMethod && !method.IsStatic && BaseTypesOf(declaring).SelectMany(t => t.GetMembers(keep.NewName)).Any(m => m is IPropertySymbol or IFieldSymbol or IEventSymbol);
                bool movedHere = Moves.Values.Any(mv => mv.TargetTypeId == Id(declaring) && mv.NewName == keep.NewName && !mv.Delete);
                if (collides || movedHere) Forwarded[Id(method)] = "Set" + keep.NewName;
            }
            KeepReasons.Add(keep.Type + "." + keep.Method + (keep.Parameters == null ? "" : "(" + string.Join(", ", keep.Parameters) + ")") + "\t" + keep.NewName + "\t" + keep.Reason);
            if (keep.Reason.Length == 0) Errors.Add(keep.Source + ": keep needs a reason (# ...)");
        }
        foreach (var stay in map.Stays) {
            var type = FindType(stay.Type, stay.Source);
            if (type == null) continue;
            foreach (var method in FindMethods(type, stay.Method, stay.Parameters, stay.Source)) Stays.Add(Id(method));
            KeepReasons.Add(stay.Type + "." + stay.Method + (stay.Parameters == null ? "" : "(" + string.Join(", ", stay.Parameters) + ")") + "\t(unchanged)\t" + stay.Reason);
            if (stay.Reason.Length == 0) Errors.Add(stay.Source + ": stay needs a reason (# ...)");
        }
    }

    internal static List<Step> ParseTemplate(string template, string source) {
        var steps = new List<Step>();
        foreach (string rawPiece in SplitTop(template, ';')) {
            string piece = rawPiece.Trim();
            bool optional = piece.StartsWith('[') && piece.EndsWith(']') && Balanced(piece[1..^1]);
            if (optional) piece = piece[1..^1].Trim();
            if (piece.StartsWith('!')) { steps.Add(new Step(Array.Empty<string>(), string.Empty, optional, piece[1..])); continue; }
            int paren = piece.IndexOf('(');
            if (paren < 0 || !piece.EndsWith(')')) throw new InvalidOperationException(source + ": bad template piece '" + piece + "'.");
            steps.Add(new Step(piece[..paren].Split('.'), piece[(paren + 1)..^1], optional, null));
        }
        return steps;
    }

    static bool Balanced(string text) {
        int depth = 0;
        foreach (char c in text) { if (c == '[') depth++; else if (c == ']' && --depth < 0) return false; }
        return depth == 0;
    }

    internal static List<string> SplitTop(string text, char separator) {
        var parts = new List<string>();
        int depth = 0, start = 0;
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c is '(' or '[' or '{' or '<') depth++;
            else if (c is ')' or ']' or '}' or '>') depth--;
            else if (c == separator && depth == 0) { parts.Add(text[start..i]); start = i + 1; }
        }
        parts.Add(text[start..]);
        return parts;
    }

    // Plain setters: { a.b = p; ...; return this; } become property-path steps.
    void Derive() {
        foreach (var compilation in _libraries) {
            foreach (var tree in compilation.SyntaxTrees) {
                if (tree.FilePath.Contains("/obj/")) continue;
                var model = compilation.GetSemanticModel(tree);
                foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()) {
                    if (!declaration.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;
                    if (model.GetDeclaredSymbol(declaration) is not IMethodSymbol method) continue;
                    string id = Id(method);
                    if (Calls.ContainsKey(id) || Keeps.ContainsKey(id) || Stays.Contains(id)) continue;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(method.Name, "^(With|Without|Use|Configure|As|Enable|Disable)([A-Z]|$)")) continue;
                    if (!ReturnsSelf(method)) continue;
                    var steps = method.Name.StartsWith("Configure", StringComparison.Ordinal) ? TryDeriveConfigure(declaration, method, model, out string? reason) : TryDerive(declaration, method, model, out reason);
                    if (steps != null) Calls[id] = new CallInfo(steps, true, "auto");
                    else Unmapped.Add(method.ContainingType.Name + "." + method.Name + "(" + string.Join(", ", method.Parameters.Select(p => Simple(p.Type))) + ")\t" + reason + "\t" + Path.GetFileName(tree.FilePath));
                }
            }
        }
    }

    // Private fields used only by members moving to one target type move with them.
    void MoveBackingFields() {
        var byType = Moves.Values.Where(m => !m.Delete && m.Path.Length > 1).GroupBy(m => m.DeclaringTypeId);
        foreach (var group in byType) {
            var type = LibraryTypes().FirstOrDefault(t => Id(t) == group.Key);
            if (type == null) continue;
            var compilation = _libraries.First(c => SymbolEqualityComparer.Default.Equals(c.Assembly, type.ContainingAssembly));
            var users = new Dictionary<IFieldSymbol, HashSet<string>>(SymbolEqualityComparer.Default);
            foreach (var reference in type.DeclaringSyntaxReferences) {
                var model = compilation.GetSemanticModel(reference.SyntaxTree);
                var declaration = (TypeDeclarationSyntax)reference.GetSyntax();
                foreach (var member in declaration.Members) {
                    ISymbol? declared = member is FieldDeclarationSyntax f ? model.GetDeclaredSymbol(f.Declaration.Variables[0]) : model.GetDeclaredSymbol(member);
                    if (declared == null) continue;
                    foreach (var name in member.DescendantNodes().OfType<IdentifierNameSyntax>()) {
                        if (model.GetSymbolInfo(name).Symbol is IFieldSymbol field && field.DeclaredAccessibility == Accessibility.Private && !field.IsStatic && SymbolEqualityComparer.Default.Equals(field.ContainingType, type)
                            && !SymbolEqualityComparer.Default.Equals(field, declared)) {
                            if (!users.TryGetValue(field, out var set)) users[field] = set = new HashSet<string>();
                            set.Add(Moves.TryGetValue(Id(declared), out var mv) && !mv.Delete && mv.Path.Length > 1 ? "->" + mv.TargetTypeId + "|" + string.Join(".", mv.Path[..^1]) : "stay:" + Id(declared));
                        }
                    }
                }
            }
            foreach (var (field, set) in users) {
                if (set.Count != 1 || !set.First().StartsWith("->", StringComparison.Ordinal) || Moves.ContainsKey(Id(field))) continue;
                string[] parts = set.First()[2..].Split('|');
                var sample = group.First(m => m.TargetTypeId == parts[0]);
                Moves[Id(field)] = new MoveInfo(parts[1].Split('.').Append(field.Name).ToArray(), false, group.Key, parts[0], sample.TargetTypeName, field.Name, "backing field");
            }
        }
    }

    static IEnumerable<INamedTypeSymbol> BaseTypesOf(INamedTypeSymbol type) {
        for (INamedTypeSymbol? current = type; current != null; current = current.BaseType) yield return current;
    }

    internal static bool ReturnsSelf(IMethodSymbol m) {
        if (m.IsExtensionMethod) return m.Parameters.Length > 0 && SymbolEqualityComparer.Default.Equals(m.ReturnType, m.Parameters[0].Type);
        if (m.IsStatic) return false;
        return SymbolEqualityComparer.Default.Equals(m.ReturnType.OriginalDefinition, m.ContainingType.OriginalDefinition) || m.ReturnType is ITypeParameterSymbol;
    }

    List<Step>? TryDerive(MethodDeclarationSyntax declaration, IMethodSymbol method, SemanticModel model, out string? reason) {
        reason = null;
        if (declaration.Body == null) { reason = "expression body"; return null; }
        var statements = declaration.Body.Statements;
        if (statements.Count < 2 || statements[^1] is not ReturnStatementSyntax) { reason = "shape"; return null; }
        var receiverParameter = method.IsExtensionMethod ? method.Parameters[0] : null;
        var steps = new List<Step>();
        bool guarded = false;
        foreach (var statement in statements.Take(statements.Count - 1)) {
            if (statement is IfStatementSyntax guard && receiverParameter != null && guard.ToString().Contains("== null") && guard.ToString().Contains(receiverParameter.Name)) continue;
            if (statement is IfStatementSyntax { Else: null } check && (check.Statement is ThrowStatementSyntax || check.Statement is BlockSyntax { Statements: [ThrowStatementSyntax] })) { guarded = true; continue; }
            if (statement is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax validation } && System.Text.RegularExpressions.Regex.IsMatch(validation.Expression.ToString(), @"(^|\.)(Validate|Require|Ensure|Finite|UnitInterval|Positive|NonNegative)\w*$|^ChartGuards\.")) { guarded = true; continue; }
            if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment } || !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)) { reason = "statement: " + Clip(statement); return null; }
            var chain = Chain(assignment.Left, model, receiverParameter);
            if (chain == null) { reason = (model.GetSymbolInfo(assignment.Left).Symbol is IFieldSymbol ? "field-target: " : "target: ") + Clip(assignment.Left); return null; }
            string argument;
            ExpressionSyntax right = assignment.Right;
            if (right is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression, Right: ThrowExpressionSyntax } coalesce) right = coalesce.Left;
            var referenced = right.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Select(n => model.GetSymbolInfo(n).Symbol).OfType<IParameterSymbol>().ToList();
            if (right is IdentifierNameSyntax && referenced.Count == 1) argument = "{" + referenced[0].Name + "}";
            else if (referenced.Count == 0 && !right.DescendantNodesAndSelf().Any(n => n is ThisExpressionSyntax) && IsStatic(right, model)) {
                argument = Qualify(right, model);
                // null for a settings-class property would match both X(value) and X(configure).
                var target = model.GetSymbolInfo(assignment.Left).Symbol as IPropertySymbol;
                if (argument == "null" && target != null && target.Type.TypeKind == TypeKind.Class && target.Type.SpecialType != SpecialType.System_String)
                    argument = "(" + target.Type.WithNullableAnnotation(NullableAnnotation.Annotated).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)) + ")null";
            }
            else { reason = "value: " + Clip(right); return null; }
            var last = chain[^1];
            if (last.SetMethod == null || last.SetMethod.DeclaredAccessibility != Accessibility.Public) NonPublicSetters.Add(Id(last) + "\t" + (last.Locations.FirstOrDefault()?.GetLineSpan().Path ?? "") + ":" + ((last.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line ?? -1) + 1));
            if (last.Type.TypeKind == TypeKind.Delegate && !Verbs.ContainsKey(Id(last))) { reason = "delegate needs verb: " + last.ContainingType.Name + "." + last.Name; return null; }
            var path = ApplyMoves(chain, method.IsExtensionMethod ? (INamedTypeSymbol)receiverParameter!.Type : method.ContainingType);
            bool isBool = last.Type.SpecialType == SpecialType.System_Boolean;
            // A bool parameter with default true elides to X() when omitted; keep the argument optional in that case.
            var parameter = referenced.FirstOrDefault();
            bool optional = parameter != null && parameter.HasExplicitDefaultValue;
            string? nullCast = last.Type.TypeKind == TypeKind.Class && last.Type.SpecialType != SpecialType.System_String
                ? "(" + last.Type.WithNullableAnnotation(NullableAnnotation.Annotated).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)) + ")null" : null;
            steps.Add(new Step(path, optional && isBool && Equals(parameter!.ExplicitDefaultValue, true) ? "[" + argument + "]" : argument, false, null, isBool, nullCast));
        }
        if (steps.Count == 0) { reason = "empty"; return null; }
        if (guarded) { reason = "guarded-plain: " + string.Join(" ; ", steps.Select(st => string.Join(".", st.Path) + "(" + st.Arguments + ")")); return null; }
        return steps;
    }

    // Configure{X}(Action<T> configure) { [guard;] X = ChartOptionConfiguration.Configure(X, configure, o => o.Clone()); return this; }
    // becomes the generated copy-then-commit X(configure).
    List<Step>? TryDeriveConfigure(MethodDeclarationSyntax declaration, IMethodSymbol method, SemanticModel model, out string? reason) {
        reason = null;
        var statements = declaration.Body?.Statements.Where(s => !(s is IfStatementSyntax i && i.ToString().Contains("== null"))).ToList();
        if (statements == null || statements.Count != 2 || statements[1] is not ReturnStatementSyntax) { reason = "configure shape"; return null; }
        if (statements[0] is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
            || assignment.Right is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Configure" } } invocation
            || invocation.ArgumentList.Arguments.Count != 3) { reason = "configure body: " + Clip(statements[0]); return null; }
        var receiverParameter = method.IsExtensionMethod ? method.Parameters[0] : null;
        var chain = Chain(assignment.Left, model, receiverParameter);
        if (chain == null) { reason = "configure target: " + Clip(assignment.Left); return null; }
        var configure = method.Parameters.Last();
        var path = ApplyMoves(chain, method.IsExtensionMethod ? (INamedTypeSymbol)receiverParameter!.Type : method.ContainingType);
        // Path a.b.c(configure): groups a, b as lambdas, then the generated configure method.
        return new List<Step> { new Step(path, "{" + configure.Name + "}", false, null) };
    }

    // A literal value copied to call sites names its types fully, so callers need no extra using.
    static string Qualify(ExpressionSyntax expression, SemanticModel model) {
        var replacements = new List<(TextSpan Span, string Text)>();
        foreach (var name in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()) {
            if (name.Parent is MemberAccessExpressionSyntax access && access.Name == name) continue;
            if (name.Parent is QualifiedNameSyntax qualified && qualified.Right == name) continue;
            if (model.GetSymbolInfo(name).Symbol is INamedTypeSymbol type) replacements.Add((name.Span, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }
        string text = expression.ToString();
        int offset = expression.SpanStart;
        foreach (var (span, replacement) in replacements.OrderByDescending(r => r.Span.Start)) text = text[..(span.Start - offset)] + replacement + text[(span.End - offset)..];
        return text;
    }

    static bool IsStatic(ExpressionSyntax expression, SemanticModel model) {
        foreach (var name in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()) {
            var symbol = model.GetSymbolInfo(name).Symbol;
            if (symbol is IFieldSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false } or ILocalSymbol) return false;
            if (symbol is IMethodSymbol { IsStatic: false }) return false;
        }
        return true;
    }

    static string Clip(SyntaxNode node) { string text = System.Text.RegularExpressions.Regex.Replace(node.ToString(), @"\s+", " "); return text.Length > 80 ? text[..80] : text; }

    // a.b.c on this (or the extension receiver) -> [a, b, c] as property symbols.
    static List<IPropertySymbol>? Chain(ExpressionSyntax target, SemanticModel model, IParameterSymbol? receiver) {
        var result = new List<IPropertySymbol>();
        ExpressionSyntax? current = target;
        while (current != null) {
            if (current is MemberAccessExpressionSyntax access) {
                if (model.GetSymbolInfo(access.Name).Symbol is not IPropertySymbol property) return null;
                result.Insert(0, property);
                if (access.Expression is ThisExpressionSyntax) break;
                if (receiver != null && access.Expression is IdentifierNameSyntax id && model.GetSymbolInfo(id).Symbol is IParameterSymbol p && SymbolEqualityComparer.Default.Equals(p, receiver)) break;
                current = access.Expression;
            } else if (current is IdentifierNameSyntax name) {
                if (model.GetSymbolInfo(name).Symbol is not IPropertySymbol property || property.IsStatic) return null;
                result.Insert(0, property);
                break;
            } else return null;
        }
        return result.Count == 0 ? null : result;
    }

    // Property chain -> fluent path segments after moves, renames, verbs, Is-stripping and inlining.
    internal string[] ApplyMoves(List<IPropertySymbol> chain, INamedTypeSymbol receiver) {
        var segments = new List<string>();
        for (int i = 0; i < chain.Count; i++) {
            var property = chain[i];
            if (Moves.TryGetValue(Id(property), out var move)) segments.AddRange(move.Path);
            else segments.Add(property.Name);
        }
        var last = chain[^1];
        if (Verbs.TryGetValue(Id(last), out string? verb)) segments[^1] = verb;
        else if (last.Type.SpecialType == SpecialType.System_Boolean && segments[^1].Length > 2 && segments[^1].StartsWith("Is", StringComparison.Ordinal) && char.IsUpper(segments[^1][2])) segments[^1] = segments[^1][2..];
        if (InlineByType.TryGetValue(Id(receiver), out string? inline) && segments.Count > 1 && segments[0] == inline) segments.RemoveAt(0);
        return segments.ToArray();
    }
}
