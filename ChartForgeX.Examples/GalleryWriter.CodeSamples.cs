/// <summary>
/// Adds source snippets to generated example pages when sidecar files exist.
/// </summary>
public static partial class GalleryWriter {
    private static void AppendCodeSamples(System.Text.StringBuilder sb, string output, string name) {
        var csharp = ReadCodeSample(output, name, "csharp");
        var powershell = ReadCodeSample(output, name, "powershell");
        if (string.IsNullOrWhiteSpace(csharp) && string.IsNullOrWhiteSpace(powershell)) return;

        sb.AppendLine("<div class=\"code-samples\">");
        if (!string.IsNullOrWhiteSpace(csharp)) AppendCodeSample(sb, "C#", "language-csharp", csharp);
        if (!string.IsNullOrWhiteSpace(powershell)) AppendCodeSample(sb, "PowerShell", "language-powershell", powershell);
        sb.AppendLine("</div>");
    }

    private static void AppendCodeSample(System.Text.StringBuilder sb, string label, string language, string code) {
        sb.AppendLine("<details class=\"code-sample\"><summary>" + EscapeHtml(label) + " example code</summary><pre><code class=\"" + language + "\">" + EscapeHtml(code.Trim()) + "</code></pre></details>");
    }

    private static string ReadCodeSample(string output, string name, string language) {
        var path = Path.Combine(output, name + "." + language + ".txt");
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
