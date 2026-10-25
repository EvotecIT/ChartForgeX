using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

/// <summary>One balanced-scope membership map shared by native compositing and finite effect serialization.</summary>
internal static class VisualSceneScopes {
    internal static int[] Ends(VisualScene scene) {
        var ends = new int[scene.Nodes.Count];
        var groups = new Stack<int>();
        for (var index = 0; index < scene.Nodes.Count; index++) {
            if (scene.Nodes[index] is VisualSceneGroup) {
                groups.Push(index);
            } else if (scene.Nodes[index] is VisualSceneEndGroup) {
                if (groups.Count == 0) throw new ArgumentException("Native scopes require balanced groups.", nameof(scene));
                ends[groups.Pop()] = index;
            }
        }
        if (groups.Count != 0) throw new ArgumentException("Native scopes require balanced groups.", nameof(scene));
        return ends;
    }
}
