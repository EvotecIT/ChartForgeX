using System;
using System.Globalization;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>Maps every railroad frontend to one native grammar model and rendering owner.</summary>
public static class MermaidRailroadRendering {
    /// <summary>Creates a naturally measured native diagram without expanding recursive rule references or repetition counts.</summary>
    public static RailroadDiagramBlock ToRailroadDiagramBlock(this MermaidRailroadDocument document, MermaidRailroadRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document)); options ??= new MermaidRailroadRenderOptions();
        if (!Enum.IsDefined(typeof(MermaidRailroadDialect), document.Dialect)) throw new ArgumentOutOfRangeException(nameof(document), "Unknown railroad source dialect.");
        var block = RailroadDiagramBlock.Create().WithTitle(MermaidHeading.Title(document, options.Title, document.Title, "Railroad grammar"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle));
        block.RuleComparison = document.Dialect == MermaidRailroadDialect.Abnf ? RailroadRuleComparison.OrdinalIgnoreCase : RailroadRuleComparison.Ordinal;
        foreach (var rule in document.Rules) block.AddRule(rule.Id, rule.Definition.Expression);
        MermaidPresentation.Apply(block, document, options.Presentation);
        return block.WithNaturalSize(options.Width, options.Height);
    }
    /// <summary>Wraps the measured native producer in the existing bounded Mermaid artifact policy.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidRailroadDocument document, MermaidRailroadRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document)); options ??= new MermaidRailroadRenderOptions();
        var block = document.ToRailroadDiagramBlock(options);
        var artifact = VisualArtifact.Create(string.IsNullOrWhiteSpace(options.Id) ? "mermaid-railroad" : options.Id!.Trim(), VisualArtifactKind.Mermaid, block);
        artifact.Title = block.Title; artifact.Subtitle = block.Subtitle; artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height); artifact.HtmlSizing = VisualArtifactHtmlSizing.PreserveSize;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString(); artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.railroadDialect"] = document.Dialect.ToString(); artifact.Metadata["mermaid.rules"] = document.Rules.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(RailroadDiagramBlock);
        return MermaidPresentation.Apply(artifact, document);
    }
    /// <summary>Renders the shared native SVG scene.</summary>
    public static string ToSvg(this MermaidRailroadDocument document, MermaidRailroadRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();
    /// <summary>Renders PNG through the same native scene and captured output policy.</summary>
    public static byte[] ToPng(this MermaidRailroadDocument document, MermaidRailroadRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();
}

/// <summary>Defines minimum natural canvas dimensions and shared presentation for railroad diagrams.</summary>
public sealed class MermaidRailroadRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the minimum natural width. Grammar geometry may require more space.</summary>
    public int Width { get; set; } = 720;
    /// <summary>Gets or sets the minimum natural height. Grammar geometry may require more space.</summary>
    public int Height { get; set; } = 320;
}
