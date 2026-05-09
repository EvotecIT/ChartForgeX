using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines options for importing a folder of SVG files as a topology icon pack.
/// </summary>
public sealed class TopologyIconSvgPackImportOptions {
    /// <summary>Gets or sets the imported pack id. When omitted, the source directory name is used.</summary>
    public string? PackId { get; set; }

    /// <summary>Gets or sets the imported pack label. When omitted, the source directory name is humanized.</summary>
    public string? PackLabel { get; set; }

    /// <summary>Gets or sets the optional vendor label.</summary>
    public string? Vendor { get; set; }

    /// <summary>Gets or sets the optional pack version.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the source repository or package URL.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Gets or sets the source repository commit, tag, or release identifier.</summary>
    public string? SourceRevision { get; set; }

    /// <summary>Gets or sets the source license label.</summary>
    public string? SourceLicense { get; set; }

    /// <summary>Gets or sets the source license URL.</summary>
    public string? SourceLicenseUrl { get; set; }

    /// <summary>Gets or sets the source license file path, when available.</summary>
    public string? SourceLicensePath { get; set; }

    /// <summary>Gets or sets whether child folders should be scanned.</summary>
    public bool Recursive { get; set; } = true;

    /// <summary>Gets or sets the SVG file search pattern.</summary>
    public string SearchPattern { get; set; } = "*.svg";

    /// <summary>Gets or sets the default topology node kind for imported artwork icons.</summary>
    public TopologyNodeKind DefaultNodeKind { get; set; } = TopologyNodeKind.Application;

    /// <summary>Gets or sets the default renderer-owned fallback shape for imported artwork icons.</summary>
    public TopologyIconShape DefaultShape { get; set; } = TopologyIconShape.Application;

    /// <summary>Gets or sets the default icon color used when a host falls back to renderer-owned glyphs.</summary>
    public string DefaultColor { get; set; } = "#0078D4";

    /// <summary>Gets or sets whether folders named SVG should be omitted from generated categories.</summary>
    public bool OmitSvgFolderFromCategory { get; set; } = true;

    /// <summary>Gets or sets whether unsafe SVG files should be skipped instead of imported as validation errors.</summary>
    public bool SkipUnsafeSvg { get; set; } = true;
}

/// <summary>
/// Describes one file processed by the SVG icon-pack importer.
/// </summary>
public sealed class TopologyIconSvgPackImportFile {
    internal TopologyIconSvgPackImportFile(string sourcePath, string relativePath, string? iconId, string? category, bool imported, string? message) {
        SourcePath = sourcePath;
        RelativePath = relativePath;
        IconId = iconId;
        Category = category;
        Imported = imported;
        Message = message;
    }

    /// <summary>Gets the absolute SVG source path.</summary>
    public string SourcePath { get; }

    /// <summary>Gets the source path relative to the imported folder.</summary>
    public string RelativePath { get; }

    /// <summary>Gets the imported icon id, when the file produced an icon.</summary>
    public string? IconId { get; }

    /// <summary>Gets the inferred icon category.</summary>
    public string? Category { get; }

    /// <summary>Gets whether the file was imported.</summary>
    public bool Imported { get; }

    /// <summary>Gets a skip or error message, when available.</summary>
    public string? Message { get; }
}

/// <summary>
/// Contains the icon pack and diagnostics produced by an SVG folder import.
/// </summary>
public sealed class TopologyIconSvgPackImportResult {
    internal TopologyIconSvgPackImportResult(TopologyIconPack pack, IReadOnlyList<TopologyIconSvgPackImportFile> files) {
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
        Files = files ?? throw new ArgumentNullException(nameof(files));
    }

    /// <summary>Gets the imported icon pack.</summary>
    public TopologyIconPack Pack { get; }

    /// <summary>Gets per-file import diagnostics.</summary>
    public IReadOnlyList<TopologyIconSvgPackImportFile> Files { get; }

    /// <summary>Gets the number of imported SVG files.</summary>
    public int ImportedCount => Files.Count(file => file.Imported);

    /// <summary>Gets the number of skipped SVG files.</summary>
    public int SkippedCount => Files.Count(file => !file.Imported);

    /// <summary>Gets whether any file was skipped or failed.</summary>
    public bool HasSkippedFiles => SkippedCount > 0;
}

/// <summary>
/// Imports dependency-free SVG artwork into topology icon packs.
/// </summary>
public static class TopologyIconSvgPackImporter {
    /// <summary>
    /// Imports SVG files from a folder into a topology icon pack.
    /// </summary>
    /// <param name="directoryPath">The source directory containing SVG files.</param>
    /// <param name="options">Optional import settings and provenance metadata.</param>
    /// <returns>The imported pack plus per-file diagnostics.</returns>
    public static TopologyIconSvgPackImportResult ImportSvgPackFromDirectory(string directoryPath, TopologyIconSvgPackImportOptions? options = null) {
        if (string.IsNullOrWhiteSpace(directoryPath)) throw new ArgumentException("Value cannot be empty.", nameof(directoryPath));
        if (!Directory.Exists(directoryPath)) throw new DirectoryNotFoundException("Topology icon SVG import directory was not found: " + directoryPath);
        options ??= new TopologyIconSvgPackImportOptions();
        TopologyModelGuards.EnumDefined(options.DefaultNodeKind, nameof(options.DefaultNodeKind));
        TopologyModelGuards.EnumDefined(options.DefaultShape, nameof(options.DefaultShape));
        if (string.IsNullOrWhiteSpace(options.SearchPattern)) throw new ArgumentException("Value cannot be empty.", nameof(options.SearchPattern));

        var root = Path.GetFullPath(directoryPath);
        var pack = CreatePack(root, options);
        var files = new List<TopologyIconSvgPackImportFile>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.GetFiles(root, options.SearchPattern, options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase)) {
            var relativePath = NormalizePath(MakeRelativePath(root, Path.GetFullPath(path)));
            var category = InferCategory(relativePath, options);
            try {
                var imported = ImportSvg(path);
                if (options.SkipUnsafeSvg && !imported.IsSafe) {
                    files.Add(new TopologyIconSvgPackImportFile(path, relativePath, null, category, imported: false, "SVG artwork contains unsafe content."));
                    continue;
                }

                var baseId = StableToken(Path.GetFileNameWithoutExtension(path));
                var id = UniqueId(baseId, ids);
                var icon = new TopologyIconDefinition(pack.Id, id, Humanize(Path.GetFileNameWithoutExtension(path)), options.DefaultNodeKind, options.DefaultShape) {
                    Category = category,
                    Color = options.DefaultColor,
                    DisplayMode = TopologyNodeDisplayMode.Tile,
                    Symbol = SymbolFromLabel(Path.GetFileNameWithoutExtension(path)),
                    Artwork = TopologyIconArtwork.InlineSvg(imported.SvgBody, imported.ViewBox)
                }.WithTags(TagsFor(relativePath, category).ToArray())
                    .WithMetadata("source.path", relativePath)
                    .WithMetadata("source.fileName", Path.GetFileName(path))
                    .WithMetadata("source.viewBox", imported.ViewBox);
                if (!string.IsNullOrWhiteSpace(options.SourceRevision)) icon.WithMetadata("source.revision", options.SourceRevision!);
                pack.AddIcon(icon);
                files.Add(new TopologyIconSvgPackImportFile(path, relativePath, id, category, imported: true, null));
            } catch (Exception exception) when (exception is XmlException || exception is InvalidOperationException || exception is ArgumentException) {
                files.Add(new TopologyIconSvgPackImportFile(path, relativePath, null, category, imported: false, exception.Message));
            }
        }

        pack.WithMetadata("import.svg.files", files.Count.ToString(CultureInfo.InvariantCulture));
        pack.WithMetadata("import.svg.imported", files.Count(file => file.Imported).ToString(CultureInfo.InvariantCulture));
        pack.WithMetadata("import.svg.skipped", files.Count(file => !file.Imported).ToString(CultureInfo.InvariantCulture));
        return new TopologyIconSvgPackImportResult(pack, files);
    }

    private static TopologyIconPack CreatePack(string root, TopologyIconSvgPackImportOptions options) {
        var sourceName = new DirectoryInfo(root).Name;
        var id = string.IsNullOrWhiteSpace(options.PackId) ? StableToken(sourceName) : StableToken(options.PackId!);
        var label = string.IsNullOrWhiteSpace(options.PackLabel) ? Humanize(sourceName) : options.PackLabel!.Trim();
        var pack = new TopologyIconPack(id, label, options.Vendor, options.Version)
            .WithTags("svg", "imported", "stencil")
            .WithMetadata("import.kind", "svg-folder")
            .WithMetadata("import.sourceDirectory", root);
        AddOptional(pack, "source.url", options.SourceUrl);
        AddOptional(pack, "source.revision", options.SourceRevision);
        AddOptional(pack, "source.license", options.SourceLicense);
        AddOptional(pack, "source.licenseUrl", options.SourceLicenseUrl);
        AddOptional(pack, "source.licensePath", options.SourceLicensePath);
        return pack;
    }

    private static ImportedSvg ImportSvg(string path) {
        var settings = new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var stream = File.OpenRead(path);
        using var reader = XmlReader.Create(stream, settings);
        var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new ArgumentException("SVG file does not contain a root element.");
        if (!string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("SVG file root element is not <svg>.");
        var isSafe = TopologyIconArtwork.IsSafeSvgFragment(string.Concat(root.Nodes().Select(node => node.ToString(SaveOptions.DisableFormatting))));
        var viewBox = ReadViewBox(root);
        RemoveNonArtworkElements(root);
        var body = string.Concat(root.Nodes().Select(node => node.ToString(SaveOptions.DisableFormatting))).Trim();
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("SVG file does not contain drawable artwork.");
        return new ImportedSvg(viewBox, body, isSafe);
    }

    private static void RemoveNonArtworkElements(XElement root) {
        var removable = root.Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "metadata", StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.Name.LocalName, "title", StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.Name.LocalName, "desc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.Name.LocalName, "script", StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var element in removable) element.Remove();
    }

    private static string ReadViewBox(XElement root) {
        var viewBox = root.Attribute("viewBox")?.Value ?? root.Attribute("viewbox")?.Value;
        if (!string.IsNullOrWhiteSpace(viewBox)) return viewBox!.Trim();
        var width = ReadLength(root.Attribute("width")?.Value);
        var height = ReadLength(root.Attribute("height")?.Value);
        return width > 0 && height > 0
            ? "0 0 " + width.ToString("0.###", CultureInfo.InvariantCulture) + " " + height.ToString("0.###", CultureInfo.InvariantCulture)
            : "0 0 48 48";
    }

    private static double ReadLength(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var text = new string(value!.Trim().TakeWhile(ch => char.IsDigit(ch) || ch == '.' || ch == '-').ToArray());
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    private static string? InferCategory(string relativePath, TopologyIconSvgPackImportOptions options) {
        var parts = relativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count <= 1) return null;
        parts.RemoveAt(parts.Count - 1);
        if (options.OmitSvgFolderFromCategory) parts.RemoveAll(part => string.Equals(part, "svg", StringComparison.OrdinalIgnoreCase));
        return parts.Count == 0 ? null : string.Join(" / ", parts.Select(Humanize));
    }

    private static IEnumerable<string> TagsFor(string relativePath, string? category) {
        foreach (var part in relativePath.Split(new[] { '/', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)) {
            var token = StableToken(part);
            if (token.Length > 1) yield return token;
        }

        if (!string.IsNullOrWhiteSpace(category)) {
            foreach (var part in category!.Split(new[] { '/', ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                var token = StableToken(part);
                if (token.Length > 1) yield return token;
            }
        }
    }

    private static string UniqueId(string baseId, HashSet<string> ids) {
        var id = string.IsNullOrWhiteSpace(baseId) ? "icon" : baseId;
        if (ids.Add(id)) return id;
        for (var index = 2; ; index++) {
            var candidate = id + "-" + index.ToString(CultureInfo.InvariantCulture);
            if (ids.Add(candidate)) return candidate;
        }
    }

    private static string StableToken(string value) {
        var builder = new StringBuilder(value.Length);
        var previousDash = false;
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch)) {
                builder.Append(char.ToLowerInvariant(ch));
                previousDash = false;
            } else if (!previousDash) {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string Humanize(string value) {
        var token = value.Replace('_', ' ').Replace('-', ' ').Trim();
        if (token.Length == 0) return "Icon";
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(token.ToLowerInvariant());
    }

    private static string SymbolFromLabel(string value) {
        var words = value.Split(new[] { '-', '_', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
        var symbol = new string(words.Select(word => char.ToUpperInvariant(word[0])).Take(4).ToArray());
        return symbol.Length == 0 ? "SVG" : symbol;
    }

    private static string MakeRelativePath(string root, string path) {
        var rootUri = new Uri(AppendDirectorySeparator(root));
        var pathUri = new Uri(path);
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString());
    }

    private static string AppendDirectorySeparator(string path) {
        var separator = Path.DirectorySeparatorChar.ToString();
        return path.EndsWith(separator, StringComparison.Ordinal) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? path : path + separator;
    }

    private static string NormalizePath(string path) {
        return path.Replace('\\', '/');
    }

    private static void AddOptional(TopologyIconPack pack, string key, string? value) {
        if (!string.IsNullOrWhiteSpace(value)) pack.WithMetadata(key, value!.Trim());
    }

    private readonly struct ImportedSvg {
        public ImportedSvg(string viewBox, string svgBody, bool isSafe) {
            ViewBox = viewBox;
            SvgBody = svgBody;
            IsSafe = isSafe;
        }

        public string ViewBox { get; }
        public string SvgBody { get; }
        public bool IsSafe { get; }
    }
}
