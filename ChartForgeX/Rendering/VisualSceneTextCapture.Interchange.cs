using System;
using System.Collections.Generic;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneTextCapture {
    internal static VisualSceneTextCapture FromOwnedRequests(IReadOnlyList<Request> requests) {
        var capture = new VisualSceneTextCapture(); capture._strict = false;
        foreach (var request in requests) {
            if (request.Face.Font == null || request.Face.Font.PaletteContext != null || !request.Clusters.Supported)
                throw new ArgumentException("Owned font replay requires the existing finite covered request/cluster contract.");
            var key = VisualArtifactInterchangeJson.NativeTextRequestKey(request.Style, includeFilePath: true);
            if (capture._requests.ContainsKey(key)) throw new ArgumentException("Captured owned font requests must be unique.");
            capture._requests.Add(key, request);
        }
        return capture.Freeze();
    }
    internal sealed partial class Request {
        internal static Request FromOwnedFacts(TextStyle style, ResolvedTypeface face, TextShaper.CapturedClusters clusters,
            IReadOnlyList<TextCaseTransform> cases) {
            if (cases.Count == 0) throw new ArgumentException("A captured font request requires an observed text-case policy.");
            var request = new Request(style, face, clusters); request._cases.Clear();
            foreach (var value in cases) {
                if (!Enum.IsDefined(typeof(TextCaseTransform), value) || !request._cases.Add(value))
                    throw new ArgumentException("Captured font text-case policies must be defined and distinct.");
            }
            if (!request._cases.Contains(style.TextCase)) throw new ArgumentException("A captured source text case must belong to its observed case set.");
            return request;
        }
    }
}
