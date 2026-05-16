using System;
using System.IO;
using System.Linq;
using ChartForgeX.Markup;
using ChartForgeX.Topology;

namespace ChartForgeX.Markup.Cli;

internal static class Program {
    private static int Main(string[] args) {
        if (args.Length < 2 || IsHelp(args[0])) {
            Help();
            return args.Length == 0 ? 1 : 0;
        }

        try {
            var command = args[0].ToLowerInvariant();
            var input = args[1];
            var source = File.ReadAllText(input);
            var result = new MarkupTopologyParser().Parse(source);
            WriteDiagnostics(result);
            if (result.HasErrors || result.Document == null) return 2;

            switch (command) {
                case "validate":
                    return Validate(result.Document);
                case "export":
                    return Export(result.Document, args.Skip(2).ToArray());
                case "emit":
                    return Emit(result.Document, args.Skip(2).ToArray());
                default:
                    Console.Error.WriteLine("Unknown command '" + args[0] + "'.");
                    Help();
                    return 1;
            }
        } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidOperationException) {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int Validate(MarkupTopologyDocument document) {
        try {
            var svg = document.ToTopologyChart().ToSvg();
            Console.WriteLine("Valid topology markup. SVG length: " + svg.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        } catch (TopologyValidationException ex) {
            foreach (var error in ex.Result.Errors) Console.Error.WriteLine("error " + error.Code + ": " + error.Message);
            foreach (var warning in ex.Result.Warnings) Console.Error.WriteLine("warning " + warning.Code + ": " + warning.Message);
            return 2;
        }
    }

    private static int Export(MarkupTopologyDocument document, string[] args) {
        var output = Option(args, "--output") ?? Option(args, "-o");
        if (string.IsNullOrWhiteSpace(output)) throw new ArgumentException("Export requires --output <path>.");
        var extension = Path.GetExtension(output).ToLowerInvariant();
        var chart = document.ToTopologyChart();
        EnsureOutputDirectory(output);
        switch (extension) {
            case ".svg":
                chart.SaveSvg(output);
                break;
            case ".html":
            case ".htm":
                chart.SaveHtml(output);
                break;
            case ".png":
                chart.SavePng(output);
                break;
            default:
                throw new ArgumentException("Unsupported export extension '" + extension + "'. Use .svg, .html, or .png.");
        }

        Console.WriteLine("Wrote " + output);
        return 0;
    }

    private static int Emit(MarkupTopologyDocument document, string[] args) {
        var target = (Option(args, "--target") ?? "csharp").ToLowerInvariant();
        if (target != "csharp") throw new ArgumentException("Only --target csharp is supported by the MVP emitter.");
        var code = MarkupTopologyCSharpEmitter.Emit(document);
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

    private static void WriteDiagnostics(MarkupParseResult<MarkupTopologyDocument> result) {
        foreach (var diagnostic in result.Diagnostics) {
            var line = diagnostic.Line > 0 ? "(" + diagnostic.Line.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")" : string.Empty;
            var text = diagnostic.Severity.ToString().ToLowerInvariant() + line + ": " + diagnostic.Message;
            if (diagnostic.Severity == MarkupDiagnosticSeverity.Error) Console.Error.WriteLine(text); else Console.WriteLine(text);
        }
    }

    private static string? Option(string[] args, string name) {
        for (var i = 0; i < args.Length - 1; i++) {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        }

        return null;
    }

    private static void EnsureOutputDirectory(string path) {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
    }

    private static bool IsHelp(string value) => value == "-h" || value == "--help" || value == "help";

    private static void Help() {
        Console.WriteLine("ChartForgeX Markup CLI");
        Console.WriteLine("Usage:");
        Console.WriteLine("  chartforgex-markup validate <file>");
        Console.WriteLine("  chartforgex-markup export <file> --output <diagram.svg|diagram.html|diagram.png>");
        Console.WriteLine("  chartforgex-markup emit <file> --target csharp [--output <file.cs>]");
    }
}
