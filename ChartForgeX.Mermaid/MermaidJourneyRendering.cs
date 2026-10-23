using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>Maps parsed Mermaid Journey notation to its reusable native diagram.</summary>
public static class MermaidJourneyRendering {
    /// <summary>Creates the native diagram, retaining authored ordering and section identity.</summary>
    public static JourneyDiagramBlock ToJourneyDiagramBlock(this MermaidJourneyDocument document, MermaidJourneyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidJourneyRenderOptions();
        var block = JourneyDiagramBlock.Create().WithTitle(MermaidHeading.Title(document, options.Title, document.Title, "Mermaid journey"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle)).WithSize(options.Width, options.Height);
        for (var index = 0; index < document.Sections.Count; index++) block.AddSection("section-" + index.ToString(CultureInfo.InvariantCulture), document.Sections[index].Name);
        for (var index = 0; index < document.Tasks.Count; index++) {
            var item = document.Tasks[index];
            var section = item.Section == null ? -1 : document.Sections.IndexOf(item.Section);
            if (item.Section != null && section < 0) throw new ArgumentException("A journey item references a section outside its document.", nameof(document));
            block.AddTask("task-" + index.ToString(CultureInfo.InvariantCulture), item.Text, item.Score, item.Actors, section < 0 ? null : "section-" + section.ToString(CultureInfo.InvariantCulture));
        }
        return MermaidPresentation.Apply(block, document, options.Presentation);
    }

    /// <summary>Wraps the native diagram in the shared bounded Mermaid artifact policy.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidJourneyDocument document, MermaidJourneyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidJourneyRenderOptions();
        var block = document.ToJourneyDiagramBlock(options);
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-journey" : options.Id!.Trim();
        var artifact = VisualArtifact.Create(id, VisualArtifactKind.Mermaid, block);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid; artifact.Title = block.Title; artifact.Subtitle = block.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString(); artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.sections"] = document.Sections.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.tasks"] = document.Tasks.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.actors"] = document.Tasks.SelectMany(task => task.Actors).Distinct(StringComparer.Ordinal).Count().ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(JourneyDiagramBlock);
        return MermaidPresentation.Apply(artifact, document);
    }
    /// <summary>Renders native static SVG.</summary>
    public static string ToSvg(this MermaidJourneyDocument document, MermaidJourneyRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();
    /// <summary>Renders native PNG from the same composed scene.</summary>
    public static byte[] ToPng(this MermaidJourneyDocument document, MermaidJourneyRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();
}

/// <summary>Defines the natural native diagram frame and shared presentation.</summary>
public sealed class MermaidJourneyRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the diagram width.</summary>
    public int Width { get; set; } = 960;
    /// <summary>Gets or sets the diagram height.</summary>
    public int Height { get; set; } = 560;
}
