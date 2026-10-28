using System;
using System.Threading;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    /// <summary>Exports SVG with operation cancellation and the selected typed effect representation.</summary>
    /// <remarks>The token is never retained. Legacy font/geometry paint and encoding retain their existing synchronous lifetime.</remarks>
    public string ToSvg(VisualSvgOptions options, CancellationToken cancellationToken) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        cancellationToken.ThrowIfCancellationRequested();
        return VisualSceneSvgRenderer.Render(_scene, _accessibility.Name, _accessibility.Description,
            _accessibility.Language, _accessibility.IsDecorative, options.IdPrefix, options, _capturePngText, cancellationToken);
    }

    /// <summary>Exports native PNG with cancellation at native scopes and effect rows, without changing prepared facts.</summary>
    /// <remarks>Cancellation is checked before and after existing encoding; it does not interrupt every synchronous encoder/font instruction.</remarks>
    public byte[] ToPng(VisualRenderOptions? options, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        options ??= new VisualRenderOptions();
        var image = VisualSceneRasterRenderer.Render(_scene, options.Scale, options.Supersampling,
            options.PixelBudget, options.TextHinting, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var encoded = PngWriter.WriteRgba(image);
        cancellationToken.ThrowIfCancellationRequested();
        return encoded;
    }
}
