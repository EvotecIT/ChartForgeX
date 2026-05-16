using System;
using System.IO;
using System.Linq;
using ChartForgeX.Markup;
using ChartForgeX.Topology;

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
            var result = new MarkupTopologyParser().Parse(source);
            WriteDiagnostics(result);
            if (result.HasErrors || result.Document == null) return 2;

            switch (command) {
                case "validate":
                    return Validate(result.Document);
                case "preview":
                    return Preview(result.Document, args.Skip(2).ToArray());
                case "export":
                    return Export(result.Document, args.Skip(2).ToArray());
                case "emit":
                    return Emit(result.Document, args.Skip(2).ToArray());
                default:
                    return 1;
            }
        } catch (TopologyValidationException ex) {
            foreach (var error in ex.Result.Errors) Console.Error.WriteLine("error " + error.Code + ": " + error.Message);
            foreach (var warning in ex.Result.Warnings) Console.Error.WriteLine("warning " + warning.Code + ": " + warning.Message);
            return 2;
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

    private static int Preview(MarkupTopologyDocument document, string[] args) {
        var html = document.ToTopologyChart().ToHtmlPage();
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
        Console.WriteLine("  chartforgex-markup emit <file> --target csharp [--output <file.cs>]");
    }
}
