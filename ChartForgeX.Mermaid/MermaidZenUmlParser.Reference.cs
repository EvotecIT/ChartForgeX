using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidZenUmlParser {
    private static List<string> ReferenceArguments(string text,MermaidSourceSpan span) {
        var parts=new List<string>();var start=0;var quote='\0';var escaped=false;
        for(var index=0;index<=text.Length;index++) {
            MermaidResourceGuard.Checkpoint();
            if(index==text.Length||text[index]==','&&quote=='\0') {
                var value=MermaidParserUtilities.Unquote(text.Substring(start,index-start).Trim());
                if(value.Length==0)throw Syntax("ZenUML ref arguments must not be empty.",span);
                parts.Add(value);start=index+1;continue;
            }
            var ch=text[index];
            if(quote!='\0'){if(escaped)escaped=false;else if(ch=='\\')escaped=true;else if(ch==quote)quote='\0';}
            else if(ch is '"' or '\'' or '`')quote=ch;
        }
        if(quote!='\0')throw Syntax("Unclosed ZenUML ref argument.",span);
        return parts;
    }
}
