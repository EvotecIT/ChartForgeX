using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ChartForgeX.Topology;
using SkiaSharp;

namespace ChartForgeX.Tools.IconImport;

internal static class TopologyIconPackRefreshCommand {
    public static int Run(string[] args) {
        var options = RefreshOptions.Parse(args);
        var packRoot = Path.GetFullPath(options.PackDirectory);
        if (!Directory.Exists(packRoot)) throw new DirectoryNotFoundException("Topology icon pack directory was not found: " + packRoot);

        var manifestPath = Path.Combine(packRoot, "manifest.json");
        if (!File.Exists(manifestPath)) throw new FileNotFoundException("Topology icon pack manifest was not found.", manifestPath);

        var pack = TopologyIconPackJson.LoadJsonManifest(manifestPath);
        ValidateSidecarFiles(pack, packRoot);

        var validation = pack.Validate();
        if (!validation.IsValid) {
            throw new InvalidOperationException("Topology icon pack '" + pack.Id + "' has validation errors: " + string.Join("; ", validation.Errors.Select(issue => issue.Path + " " + issue.Message)));
        }

        Program.PreviewReport previews;
        if (options.ValidateOnly) {
            previews = new Program.PreviewReport(0, 0, Array.Empty<string>());
        } else {
            PrepareAuthoredSvgManifest(pack);
            previews = Program.GeneratePreviews(pack, packRoot, options.PreviewSize);
            pack.SaveJsonManifest(manifestPath);
            WriteRefreshReport(pack, packRoot, previews, options.PreviewSize);
        }

        if (!string.IsNullOrWhiteSpace(options.ContactSheetPath)) {
            WriteContactSheet(pack, packRoot, Path.GetFullPath(options.ContactSheetPath!), options);
        }

        Console.WriteLine(pack.Id + ": " + pack.Icons.Count.ToString(CultureInfo.InvariantCulture) + " icons, " + previews.Written.ToString(CultureInfo.InvariantCulture) + " preview(s), " + previews.Failed.ToString(CultureInfo.InvariantCulture) + " preview failure(s)");
        return previews.Failed == 0 ? 0 : 1;
    }

    private static void PrepareAuthoredSvgManifest(TopologyIconPack pack) {
        pack.Metadata.Remove("artwork.generator");
        pack.WithMetadata("artwork.workflow", "svg-authored");
        pack.WithMetadata("previews.refreshedBy", "ChartForgeX.Tools.IconImport --refresh-pack");
        foreach (var icon in pack.Icons) {
            if (icon.Artwork == null) continue;
            icon.Artwork.SvgBody = null;
            if (string.IsNullOrWhiteSpace(icon.Artwork.PreviewPath)) icon.Artwork.PreviewPath = "previews/" + icon.Id + ".png";
        }
    }

    private static void ValidateSidecarFiles(TopologyIconPack pack, string packRoot) {
        foreach (var icon in pack.Icons) {
            if (icon.Artwork == null || !icon.Artwork.HasSvgPath) throw new InvalidOperationException("Icon '" + icon.QualifiedId + "' does not reference artwork.svgPath.");
            ResolvePackAssetPath(packRoot, icon.Artwork.SvgPath!, icon.QualifiedId + ".artwork.svgPath", requireExists: true);
        }
    }

    private static void WriteRefreshReport(TopologyIconPack pack, string packRoot, Program.PreviewReport previews, int previewSize) {
        var reportRoot = Path.Combine(packRoot, "_reports");
        Directory.CreateDirectory(reportRoot);
        var issues = previews.Errors.Count == 0 ? "[]" : "[\"" + string.Join("\",\"", previews.Errors.Select(EscapeJson)) + "\"]";
        var text =
            "{\n" +
            "  \"refreshedAtUtc\": \"" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + "\",\n" +
            "  \"workflow\": \"svg-authored\",\n" +
            "  \"tool\": \"ChartForgeX.Tools.IconImport --refresh-pack\",\n" +
            "  \"packId\": \"" + EscapeJson(pack.Id) + "\",\n" +
            "  \"iconCount\": " + pack.Icons.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"previewSize\": " + previewSize.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"previewPngFiles\": " + previews.Written.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"previewFailures\": " + previews.Failed.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"issues\": " + issues + "\n" +
            "}\n";
        Program.WriteUtf8NoBomFile(Path.Combine(reportRoot, "refresh-report.json"), text);
    }

    private static void WriteContactSheet(TopologyIconPack pack, string packRoot, string outputPath, RefreshOptions options) {
        var icons = pack.Icons.ToList();
        var columns = Math.Max(1, options.ContactSheetColumns);
        var rows = (int)Math.Ceiling(icons.Count / (double)columns);
        var cellWidth = Math.Max(116, options.PreviewSize + 46);
        var cellHeight = Math.Max(138, options.PreviewSize + 72);
        var headerHeight = 52;
        using var bitmap = new SKBitmap(columns * cellWidth, headerHeight + rows * cellHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(248, 250, 252));

        using var titlePaint = new SKPaint {
            Color = new SKColor(15, 23, 42),
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            TextSize = 22
        };
        using var packPaint = new SKPaint {
            Color = new SKColor(71, 85, 105),
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            TextSize = 10
        };
        using var labelPaint = new SKPaint {
            Color = new SKColor(15, 23, 42),
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Segoe UI"),
            TextSize = 11
        };
        using var borderPaint = new SKPaint {
            Color = new SKColor(226, 232, 240),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };
        using var cardPaint = new SKPaint {
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawText(pack.Label + " - " + icons.Count.ToString(CultureInfo.InvariantCulture) + " icons", 20, 33, titlePaint);
        for (var index = 0; index < icons.Count; index++) {
            var icon = icons[index];
            var column = index % columns;
            var row = index / columns;
            var x = column * cellWidth + 10;
            var y = headerHeight + row * cellHeight + 10;
            var rect = new SKRect(x, y, x + cellWidth - 20, y + cellHeight - 18);
            canvas.DrawRoundRect(rect, 8, 8, cardPaint);
            canvas.DrawRoundRect(rect, 8, 8, borderPaint);
            var previewPath = ResolvePackAssetPath(packRoot, icon.Artwork?.PreviewPath ?? ("previews/" + icon.Id + ".png"), icon.QualifiedId + ".artwork.previewPath", requireExists: false);
            if (File.Exists(previewPath)) {
                RejectReparsePointPath(packRoot, previewPath, icon.QualifiedId + ".artwork.previewPath");
                using var preview = SKBitmap.Decode(previewPath);
                if (preview != null) {
                    var size = Math.Min(options.PreviewSize, Math.Min(cellWidth - 52, cellHeight - 86));
                    var imageRect = new SKRect(x + (cellWidth - size) / 2f - 10, y + 12, x + (cellWidth + size) / 2f - 10, y + 12 + size);
                    canvas.DrawBitmap(preview, imageRect);
                }
            }

            canvas.DrawText(pack.Id, x + 12, y + cellHeight - 56, packPaint);
            canvas.DrawText(Trim(icon.Label, 24), x + 12, y + cellHeight - 35, labelPaint);
        }

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory)) Directory.CreateDirectory(outputDirectory!);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(outputPath);
        data.SaveTo(stream);
    }

    private static string ResolvePackAssetPath(string packRoot, string assetPath, string pathName, bool requireExists) {
        if (!TopologyIconArtwork.IsSafeAssetPath(assetPath)) throw new ArgumentException("Unsafe topology icon asset path: " + pathName);
        var baseDirectory = Path.GetFullPath(packRoot);
        var normalizedAssetPath = assetPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, normalizedAssetPath));
        var baseWithSeparator = AppendDirectorySeparator(baseDirectory);
        if (!fullPath.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Topology icon asset path escapes its pack directory: " + pathName);
        if (requireExists && !File.Exists(fullPath)) throw new FileNotFoundException("Topology icon asset was not found.", fullPath);
        if (File.Exists(fullPath)) RejectReparsePointPath(baseDirectory, fullPath, pathName);
        return fullPath;
    }

    private static void RejectReparsePointPath(string baseDirectory, string fullPath, string pathName) {
        var current = Path.GetFullPath(baseDirectory);
        var relative = MakeRelativePath(current, fullPath);
        foreach (var part in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)) {
            current = Path.Combine(current, part);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("Topology icon asset path uses a reparse point: " + pathName);
        }
    }

    private static string MakeRelativePath(string baseDirectory, string fullPath) {
        var baseUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(baseDirectory)), UriKind.Absolute);
        var fileUri = new Uri(Path.GetFullPath(fullPath), UriKind.Absolute);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparator(string path) {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? path : path + Path.DirectorySeparatorChar;
    }

    private static string Trim(string value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 1) + "...";
    }

    private static string EscapeJson(string value) {
        var builder = new System.Text.StringBuilder(value.Length + 8);
        foreach (var character in value) {
            switch (character) {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(character)) {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    } else {
                        builder.Append(character);
                    }
                    break;
            }
        }
        return builder.ToString();
    }

    internal sealed class RefreshOptions {
        public string PackDirectory { get; private set; } = string.Empty;
        public int PreviewSize { get; private set; } = 128;
        public bool ValidateOnly { get; private set; }
        public string? ContactSheetPath { get; private set; }
        public int ContactSheetColumns { get; private set; } = 8;

        public static RefreshOptions Parse(string[] args) {
            var options = new RefreshOptions();
            for (var index = 0; index < args.Length; index++) {
                switch (args[index]) {
                    case "--refresh-pack":
                        options.PackDirectory = RequiredValue(args, ref index, "--refresh-pack");
                        break;
                    case "--preview-size":
                        options.PreviewSize = int.Parse(RequiredValue(args, ref index, "--preview-size"), CultureInfo.InvariantCulture);
                        break;
                    case "--validate-only":
                        options.ValidateOnly = true;
                        break;
                    case "--contact-sheet":
                        options.ContactSheetPath = RequiredValue(args, ref index, "--contact-sheet");
                        break;
                    case "--contact-sheet-columns":
                        options.ContactSheetColumns = int.Parse(RequiredValue(args, ref index, "--contact-sheet-columns"), CultureInfo.InvariantCulture);
                        break;
                    default:
                        throw new ArgumentException("Unknown refresh-pack argument: " + args[index]);
                }
            }

            if (string.IsNullOrWhiteSpace(options.PackDirectory)) throw new ArgumentException("Missing --refresh-pack.");
            if (options.PreviewSize < 32 || options.PreviewSize > 512) throw new ArgumentException("--preview-size must be between 32 and 512.");
            if (options.ContactSheetColumns < 1 || options.ContactSheetColumns > 20) throw new ArgumentException("--contact-sheet-columns must be between 1 and 20.");
            return options;
        }

        private static string RequiredValue(string[] args, ref int index, string name) {
            if (index + 1 >= args.Length) throw new ArgumentException("Missing value for " + name + ".");
            index++;
            return args[index];
        }
    }
}
