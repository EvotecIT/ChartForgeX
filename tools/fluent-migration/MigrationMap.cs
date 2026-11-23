using System.Text.RegularExpressions;

/// <summary>The parsed migration map: member moves, call templates, kept-method renames, inlined properties, delegate verbs.</summary>
sealed class MigrationMap {
    internal sealed record Move(string Type, string Member, string Path, bool Delete, string Source);
    internal sealed record Call(string Type, string Method, string[]? Parameters, string Template, string Source);
    internal sealed record Keep(string Type, string Method, string[]? Parameters, string NewName, string Source, string Reason);

    internal List<Move> Moves { get; } = new();
    internal List<Call> Calls { get; } = new();
    internal List<Keep> Keeps { get; } = new();
    internal List<Keep> Stays { get; } = new();
    internal List<(string Type, string Property)> Inlines { get; } = new();
    internal Dictionary<string, string> Verbs { get; } = new(StringComparer.Ordinal); // "Type.Property" -> verb

    static readonly Regex Signature = new(@"^(?<type>[\w`]+)\.(?<member>\w+)(\((?<params>[^)]*)\))?$");

    internal static MigrationMap Load(IEnumerable<string> files) {
        var map = new MigrationMap();
        foreach (string file in files) {
            int lineNumber = 0;
            foreach (string raw in File.ReadLines(file)) {
                lineNumber++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                string reason = string.Empty;
                int comment = line.IndexOf(" # ", StringComparison.Ordinal);
                if (comment >= 0) { reason = line[(comment + 3)..].Trim(); line = line[..comment].Trim(); }
                string source = Path.GetFileName(file) + ":" + lineNumber;
                int space = line.IndexOf(' ');
                string kind = line[..space];
                string rest = line[(space + 1)..].Trim();
                switch (kind) {
                    case "inline": {
                        var (type, member, _) = Parse(rest, source);
                        map.Inlines.Add((type, member));
                        break;
                    }
                    case "move": {
                        var (left, right) = Split(rest, source);
                        bool delete = right.EndsWith(" delete", StringComparison.Ordinal);
                        if (delete) right = right[..^" delete".Length].Trim();
                        var (type, member, _) = Parse(left, source);
                        map.Moves.Add(new Move(type, member, right, delete, source));
                        break;
                    }
                    case "call": {
                        var (left, right) = Split(rest, source);
                        var (type, member, parameters) = Parse(left, source);
                        map.Calls.Add(new Call(type, member, parameters, right, source));
                        break;
                    }
                    case "keep": {
                        var (left, right) = Split(rest, source);
                        var (type, member, parameters) = Parse(left, source);
                        map.Keeps.Add(new Keep(type, member, parameters, right, source, reason));
                        break;
                    }
                    case "stay": {
                        var (type, member, parameters) = Parse(rest, source);
                        map.Stays.Add(new Keep(type, member, parameters, member, source, reason));
                        break;
                    }
                    case "verb": {
                        var (left, right) = Split(rest, source);
                        var (type, member, _) = Parse(left, source);
                        map.Verbs[type + "." + member] = right;
                        break;
                    }
                    default:
                        throw new InvalidOperationException(source + ": unknown entry '" + kind + "'.");
                }
            }
        }
        return map;
    }

    static (string Left, string Right) Split(string text, string source) {
        int arrow = text.IndexOf("->", StringComparison.Ordinal);
        if (arrow < 0) throw new InvalidOperationException(source + ": missing '->'.");
        return (text[..arrow].Trim(), text[(arrow + 2)..].Trim());
    }

    static (string Type, string Member, string[]? Parameters) Parse(string text, string source) {
        Match match = Signature.Match(text.Replace(" ", string.Empty));
        if (!match.Success) throw new InvalidOperationException(source + ": cannot parse '" + text + "'.");
        string[]? parameters = match.Groups["params"].Success
            ? match.Groups["params"].Value.Length == 0 ? Array.Empty<string>() : SplitParameters(match.Groups["params"].Value)
            : null;
        return (match.Groups["type"].Value, match.Groups["member"].Value, parameters);
    }

    static string[] SplitParameters(string text) {
        var result = new List<string>();
        int depth = 0, start = 0;
        for (int i = 0; i < text.Length; i++) {
            if (text[i] == '<') depth++;
            else if (text[i] == '>') depth--;
            else if (text[i] == ',' && depth == 0) { result.Add(text[start..i]); start = i + 1; }
        }
        result.Add(text[start..]);
        return result.ToArray();
    }
}
