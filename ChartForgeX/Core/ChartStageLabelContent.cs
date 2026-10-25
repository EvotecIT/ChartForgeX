namespace ChartForgeX.Core;

/// <summary>Controls whether an ordered Funnel or Pyramid displays one caption or independently placed component captions.</summary>
public enum ChartStageLabelContent {
    /// <summary>Retains the existing combined category/value caption and family metrics layout.</summary>
    Combined,
    /// <summary>Places category, value and any Funnel metrics independently, using shared measured collision and visibility policies.</summary>
    Separate
}
