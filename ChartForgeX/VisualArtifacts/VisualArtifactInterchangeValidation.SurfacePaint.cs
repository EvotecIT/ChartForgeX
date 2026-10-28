using System;
using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateSurfacePaint(double? width,double? opacity,double? surface,IReadOnlyList<double> dash) {
        if(TopologySurfacePaintRules.Error(width,opacity,surface,dash) is {} error)throw new ArgumentException("Topology surface "+error+".");
    }
}
