```mermaid
railroad-peg-beta
title "Identifier grammar"
# Predicates inspect without advancing the input.
identifier <- !keyword &letter letter (letter / digit)* ;
keyword <- "if" / "else" ;
letter <- "a" / "b" / "_" ;
digit <- "0" / "1" / . ;
```
