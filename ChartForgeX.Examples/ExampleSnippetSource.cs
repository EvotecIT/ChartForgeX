using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Turns one compiled recipe into the short, linear snippet the website shows for a single variant: the recipe's
/// Create body specialized to that variant (branches on the variant resolved and removed), followed by the helpers it
/// calls. Imports and output lines from the complete program sit in Setup and Output regions that the site folds away.
/// The complete-program download is unchanged; this text is for reading.
/// </summary>
public static class ExampleSnippetSource {
    private static readonly Regex FactoryLine = new(@"^var model = (?<factory>\w+)\.Create\([^\n]*\);\n", RegexOptions.Multiline);

    /// <summary>Creates the snippet for a program emitted by <see cref="ExampleRecipeSource.Emit"/>, or returns the program when the recipe cannot be specialized.</summary>
    public static string Emit(ExampleRecipe recipe, string variant, string program) {
        if (recipe == null) throw new ArgumentNullException(nameof(recipe));
        if (program == null) throw new ArgumentNullException(nameof(program));
        var factory = FactoryLine.Match(program);
        var setupEnd = program.IndexOf("#endregion\n", StringComparison.Ordinal);
        if (!factory.Success || setupEnd < factory.Index) return program;
        var snippet = Specialize(ExampleRecipeSource.RecipeSource(recipe), recipe.Factory, variant);
        if (snippet == null) return program;
        var setup = program.Substring(0, factory.Index);
        var output = program.Substring(factory.Index + factory.Length, setupEnd - factory.Index - factory.Length);
        output = Regex.Replace(output, @"\bmodel\b", snippet.Value.Name);
        // Setup and output names give way to the recipe's own locals.
        foreach (var local in new[] { "theme", "context", "prepared", "natural", "topology" }.Where(snippet.Value.Declared.Contains)) {
            var renamed = "output" + char.ToUpperInvariant(local[0]) + local.Substring(1);
            setup = Regex.Replace(setup, @"(?<![\w.])" + local + @"\b", renamed);
            output = Regex.Replace(output, @"(?<![\w.])" + local + @"\b", renamed);
        }
        if (snippet.Value.Helpers.Length > 0) {
            // The helpers keep the recipe's class name; one using static lets the linear code call them by name.
            var imports = Regex.Matches(setup, @"^using [^\n]+;\n", RegexOptions.Multiline);
            var at = imports.Count == 0 ? setup.IndexOf('\n') + 1 : imports[imports.Count - 1].Index + imports[imports.Count - 1].Length;
            setup = setup.Insert(at, "using static " + recipe.Factory + ";\n");
        }
        var text = new StringBuilder();
        text.Append(setup.TrimEnd('\n')).Append("\n#endregion\n\n");
        text.Append(snippet.Value.Body.TrimEnd()).Append("\n\n");
        text.Append("#region Output\n").Append(output.TrimEnd('\n')).Append("\n#endregion\n");
        if (snippet.Value.Helpers.Length > 0) text.Append('\n').Append(snippet.Value.Helpers.TrimEnd()).Append('\n');
        var result = text.ToString();
        if (SnippetCompiler.Errors(result) is { } error) {
            Console.Error.WriteLine("Snippet for " + recipe.Factory + " (" + variant + ") shows the complete program: " + error);
            return program;
        }
        return result;
    }

    /// <summary>Specializes the factory's Create method to one variant; null when the recipe has no such method.</summary>
    internal static (string Body, string Helpers, string Name, HashSet<string> Declared)? Specialize(string source, string factory, string variant) {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var type = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(node => node.Identifier.Text == factory);
        var create = type?.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(method => method.Identifier.Text == "Create"
            && method.Body != null && method.ParameterList.Parameters.Count > 0
            && method.ParameterList.Parameters[0].Type is PredefinedTypeSyntax { Keyword.Text: "string" });
        if (type == null || create?.Body == null) return null;
        var rewriter = new VariantRewriter(create.ParameterList.Parameters[0].Identifier.Text, variant);
        var body = (BlockSyntax)rewriter.Visit(create.Body);
        if (rewriter.Failed) return null;
        // Create stays out of the helpers unless the variant's code calls it for another variant.
        var recursive = body.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call => call.Expression is IdentifierNameSyntax { Identifier.Text: "Create" });
        var members = type.Members.Where(member => member != create || recursive).ToList();
        if (!recursive) body = BindHelpers(body, members);
        var statements = body.Statements.ToList();
        if (statements.Count == 0 || statements.OfType<ReturnStatementSyntax>().Count() != 1 || statements[statements.Count - 1] is not ReturnStatementSyntax last
            || last.Expression == null || body.DescendantNodes().OfType<ReturnStatementSyntax>().Count(node => !InsideLambdaOrLocalFunction(node)) != 1)
            return null;
        statements.RemoveAt(statements.Count - 1);
        var locals = new HashSet<string>(statements.OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(statement => statement.Declaration.Variables).Select(variable => variable.Identifier.Text), StringComparer.Ordinal);
        string name;
        var text = new StringBuilder();
        // Other Create parameters keep their defaults; the theme mode comes from the setup's mode.
        foreach (var parameter in create.ParameterList.Parameters.Skip(1)) {
            var parameterName = parameter.Identifier.Text;
            if (parameterName == "mode" && parameter.Type?.ToString() == "VisualThemeMode") continue;
            var value = parameter.Type?.ToString() == "VisualThemeMode" ? "mode" : parameter.Default?.Value.ToString();
            if (value == null) return null;
            text.Append(parameter.Type).Append(' ').Append(parameterName).Append(" = ").Append(value).Append(";\n");
        }
        foreach (var statement in statements) text.Append(Dedent(statement.ToFullString())).Append('\n');
        if (last.Expression is IdentifierNameSyntax returned && locals.Contains(returned.Identifier.Text)) {
            name = returned.Identifier.Text;
        } else if (Root(last.Expression) is { } builder && locals.Contains(builder)) {
            // return chart.AddSeries(...): the builders return the same chart, so the call stays a statement on that local.
            name = builder;
            text.Append(Dedent(last.GetLeadingTrivia().ToFullString() + last.Expression.ToFullString().Trim() + ";")).Append('\n');
        } else {
            name = locals.Contains("chart") ? "model" : "chart";
            text.Append(Dedent(last.GetLeadingTrivia().ToFullString() + "var " + name + " = " + last.Expression.ToFullString().Trim() + ";")).Append('\n');
        }
        var declared = new HashSet<string>(body.DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(variable => variable.Identifier.Text)
            .Concat(body.DescendantNodes().OfType<ForEachStatementSyntax>().Select(loop => loop.Identifier.Text))
            .Concat(create.ParameterList.Parameters.Skip(1).Select(parameter => parameter.Identifier.Text)), StringComparer.Ordinal);
        return (Regex.Replace(text.ToString(), @"\n{3,}", "\n\n").Trim('\n') + "\n", Helpers(type.Identifier.Text, members, body), name, declared);
    }

    // The local a fluent call chain starts from (chart in chart.AddSeries(a).WithTitle(b)), or null.
    private static string? Root(ExpressionSyntax expression) {
        if (expression is not InvocationExpressionSyntax) return null;
        while (true) {
            switch (expression) {
                case InvocationExpressionSyntax invocation: expression = invocation.Expression; break;
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax root }: return root.Identifier.Text;
                case MemberAccessExpressionSyntax access: expression = access.Expression; break;
                default: return null;
            }
        }
    }

    private static bool InsideLambdaOrLocalFunction(SyntaxNode node) =>
        node.Ancestors().Any(ancestor => ancestor is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax or LocalFunctionStatementSyntax);

    // Members of the recipe the specialized body still calls, followed transitively.
    private static HashSet<MemberDeclarationSyntax> Used(SyntaxNode body, IReadOnlyList<MemberDeclarationSyntax> members) {
        var used = new HashSet<MemberDeclarationSyntax>();
        var pending = new Queue<SyntaxNode>(new[] { body });
        while (pending.Count > 0) {
            var identifiers = new HashSet<string>(pending.Dequeue().DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken)).Select(token => token.Text), StringComparer.Ordinal);
            foreach (var member in members) {
                if (used.Contains(member) || MemberName(member) is not { } name || !identifiers.Contains(name)) continue;
                used.Add(member);
                pending.Enqueue(member);
            }
        }
        return used;
    }

    // A helper that every remaining call passes the same text for, such as Observations("wide"), is specialized to that
    // value too, and the argument goes, so the helpers carry no variant switching either.
    private static BlockSyntax BindHelpers(BlockSyntax body, List<MemberDeclarationSyntax> members) {
        for (var changed = true; changed;) {
            changed = false;
            var used = Used(body, members);
            foreach (var helper in members.OfType<MethodDeclarationSyntax>().Where(used.Contains).ToList()) {
                var name = helper.Identifier.Text;
                if (members.OfType<MethodDeclarationSyntax>().Count(method => method.Identifier.Text == name) != 1) continue;
                var callers = new SyntaxNode[] { body }.Concat(members.Where(member => member != helper && used.Contains(member))).ToList();
                var calls = callers.SelectMany(node => node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    .Where(call => call.Expression is IdentifierNameSyntax identifier && identifier.Identifier.Text == name).ToList();
                if (calls.Count == 0 || helper.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call => call.Expression is IdentifierNameSyntax { Identifier.Text: var self } && self == name)
                    || callers.Any(node => node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(identifier => identifier.Identifier.Text == name && identifier.Parent is not InvocationExpressionSyntax)))
                    continue;
                for (var index = 0; index < helper.ParameterList.Parameters.Count; index++) {
                    var parameter = helper.ParameterList.Parameters[index];
                    if (parameter.Type is not PredefinedTypeSyntax { Keyword.Text: "string" } || parameter.Modifiers.Count > 0) continue;
                    var values = calls.Select(call => call.ArgumentList.Arguments.Count > index && call.ArgumentList.Arguments.Take(index + 1).All(argument => argument.NameColon == null)
                        && call.ArgumentList.Arguments[index].Expression is LiteralExpressionSyntax { Token.Value: string text } ? text : null).Distinct().ToList();
                    if (values.Count != 1 || values[0] == null) continue;
                    var rewriter = new VariantRewriter(parameter.Identifier.Text, values[0]!);
                    var bound = (MethodDeclarationSyntax)rewriter.Visit(helper);
                    if (rewriter.Failed) continue;
                    bound = bound.WithParameterList(bound.ParameterList.WithParameters(bound.ParameterList.Parameters.RemoveAt(index)));
                    var remover = new ArgumentRemover(name, index);
                    body = (BlockSyntax)remover.Visit(body);
                    for (var position = 0; position < members.Count; position++)
                        members[position] = members[position] == helper ? bound : (MemberDeclarationSyntax)remover.Visit(members[position]);
                    changed = true;
                    break;
                }
                if (changed) break;
            }
        }
        return body;
    }

    private sealed class ArgumentRemover : CSharpSyntaxRewriter {
        private readonly string _name;
        private readonly int _index;
        public ArgumentRemover(string name, int index) { _name = name; _index = index; }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node) {
            var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
            return visited.Expression is IdentifierNameSyntax identifier && identifier.Identifier.Text == _name && visited.ArgumentList.Arguments.Count > _index
                ? visited.WithArgumentList(visited.ArgumentList.WithArguments(visited.ArgumentList.Arguments.RemoveAt(_index)))
                : visited;
        }
    }

    private static string Helpers(string typeName, IReadOnlyList<MemberDeclarationSyntax> members, SyntaxNode body) {
        var used = Used(body, members);
        if (used.Count == 0) return string.Empty;
        var text = new StringBuilder("static class " + typeName + " {\n");
        foreach (var member in members.Where(used.Contains))
            text.Append("    ").Append(Dedent(Accessible(StripDocs(member)).ToFullString()).Trim('\n').Replace("\n", "\n    ")).Append("\n\n");
        return text.ToString().TrimEnd('\n') + "\n}\n";
    }

    // Helpers are called from the top-level code, so private members become internal.
    private static MemberDeclarationSyntax Accessible(MemberDeclarationSyntax member) {
        var modifiers = member.Modifiers;
        if (modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword) || modifier.IsKind(SyntaxKind.InternalKeyword))) return member;
        var kept = SyntaxFactory.TokenList(modifiers.Where(modifier => !modifier.IsKind(SyntaxKind.PrivateKeyword) && !modifier.IsKind(SyntaxKind.ProtectedKeyword)));
        var leading = member.GetLeadingTrivia();
        var stripped = member.WithModifiers(kept).WithoutLeadingTrivia();
        stripped = stripped.WithModifiers(stripped.Modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.InternalKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
        return stripped.WithLeadingTrivia(leading);
    }

    private static string? MemberName(MemberDeclarationSyntax member) => member switch {
        MethodDeclarationSyntax method => method.Identifier.Text,
        BaseTypeDeclarationSyntax nested => nested.Identifier.Text,
        DelegateDeclarationSyntax nested => nested.Identifier.Text,
        PropertyDeclarationSyntax property => property.Identifier.Text,
        FieldDeclarationSyntax field => field.Declaration.Variables.Count == 1 ? field.Declaration.Variables[0].Identifier.Text : null,
        _ => null
    };

    private static MemberDeclarationSyntax StripDocs(MemberDeclarationSyntax member) =>
        member.WithLeadingTrivia(member.GetLeadingTrivia().Where(trivia => !trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)));

    // Removes the indentation of the text's first code line from every line, keeping relative indentation.
    private static string Dedent(string text) {
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
        if (lines.Count == 0) return string.Empty;
        var indent = lines[0].Length - lines[0].TrimStart().Length;
        return string.Join("\n", lines.Select(line => line.Length >= indent && line.Substring(0, indent).Trim().Length == 0 ? line.Substring(indent) : line.TrimStart())).TrimEnd();
    }

    /// <summary>Resolves every branch on the variant parameter, and on locals derived from it, for one variant value.</summary>
    private sealed class VariantRewriter : CSharpSyntaxRewriter {
        private static readonly object Unknown = new();
        private readonly string _parameter;
        private readonly Dictionary<string, object> _known = new(StringComparer.Ordinal);

        public VariantRewriter(string parameter, string variant) {
            _parameter = parameter;
            _known[parameter] = variant;
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node) {
            var statements = new List<StatementSyntax>();
            foreach (var statement in node.Statements) {
                var rewritten = Visit(statement);
                if (rewritten is BlockSyntax block && statement is not BlockSyntax) {
                    // A resolved branch: its statements join the enclosing block.
                    statements.AddRange(block.Statements);
                }
                else if (rewritten is StatementSyntax kept && rewritten is not EmptyStatementSyntax) statements.Add(kept);
                // Statements after a return that the variant reaches cannot run.
                if (statements.Count > 0 && statements[statements.Count - 1] is ReturnStatementSyntax or ThrowStatementSyntax) break;
            }
            return node.WithStatements(SyntaxFactory.List(statements));
        }

        /// <summary>Set when the variant's flow could not be followed; the caller then shows the complete program.</summary>
        public bool Failed { get; private set; }

        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node) {
            if (node.Expression is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax target } assignment
                && target.Identifier.Text != _parameter && _known.ContainsKey(target.Identifier.Text)) {
                // A reassigned flag or variant name: follow the new value when it is known on this path.
                var value = Evaluate(assignment.Right);
                if (value == Unknown || _uncertain > 0) Failed = true;
                else _known[target.Identifier.Text] = value;
                return SyntaxFactory.EmptyStatement();
            }
            return base.VisitExpressionStatement(node);
        }

        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node) {
            var condition = Evaluate(node.Condition);
            if (condition is bool value) {
                if (value) return Visit(node.Statement);
                return node.Else == null ? SyntaxFactory.EmptyStatement() : Visit(node.Else.Statement);
            }
            return Uncertain(() => base.VisitIfStatement(node));
        }

        // Code that may or may not run on this variant's path: an assignment to a resolved name there cannot be followed.
        private int _uncertain;
        private SyntaxNode? Uncertain(Func<SyntaxNode?> visit) {
            _uncertain++;
            try { return visit(); } finally { _uncertain--; }
        }

        public override SyntaxNode? VisitForStatement(ForStatementSyntax node) => Uncertain(() => base.VisitForStatement(node));
        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node) => Uncertain(() => base.VisitForEachStatement(node));
        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node) => Uncertain(() => base.VisitWhileStatement(node));
        public override SyntaxNode? VisitDoStatement(DoStatementSyntax node) => Uncertain(() => base.VisitDoStatement(node));
        public override SyntaxNode? VisitSwitchStatement(SwitchStatementSyntax node) => Uncertain(() => base.VisitSwitchStatement(node));
        public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => Uncertain(() => base.VisitSimpleLambdaExpression(node));
        public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => Uncertain(() => base.VisitParenthesizedLambdaExpression(node));

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) {
            if (node.Declaration.Variables.Count == 1 && node.Declaration.Variables[0].Initializer is { } initializer && Mentions(initializer.Value)) {
                var value = Evaluate(initializer.Value);
                if (value is bool or string) {
                    // A flag or name derived from the variant disappears once its branches are resolved.
                    _known[node.Declaration.Variables[0].Identifier.Text] = value;
                    return SyntaxFactory.EmptyStatement();
                }
            }
            return base.VisitLocalDeclarationStatement(node);
        }

        public override SyntaxNode? VisitConditionalExpression(ConditionalExpressionSyntax node) =>
            Evaluate(node.Condition) is bool value ? Visit(value ? node.WhenTrue : node.WhenFalse)!.WithTriviaFrom(node) : base.VisitConditionalExpression(node);

        public override SyntaxNode? VisitSwitchExpression(SwitchExpressionSyntax node) {
            if (Evaluate(node.GoverningExpression) is string value) {
                foreach (var arm in node.Arms) {
                    if (arm.WhenClause != null) break;
                    if (Matches(arm.Pattern, value) is true) return Visit(arm.Expression)!.WithTriviaFrom(node);
                    if (Matches(arm.Pattern, value) is not false) break;
                }
            }
            return base.VisitSwitchExpression(node);
        }

        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node) {
            var value = Evaluate(node);
            if (value is bool known && (node.IsKind(SyntaxKind.LogicalAndExpression) || node.IsKind(SyntaxKind.LogicalOrExpression) || Mentions(node)))
                return SyntaxFactory.LiteralExpression(known ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression).WithTriviaFrom(node);
            if (node.IsKind(SyntaxKind.LogicalAndExpression) || node.IsKind(SyntaxKind.LogicalOrExpression)) {
                var left = Evaluate(node.Left);
                var neutral = node.IsKind(SyntaxKind.LogicalAndExpression);
                if (left is bool l && l == neutral) return Visit(node.Right)!.WithTriviaFrom(node);
                if (Evaluate(node.Right) is bool r && r == neutral) return Visit(node.Left)!.WithTriviaFrom(node);
            }
            return base.VisitBinaryExpression(node);
        }

        // Any remaining use of the variant parameter reads its value directly, except a member name or argument label.
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) {
            if (!_known.TryGetValue(node.Identifier.Text, out var value) || node.Parent is NameColonSyntax
                || node.Parent is MemberAccessExpressionSyntax access && access.Name == node) return node;
            return (value switch {
                string text => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(text)),
                true => SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
                _ => SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)
            }).WithTriviaFrom(node);
        }

        // (variant == "sparse" ? a : b).Select(...) keeps one branch; parentheses around a plain call or value then go.
        public override SyntaxNode? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) {
            var visited = (ParenthesizedExpressionSyntax)base.VisitParenthesizedExpression(node)!;
            return Mentions(node.Expression) && visited.Expression is InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax
                or LiteralExpressionSyntax or ObjectCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or ArrayCreationExpressionSyntax or ElementAccessExpressionSyntax
                ? visited.Expression.WithTriviaFrom(visited)
                : visited;
        }

        public override SyntaxNode? VisitIsPatternExpression(IsPatternExpressionSyntax node) =>
            Evaluate(node) is bool value
                ? SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression).WithTriviaFrom(node)
                : base.VisitIsPatternExpression(node);

        public override SyntaxNode? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) =>
            node.IsKind(SyntaxKind.LogicalNotExpression) && Mentions(node) && Evaluate(node) is bool value
                ? SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression).WithTriviaFrom(node)
                : base.VisitPrefixUnaryExpression(node);

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node) =>
            // nameof(variant) in a guard that survived names the parameter the snippet no longer declares.
            node.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" } && node.ArgumentList.Arguments.Count == 1
                && node.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax argument && argument.Identifier.Text == _parameter
                ? SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(_parameter)).WithTriviaFrom(node)
                : base.VisitInvocationExpression(node);

        private bool Mentions(SyntaxNode node) => node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(identifier => _known.ContainsKey(identifier.Identifier.Text));

        private object Evaluate(ExpressionSyntax expression) {
            switch (expression) {
                case ParenthesizedExpressionSyntax parenthesized: return Evaluate(parenthesized.Expression);
                case LiteralExpressionSyntax literal: return literal.Token.Value ?? Unknown;
                case IdentifierNameSyntax identifier: return _known.TryGetValue(identifier.Identifier.Text, out var value) ? value : Unknown;
                case MemberAccessExpressionSyntax { Name.Identifier.Text: "Length" } length when Evaluate(length.Expression) is string measured: return measured.Length;
                case PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } not:
                    return Evaluate(not.Operand) is bool operand ? !operand : Unknown;
                case BinaryExpressionSyntax binary: {
                    var left = Evaluate(binary.Left);
                    if (binary.IsKind(SyntaxKind.LogicalAndExpression) && left is false) return false;
                    if (binary.IsKind(SyntaxKind.LogicalOrExpression) && left is true) return true;
                    var right = Evaluate(binary.Right);
                    if (binary.IsKind(SyntaxKind.LogicalAndExpression)) return right is false ? false : left is bool a && right is bool b ? a && b : Unknown;
                    if (binary.IsKind(SyntaxKind.LogicalOrExpression)) return right is true ? true : left is bool c && right is bool d ? c || d : Unknown;
                    if (left == Unknown || right == Unknown) return Unknown;
                    if (binary.IsKind(SyntaxKind.EqualsExpression)) return Equals(left, right);
                    if (binary.IsKind(SyntaxKind.NotEqualsExpression)) return !Equals(left, right);
                    return Unknown;
                }
                case IsPatternExpressionSyntax pattern:
                    return Evaluate(pattern.Expression) is string subject ? Matches(pattern.Pattern, subject) ?? Unknown : Unknown;
                case ConditionalExpressionSyntax conditional:
                    return Evaluate(conditional.Condition) is bool chosen ? Evaluate(chosen ? conditional.WhenTrue : conditional.WhenFalse) : Unknown;
                case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Substring" } substring } call
                    when call.ArgumentList.Arguments.Count == 1 && Evaluate(substring.Expression) is string whole
                        && Evaluate(call.ArgumentList.Arguments[0].Expression) is int start && start >= 0 && start <= whole.Length:
                    return whole.Substring(start);
                case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation
                    when invocation.ArgumentList.Arguments.Count is 1 or 2 && Evaluate(access.Expression) is string text
                        && Evaluate(invocation.ArgumentList.Arguments[0].Expression) is string argument:
                    return access.Name.Identifier.Text switch {
                        "StartsWith" => text.StartsWith(argument, StringComparison.Ordinal),
                        "EndsWith" => text.EndsWith(argument, StringComparison.Ordinal),
                        "Contains" => text.Contains(argument),
                        "Equals" => string.Equals(text, argument, StringComparison.Ordinal),
                        _ => Unknown
                    };
                default: return Unknown;
            }
        }

        private static bool? Matches(PatternSyntax pattern, string subject) => pattern switch {
            DiscardPatternSyntax => true,
            ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } => literal.Token.Value is string text ? text == subject : false,
            ParenthesizedPatternSyntax parenthesized => Matches(parenthesized.Pattern, subject),
            UnaryPatternSyntax unary => Matches(unary.Pattern, subject) is bool inner ? !inner : null,
            BinaryPatternSyntax binary when binary.IsKind(SyntaxKind.OrPattern) =>
                Matches(binary.Left, subject) is true || Matches(binary.Right, subject) is true ? true
                    : Matches(binary.Left, subject) is false && Matches(binary.Right, subject) is false ? false : null,
            BinaryPatternSyntax binary when binary.IsKind(SyntaxKind.AndPattern) =>
                Matches(binary.Left, subject) is false || Matches(binary.Right, subject) is false ? false
                    : Matches(binary.Left, subject) is true && Matches(binary.Right, subject) is true ? true : null,
            _ => null
        };
    }
}
