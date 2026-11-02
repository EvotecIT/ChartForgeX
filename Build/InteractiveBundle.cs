using System;
using System.Collections.Generic;
using System.Linq;
using NUglify.JavaScript;
using NUglify.JavaScript.Syntax;

// Build-only linking uses the parser shipped with the existing PowerForge optimizer.
// Runtime declarations remain owned by interactive.source; entry wrappers select roots.
public static class InteractiveBundle {
    public static string Link(string runtime, string entry) {
        var settings = new CodeSettings { MinifyCode = false };
        var original = Descendants(Parse(runtime, settings)).OfType<FunctionObject>().First().Body;
        var overrides = new HashSet<string>(Parse(entry, settings).Children.OfType<Declaration>()
            .SelectMany(node => node.Children.OfType<VariableDeclaration>()).Select(node => ((BindingIdentifier)node.Binding).Name));
        var shared = string.Join("\n", original.Children.OfType<Declaration>().Where(node =>
            !node.Children.OfType<VariableDeclaration>().Any(variable => overrides.Contains(((BindingIdentifier)variable.Binding).Name)))
            .Select(node => node.Context.Code + ";"));
        var prefix = "(() => {\n" + shared + "\n";
        var body = Descendants(Parse(prefix + entry + "\n})();", settings)).OfType<FunctionObject>().First().Body;
        var declarations = new Dictionary<JSVariableField, AstNode>();
        foreach (var statement in body.Children) {
            if (statement is Declaration declaration) {
                foreach (var variable in declaration.Children.OfType<VariableDeclaration>()) {
                    if (!(variable.Binding is BindingIdentifier binding)) throw new InvalidOperationException("Bundle declarations require simple bindings.");
                    declarations.Add(Field(binding.VariableField), statement);
                }
            }
        }
        var selected = new HashSet<AstNode>(body.Children.Where(node => node.Context.StartPosition >= prefix.Length));
        var pending = new Queue<JSVariableField>(selected.SelectMany(References));
        while (pending.Count != 0) {
            if (!declarations.TryGetValue(pending.Dequeue(), out var declaration) || !selected.Add(declaration)) continue;
            foreach (var reference in References(declaration)) pending.Enqueue(reference);
        }
        return "(() => {\n" + string.Join("\n", body.Children.Where(selected.Contains).Select(node => node.Context.Code + ";")) + "\n})();\n";
    }

    private static BlockStatement Parse(string source, CodeSettings settings) {
        var parser = new JSParser();
        parser.CompilerError += (_, args) => {
            if (args.Error.IsError) throw new InvalidOperationException("Interactive bundle syntax error: " + args.Error);
        };
        return parser.Parse(source, settings);
    }

    private static JSVariableField Field(JSVariableField field) {
        while (field.OuterField != null) field = field.OuterField;
        return field;
    }

    private static IEnumerable<JSVariableField> References(AstNode node) => Descendants(node).OfType<LookupExpression>()
        .Where(lookup => lookup.VariableField != null).Select(lookup => Field(lookup.VariableField));

    private static IEnumerable<AstNode> Descendants(AstNode node) {
        yield return node;
        foreach (var child in node.Children) {
            if (child == null) continue;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
