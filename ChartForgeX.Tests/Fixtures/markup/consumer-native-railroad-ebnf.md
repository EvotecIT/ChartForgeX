```mermaid
railroad-ebnf-beta
title "Identifier grammar"
identifier = (letter , { letter | digit | "_" }) - keyword ;
letter = "a" | "b" ;
digit = ? decimal digit ? ;
keyword = "if" ;
```
