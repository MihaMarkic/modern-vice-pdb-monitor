--------------------------------------------------------------------
startup: ; startup
0801 : 0b __ __ INV
0802 : 08 __ __ PHP
0803 : 0a __ __ ASL
0804 : 00 __ __ BRK
0805 : 9e __ __ INV
0806 : 32 __ __ INV
0807 : 30 36 __ BMI $083f ; (startup + 62)
0809 : 31 00 __ AND ($00),y 
080b : 00 __ __ BRK
080c : 00 __ __ BRK
080d : ba __ __ TSX
080e : 8e 0d 09 STX $090d ; (spentry + 0)
0811 : a9 12 __ LDA #$12
0813 : 85 19 __ STA IP + 0 
0815 : a9 09 __ LDA #$09
0817 : 85 1a __ STA IP + 1 
0819 : 38 __ __ SEC
081a : a9 09 __ LDA #$09
081c : e9 09 __ SBC #$09
081e : f0 0f __ BEQ $082f ; (startup + 46)
0820 : aa __ __ TAX
0821 : a9 00 __ LDA #$00
0823 : a0 00 __ LDY #$00
0825 : 91 19 __ STA (IP + 0),y 
0827 : c8 __ __ INY
0828 : d0 fb __ BNE $0825 ; (startup + 36)
082a : e6 1a __ INC IP + 1 
082c : ca __ __ DEX
082d : d0 f6 __ BNE $0825 ; (startup + 36)
082f : 38 __ __ SEC
0830 : a9 12 __ LDA #$12
0832 : e9 12 __ SBC #$12
0834 : f0 08 __ BEQ $083e ; (startup + 61)
0836 : a8 __ __ TAY
0837 : a9 00 __ LDA #$00
0839 : 88 __ __ DEY
083a : 91 19 __ STA (IP + 0),y 
083c : d0 fb __ BNE $0839 ; (startup + 56)
083e : a2 f7 __ LDX #$f7
0840 : e0 f7 __ CPX #$f7
0842 : f0 07 __ BEQ $084b ; (startup + 74)
0844 : 95 00 __ STA $00,x 
0846 : e8 __ __ INX
0847 : e0 f7 __ CPX #$f7
0849 : d0 f9 __ BNE $0844 ; (startup + 67)
084b : a9 f8 __ LDA #$f8
084d : 85 23 __ STA SP + 0 
084f : a9 9f __ LDA #$9f
0851 : 85 24 __ STA SP + 1 
0853 : 20 80 08 JSR $0880 ; (main.s0 + 0)
0856 : a9 4c __ LDA #$4c
0858 : 85 54 __ STA $54 
085a : a9 00 __ LDA #$00
085c : 85 13 __ STA P6 
085e : a9 19 __ LDA #$19
0860 : 85 16 __ STA P9 
0862 : 60 __ __ RTS
--------------------------------------------------------------------
spentry:
090d : __ __ __ BYT 00                                              : .
--------------------------------------------------------------------
main: ; main()->i16
.s0:
;  24, "D:/Temp/oscar64/memory/main.cpp"
0880 : a9 03 __ LDA #$03
0882 : 8d fe 9f STA $9ffe ; (i + 0)
0885 : a9 00 __ LDA #$00
0887 : 8d ff 9f STA $9fff ; (i + 1)
088a : a9 37 __ LDA #$37
088c : 85 0d __ STA P0 
088e : a9 00 __ LDA #$00
0890 : 85 0e __ STA P1 
0892 : ad 0e 09 LDA $090e ; (test + 0)
0895 : 85 43 __ STA T0 + 0 
0897 : ad 0f 09 LDA $090f ; (test + 1)
089a : 85 44 __ STA T0 + 1 
089c : a9 12 __ LDA #$12
089e : a0 00 __ LDY #$00
;  15, "D:/Temp/oscar64/memory/main.cpp"
08a0 : 91 43 __ STA (T0 + 0),y 
08a2 : 98 __ __ TYA
08a3 : c8 __ __ INY
08a4 : 91 43 __ STA (T0 + 0),y 
08a6 : ad 10 09 LDA $0910 ; (out + 0)
08a9 : 85 45 __ STA T1 + 0 
08ab : ad 11 09 LDA $0911 ; (out + 1)
08ae : 85 46 __ STA T1 + 1 
08b0 : 98 __ __ TYA
08b1 : 88 __ __ DEY
;  17, "D:/Temp/oscar64/memory/main.cpp"
08b2 : 91 45 __ STA (T1 + 0),y 
08b4 : 98 __ __ TYA
08b5 : c8 __ __ INY
08b6 : 91 45 __ STA (T1 + 0),y 
08b8 : a9 32 __ LDA #$32
08ba : 88 __ __ DEY
;  19, "D:/Temp/oscar64/memory/main.cpp"
08bb : 91 43 __ STA (T0 + 0),y 
08bd : 98 __ __ TYA
08be : c8 __ __ INY
08bf : 91 43 __ STA (T0 + 0),y 
08c1 : a9 02 __ LDA #$02
08c3 : 88 __ __ DEY
;  21, "D:/Temp/oscar64/memory/main.cpp"
08c4 : 91 45 __ STA (T1 + 0),y 
08c6 : 98 __ __ TYA
08c7 : c8 __ __ INY
08c8 : 91 45 __ STA (T1 + 0),y 
08ca : a9 49 __ LDA #$49
08cc : 88 __ __ DEY
;  23, "D:/Temp/oscar64/memory/main.cpp"
08cd : 91 43 __ STA (T0 + 0),y 
08cf : a9 45 __ LDA #$45
08d1 : c8 __ __ INY
08d2 : 91 43 __ STA (T0 + 0),y 
;  26, "D:/Temp/oscar64/memory/main.cpp"
08d4 : 20 f9 08 JSR $08f9 ; (sample_function.s0 + 0)
08d7 : a5 1b __ LDA ACCU + 0 
08d9 : 8d fc 9f STA $9ffc ; (u + 0)
08dc : a5 1c __ LDA ACCU + 1 
08de : 8d fd 9f STA $9ffd ; (u + 1)
08e1 : a9 08 __ LDA #$08
08e3 : 85 0d __ STA P0 
;  27, "D:/Temp/oscar64/memory/main.cpp"
08e5 : 20 02 09 JSR $0902 ; (OtherFunc.s0 + 0)
08e8 : a5 1b __ LDA ACCU + 0 
08ea : 8d fa 9f STA $9ffa ; (other + 0)
08ed : a5 1c __ LDA ACCU + 1 
08ef : 8d fb 9f STA $9ffb ; (other + 1)
08f2 : a9 00 __ LDA #$00
08f4 : 85 1b __ STA ACCU + 0 
08f6 : 85 1c __ STA ACCU + 1 
.s1001:
;  29, "D:/Temp/oscar64/memory/main.cpp"
08f8 : 60 __ __ RTS
--------------------------------------------------------------------
test:
090e : __ __ __ BYT 00 c0                                           : ..
--------------------------------------------------------------------
out:
0910 : __ __ __ BYT 00 10                                           : ..
--------------------------------------------------------------------
sample_function: ; sample_function(i16)->i16
.s0:
;   8, "D:/Temp/oscar64/memory/main.cpp"
08f9 : a9 05 __ LDA #$05
08fb : 85 1b __ STA ACCU + 0 
08fd : a9 00 __ LDA #$00
08ff : 85 1c __ STA ACCU + 1 
.s1001:
0901 : 60 __ __ RTS
--------------------------------------------------------------------
OtherFunc: ; OtherFunc(i16)->i16
.s0:
0902 : a5 0d __ LDA P0 ; (a + 0)
;   2, "D:/Temp/oscar64/memory/other.cpp"
0904 : 0a __ __ ASL
0905 : 85 1b __ STA ACCU + 0 
0907 : a5 0e __ LDA P1 ; (a + 1)
0909 : 2a __ __ ROL
090a : 85 1c __ STA ACCU + 1 
.s1001:
090c : 60 __ __ RTS
