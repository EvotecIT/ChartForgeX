```mermaid
railroad-abnf-beta
title "Address grammar"
; These are symbolic references to standard ABNF core rules.
address = local-part "@" domain ; trailing comment
local-part = 1*( ALPHA / DIGIT / %x2E-2F ) ;
domain = 2*4label *( "." label ) ;
label = 1*( ALPHA / DIGIT / "-" ) ;
```
