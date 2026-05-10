using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ChartForgeX.Topology;
using SkiaSharp;
using Svg.Skia;

namespace ChartForgeX.Tools.IconImport;

internal static class Program {
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly string[] ExcludedTopLevelFolders = {
        ".git",
        "media",
        "Deprecated",
        "Previous Versions"
    };

    public static int Main(string[] args) {
        try {
            if (ToolOptions.IsHelp(args)) {
                Console.WriteLine(ToolOptions.Usage);
                return 0;
            }

            var options = ToolOptions.Parse(args);
            var sourceRoot = Path.GetFullPath(options.SourceDirectory);
            var outputRoot = Path.GetFullPath(options.OutputDirectory);
            if (!Directory.Exists(sourceRoot)) throw new DirectoryNotFoundException("Source directory was not found: " + sourceRoot);

            Directory.CreateDirectory(outputRoot);
            CopyLicense(sourceRoot, outputRoot, options.SourceLicensePath);
            WriteSourceReadme(sourceRoot, outputRoot, options);

            var reports = new List<PackImportReport>();
            var usedFolderTokens = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in Directory.GetDirectories(sourceRoot).OrderBy(path => path, StringComparer.OrdinalIgnoreCase)) {
                var folderName = Path.GetFileName(folder);
                if (ExcludedTopLevelFolders.Any(excluded => string.Equals(excluded, folderName, StringComparison.OrdinalIgnoreCase))) continue;
                var svgCount = Directory.GetFiles(folder, "*.svg", SearchOption.AllDirectories).Length;
                if (svgCount == 0) continue;

                var folderToken = DeconflictToken(StableToken(folderName), usedFolderTokens);
                var packId = PackIdFor(options, folderToken);
                var packOutput = Path.Combine(outputRoot, folderToken);
                Directory.CreateDirectory(packOutput);
                var import = TopologyIconSvgPackImporter.ImportSvgPackFromDirectory(folder, new TopologyIconSvgPackImportOptions {
                    PackId = packId,
                    PackLabel = PackLabelFor(options, folderName),
                    Vendor = options.Vendor,
                    Version = options.SourceRevision.Length >= 8 ? options.SourceRevision.Substring(0, 8) : options.SourceRevision,
                    SourceUrl = options.SourceUrl,
                    SourceRevision = options.SourceRevision,
                    SourceLicense = options.SourceLicense,
                    SourceLicenseUrl = options.SourceLicenseUrl,
                    SourceLicensePath = options.SourceLicensePath,
                    CategoryPrefix = folderName,
                    Recursive = true,
                    StripDoctypeDeclarations = true,
                    DefaultColor = ColorFor(folderName)
                });
                import.Pack.Metadata.Remove("import.sourceDirectory");
                import.Pack.WithMetadata("source.folder", folderName);
                foreach (var icon in import.Pack.Icons) {
                    if (icon.Category != null) icon.Category = icon.Category.Replace("Svg Old Versions", "Old Versions", StringComparison.Ordinal);
                }
                var svgSidecars = WriteSvgSidecars(import.Pack, packOutput);
                var previews = options.GeneratePng ? GeneratePreviews(import.Pack, packOutput, options.PreviewSize) : new PreviewReport(0, 0, Array.Empty<string>());
                var validation = import.Pack.Validate();
                if (!validation.IsValid) {
                    throw new InvalidOperationException("Imported pack '" + packId + "' has validation errors: " + string.Join("; ", validation.Errors.Select(issue => issue.Path + " " + issue.Message)));
                }

                import.Pack.SaveJsonManifest(Path.Combine(packOutput, "manifest.json"));
                reports.Add(new PackImportReport(folderName, packId, svgCount, import.ImportedCount, import.SkippedCount, svgSidecars, previews.Written, previews.Failed, import.Files.Where(file => !file.Imported).Select(file => file.RelativePath + ": " + file.Message).Concat(previews.Errors).ToList()));
                Console.WriteLine(packId + ": " + import.ImportedCount.ToString(CultureInfo.InvariantCulture) + " icons, " + import.SkippedCount.ToString(CultureInfo.InvariantCulture) + " skipped, " + svgSidecars.ToString(CultureInfo.InvariantCulture) + " svg files, " + previews.Written.ToString(CultureInfo.InvariantCulture) + " previews");
            }

            WriteReport(Path.Combine(outputRoot, "_reports"), options, reports);
            return 0;
        } catch (Exception exception) {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int WriteSvgSidecars(TopologyIconPack pack, string packOutput) {
        var svgRoot = Path.Combine(packOutput, "svg");
        Directory.CreateDirectory(svgRoot);
        var written = 0;
        foreach (var icon in pack.Icons) {
            var artwork = icon.Artwork;
            if (artwork == null || !artwork.HasSvgBody) continue;
            var relativeSvgPath = "svg/" + icon.Id + ".svg";
            var outputPath = Path.Combine(packOutput, relativeSvgPath.Replace('/', Path.DirectorySeparatorChar));
            var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"" + EscapeXml(artwork.SvgViewBox) + "\" preserveAspectRatio=\"" + EscapeXml(artwork.PreserveAspectRatio) + "\">\n" + NormalizeSvgBodyForSidecar(artwork.SvgBody!) + "\n</svg>\n";
            WriteUtf8NoBomFile(outputPath, svg);
            artwork.SvgBody = null;
            artwork.SvgPath = relativeSvgPath;
            written++;
        }

        return written;
    }

    private static PreviewReport GeneratePreviews(TopologyIconPack pack, string packOutput, int size) {
        var previewRoot = Path.Combine(packOutput, "previews");
        Directory.CreateDirectory(previewRoot);
        var written = 0;
        var failed = 0;
        var errors = new List<string>();
        foreach (var icon in pack.Icons.Where(icon => icon.Artwork != null && icon.Artwork.HasSvgPath)) {
            var outputPath = Path.Combine(previewRoot, icon.Id + ".png");
            try {
                RenderSvgToPng(Path.Combine(packOutput, icon.Artwork!.SvgPath!.Replace('/', Path.DirectorySeparatorChar)), outputPath, size);
                icon.Artwork!.PreviewPath = "previews/" + icon.Id + ".png";
                written++;
            } catch (Exception exception) when (exception is InvalidOperationException || exception is IOException || exception is ArgumentException) {
                failed++;
                errors.Add(icon.Id + ": " + exception.Message);
            }
        }

        return new PreviewReport(written, failed, errors);
    }

    private static void RenderSvgToPng(string sourcePath, string outputPath, int size) {
        using var svg = new SKSvg();
        var picture = svg.Load(sourcePath);
        if (picture == null) throw new InvalidOperationException("SVG could not be loaded by Svg.Skia.");

        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new InvalidOperationException("SVG has no drawable bounds.");

        var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        var padding = Math.Max(2f, size * 0.14f);
        var scale = Math.Min((size - padding * 2) / bounds.Width, (size - padding * 2) / bounds.Height);
        var left = (size - bounds.Width * scale) / 2f - bounds.Left * scale;
        var top = (size - bounds.Height * scale) / 2f - bounds.Top * scale;
        canvas.Translate(left, top);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        canvas.Flush();

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(outputPath);
        data.SaveTo(stream);
    }

    private static void CopyLicense(string sourceRoot, string outputRoot, string licensePath) {
        var sourceLicense = Path.Combine(sourceRoot, string.IsNullOrWhiteSpace(licensePath) ? "LICENSE" : licensePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(sourceLicense)) File.Copy(sourceLicense, Path.Combine(outputRoot, Path.GetFileName(sourceLicense)), overwrite: true);
    }

    private static void WriteSourceReadme(string sourceRoot, string outputRoot, ToolOptions options) {
        var builder = new StringBuilder();
        builder.AppendLine("# ChartForgeX Topology Icon Import");
        builder.AppendLine();
        builder.AppendLine("Generated from: " + options.SourceUrl);
        builder.AppendLine("Source revision: `" + options.SourceRevision + "`");
        builder.AppendLine("License: " + options.SourceLicense + ", copied to `LICENSE` in this folder when available.");
        builder.AppendLine("Local source directory used during generation is intentionally not written into manifests.");
        builder.AppendLine();
        builder.AppendLine("The generated manifests reference pack-local SVG files under `svg/` and optional PNG thumbnails under `previews/`. ChartForgeX core does not depend on Svg.Skia or SkiaSharp at runtime.");
        WriteUtf8NoBomFile(Path.Combine(outputRoot, "SOURCE.md"), builder.ToString());
    }

    private static void WriteReport(string reportRoot, ToolOptions options, IReadOnlyList<PackImportReport> reports) {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        WriteProperty(builder, 1, "sourceUrl", options.SourceUrl, comma: true);
        WriteProperty(builder, 1, "sourceRevision", options.SourceRevision, comma: true);
        WriteProperty(builder, 1, "license", options.SourceLicense, comma: true);
        builder.AppendLine("  \"generatedAtUtc\": \"" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + "\",");
        builder.AppendLine("  \"generatePng\": " + (options.GeneratePng ? "true" : "false") + ",");
        builder.AppendLine("  \"previewSize\": " + options.PreviewSize.ToString(CultureInfo.InvariantCulture) + ",");
        builder.AppendLine("  \"packs\": [");
        for (var index = 0; index < reports.Count; index++) {
            var report = reports[index];
            builder.AppendLine("    {");
            WriteProperty(builder, 3, "category", report.Category, comma: true);
            WriteProperty(builder, 3, "packId", report.PackId, comma: true);
            builder.AppendLine("      \"sourceSvgFiles\": " + report.SourceSvgFiles.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("      \"importedIcons\": " + report.ImportedIcons.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("      \"skippedFiles\": " + report.SkippedFiles.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("      \"svgFiles\": " + report.SvgFiles.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("      \"previewPngFiles\": " + report.PreviewPngFiles.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("      \"previewFailures\": " + report.PreviewFailures.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("      \"issues\": [");
            for (var issueIndex = 0; issueIndex < report.Issues.Count; issueIndex++) {
                builder.Append("        ");
                WriteJsonString(builder, report.Issues[issueIndex]);
                builder.AppendLine(issueIndex + 1 == report.Issues.Count ? string.Empty : ",");
            }

            builder.AppendLine("      ]");
            builder.AppendLine("    }" + (index + 1 == reports.Count ? string.Empty : ","));
        }

        builder.AppendLine("  ]");
        builder.AppendLine("}");
        Directory.CreateDirectory(reportRoot);
        WriteUtf8NoBomFile(Path.Combine(reportRoot, "import-report.json"), builder.ToString());
    }

    private static void WriteProperty(StringBuilder builder, int indent, string name, string value, bool comma) {
        builder.Append(' ', indent * 2);
        WriteJsonString(builder, name);
        builder.Append(": ");
        WriteJsonString(builder, value);
        if (comma) builder.Append(',');
        builder.AppendLine();
    }

    private static void WriteJsonString(StringBuilder builder, string value) {
        builder.Append('"');
        foreach (var ch in value) {
            switch (ch) {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        builder.Append('"');
    }

    private static string EscapeXml(string value) {
        return value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private static void WriteUtf8NoBomFile(string path, string content) {
        File.WriteAllText(path, NormalizeNewLines(content), Utf8NoBom);
    }

    private static string NormalizeNewLines(string content) {
        return content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
    }

    private static string NormalizeSvgBodyForSidecar(string content) {
        return string.Join("\n", NormalizeNewLines(content).Split('\n').Select(line => line.TrimEnd()));
    }

    private static string ColorFor(string folderName) {
        if (folderName.IndexOf("Security", StringComparison.OrdinalIgnoreCase) >= 0) return "#7C3AED";
        if (folderName.IndexOf("Office", StringComparison.OrdinalIgnoreCase) >= 0) return "#D83B01";
        if (folderName.IndexOf("Power", StringComparison.OrdinalIgnoreCase) >= 0) return "#742774";
        if (folderName.IndexOf("IoT", StringComparison.OrdinalIgnoreCase) >= 0) return "#00A4EF";
        if (folderName.IndexOf("Database", StringComparison.OrdinalIgnoreCase) >= 0) return "#0F766E";
        return "#0078D4";
    }

    private static string StableToken(string value) {
        var builder = new StringBuilder(value.Length);
        var lastDash = false;
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch)) {
                builder.Append(char.ToLowerInvariant(ch));
                lastDash = false;
            } else if (!lastDash) {
                builder.Append('-');
                lastDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string DeconflictToken(string token, Dictionary<string, int> usedTokens) {
        token = string.IsNullOrWhiteSpace(token) ? "pack" : token;
        var suffix = 1;
        var candidate = token;
        while (usedTokens.ContainsKey(candidate)) {
            suffix++;
            candidate = token + "-" + suffix.ToString(CultureInfo.InvariantCulture);
        }

        usedTokens[candidate] = suffix;
        return candidate;
    }

    private static string PackIdFor(ToolOptions options, string folderToken) {
        return string.IsNullOrWhiteSpace(options.PackIdPrefix) ? folderToken : StableToken(options.PackIdPrefix + "-" + folderToken);
    }

    private static string PackLabelFor(ToolOptions options, string folderName) {
        return string.IsNullOrWhiteSpace(options.PackLabelPrefix) ? folderName : options.PackLabelPrefix.Trim() + " - " + folderName;
    }

    private sealed class ToolOptions {
        public const string Usage =
            "ChartForgeX.Tools.IconImport\n" +
            "\n" +
            "Imports a folder tree of SVG files into ChartForgeX topology icon packs.\n" +
            "Generated packs use manifest.json plus pack-local svg/*.svg sidecars and optional previews/*.png thumbnails.\n" +
            "\n" +
            "Usage:\n" +
            "  dotnet run --project ChartForgeX.Tools.IconImport -- --source <folder> [options]\n" +
            "\n" +
            "Required:\n" +
            "  --source <folder>              Source folder containing one or more category folders with SVG files.\n" +
            "\n" +
            "Options:\n" +
            "  --output <folder>              Output folder. Default: assets/topology-icons/imported-svg-pack\n" +
            "  --source-revision <revision>   Source revision. Defaults to git rev-parse HEAD when source is a git repo.\n" +
            "  --source-url <url>             Upstream URL stored in generated provenance metadata.\n" +
            "  --source-license <name>        License name stored in generated provenance metadata.\n" +
            "  --source-license-url <url>     License URL stored in generated provenance metadata.\n" +
            "  --source-license-path <path>   License file path inside source folder. Default: LICENSE.\n" +
            "  --vendor <name>                Vendor/owner label. Default: source folder name.\n" +
            "  --pack-id-prefix <prefix>      Prefix for generated pack ids.\n" +
            "  --pack-label-prefix <prefix>   Prefix for generated pack labels.\n" +
            "  --generate-png                 Generate preview PNG thumbnails beside SVG sidecars.\n" +
            "  --preview-size <pixels>        Preview PNG size from 32 to 512. Default: 128.\n" +
            "  --help                         Show this help.\n";

        public string SourceDirectory { get; private set; } = string.Empty;
        public string OutputDirectory { get; private set; } = Path.Combine("assets", "topology-icons", "imported-svg-pack");
        public string SourceRevision { get; private set; } = string.Empty;
        public string SourceUrl { get; private set; } = "unknown";
        public string SourceLicense { get; private set; } = "unknown";
        public string SourceLicenseUrl { get; private set; } = string.Empty;
        public string SourceLicensePath { get; private set; } = "LICENSE";
        public string Vendor { get; private set; } = string.Empty;
        public string PackIdPrefix { get; private set; } = string.Empty;
        public string PackLabelPrefix { get; private set; } = string.Empty;
        public bool GeneratePng { get; private set; }
        public int PreviewSize { get; private set; } = 128;

        public static bool IsHelp(string[] args) {
            return args.Any(arg => string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase));
        }

        public static ToolOptions Parse(string[] args) {
            var options = new ToolOptions();
            for (var index = 0; index < args.Length; index++) {
                var arg = args[index];
                switch (arg) {
                    case "--source":
                        options.SourceDirectory = RequiredValue(args, ref index, arg);
                        break;
                    case "--output":
                        options.OutputDirectory = RequiredValue(args, ref index, arg);
                        break;
                    case "--source-revision":
                        options.SourceRevision = RequiredValue(args, ref index, arg);
                        break;
                    case "--source-url":
                        options.SourceUrl = RequiredValue(args, ref index, arg);
                        break;
                    case "--source-license":
                        options.SourceLicense = RequiredValue(args, ref index, arg);
                        break;
                    case "--source-license-url":
                        options.SourceLicenseUrl = RequiredValue(args, ref index, arg);
                        break;
                    case "--source-license-path":
                        options.SourceLicensePath = RequiredValue(args, ref index, arg);
                        break;
                    case "--vendor":
                        options.Vendor = RequiredValue(args, ref index, arg);
                        break;
                    case "--pack-id-prefix":
                        options.PackIdPrefix = RequiredValue(args, ref index, arg);
                        break;
                    case "--pack-label-prefix":
                        options.PackLabelPrefix = RequiredValue(args, ref index, arg);
                        break;
                    case "--generate-png":
                        options.GeneratePng = true;
                        break;
                    case "--preview-size":
                        options.PreviewSize = int.Parse(RequiredValue(args, ref index, arg), CultureInfo.InvariantCulture);
                        break;
                    default:
                        throw new ArgumentException("Unknown argument: " + arg);
                }
            }

            if (string.IsNullOrWhiteSpace(options.SourceDirectory)) throw new ArgumentException("Missing --source.");
            if (string.IsNullOrWhiteSpace(options.SourceRevision)) options.SourceRevision = ReadGitRevision(options.SourceDirectory);
            if (string.IsNullOrWhiteSpace(options.Vendor)) options.Vendor = new DirectoryInfo(options.SourceDirectory).Name;
            if (options.PreviewSize < 32 || options.PreviewSize > 512) throw new ArgumentException("--preview-size must be between 32 and 512.");
            return options;
        }

        private static string RequiredValue(string[] args, ref int index, string name) {
            if (index + 1 >= args.Length) throw new ArgumentException("Missing value for " + name + ".");
            index++;
            return args[index];
        }

        private static string ReadGitRevision(string sourceDirectory) {
            try {
                var start = new ProcessStartInfo("git", "-C \"" + sourceDirectory + "\" rev-parse HEAD") {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var process = Process.Start(start);
                if (process == null) return "unknown";
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return process.ExitCode == 0 && output.Length > 0 ? output : "unknown";
            } catch {
                return "unknown";
            }
        }
    }

    private sealed record PreviewReport(int Written, int Failed, IReadOnlyList<string> Errors);

    private sealed record PackImportReport(string Category, string PackId, int SourceSvgFiles, int ImportedIcons, int SkippedFiles, int SvgFiles, int PreviewPngFiles, int PreviewFailures, IReadOnlyList<string> Issues);
}
