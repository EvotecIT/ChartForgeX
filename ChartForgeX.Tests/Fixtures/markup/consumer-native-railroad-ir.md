```mermaid
railroad-beta
title "Expression grammar"
expression = sequence(nonterminal("term"), zeroOrMore(sequence(choice(terminal("+"), terminal("-")), nonterminal("term")))) ;
term = sequence(optional(terminal("-")), oneOrMore(nonterminal("digit"))) ;
digit = choice(terminal("0"), terminal("1"), special("another decimal digit")) ;
```
