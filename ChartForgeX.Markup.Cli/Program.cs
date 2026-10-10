using System;
using System.IO;
using System.Linq;
using System.Net;
using ChartForgeX.Markup;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Markup.Cli;

internal static class Program {
    private static int Main(string[] args) {
        if (args.Length == 0 || IsHelp(args[0])) {
            Help();
            return args.Length == 0 ? 1 : 0;
        }

        if (args.Length < 2) {
            Console.Error.WriteLine("Missing input file.");
            Help();
            return 1;
        }

        try {
            var command = args[0].ToLowerInvariant();
            if (!IsCommand(command)) {
                Console.Error.WriteLine("Unknown command '" + args[0] + "'.");
                Help();
                return 1;
            }

            var input = args[1];
            var source = File.ReadAllText(input);
            switch (command) {
                case "validate":
                    return Validate(source);
                case "preview":
                    return Preview(source, args.Skip(2).ToArray());
                case "export":
                    return Export(source, args.Skip(2).ToArray());
                case "emit":
                    return Emit(source, args.Skip(2).ToArray());
                default:
                    return 1;
            }
        } catch (MarkupCliErrorException ex) {
            Console.Error.WriteLine(ex.Message);
            return ex.ExitCode;
        } catch (TopologyValidationException ex) {
            foreach (var error in ex.Result.Errors) Console.Error.WriteLine("error " + error.Code + ": " + error.Message);
            foreach (var warning in ex.Result.Warnings) Console.Error.WriteLine("warning " + warning.Code + ": " + warning.Message);
            return 2;
        } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidOperationException) {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int Validate(string source) {
        var result = ParseVisuals(source);
        WriteDiagnostics(result);
        if (result.HasErrors) return 2;
        if (result.Artifacts.Count == 0) {
            Console.Error.WriteLine("error: No supported visual artifacts were found.");
            return 2;
        }

        try {
            foreach (var artifact in result.Artifacts) _ = artifact.ToSvg();
            Console.WriteLine("Valid visual markup. Artifacts: " + result.Artifacts.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        } catch (TopologyValidationException ex) {
            foreach (var error in ex.Result.Errors) Console.Error.WriteLine("error " + error.Code + ": " + error.Message);
            foreach (var warning in ex.Result.Warnings) Console.Error.WriteLine("warning " + warning.Code + ": " + warning.Message);
            return 2;
        }
    }

    private static int Export(string source, string[] args) {
        var result = ParseRequiredVisuals(source);
        var output = Option(args, "--output") ?? Option(args, "-o");
        if (string.IsNullOrWhiteSpace(output)) throw new ArgumentException("Export requires --output <path>.");
        var extension = Path.GetExtension(output).ToLowerInvariant();
        EnsureOutputDirectory(output);
        switch (extension) {
            case ".svg":
                File.WriteAllText(output, OnlyArtifact(result).ToSvg(), System.Text.Encoding.UTF8);
                break;
            case ".html":
            case ".htm":
                File.WriteAllText(output, ArtifactsToHtmlPage(result.Artifacts), System.Text.Encoding.UTF8);
                break;
            case ".png":
                File.WriteAllBytes(output, OnlyArtifact(result).ToPng());
                break;
            default:
                throw new ArgumentException("Unsupported export extension '" + extension + "'. Use .svg, .html, or .png.");
        }

        Console.WriteLine("Wrote " + output);
        return 0;
    }

    private static int Preview(string source, string[] args) {
        var result = ParseRequiredVisuals(source);
        var html = ArtifactsToHtmlPage(result.Artifacts);
        var output = Option(args, "--output") ?? Option(args, "-o");
        if (string.IsNullOrWhiteSpace(output)) {
            Console.Write(html);
        } else {
            EnsureOutputDirectory(output);
            File.WriteAllText(output, html);
            Console.WriteLine("Wrote " + output);
        }

        return 0;
    }

    private static int Emit(string source, string[] args) {
        var result = new MarkupTopologyParser().Parse(source);
        WriteDiagnostics(result);
        if (result.HasErrors || result.Document == null) return 2;
        var target = (Option(args, "--target") ?? "csharp").ToLowerInvariant();
        if (target != "csharp") throw new ArgumentException("Only --target csharp is supported by the topology emitter.");
        var code = MarkupTopologyCSharpEmitter.Emit(result.Document);
        var output = Option(args, "--output") ?? Option(args, "-o");
        if (string.IsNullOrWhiteSpace(output)) {
            Console.Write(code);
        } else {
            EnsureOutputDirectory(output);
            File.WriteAllText(output, code);
            Console.WriteLine("Wrote " + output);
        }

        return 0;
    }

    private static VisualMarkupParseResult ParseVisuals(string source) {
        return new MermaidVisualMarkupParser().Parse(source);
    }

    private static VisualMarkupParseResult ParseRequiredVisuals(string source) {
        var result = ParseVisuals(source);
        WriteDiagnostics(result);
        if (result.HasErrors) throw new MarkupCliErrorException("Markup contains errors.", 2);
        if (result.Artifacts.Count == 0) throw new MarkupCliErrorException("No supported visual artifacts were found.", 2);
        return result;
    }

    private static VisualArtifact OnlyArtifact(VisualMarkupParseResult result) {
        if (result.Artifacts.Count == 1) return result.Artifacts[0];
        throw new InvalidOperationException("This export format requires exactly one visual artifact. Use .html to export a multi-artifact preview.");
    }

    private static string ArtifactsToHtmlPage(System.Collections.Generic.IReadOnlyList<VisualArtifact> artifacts) {
        var title = artifacts.Count == 1 && !string.IsNullOrWhiteSpace(artifacts[0].Title) ? artifacts[0].Title : "ChartForgeX Markup Preview";
        var writer = new System.Text.StringBuilder();
        writer.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>");
        writer.Append(WebUtility.HtmlEncode(title));
        writer.Append("</title><style>html,body{margin:0;background:#f8fafc;color:#0f172a;font-family:Inter,ui-sans-serif,system-ui,Segoe UI,Arial,sans-serif}body{padding:24px}.artifact{margin:0 auto 24px;max-width:1200px}.artifact-title{font-size:15px;font-weight:700;margin:0 0 10px}.artifact svg{max-width:100%;height:auto;display:block}</style></head><body>");
        foreach (var artifact in artifacts) {
            writer.Append("<section class=\"artifact\">");
            if (!string.IsNullOrWhiteSpace(artifact.Title)) {
                writer.Append("<h2 class=\"artifact-title\">");
                writer.Append(WebUtility.HtmlEncode(artifact.Title));
                writer.Append("</h2>");
            }

            writer.Append(artifact.ToSvg());
            writer.Append("</section>");
        }

        writer.Append("</body></html>");
        return writer.ToString();
    }

    private static void WriteDiagnostics(MarkupParseResult<MarkupTopologyDocument> result) {
        foreach (var diagnostic in result.Diagnostics) {
            var line = diagnostic.Line > 0 ? "(" + diagnostic.Line.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")" : string.Empty;
            var text = diagnostic.Severity.ToString().ToLowerInvariant() + line + ": " + diagnostic.Message;
            Console.Error.WriteLine(text);
        }
    }

    private static void WriteDiagnostics(VisualMarkupParseResult result) {
        foreach (var diagnostic in result.Diagnostics) {
            var line = diagnostic.Line > 0 ? "(" + diagnostic.Line.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")" : string.Empty;
            var code = string.IsNullOrWhiteSpace(diagnostic.Code) ? string.Empty : " " + diagnostic.Code;
            var text = diagnostic.Severity.ToString().ToLowerInvariant() + code + line + ": " + diagnostic.Message;
            Console.Error.WriteLine(text);
        }
    }

    private static string? Option(string[] args, string name) {
        for (var i = 0; i < args.Length; i++) {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) continue;
            if (i + 1 >= args.Length || IsOptionName(args[i + 1])) throw new ArgumentException("Option " + name + " requires a value.");
            return args[i + 1];
        }

        return null;
    }

    private static bool IsOptionName(string value) => value.StartsWith("-", StringComparison.Ordinal);

    private static void EnsureOutputDirectory(string path) {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
    }

    private static bool IsHelp(string value) => value == "-h" || value == "--help" || value == "help";
    private static bool IsCommand(string value) => value == "validate" || value == "preview" || value == "export" || value == "emit";

    private static void Help() {
        Console.WriteLine("ChartForgeX Markup CLI");
        Console.WriteLine("Usage:");
        Console.WriteLine("  chartforgex-markup validate <file>");
        Console.WriteLine("  chartforgex-markup preview <file> [--output <preview.html>]");
        Console.WriteLine("  chartforgex-markup export <file> --output <diagram.svg|diagram.html|diagram.png>");
        Console.WriteLine("  chartforgex-markup emit <topology-file> --target csharp [--output <file.cs>]");
    }

    private sealed class MarkupCliErrorException : Exception {
        public MarkupCliErrorException(string message, int exitCode) : base(message) {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }
}
