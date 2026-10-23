using System.Text;

/// <summary>Shares canonical authored fixture text with the native showcase without loading any reference engine.</summary>
internal static class MermaidFixtureSources {
    internal static string Read(string name) {
        var resource = "ChartForgeX.Examples.MermaidFixtures." + name + ".mmd";
        using var stream = typeof(MermaidFixtureSources).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("Missing Mermaid showcase source fixture: " + name);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
