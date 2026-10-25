using System;
using System.Collections.Generic;
namespace ChartForgeX.Mermaid;
public sealed partial class MermaidParser {
    // The same YAML scalar readers retain a bounded, ordered color sequence; no general YAML parser is added.
    private static void ReadConfigurationPaletteYaml(string[] lines,ref int index,int indent,string path,MermaidSourceSpan span,MermaidSourceConfiguration configuration,MermaidParseResult<MermaidDocument> result) {
        var values=new List<string?>();var last=index;
        for(;last<lines.Length;last++) {
            var raw=lines[last];var text=raw.Trim();if(text.Length==0||text.StartsWith("#",StringComparison.Ordinal))continue;
            if(ConfigurationYamlIndent(raw)!=indent||!IsConfigurationYamlSequenceItem(text))break;
            if(values.Count>=256){ConfigurationError(result,span,"Mermaid palette arrays cannot exceed 256 values.");break;}
            var scalar=TrimConfigurationYaml(TrimYamlComment(text.Substring(1).TrimStart(),false));
            try {
                if(scalar.StartsWith("\"",StringComparison.Ordinal))values.Add(ReadDoubleQuotedConfigurationYaml(scalar));
                else if(scalar.StartsWith("'",StringComparison.Ordinal))values.Add(MermaidYamlScalar.ReadSingleQuoted(scalar));
                else {if(scalar.Length==0){values.Add(null);continue;}ValidateConfigurationYamlPlainScalar(scalar);values.Add(IsTypedConfigurationYamlScalar(scalar)||IsOpaqueConfigurationYamlValue(scalar)?null:scalar);}
            }catch(ArgumentException exception){ConfigurationError(result,new MermaidSourceSpan(last+2,indent+1,text.Length),exception.Message);values.Add(null);}
        }
        configuration.Add(new MermaidConfigurationSetting(path,null,false,span).WithPalette(values));index=last-1;
    }
}
