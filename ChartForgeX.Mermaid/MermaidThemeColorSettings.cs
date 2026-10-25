using System;
namespace ChartForgeX.Mermaid;
/// <summary>Retains explicit canvas presence separately from finite resolved preset roles.</summary>
internal sealed class MermaidThemeColorSettings {
    private readonly MermaidThemePresetInputs _inputs;
    private MermaidThemeColorSettings(MermaidThemePresetInputs inputs){_inputs=inputs;}
    internal bool HasBackground=>_inputs.HasAuthored("background");
    internal static bool IsScalarPath(string path)=>MermaidThemePresetInputs.IsScalarPath(path);
    internal bool Handles(string path)=>_inputs.Handles(path);
    internal static MermaidThemeColorSettings Read(MermaidSourceConfiguration configuration,Action<MermaidConfigurationSetting,string> error)=>new(configuration.Preset??throw new InvalidOperationException("Preset inputs must be validated first."));
}
