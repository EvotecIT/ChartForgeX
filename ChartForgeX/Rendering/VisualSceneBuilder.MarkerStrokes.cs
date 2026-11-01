using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    private HashSet<VisualSceneMark>? _capturedMarkerStrokes;
    internal int NodeCount => _nodes.Count;

    /// <summary>Records exact native marker instances whose item outline was resolved before layout and paint.</summary>
    internal void CaptureMarkerStroke(int first) {
        for (var index = first; index < _nodes.Count; index++)
            if (_nodes[index] is VisualSceneMark mark)
                (_capturedMarkerStrokes ??= new HashSet<VisualSceneMark>()).Add(mark);
    }
}
