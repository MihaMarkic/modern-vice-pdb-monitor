grammar Acme;
pseudoOps: (expressionPseudoOps | hexByteValues | fillValues | skipValues | alignValues | convtab | stringValues | scrxor | to | source | binary | zone | symbollist
	| ifFlow | ifDefFlow);
expressionPseudoOps: expressionPseudoCodes expression (',' expression)* ;
hexByteValues: HEX decNumber+;
fillValues: FILL expression (',' expression)? ;
skipValues: SKIP_VALUES expression ;
alignValues: ALIGN expression ',' expression (',' expression)? ;
convtab: CONVERSION_TABLE (CONVERSION_KEYWORD | filename ) block?; // STRING is filename
stringValues: (TEXT | CONVERSION_KEYWORD ) STRING (',' (STRING | expression))* ;
scrxor: SCRXOR number STRING (',' (STRING | expression))* ;
to: TO filename FILEFORMAT ;
source: SOURCE filename ;
binary: BINARY filename (',' expression (',' expression)? )? ;
zone: ZONE SYMBOL? block? ;
symbollist: SYMBOLLIST filename ;
// flow control
flowOps: '!' (ifFlow | ifDefFlow | forFlow) ;
ifFlow: 'if' condition block ('else' (block | ifFlow))? ;
ifDefFlow: ('ifdef' | 'ifndef' ) SYMBOL block ('else' (block | ifDefFlow))? ;
forFlow: 'for' symbol ((',' number ',' number) | ('in' symbol)) block ;

expressionPseudoCodes: BYTE_VALUES_OP | WORD_VALUES_OP | BE_WORD_VALUES_OP | THREE_BYTES_VALUES_OP | BE_THREE_BYTES_VALUES_OP | QUAD_VALUES_OP | BE_QUAD_VALUES_OP ;

BYTE_VALUES_OP: '!' ('8' | '08' | 'by' | 'byte') ;
WORD_VALUES_OP: '!' ('16' | 'wo' | 'word' | 'le16') ;
BE_WORD_VALUES_OP: '!be16' ;
THREE_BYTES_VALUES_OP: '!' ('24' | 'le24') ;
BE_THREE_BYTES_VALUES_OP: '!be24';
QUAD_VALUES_OP: '!' ('32' | 'le32') ;
BE_QUAD_VALUES_OP: '!be32';
HEX: '!' ('h' | 'hex') ;
FILL: '!' ('fill' | 'fi') ;
SKIP_VALUES: '!skip' ;
ALIGN: '!align' ;
CONVERSION_TABLE: '!' ('convtab' | 'ct') ;
TEXT: '!' ('text' | 'tx') ;
SCRXOR: '!scrxor' ;
TO: '!to' ;
SOURCE: '!' ('source' | 'src') ;
BINARY: '!' ('binary' | 'bin') ;
ZONE: '!' ('zone' | 'zn') ;
SYMBOLLIST: '!' ('symbollist' | 'sl') ;

block: '{' statement '}' ;
statement: expression ;
filename: STRING | LIB_FILENAME ;
condition: expression ;

 expression:  
 	'(' expression ')'
 	| expression binaryop expression
 	| expression logicalop expression
 	| expression '*' expression
 	| expression '/' expression
 	| expression '+' expression
 	| expression '-' expression
 	| ('>' | '<') expression
 	| expression logicalop expression
 	| pseudoOps
 	| number
 	| CHAR 
 	| symbol ;

number: decNumber | hexNumber | binNumber ; //| nonPrefixedHexNumber;

decNumber: DEC_NUMBER ; 
hexNumber: HEX_NUMBER ;
binNumber: BIN_NUMBER ;

logicalop: OR | XOR | AND | '==' | '<' | '<=' | '>' | '>=' ;
symbol: '*' | SYMBOL ;

CONVERSION_KEYWORD: 'pet' | 'raw' | 'scr' ;
FILEFORMAT: 'cbm' | 'plain' | 'apple' ;
DEC_NUMBER: DEC_DIGIT+; 
HEX_NUMBER: '$' HEX_DIGIT+ ;
BIN_NUMBER: '%' BIN_DIGIT+ ;
fragment DEC_DIGIT: [0-9] ;
fragment HEX_DIGIT: [0-9a-fA-F] ;
fragment BIN_DIGIT: [01] ;
CHAR: '\'' . '\'' ;
STRING:  '"' .*? '"' ;
//UNQUOTED_STRING: [a-zA-Z0-9]+ ;
LIB_FILENAME: '<' [a-zA-Z0-9/.]+ '>';
XOR: X O R ;
OR: O R ;
binaryop: '&' | '|' | '^' | '<<' | '>>' ;
SYMBOL: '.'? [a-zA-Z0-9_]+ ;
COMMENT: ';' .*? '\r'? '\n' -> skip ; // Match ";" stuff '\n'
EQUALITY: '==' ;
WS : [ \t\r\n]+ -> skip ; // skip spaces, tabs, newlines
opcode
   : ADC
   | AND
   | ASL
   | BCC
   | BCS
   | BEQ
   | BIT
   | BMI
   | BNE
   | BPL
   | BRA
   | BRK
   | BVC
   | BVS
   | CLC
   | CLD
   | CLI
   | CLV
   | CMP
   | CPX
   | CPY
   | DEC
   | DEX
   | DEY
   | EOR
   | INC
   | INX
   | INY
   | JMP
   | JSR
   | LDA
   | LDY
   | LDX
   | LSR
   | NOP
   | ORA
   | PHA
   | PHX
   | PHY
   | PHP
   | PLA
   | PLP
   | PLY
   | ROL
   | ROR
   | RTI
   | RTS
   | SBC
   | SEC
   | SED
   | SEI
   | STA
   | STX
   | STY
   | STZ
   | TAX
   | TAY
   | TSX
   | TXA
   | TXS
   | TYA
   ;

ADC
   : A D C   ;


AND
   : A N D   ;


ASL
   : A S L   ;


BCC
   : B C C   ;


BCS
   : B C S   ;


BEQ
   : B E Q   ;


BIT
   : B I T   ;


BMI
   : B M I   ;


BNE
   : B N E   ;


BPL
   : B P L   ;


BRA
   : B R A   ;


BRK
   : B R K   ;


BVC
   : B V C   ;


BVS
   : B V S   ;

CLC
   : C L C   ;


CLD
   : C L D   ;


CLI
   : C L I   ;


CLV
   : C L V   ;


CMP
   : C M P   ;


CPX
   : C P X   ;


CPY
   : C P Y   ;


DEC
   : D E C   ;


DEX
   : D E X   ;


DEY
   : D E Y   ;


EOR
   : E O R   ;


INC
   : I N C   ;


INX
   : I N X   ;


INY
   : I N Y   ;


JMP
   : J M P   ;


JSR
   : J S R   ;


LDA
   : L D A   ;


LDY
   : L D Y   ;


LDX
   : L D X   ;


LSR
   : L S R   ;


NOP
   : N O P   ;


ORA
   : O R A   ;


PHA
   : P H A   ;


PHX
   : P H X   ;


PHY
   : P H Y   ;


PHP
   : P H P   ;


PLA
   : P L A   ;


PLP
   : P L P   ;


PLY
   : P L Y   ;


ROL
   : R O L   ;


ROR
   : R O R   ;


RTI
   : R T I   ;


RTS
   : R T S   ;


SBC
   : S B C   ;


SEC
   : S E C   ;


SED
   : S E D   ;


SEI
   : S E I   ;


STA
   : S T A   ;


STX
   : S T X   ;


STY
   : S T Y   ;


STZ
   : S T Z   ;


TAX
   : T A X   ;


TAY
   : T A Y   ;


TSX
   : T S X   ;


TXA
   : T X A   ;


TXS
   : T X S   ;


TYA
   : T Y A   ;
// chars
fragment A
   : ('a' | 'A')
   ;


fragment B
   : ('b' | 'B')
   ;


fragment C
   : ('c' | 'C')
   ;


fragment D
   : ('d' | 'D')
   ;


fragment E
   : ('e' | 'E')
   ;


fragment F
   : ('f' | 'F')
   ;


fragment G
   : ('g' | 'G')
   ;


fragment H
   : ('h' | 'H')
   ;


fragment I
   : ('i' | 'I')
   ;


fragment J
   : ('j' | 'J')
   ;


fragment K
   : ('k' | 'K')
   ;


fragment L
   : ('l' | 'L')
   ;


fragment M
   : ('m' | 'M')
   ;


fragment N
   : ('n' | 'N')
   ;


fragment O
   : ('o' | 'O')
   ;


fragment P
   : ('p' | 'P')
   ;


fragment Q
   : ('q' | 'Q')
   ;


fragment R
   : ('r' | 'R')
   ;


fragment S
   : ('s' | 'S')
   ;


fragment T
   : ('t' | 'T')
   ;


fragment U
   : ('u' | 'U')
   ;


fragment V
   : ('v' | 'V')
   ;


fragment W
   : ('w' | 'W')
   ;


fragment X
   : ('x' | 'X')
   ;


fragment Y
   : ('y' | 'Y')
   ;

fragment Z
   : ('z' | 'Z')
   ;