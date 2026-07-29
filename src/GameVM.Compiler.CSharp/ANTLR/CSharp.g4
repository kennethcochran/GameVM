grammar CSharp;

program
    : statement* EOF
    ;

statement
    : variableDeclaration
    | expression
    ;

variableDeclaration
    : type identifier ('=' expression)? ';'
    ;

type
    : 'int' | 'string' | 'bool' | identifier
    ;

identifier
    : IDENT
    ;

expression
    : literal
    | identifier
    ;

literal
    : INT_LITERAL
    | STRING_LITERAL
    | BOOL_LITERAL
    ;

INT_LITERAL : [0-9]+ ;
STRING_LITERAL : '"' (~["] | '""')* '"' ;
BOOL_LITERAL : 'true' | 'false' ;
IDENT : [a-zA-Z_][a-zA-Z0-9_]*;
WS : [ \t\r\n]+ -> skip;