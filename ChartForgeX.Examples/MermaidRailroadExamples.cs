internal static class MermaidRailroadExamples {
    internal const string Ebnf = "railroad-ebnf-beta\ntitle Identifier grammar\nidentifier = (letter , { letter | digit | \"_\" }) - keyword ;\nletter = \"a\" | \"b\" ;\ndigit = ? decimal digit ? ;\nkeyword = \"if\" ;\n";
    internal const string Abnf = "railroad-abnf-beta\ntitle Address grammar\naddress = local-part \"@\" domain ;\nlocal-part = 1*( ALPHA / DIGIT / %x2E-2F ) ;\ndomain = 2*4label *( \".\" label ) ;\nlabel = 1*( ALPHA / DIGIT / \"-\" ) ;\n";
    internal const string Peg = "railroad-peg-beta\ntitle Identifier predicates\nidentifier <- !keyword &letter letter (letter / digit)* ;\nkeyword <- \"if\" / \"else\" ;\nletter <- \"a\" / \"b\" / \"_\" ;\ndigit <- \"0\" / \"1\" / . ;\n";
    internal const string IntermediateRepresentation = "railroad-beta\ntitle Expression grammar\nexpression = sequence(nonterminal(\"term\"), zeroOrMore(sequence(choice(terminal(\"+\"), terminal(\"-\")), nonterminal(\"term\")))) ;\nterm = sequence(optional(terminal(\"-\")), oneOrMore(nonterminal(\"digit\"))) ;\ndigit = choice(terminal(\"0\"), terminal(\"1\"), special(\"another decimal digit\")) ;\n";
}
