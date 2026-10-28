using System;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Typography;

public sealed partial class FontSpec {
    private CapturedSelection? _capturedSelection;
    internal VisualSceneTextCapture? NativeTextCapture => Selection?.Capture;
    internal TrueTypeFont? OwnedFileFont => Selection?.Root;
    internal bool CapturedExplicitFile => Selection?.ExplicitFile == true;
    private CapturedSelection? Selection => _capturedSelection is {} selection && selection.Matches(this) ? selection : null;
    internal string? CapturedFileKey => CapturedExplicitFile ? OwnedFileFont?.CapturedByteIdentity ?? "unavailable-explicit-file" : null;

    // Only a compiler-owned clone calls this. Caller options/contexts never receive the recording reference.
    internal FontSpec WithCapturedText(VisualSceneTextCapture capture) {
        if (capture == null) throw new ArgumentNullException(nameof(capture));
        var copy = Clone(); var root = OwnedFileFont;
        if (root == null && _capturedSelection is {} previous && previous.SameFile(this) && previous.Root != null)
            root = previous.Root.SelectCapturedFace(CollectionIndex, FaceName);
        if (root == null && FilePath != null) root = TrueTypeFont.TryLoadFromPath(FilePath, CollectionIndex, FaceName);
        var explicitFile = FilePath != null || _capturedSelection is {} captured && captured.SameFile(this) && captured.ExplicitFile;
        copy._capturedSelection = new CapturedSelection(copy, capture, root, explicitFile);
        return copy;
    }
    internal FontSpec WithoutCapturedText() {
        var copy = Clone(); var previous = Selection;
        copy._capturedSelection = previous == null ? null : new CapturedSelection(copy, null, previous.Root, previous.ExplicitFile);
        return copy;
    }
    // The codec passes an already-owned immutable parsed root. No imported fact carries a path or registers a family.
    internal FontSpec WithOwnedFile(TrueTypeFont? root, bool explicitFile) {
        var copy = Clone(); copy._capturedSelection = new CapturedSelection(copy, null, root, explicitFile); return copy;
    }
    private sealed class CapturedSelection {
        private readonly string? _file, _face;
        private readonly int? _collection;
        internal CapturedSelection(FontSpec request, VisualSceneTextCapture? capture, TrueTypeFont? root, bool explicitFile) {
            _file = request.FilePath; _face = request.FaceName; _collection = request.CollectionIndex;
            Capture = capture; Root = root; ExplicitFile = explicitFile;
        }
        internal VisualSceneTextCapture? Capture { get; }
        internal TrueTypeFont? Root { get; }
        internal bool ExplicitFile { get; }
        internal bool SameFile(FontSpec request) => string.Equals(_file, request.FilePath, StringComparison.Ordinal);
        internal bool Matches(FontSpec request) => SameFile(request)
            && string.Equals(_face, request.FaceName, StringComparison.Ordinal) && _collection == request.CollectionIndex;
    }
}
