grammar Acme;
// pseudoOpCodeStart: '!' pseudoOpCode ;
// pseudoOpCode: byteValues ;
// byteValues: ('!8'|'!08'|'!by'|'!byte') expression (',' expression)* ;
// expression:  
// 	| '(' expression ')'
// 	| expression BINARYOPERATOR expression
// 	| expression LOGICALOPERATOR expression
// 	| expression MULTIPLY expression
// 	| expression DIVIDE expression
// 	| expression PLUS expression
// 	| expression MINUS expression
// 	| NUMBER 
// 	| CHAR 
// 	| SYMBOL;
// NUMBER: DECNUMBER | HEXNUMBER | BINNUMBER ;
decnumber: DECNUMBER ; // # for testing purposes
DECNUMBER: DECDIGIT+ ;
//HEXNUMBER: '$' HEXDIGIT+ ;
//BINNUMBER: '%' BINDIGIT+ ;
fragment DECDIGIT: [0-9] ;
// fragment HEXDIGIT: [0-9a-fA-F] ;
// fragment BINDIGIT: [01] ;
// CHAR: '\'' [a-zA-Z] '\'' ;
// STRING:  '"' .*? '"' ;
// MINUS: '-' ;
// PLUS: '+' ;
// MULTIPLY: '*' ;
// DIVIDE: '/' ;
// LOGICALOPERATOR: 'XOR' | 'AND' ;
// BINARYOPERATOR: '&' | '|' | '^' ;
// SYMBOL: .?[a-zA-Z0-9]+ ;
// COMMENT: '//' .*? '\r'? '\n' -> skip ; // Match "//" stuff '\n'
// ID : [a-z]+ ;
WS : [ \t\r\n]+ -> skip ; // skip spaces, tabs, newlines