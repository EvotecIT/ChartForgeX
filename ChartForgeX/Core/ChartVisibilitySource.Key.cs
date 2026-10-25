using System;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Core;

internal sealed partial class ChartVisibilitySource {
    internal string CanonicalFacts(VisualRenderContext context) => VisualArtifactInterchangeJson.SerializeVisibilitySource(this, context);
    /// <summary>Binds complete copied authored facts and the actual native full-source paint, including captured glyph resources.</summary>
    internal string SourceKey(VisualRenderContext context, PreparedVisual complete) {
        var facts = CanonicalFacts(context);
        if (_pie != null || _hierarchy != null) facts += "\0" + (complete.NativeTextCapture?.CanonicalFacts(includeRuns: true)
            ?? throw new NotSupportedException("Radial Reflow needs the complete native measured-font capture."));
        var paint = complete.ForInteractivePngExport().ToSvg("native-visibility-source-paint");
        return BindNativeSourceKey(facts, paint);
    }
    /// <summary>Adds exact ordered appearance facts and completed native paint to the same detached authored-source key.</summary>
    internal static string AppearanceSourceKey(string bareSourceKey, PreparedVisual complete) =>
        BindNativeSourceKey(bareSourceKey + "\0" + complete.CapturedNativeIdentity,
            complete.ForInteractivePngExport().ToSvg("native-visibility-source-paint"));

    private static string BindNativeSourceKey(string facts, string paint) {
        return HashNativeSource(facts + "\0" + paint);
    }
    // Raw complete replay retains the same immutable request/cluster catalog; measured replay visits need not duplicate recording-only latent observations.
    internal string RawValueSourceKey(VisualRenderContext context, VisualSceneTextCapture text, PreparedVisual complete) => BindNativeSourceKey(
        CanonicalFacts(context) + "\0" + text.CanonicalFacts(includeRuns: false),
        complete.ForInteractivePngExport().ToSvg("native-visibility-source-paint"));
    internal string ValueStructureKey(VisualRenderContext context, VisualSceneTextCapture text) => HashNativeSource(
        VisualArtifactInterchangeJson.SerializeVisibilitySource(this, context, includeValues: false, includeDerivedMembership: false) + "\0" + text.CanonicalFacts(includeRuns: false));
    private static string HashNativeSource(string facts) {
        using var sha = SHA256.Create(); var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(facts));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }

}
