namespace SiteVisuals;

/// <summary>Publishes named steps from the exact recipe files compiled into this generator.</summary>
internal static class SiteRecipeSource {
    private const string ThemeMode = "var mode = args.Contains(\"--dark\")\n    ? VisualThemeMode.Dark : VisualThemeMode.Light;";

    /// <summary>Reads uniquely named, non-nested source regions; incomplete recipes fail generation.</summary>
    internal static IReadOnlyDictionary<string, string> Read(string recipeName) {
        var resource = "SiteVisuals.WebsiteRecipes." + recipeName + ".cs";
        using var stream = typeof(SiteRecipeSource).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("Compiled website recipe source is missing: " + resource);
        using var reader = new StreamReader(stream);
        var regions = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = new List<string>();
        string? current = null;
        while (reader.ReadLine() is { } line) {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#region ", StringComparison.Ordinal)) {
                if (current != null) throw new InvalidOperationException("Nested source region in " + resource);
                current = trimmed.Substring("#region ".Length).Trim();
                if (current.Length == 0 || regions.ContainsKey(current)) throw new InvalidOperationException("Duplicate or empty source region in " + resource + ": " + current);
                lines.Clear();
            } else if (trimmed == "#endregion") {
                if (current == null) throw new InvalidOperationException("Unmatched source region in " + resource);
                var indent = lines.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.TakeWhile(character => character == ' ').Count()).DefaultIfEmpty(0).Min();
                regions.Add(current, string.Join("\n", lines.Select(value => value.Length >= indent ? value.Substring(indent) : string.Empty)).TrimEnd());
                current = null;
            } else if (current != null) lines.Add(line);
        }
        if (current != null) throw new InvalidOperationException("Unclosed source region in " + resource + ": " + current);
        return regions;
    }

    /// <summary>Assembles a normal console program from displayed executable steps without rewriting their C#.</summary>
    internal static string CompleteProgram(IReadOnlyDictionary<string, string> regions, bool includeThemeMode, params string[] sections) =>
        string.Join("\n\n", new[] { Required(regions, "imports"), includeThemeMode ? ThemeMode : string.Empty }
            .Concat(sections.Select(section => Required(regions, section))).Where(value => value.Length > 0)) + "\n";

    /// <summary>Writes the complete download and exposes its exact steps to the example-page shortcode.</summary>
    internal static void Attach(VisualOutput output, Dictionary<string, object> entry, string slug, string recipeName,
        bool includeThemeMode, params string[] sections) {
        AttachProgram(output, entry, slug, recipeName, includeThemeMode, string.Empty, false, sections);
    }

    /// <summary>Exposes the complete generated program for a documentation code block and its identical download.</summary>
    internal static void AttachComplete(VisualOutput output, Dictionary<string, object> entry, string slug, string recipeName,
        bool includeThemeMode, params string[] sections) =>
        AttachProgram(output, entry, slug, recipeName, includeThemeMode, string.Empty, true, sections);

    /// <summary>Appends an exact compiled shared recipe class so the complete download is standalone.</summary>
    internal static void AttachShared(VisualOutput output, Dictionary<string, object> entry, string slug, string recipeName,
        string sharedRecipeName, bool includeThemeMode, params string[] sections) {
        using var stream = typeof(SiteRecipeSource).Assembly.GetManifestResourceStream("SiteVisuals.SharedRecipes." + sharedRecipeName + ".cs")
            ?? throw new InvalidOperationException("Compiled shared recipe source is missing: " + sharedRecipeName);
        using var reader = new StreamReader(stream);
        AttachProgram(output, entry, slug, recipeName, includeThemeMode, reader.ReadToEnd(), true, sections);
    }

    private static void AttachProgram(VisualOutput output, Dictionary<string, object> entry, string slug, string recipeName,
        bool includeThemeMode, string sharedSource, bool includeCompleteProgram, string[] sections) {
        var regions = Read(recipeName);
        var code = sections.ToDictionary(section => section, section => Required(regions, section), StringComparer.Ordinal);
        code[sections[0]] = string.Join("\n\n", new[] { Required(regions, "imports"), includeThemeMode ? ThemeMode : string.Empty, code[sections[0]] }.Where(value => value.Length > 0));
        var program = CompleteProgram(regions, includeThemeMode, sections);
        if (!string.IsNullOrWhiteSpace(sharedSource)) program += "\n" + sharedSource.Replace("\r\n", "\n").TrimEnd() + "\n";
        entry["code"] = code;
        if (includeCompleteProgram) entry["program"] = program;
        entry["source"] = "/site/examples/" + slug + ".csharp.txt";
        output.Text("examples/" + slug + ".csharp.txt", program);
    }

    private static string Required(IReadOnlyDictionary<string, string> regions, string name) =>
        regions.TryGetValue(name, out var source) && !string.IsNullOrWhiteSpace(source) ? source
            : throw new InvalidOperationException("Compiled website recipe source region is missing or empty: " + name);
}
