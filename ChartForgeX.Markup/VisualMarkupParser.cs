using System;
using System.Collections.Generic;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Markup;

/// <summary>
/// Parses Markdown visual fences into product-neutral ChartForgeX visual artifacts.
/// </summary>
public sealed class VisualMarkupParser {
    private readonly List<IVisualMarkupBlockParser> _blockParsers;

    /// <summary>
    /// Initializes a visual markup parser with built-in ChartForgeX block support.
    /// </summary>
    public VisualMarkupParser() {
        _blockParsers = new List<IVisualMarkupBlockParser>();
    }

    /// <summary>
    /// Initializes a visual markup parser with optional block parsers such as Mermaid adapters.
    /// </summary>
    /// <param name="blockParsers">Optional block parsers for visual languages outside the core markup package.</param>
    public VisualMarkupParser(IEnumerable<IVisualMarkupBlockParser> blockParsers) {
        if (blockParsers == null) throw new ArgumentNullException(nameof(blockParsers));
        _blockParsers = new List<IVisualMarkupBlockParser>(blockParsers);
    }

    /// <summary>
    /// Initializes a visual markup parser with optional block parsers such as Mermaid adapters.
    /// </summary>
    /// <param name="blockParsers">Optional block parsers for visual languages outside the core markup package.</param>
    public VisualMarkupParser(params IVisualMarkupBlockParser[] blockParsers) : this((IEnumerable<IVisualMarkupBlockParser>)blockParsers) {
    }

    /// <summary>
    /// Parses supported Markdown visual fences into typed visual artifacts and line-aware diagnostics.
    /// </summary>
    /// <param name="text">The Markdown text.</param>
    /// <returns>The visual markup parse result.</returns>
    public VisualMarkupParseResult Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var scan = VisualMarkupScanner.Scan(text);
        var result = new VisualMarkupParseResult();
        foreach (var diagnostic in scan.Diagnostics) result.Diagnostics.Add(diagnostic);
        ParseBlocks(result, scan.Blocks);
        return result;
    }

    /// <summary>
    /// Parses visual blocks that were already discovered by another Markdown parser into typed visual artifacts.
    /// </summary>
    /// <param name="blocks">Pre-scanned visual blocks, for example blocks discovered by a host Markdown pipeline.</param>
    /// <returns>The visual markup parse result.</returns>
    public VisualMarkupParseResult ParseBlocks(IEnumerable<VisualMarkupBlock> blocks) {
        if (blocks == null) throw new ArgumentNullException(nameof(blocks));
        var result = new VisualMarkupParseResult();
        ParseBlocks(result, blocks);
        return result;
    }

    private void ParseBlocks(VisualMarkupParseResult result, IEnumerable<VisualMarkupBlock> blocks) {
        foreach (var block in blocks) ParseBlock(result, block);
    }

    private void ParseBlock(VisualMarkupParseResult result, VisualMarkupBlock block) {
        if (block == null) throw new ArgumentException("Block collection cannot contain null values.", nameof(block));

        foreach (var parser in _blockParsers) {
            if (!parser.CanParse(block)) continue;
            parser.Parse(block, result);
            return;
        }

        switch (block.Kind) {
            case VisualMarkupKind.Topology:
                ParseTopology(result, block);
                break;
            case VisualMarkupKind.Table:
                ParseTable(result, block);
                break;
            case VisualMarkupKind.Chart:
                ParseChart(result, block);
                break;
            case VisualMarkupKind.Flow:
            case VisualMarkupKind.Timeline:
            case VisualMarkupKind.Mermaid:
                Add(result, block.FenceLine, MarkupDiagnosticSeverity.Warning, "Parser dispatch for '" + block.FenceName + "' visual fences is not implemented yet.");
                break;
            default:
                Add(result, block.FenceLine, MarkupDiagnosticSeverity.Warning, "Parser dispatch for visual kind '" + block.Kind + "' is not implemented.");
                break;
        }
    }

    private static void ParseTopology(VisualMarkupParseResult result, VisualMarkupBlock block) {
        var topologyResult = new MarkupTopologyParser().Parse(block.Payload);
        foreach (var diagnostic in topologyResult.Diagnostics) {
            result.Diagnostics.Add(new MarkupDiagnostic {
                Line = diagnostic.Line <= 0 ? block.FenceLine : block.StartLine + diagnostic.Line - 1,
                Severity = diagnostic.Severity,
                Message = diagnostic.Message
            });
        }

        if (topologyResult.HasErrors || topologyResult.Document == null) return;
        var document = topologyResult.Document;
        var chart = document.ToTopologyChart();
        var artifactId = string.IsNullOrWhiteSpace(document.Id) ? "topology-" + (result.Artifacts.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) : document.Id!;
        var artifact = VisualArtifact.Create(artifactId, VisualArtifactKind.Topology, chart);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.ChartForgeX;
        artifact.Title = document.Title ?? string.Empty;
        artifact.Subtitle = document.Subtitle ?? string.Empty;
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html;
        artifact.Metadata["fence"] = block.FenceName;
        artifact.Metadata["sourceLine"] = block.FenceLine.ToString(System.Globalization.CultureInfo.InvariantCulture);
        result.Artifacts.Add(artifact);
    }

    private static void ParseTable(VisualMarkupParseResult result, VisualMarkupBlock block) {
        var tableResult = new MarkupTableParser().Parse(block.Payload);
        foreach (var diagnostic in tableResult.Diagnostics) {
            result.Diagnostics.Add(new MarkupDiagnostic {
                Line = diagnostic.Line <= 0 ? block.FenceLine : block.StartLine + diagnostic.Line - 1,
                Severity = diagnostic.Severity,
                Message = diagnostic.Message
            });
        }

        if (tableResult.HasErrors || tableResult.Document == null) return;
        var table = tableResult.Document;
        var artifact = table.ToVisualArtifact();
        artifact.SourceLanguage = VisualArtifactSourceLanguage.ChartForgeX;
        artifact.Metadata["fence"] = block.FenceName;
        artifact.Metadata["sourceLine"] = block.FenceLine.ToString(System.Globalization.CultureInfo.InvariantCulture);
        result.Artifacts.Add(artifact);
    }

    private static void ParseChart(VisualMarkupParseResult result, VisualMarkupBlock block) {
        var chartResult = new MarkupChartParser().Parse(block.Payload);
        foreach (var diagnostic in chartResult.Diagnostics) {
            result.Diagnostics.Add(new MarkupDiagnostic {
                Line = diagnostic.Line <= 0 ? block.FenceLine : block.StartLine + diagnostic.Line - 1,
                Severity = diagnostic.Severity,
                Message = diagnostic.Message
            });
        }

        if (chartResult.HasErrors || chartResult.Document == null) return;
        var document = chartResult.Document;
        var artifact = document.Chart.ToVisualArtifact(document.Id, VisualArtifactKind.Chart, VisualArtifactSourceLanguage.ChartForgeX);
        artifact.Metadata["fence"] = block.FenceName;
        artifact.Metadata["sourceLine"] = block.FenceLine.ToString(System.Globalization.CultureInfo.InvariantCulture);
        result.Artifacts.Add(artifact);
    }

    private static void Add(VisualMarkupParseResult result, int line, MarkupDiagnosticSeverity severity, string message) {
        result.Diagnostics.Add(new MarkupDiagnostic {
            Line = line,
            Severity = severity,
            Message = message
        });
    }
}
