namespace ChartForgeX.Rendering;
/// <summary>Serialization policy for retained typed native scene effects. Ordinary scenes and explicit baked image layers retain their existing representation.</summary>
public enum VisualSvgEffectProfile {
    /// <summary>Paints shadow-only native pixels behind vector surfaces; an explicitly masked complete object is a static native image.</summary>
    Baked,
    /// <summary>Emits supported finite script-free filters/masks on retained source vectors. Browser Gaussian pixels are not promised identical to the native three-box kernel.</summary>
    NativeFilters
}
