namespace ChartForgeX.VisualArtifacts;

/// <summary>An optional policy around existing artifact exports, without replacing their rendering or encoding owners.</summary>
public interface IVisualArtifactExportPolicy {
    /// <summary>Checks a request before SVG/HTML rendering or native raster allocation.</summary>
    /// <remarks>A policy can inspect completed SVG from a raster check. SVG checks must not recursively request
    /// another SVG export. Implementations throw on rejection; native rendering options remain authoritative.</remarks>
    void BeforeExport(VisualArtifact artifact, VisualArtifactExportFormat format, VisualArtifactRenderOptions? options);
    /// <summary>Checks a completed export. A null byte count marks native pixels before format encoding.</summary>
    void ValidateOutput(VisualArtifactExportFormat format, long? byteCount);
}
