// $antlr-format alignTrailingComments true, columnLimit 150, minEmptyLines 1, maxEmptyLinesToKeep 1, reflowComments false, useTab false
// $antlr-format allowShortRulesOnASingleLine false, allowShortBlocksOnASingleLine true, alignSemicolons hanging, alignColons hanging

grammar Pascal;

options {
    caseInsensitive = true;
}

program
    : programHeading (INTERFACE)? block DOT EOF
    ;

programHeading
    : PROGRAM identifier (LPAREN identifierList RPAREN)? SEMI
    | UNIT identifier SEMI
    ;

identifier
    : IDENT
    ;

block
    : (labelDeclarationPart | constantDefinitionPart | typeDefinitionPart | variableDeclarationPart | procedureAndFunctionDeclarationPart | usesUnitsPart | IMPLEMENTATION)* compoundStatement
    ;

usesUnitsPart
    : USES identifierList SEMI
    ;

labelDeclarationPart
    : LABEL label (COMMA label)* SEMI
    ;

label
    : unsignedInteger
    ;

constantDefinitionPart
    : CONST (constantDefinition SEMI)+
    ;

constantDefinition
    : identifier EQUAL constant
    ;

constantChr
    : CHR LPAREN unsignedInteger RPAREN
    ;

constant
    : unsignedNumber | sign unsignedNumber | identifier | sign identifier | string | constantChr
    ;

unsignedNumber
    : unsignedInteger | unsignedReal
    ;

unsignedInteger
    : NUM_INT
    ;

unsignedReal
    : NUM_REAL
    ;

sign
    : PLUS | MINUS
    ;

bool_
    : TRUE | FALSE
    ;

string
    : STRING_LITERAL
    ;

typeDefinitionPart
    : TYPE (typeDefinition SEMI)+
    ;

typeDefinition
    : identifier EQUAL (type_ | functionType | procedureType)
    ;

functionType
    : FUNCTION (formalParameterList)? COLON resultType
    ;

procedureType
    : PROCEDURE (formalParameterList)?
    ;

type_
    : simpleType | structuredType | pointerType
    ;

simpleType
    : scalarType | subrangeType | typeIdentifier | stringtype
    ;

scalarType
    : LPAREN identifierList RPAREN
    ;

subrangeType
    : constant DOTDOT constant
    ;

typeIdentifier
    : identifier | CHAR | BOOLEAN | INTEGER | REAL | STRING
    ;

structuredType
    : PACKED unpackedStructuredType | unpackedStructuredType
    ;

unpackedStructuredType
    : arrayType | recordType | setType | fileType
    ;

stringtype
    : STRING LBRACK (identifier | unsignedNumber) RBRACK
    ;

arrayType
    : ARRAY LBRACK typeList RBRACK OF componentType | ARRAY LBRACK2 typeList RBRACK2 OF componentType
    ;

typeList
    : indexType (COMMA indexType)*
    ;

indexType
    : simpleType
    ;

componentType
    : type_
    ;

recordType
    : RECORD fieldList? END
    ;

fieldList
    : fixedPart (SEMI variantPart)? | variantPart
    ;

fixedPart
    : recordSection (SEMI recordSection)*
    ;

recordSection
    : identifierList COLON type_
    ;

variantPart
    : CASE tag OF variant (SEMI variant)*
    ;

tag
    : identifier COLON typeIdentifier | typeIdentifier
    ;

variant
    : constList COLON LPAREN fieldList RPAREN
    ;

setType
    : SET OF baseType
    ;

baseType
    : simpleType
    ;

fileType
    : FILE OF type_ | FILE
    ;

pointerType
    : POINTER typeIdentifier
    ;

variableDeclarationPart
    : VAR variableDeclaration (SEMI variableDeclaration)* SEMI
    ;

variableDeclaration
    : identifierList COLON type_
    ;

procedureAndFunctionDeclarationPart
    : procedureOrFunctionDeclaration SEMI
    ;

procedureOrFunctionDeclaration
    : procedureDeclaration | functionDeclaration
    ;

procedureDeclaration
    : PROCEDURE identifier (formalParameterList)? SEMI block
    ;

formalParameterList
    : LPAREN formalParameterSection (SEMI formalParameterSection)* RPAREN
    ;

formalParameterSection
    : parameterGroup | VAR parameterGroup | FUNCTION parameterGroup | PROCEDURE parameterGroup
    ;

parameterGroup
    : identifierList COLON typeIdentifier
    ;

identifierList
    : identifier (COMMA identifier)*
    ;

constList
    : constant (COMMA constant)*
    ;

functionDeclaration
    : FUNCTION identifier (formalParameterList)? COLON resultType SEMI block
    ;

resultType
    : typeIdentifier
    ;

statement
    : label COLON unlabelledStatement | unlabelledStatement
    ;

unlabelledStatement
    : simpleStatement | structuredStatement
    ;

simpleStatement
    : assignmentStatement | procedureStatement | gotoStatement | emptyStatement_
    ;

assignmentStatement
    : variable ASSIGN expression
    ;

variable
    : (AT identifier | identifier) (LBRACK expression (COMMA expression)* RBRACK | LBRACK2 expression (COMMA expression)* RBRACK2 | DOT identifier | POINTER)*
    ;

expression
    : simpleExpression (relationaloperator expression)?
    ;

relationaloperator
    : EQUAL | NOT_EQUAL | LT | LE | GE | GT | IN
    ;

simpleExpression
    : term (additiveoperator simpleExpression)?
    ;

additiveoperator
    : PLUS | MINUS | OR
    ;

term
    : signedFactor (multiplicativeoperator term)?
    ;

multiplicativeoperator
    : STAR | SLASH | DIV | MOD | AND
    ;

signedFactor
    : (PLUS | MINUS)? factor
    ;

factor
    : variable | LPAREN expression RPAREN | functionDesignator | unsignedConstant | set_ | NOT factor | bool_
    ;

unsignedConstant
    : unsignedNumber | constantChr | string | NIL
    ;

functionDesignator
    : identifier LPAREN parameterList RPAREN
    ;

parameterList
    : actualParameter (COMMA actualParameter)*
    ;

set_
    : LBRACK elementList RBRACK | LBRACK2 elementList RBRACK2
    ;

elementList
    : element (COMMA element)* |
    ;

element
    : expression (DOTDOT expression)?
    ;

procedureStatement
    : identifier (LPAREN parameterList RPAREN)?
    ;

actualParameter
    : expression parameterwidth*
    ;

parameterwidth
    : COLON expression
    ;

gotoStatement
    : GOTO label
    ;

emptyStatement_
    :
    ;

empty_
    :
    /* empty */
    ;

structuredStatement
    : compoundStatement | conditionalStatement | repetetiveStatement | withStatement
    ;

compoundStatement
    : BEGIN statements END
    ;

statements
    : statement (SEMI statement)*
    ;

conditionalStatement
    : ifStatement | caseStatement
    ;

ifStatement
    : IF expression THEN statement (ELSE statement)?
    ;

caseStatement
    : CASE expression OF caseListElement (SEMI caseListElement)* (SEMI ELSE statements)? END
    ;

caseListElement
    : constList COLON statement
    ;

repetetiveStatement
    : whileStatement | repeatStatement | forStatement
    ;

whileStatement
    : WHILE expression DO statement
    ;

repeatStatement
    : REPEAT statements UNTIL expression
    ;

forStatement
    : FOR identifier ASSIGN forList DO statement
    ;

forList
    : initialValue (TO | DOWNTO) finalValue
    ;

initialValue
    : expression
    ;

finalValue
    : expression
    ;

withStatement
    : WITH recordVariableList DO statement
    ;

recordVariableList
    : variable (COMMA variable)*
    ;

AND : 'AND';
ARRAY : 'ARRAY';
BEGIN : 'BEGIN';
BOOLEAN : 'BOOLEAN';
CASE : 'CASE';
CHAR : 'CHAR';
CHR : 'CHR';
CONST : 'CONST';
DIV : 'DIV';
DO : 'DO';
DOWNTO : 'DOWNTO';
ELSE : 'ELSE';
END : 'END';
FILE : 'FILE';
FOR : 'FOR';
FUNCTION : 'FUNCTION';
GOTO : 'GOTO';
IF : 'IF';
IN : 'IN';
INTEGER : 'INTEGER';
LABEL : 'LABEL';
MOD : 'MOD';
NIL : 'NIL';
NOT : 'NOT';
OF : 'OF';
OR : 'OR';
PACKED : 'PACKED';
PROCEDURE : 'PROCEDURE';
PROGRAM : 'PROGRAM';
REAL : 'REAL';
RECORD : 'RECORD';
REPEAT : 'REPEAT';
SET : 'SET';
THEN : 'THEN';
TO : 'TO';
TYPE : 'TYPE';
UNTIL : 'UNTIL';
VAR : 'VAR';
WHILE : 'WHILE';
WITH : 'WITH';
PLUS : '+';
MINUS : '-';
STAR : '*';
SLASH : '/';
ASSIGN : ':=';
COMMA : ',';
SEMI : ';';
COLON : ':';
EQUAL : '=';
NOT_EQUAL : '<>';
LT : '<';
LE : '<=';
GE : '>=';
GT : '>';
LPAREN : '(';
RPAREN : ')';
LBRACK : '[';
LBRACK2 : '(.'; 
RBRACK : ']';
RBRACK2 : '.)';
POINTER : '^';
AT : '@';
DOT : '.';
DOTDOT : '..';
LCURLY : '{';
RCURLY : '}';
UNIT : 'UNIT';
INTERFACE : 'INTERFACE';
USES : 'USES';
STRING : 'STRING';
IMPLEMENTATION : 'IMPLEMENTATION';
TRUE : 'TRUE';
FALSE : 'FALSE';

WS : [ \t\r\n] -> skip;
COMMENT_1 : '(*' .*? '*)' -> skip;
COMMENT_2 : '{' .*? '}' -> skip;
IDENT : ('A' .. 'Z') ('A' .. 'Z' | '0' .. '9' | '_')*;
STRING_LITERAL : '\'' ('\'\'' | ~ ('\''))* '\'';
NUM_INT : ('0' .. '9')+;
NUM_REAL : ('0' .. '9')+ (('.' ('0' .. '9')+ (EXPONENT)?)? | EXPONENT);

fragment EXPONENT : ('E') ('+' | '-')? ('0' .. '9')+;

