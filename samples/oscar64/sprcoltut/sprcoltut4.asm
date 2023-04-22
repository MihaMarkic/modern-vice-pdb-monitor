--------------------------------------------------------------------
startup:
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
080e : 8e 1a 0d STX $0d1a ; (spentry + 0)
0811 : a9 94 __ LDA #$94
0813 : 85 19 __ STA IP + 0 
0815 : a9 0d __ LDA #$0d
0817 : 85 1a __ STA IP + 1 
0819 : 38 __ __ SEC
081a : a9 0d __ LDA #$0d
081c : e9 0d __ SBC #$0d
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
0830 : a9 9c __ LDA #$9c
0832 : e9 94 __ SBC #$94
0834 : f0 08 __ BEQ $083e ; (startup + 61)
0836 : a8 __ __ TAY
0837 : a9 00 __ LDA #$00
0839 : 88 __ __ DEY
083a : 91 19 __ STA (IP + 0),y 
083c : d0 fb __ BNE $0839 ; (startup + 56)
083e : a9 fe __ LDA #$fe
0840 : 85 23 __ STA SP + 0 
0842 : a9 9f __ LDA #$9f
0844 : 85 24 __ STA SP + 1 
0846 : 20 80 08 JSR $0880 ; (main.s0 + 0)
0849 : a9 4c __ LDA #$4c
084b : 85 54 __ STA $54 
084d : a9 00 __ LDA #$00
084f : 85 13 __ STA P6 
0851 : a9 19 __ LDA #$19
0853 : 85 16 __ STA P9 
0855 : 60 __ __ RTS
--------------------------------------------------------------------
spentry:
0d1a : __ __ __ BYT 00                                              : .
--------------------------------------------------------------------
main:
.s0:
;  55, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0880 : 20 82 0a JSR $0a82 ; (display_init.s0 + 0)
;  59, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0883 : a9 00 __ LDA #$00
0885 : 85 0d __ STA P0 
0887 : 85 0f __ STA P2 
0889 : 85 10 __ STA P3 
088b : 85 11 __ STA P4 
088d : 85 12 __ STA P5 
088f : 85 15 __ STA P8 
0891 : 85 16 __ STA P9 
0893 : 85 17 __ STA P10 
0895 : a9 01 __ LDA #$01
0897 : 85 0e __ STA P1 
0899 : 85 14 __ STA P7 
089b : a9 0d __ LDA #$0d
089d : 85 13 __ STA P6 
089f : 20 f8 0a JSR $0af8 ; (spr_set.s0 + 0)
;  57, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
08a2 : a9 a0 __ LDA #$a0
08a4 : 85 45 __ STA T0 + 0 
;  63, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
08a6 : 85 0f __ STA P2 
;  57, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
08a8 : a9 00 __ LDA #$00
08aa : 85 46 __ STA T0 + 1 
08ac : 85 48 __ STA T1 + 1 
08ae : a9 64 __ LDA #$64
08b0 : 85 47 __ STA T1 + 0 
;  61, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
08b2 : 85 49 __ STA T2 + 0 
;  63, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
08b4 : a9 0e __ LDA #$0e
08b6 : 85 10 __ STA P3 
.l1030:
08b8 : 20 d2 0b JSR $0bd2 ; (rand.s0 + 0)
08bb : a5 1b __ LDA ACCU + 0 
08bd : 85 4a __ STA T3 + 0 
08bf : a5 1c __ LDA ACCU + 1 
08c1 : 85 4b __ STA T3 + 1 
08c3 : 20 d2 0b JSR $0bd2 ; (rand.s0 + 0)
08c6 : a5 1b __ LDA ACCU + 0 
08c8 : 85 4c __ STA T4 + 0 
08ca : a5 1c __ LDA ACCU + 1 
08cc : 85 4d __ STA T4 + 1 
08ce : a5 4a __ LDA T3 + 0 
08d0 : 85 1b __ STA ACCU + 0 
08d2 : a5 4b __ LDA T3 + 1 
08d4 : 85 1c __ STA ACCU + 1 
08d6 : a9 28 __ LDA #$28
08d8 : 85 03 __ STA WORK + 0 
08da : a9 00 __ LDA #$00
08dc : 85 04 __ STA WORK + 1 
08de : 20 95 0c JSR $0c95 ; (divmod + 0)
08e1 : a5 05 __ LDA WORK + 2 
08e3 : 85 0d __ STA P0 
08e5 : a5 4c __ LDA T4 + 0 
08e7 : 85 1b __ STA ACCU + 0 
08e9 : a5 4d __ LDA T4 + 1 
08eb : 85 1c __ STA ACCU + 1 
08ed : a9 19 __ LDA #$19
08ef : 85 03 __ STA WORK + 0 
08f1 : a9 00 __ LDA #$00
08f3 : 85 04 __ STA WORK + 1 
08f5 : 20 95 0c JSR $0c95 ; (divmod + 0)
08f8 : a5 05 __ LDA WORK + 2 
08fa : 85 0e __ STA P1 
08fc : 20 95 0b JSR $0b95 ; (char_put.s0 + 0)
;  61, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
08ff : c6 49 __ DEC T2 + 0 
0901 : d0 b5 __ BNE $08b8 ; (main.l1030 + 0)
.l159:
;  68, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0903 : a9 00 __ LDA #$00
0905 : 20 f9 0b JSR $0bf9 ; (joy_poll.s0 + 0)
;  69, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0908 : ad 98 0d LDA $0d98 ; (joyx + 0)
090b : 10 1b __ BPL $0928 ; (main.s1028 + 0)
.s12:
090d : a5 46 __ LDA T0 + 1 
090f : 30 12 __ BMI $0923 ; (main.s10 + 0)
.s1029:
0911 : 05 45 __ ORA T0 + 0 
0913 : f0 0e __ BEQ $0923 ; (main.s10 + 0)
.s9:
;  70, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0915 : 18 __ __ CLC
0916 : a5 45 __ LDA T0 + 0 
0918 : 69 ff __ ADC #$ff
091a : 85 45 __ STA T0 + 0 
091c : a5 46 __ LDA T0 + 1 
091e : 69 ff __ ADC #$ff
0920 : 4c 42 09 JMP $0942 ; (main.s1032 + 0)
.s10:
;  71, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0923 : ad 98 0d LDA $0d98 ; (joyx + 0)
0926 : 30 1c __ BMI $0944 ; (main.s11 + 0)
.s1028:
0928 : f0 1a __ BEQ $0944 ; (main.s11 + 0)
.s16:
092a : a5 46 __ LDA T0 + 1 
092c : 49 80 __ EOR #$80
092e : c9 81 __ CMP #$81
0930 : d0 04 __ BNE $0936 ; (main.s1027 + 0)
.s1026:
0932 : a5 45 __ LDA T0 + 0 
0934 : c9 38 __ CMP #$38
.s1027:
0936 : b0 0c __ BCS $0944 ; (main.s11 + 0)
.s13:
;  72, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0938 : a5 45 __ LDA T0 + 0 
093a : 69 01 __ ADC #$01
093c : 85 45 __ STA T0 + 0 
093e : a5 46 __ LDA T0 + 1 
0940 : 69 00 __ ADC #$00
.s1032:
;  70, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0942 : 85 46 __ STA T0 + 1 
.s11:
;  79, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0944 : a5 45 __ LDA T0 + 0 
0946 : 85 0d __ STA P0 
0948 : a5 46 __ LDA T0 + 1 
094a : 85 0e __ STA P1 
;  74, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
094c : ad 96 0d LDA $0d96 ; (joyy + 0)
094f : 10 1b __ BPL $096c ; (main.s1024 + 0)
.s20:
0951 : a5 48 __ LDA T1 + 1 
0953 : 30 12 __ BMI $0967 ; (main.s18 + 0)
.s1025:
0955 : 05 47 __ ORA T1 + 0 
0957 : f0 0e __ BEQ $0967 ; (main.s18 + 0)
.s17:
;  75, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0959 : 18 __ __ CLC
095a : a5 47 __ LDA T1 + 0 
095c : 69 ff __ ADC #$ff
095e : 85 47 __ STA T1 + 0 
0960 : a5 48 __ LDA T1 + 1 
0962 : 69 ff __ ADC #$ff
0964 : 4c 85 09 JMP $0985 ; (main.s1033 + 0)
.s18:
;  76, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0967 : ad 96 0d LDA $0d96 ; (joyy + 0)
096a : 30 1b __ BMI $0987 ; (main.s19 + 0)
.s1024:
096c : f0 19 __ BEQ $0987 ; (main.s19 + 0)
.s24:
096e : a5 48 __ LDA T1 + 1 
0970 : 30 08 __ BMI $097a ; (main.s21 + 0)
.s1023:
0972 : d0 13 __ BNE $0987 ; (main.s19 + 0)
.s1022:
0974 : a5 47 __ LDA T1 + 0 
0976 : c9 c0 __ CMP #$c0
0978 : b0 0d __ BCS $0987 ; (main.s19 + 0)
.s21:
;  77, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
097a : 18 __ __ CLC
097b : a5 47 __ LDA T1 + 0 
097d : 69 01 __ ADC #$01
097f : 85 47 __ STA T1 + 0 
0981 : a5 48 __ LDA T1 + 1 
0983 : 69 00 __ ADC #$00
.s1033:
;  75, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0985 : 85 48 __ STA T1 + 1 
.s19:
;  79, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0987 : a5 47 __ LDA T1 + 0 
0989 : 85 0f __ STA P2 
098b : a5 48 __ LDA T1 + 1 
098d : 85 10 __ STA P3 
098f : 20 39 0c JSR $0c39 ; (char_get_pix.s0 + 0)
0992 : 85 49 __ STA T2 + 0 
;  80, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0994 : 18 __ __ CLC
0995 : a5 45 __ LDA T0 + 0 
0997 : 69 07 __ ADC #$07
0999 : 85 4a __ STA T3 + 0 
099b : 85 0d __ STA P0 
099d : a5 0e __ LDA P1 
099f : 69 00 __ ADC #$00
09a1 : 85 4b __ STA T3 + 1 
09a3 : 85 0e __ STA P1 
09a5 : 20 39 0c JSR $0c39 ; (char_get_pix.s0 + 0)
09a8 : aa __ __ TAX
;  81, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09a9 : a5 45 __ LDA T0 + 0 
09ab : 85 0d __ STA P0 
09ad : a5 46 __ LDA T0 + 1 
09af : 85 0e __ STA P1 
;  80, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09b1 : 8a __ __ TXA
09b2 : 30 04 __ BMI $09b8 ; (main.s1002 + 0)
.s1003:
09b4 : a9 00 __ LDA #$00
09b6 : f0 02 __ BEQ $09ba ; (main.s1004 + 0)
.s1002:
09b8 : a9 01 __ LDA #$01
.s1004:
09ba : 85 4e __ STA T5 + 0 
;  81, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09bc : 18 __ __ CLC
09bd : a5 0f __ LDA P2 
09bf : 69 07 __ ADC #$07
09c1 : 85 0f __ STA P2 
09c3 : 90 02 __ BCC $09c7 ; (main.s1037 + 0)
.s1036:
09c5 : e6 10 __ INC P3 
.s1037:
09c7 : 20 39 0c JSR $0c39 ; (char_get_pix.s0 + 0)
09ca : aa __ __ TAX
;  82, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09cb : a5 4a __ LDA T3 + 0 
09cd : 85 0d __ STA P0 
09cf : a5 4b __ LDA T3 + 1 
09d1 : 85 0e __ STA P1 
;  81, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09d3 : 8a __ __ TXA
09d4 : 30 04 __ BMI $09da ; (main.s1007 + 0)
.s1008:
;  81, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09d6 : a9 00 __ LDA #$00
09d8 : f0 02 __ BEQ $09dc ; (main.s1009 + 0)
.s1007:
09da : a9 01 __ LDA #$01
.s1009:
09dc : 85 4f __ STA T6 + 0 
;  82, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09de : 20 39 0c JSR $0c39 ; (char_get_pix.s0 + 0)
09e1 : c9 80 __ CMP #$80
09e3 : a9 00 __ LDA #$00
09e5 : 2a __ __ ROL
09e6 : a8 __ __ TAY
;  79, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09e7 : a5 49 __ LDA T2 + 0 
;  82, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09e9 : 30 04 __ BMI $09ef ; (main.s1017 + 0)
.s1018:
;  79, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09eb : a9 00 __ LDA #$00
09ed : f0 02 __ BEQ $09f1 ; (main.s1019 + 0)
.s1017:
09ef : a9 01 __ LDA #$01
.s1019:
09f1 : aa __ __ TAX
;  89, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09f2 : f0 12 __ BEQ $0a06 ; (main.s26 + 0)
.s28:
09f4 : a5 4f __ LDA T6 + 0 
09f6 : f0 0e __ BEQ $0a06 ; (main.s26 + 0)
.s25:
;  90, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
09f8 : 18 __ __ CLC
09f9 : a5 45 __ LDA T0 + 0 
09fb : 69 01 __ ADC #$01
09fd : 85 45 __ STA T0 + 0 
09ff : a5 46 __ LDA T0 + 1 
0a01 : 69 00 __ ADC #$00
0a03 : 4c 7d 0a JMP $0a7d ; (main.s1034 + 0)
.s26:
;  91, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a06 : a5 4e __ LDA T5 + 0 
0a08 : f0 11 __ BEQ $0a1b ; (main.s30 + 0)
.s32:
0a0a : 98 __ __ TYA
0a0b : f0 0e __ BEQ $0a1b ; (main.s30 + 0)
.s29:
;  92, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a0d : 18 __ __ CLC
0a0e : a5 45 __ LDA T0 + 0 
0a10 : 69 ff __ ADC #$ff
0a12 : 85 45 __ STA T0 + 0 
0a14 : a5 46 __ LDA T0 + 1 
0a16 : 69 ff __ ADC #$ff
0a18 : 4c 7d 0a JMP $0a7d ; (main.s1034 + 0)
.s30:
;  93, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a1b : 8a __ __ TXA
0a1c : d0 04 __ BNE $0a22 ; (main.s33 + 0)
.s36:
0a1e : a5 4e __ LDA T5 + 0 
0a20 : f0 0e __ BEQ $0a30 ; (main.s34 + 0)
.s33:
;  94, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a22 : 18 __ __ CLC
0a23 : a5 47 __ LDA T1 + 0 
0a25 : 69 01 __ ADC #$01
0a27 : 85 47 __ STA T1 + 0 
0a29 : a5 48 __ LDA T1 + 1 
0a2b : 69 00 __ ADC #$00
0a2d : 4c 42 0a JMP $0a42 ; (main.s1035 + 0)
.s34:
;  95, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a30 : a5 4f __ LDA T6 + 0 
0a32 : d0 03 __ BNE $0a37 ; (main.s37 + 0)
.s40:
0a34 : 98 __ __ TYA
0a35 : f0 0d __ BEQ $0a44 ; (main.s27 + 0)
.s37:
;  96, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a37 : 18 __ __ CLC
0a38 : a5 47 __ LDA T1 + 0 
0a3a : 69 ff __ ADC #$ff
0a3c : 85 47 __ STA T1 + 0 
0a3e : a5 48 __ LDA T1 + 1 
0a40 : 69 ff __ ADC #$ff
.s1035:
;  94, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a42 : 85 48 __ STA T1 + 1 
.s27:
;  98, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a44 : 18 __ __ CLC
0a45 : a5 47 __ LDA T1 + 0 
0a47 : 69 32 __ ADC #$32
;  64, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0a49 : 8d 01 d0 STA $d001 
;  98, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a4c : 18 __ __ CLC
0a4d : a5 45 __ LDA T0 + 0 
0a4f : 69 18 __ ADC #$18
;  65, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0a51 : 8d 00 d0 STA $d000 
;  98, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a54 : a5 46 __ LDA T0 + 1 
0a56 : 69 00 __ ADC #$00
;  66, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0a58 : 4a __ __ LSR
;  67, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0a59 : ad 10 d0 LDA $d010 
;  66, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0a5c : b0 12 __ BCS $0a70 ; (main.s44 + 0)
.s45:
;  69, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0a5e : 29 fe __ AND #$fe
0a60 : 8d 10 d0 STA $d010 
.l48:
;  70, "E:/Projects/C64Repo/oscar64/include/c64/vic.c"
0a63 : ad 11 d0 LDA $d011 
0a66 : 30 fb __ BMI $0a63 ; (main.l48 + 0)
.l51:
;  72, "E:/Projects/C64Repo/oscar64/include/c64/vic.c"
0a68 : ad 11 d0 LDA $d011 
0a6b : 10 fb __ BPL $0a68 ; (main.l51 + 0)
0a6d : 4c 03 09 JMP $0903 ; (main.l159 + 0)
.s44:
;  67, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0a70 : 09 01 __ ORA #$01
0a72 : 8d 10 d0 STA $d010 
;  70, "E:/Projects/C64Repo/oscar64/include/c64/vic.c"
0a75 : ad 11 d0 LDA $d011 
0a78 : 10 ee __ BPL $0a68 ; (main.l51 + 0)
0a7a : 4c 63 0a JMP $0a63 ; (main.l48 + 0)
.s1034:
;  90, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a7d : 85 46 __ STA T0 + 1 
0a7f : 4c 44 0a JMP $0a44 ; (main.s27 + 0)
--------------------------------------------------------------------
display_init:
.s0:
;  31, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a82 : a2 40 __ LDX #$40
.l1002:
0a84 : bd 1a 0d LDA $0d1a,x ; (spentry + 0)
0a87 : 9d 3f 03 STA $033f,x 
0a8a : ca __ __ DEX
0a8b : d0 f7 __ BNE $0a84 ; (display_init.l1002 + 0)
.s1003:
;  32, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a8d : a9 00 __ LDA #$00
0a8f : 85 0d __ STA P0 
0a91 : a9 04 __ LDA #$04
0a93 : 85 0e __ STA P1 
0a95 : 20 c4 0a JSR $0ac4 ; (spr_init.s0 + 0)
;  33, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0a98 : a9 20 __ LDA #$20
0a9a : 85 0f __ STA P2 
0a9c : a9 00 __ LDA #$00
0a9e : 85 10 __ STA P3 
0aa0 : a9 e8 __ LDA #$e8
0aa2 : 85 11 __ STA P4 
0aa4 : a9 03 __ LDA #$03
0aa6 : 85 12 __ STA P5 
0aa8 : 20 d4 0a JSR $0ad4 ; (memset.s0 + 0)
;  34, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0aab : a9 0e __ LDA #$0e
0aad : 85 0f __ STA P2 
0aaf : a9 00 __ LDA #$00
0ab1 : 85 10 __ STA P3 
0ab3 : 85 0d __ STA P0 
0ab5 : a9 e8 __ LDA #$e8
0ab7 : 85 11 __ STA P4 
0ab9 : a9 03 __ LDA #$03
0abb : 85 12 __ STA P5 
0abd : a9 d8 __ LDA #$d8
0abf : 85 0e __ STA P1 
0ac1 : 4c d4 0a JMP $0ad4 ; (memset.s0 + 0)
--------------------------------------------------------------------
spimage:
0d1b : __ __ __ BYT ff 00 00 81 00 00 81 00 00 81 00 00 81 00 00 81 : ................
0d2b : __ __ __ BYT 00 00 81 00 00 ff 00 00 00 00 00 00 00 00 00 00 : ................
0d3b : __ __ __ BYT 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 : ................
0d4b : __ __ __ BYT 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00    : ...............
--------------------------------------------------------------------
spr_init:
.s0:
0ac4 : 18 __ __ CLC
0ac5 : a5 0d __ LDA P0 
;   9, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0ac7 : 69 f8 __ ADC #$f8
0ac9 : 8d 94 0d STA $0d94 ; (vspriteScreen + 0)
0acc : a5 0e __ LDA P1 
0ace : 69 03 __ ADC #$03
0ad0 : 8d 95 0d STA $0d95 ; (vspriteScreen + 1)
.s1001:
0ad3 : 60 __ __ RTS
--------------------------------------------------------------------
vspriteScreen:
0d94 : __ __ __ BSS	2
--------------------------------------------------------------------
memset:
.s0:
; 144, "E:/Projects/C64Repo/oscar64/include/string.c"
0ad4 : a5 0f __ LDA P2 
0ad6 : a6 12 __ LDX P5 
0ad8 : f0 0c __ BEQ $0ae6 ; (memset.s0 + 18)
0ada : a0 00 __ LDY #$00
0adc : 91 0d __ STA (P0),y 
0ade : c8 __ __ INY
0adf : d0 fb __ BNE $0adc ; (memset.s0 + 8)
0ae1 : e6 0e __ INC P1 
0ae3 : ca __ __ DEX
0ae4 : d0 f6 __ BNE $0adc ; (memset.s0 + 8)
0ae6 : a4 11 __ LDY P4 
0ae8 : f0 05 __ BEQ $0aef ; (memset.s0 + 27)
0aea : 88 __ __ DEY
0aeb : 91 0d __ STA (P0),y 
0aed : d0 fb __ BNE $0aea ; (memset.s0 + 22)
; 165, "E:/Projects/C64Repo/oscar64/include/string.c"
0aef : a5 0d __ LDA P0 
0af1 : 85 1b __ STA ACCU + 0 
0af3 : a5 0e __ LDA P1 
0af5 : 85 1c __ STA ACCU + 1 
.s1001:
0af7 : 60 __ __ RTS
--------------------------------------------------------------------
spr_set:
.s0:
;  15, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0af8 : a6 0d __ LDX P0 
;  17, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0afa : bd 64 0d LDA $0d64,x ; (bitshift + 8)
0afd : 85 1b __ STA ACCU + 0 
;  19, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0aff : a5 0e __ LDA P1 
0b01 : d0 0a __ BNE $0b0d ; (spr_set.s3 + 0)
.s4:
;  22, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b03 : a9 ff __ LDA #$ff
0b05 : 45 1b __ EOR ACCU + 0 
0b07 : 2d 15 d0 AND $d015 
0b0a : 4c 12 0b JMP $0b12 ; (spr_set.s19 + 0)
.s3:
;  20, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b0d : a5 1b __ LDA ACCU + 0 
0b0f : 0d 15 d0 ORA $d015 
.s19:
;  22, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b12 : 8d 15 d0 STA $d015 
;  24, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b15 : a5 15 __ LDA P8 
0b17 : d0 0a __ BNE $0b23 ; (spr_set.s6 + 0)
.s7:
;  27, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b19 : a9 ff __ LDA #$ff
0b1b : 45 1b __ EOR ACCU + 0 
0b1d : 2d 1c d0 AND $d01c 
0b20 : 4c 28 0b JMP $0b28 ; (spr_set.s20 + 0)
.s6:
;  25, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b23 : ad 1c d0 LDA $d01c 
0b26 : 05 1b __ ORA ACCU + 0 
.s20:
;  27, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b28 : 8d 1c d0 STA $d01c 
;  29, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b2b : a5 16 __ LDA P9 
0b2d : d0 0a __ BNE $0b39 ; (spr_set.s9 + 0)
.s10:
;  32, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b2f : a9 ff __ LDA #$ff
0b31 : 45 1b __ EOR ACCU + 0 
0b33 : 2d 1d d0 AND $d01d 
0b36 : 4c 3e 0b JMP $0b3e ; (spr_set.s21 + 0)
.s9:
;  30, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b39 : ad 1d d0 LDA $d01d 
0b3c : 05 1b __ ORA ACCU + 0 
.s21:
;  32, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b3e : 8d 1d d0 STA $d01d 
;  34, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b41 : a5 17 __ LDA P10 
0b43 : d0 0a __ BNE $0b4f ; (spr_set.s12 + 0)
.s13:
;  37, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b45 : a9 ff __ LDA #$ff
0b47 : 45 1b __ EOR ACCU + 0 
0b49 : 2d 17 d0 AND $d017 
0b4c : 4c 54 0b JMP $0b54 ; (spr_set.s14 + 0)
.s12:
;  35, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b4f : ad 17 d0 LDA $d017 
0b52 : 05 1b __ ORA ACCU + 0 
.s14:
;  37, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b54 : 8d 17 d0 STA $d017 
;  39, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b57 : 8a __ __ TXA
0b58 : 0a __ __ ASL
0b59 : a8 __ __ TAY
0b5a : a5 11 __ LDA P4 
0b5c : 99 01 d0 STA $d001,y 
;  40, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b5f : a5 0f __ LDA P2 
0b61 : 99 00 d0 STA $d000,y 
;  41, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b64 : 46 10 __ LSR P3 
0b66 : b0 0a __ BCS $0b72 ; (spr_set.s15 + 0)
.s16:
;  44, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b68 : a9 ff __ LDA #$ff
0b6a : 45 1b __ EOR ACCU + 0 
0b6c : 2d 10 d0 AND $d010 
0b6f : 4c 77 0b JMP $0b77 ; (spr_set.s17 + 0)
.s15:
;  42, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b72 : a5 1b __ LDA ACCU + 0 
0b74 : 0d 10 d0 ORA $d010 
.s17:
;  44, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b77 : 8d 10 d0 STA $d010 
;  46, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b7a : ad 94 0d LDA $0d94 ; (vspriteScreen + 0)
0b7d : 18 __ __ CLC
0b7e : 65 0d __ ADC P0 
0b80 : 85 1b __ STA ACCU + 0 
0b82 : ad 95 0d LDA $0d95 ; (vspriteScreen + 1)
0b85 : 69 00 __ ADC #$00
0b87 : 85 1c __ STA ACCU + 1 
0b89 : a5 13 __ LDA P6 
0b8b : a0 00 __ LDY #$00
0b8d : 91 1b __ STA (ACCU + 0),y 
;  47, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b8f : a5 14 __ LDA P7 
0b91 : 9d 27 d0 STA $d027,x 
.s1001:
;  19, "E:/Projects/C64Repo/oscar64/include/c64/sprites.c"
0b94 : 60 __ __ RTS
--------------------------------------------------------------------
char_put:
.s0:
;  39, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0b95 : a5 0e __ LDA P1 
0b97 : 0a __ __ ASL
0b98 : 85 1b __ STA ACCU + 0 
0b9a : a9 00 __ LDA #$00
0b9c : 2a __ __ ROL
0b9d : 06 1b __ ASL ACCU + 0 
0b9f : 2a __ __ ROL
0ba0 : aa __ __ TAX
0ba1 : a5 1b __ LDA ACCU + 0 
0ba3 : 65 0e __ ADC P1 
0ba5 : 85 1b __ STA ACCU + 0 
0ba7 : 8a __ __ TXA
0ba8 : 69 00 __ ADC #$00
0baa : 06 1b __ ASL ACCU + 0 
0bac : 2a __ __ ROL
0bad : 06 1b __ ASL ACCU + 0 
0baf : 2a __ __ ROL
0bb0 : 06 1b __ ASL ACCU + 0 
0bb2 : 2a __ __ ROL
0bb3 : aa __ __ TAX
0bb4 : 18 __ __ CLC
0bb5 : a5 1b __ LDA ACCU + 0 
0bb7 : 65 0d __ ADC P0 
0bb9 : 85 43 __ STA T1 + 0 
;  40, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0bbb : 85 1b __ STA ACCU + 0 
;  39, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0bbd : 8a __ __ TXA
0bbe : 69 04 __ ADC #$04
0bc0 : 85 44 __ STA T1 + 1 
;  40, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0bc2 : 18 __ __ CLC
0bc3 : 69 d4 __ ADC #$d4
0bc5 : 85 1c __ STA ACCU + 1 
;  39, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0bc7 : a5 0f __ LDA P2 
0bc9 : a0 00 __ LDY #$00
0bcb : 91 43 __ STA (T1 + 0),y 
;  40, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0bcd : a5 10 __ LDA P3 
0bcf : 91 1b __ STA (ACCU + 0),y 
.s1001:
;  41, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0bd1 : 60 __ __ RTS
--------------------------------------------------------------------
rand:
.s0:
; 653, "E:/Projects/C64Repo/oscar64/include/stdlib.c"
0bd2 : ad 5b 0d LDA $0d5b ; (seed + 1)
0bd5 : 4a __ __ LSR
0bd6 : ad 5a 0d LDA $0d5a ; (seed + 0)
0bd9 : 6a __ __ ROR
0bda : aa __ __ TAX
0bdb : a9 00 __ LDA #$00
0bdd : 6a __ __ ROR
0bde : 4d 5a 0d EOR $0d5a ; (seed + 0)
0be1 : 85 1b __ STA ACCU + 0 
0be3 : 8a __ __ TXA
0be4 : 4d 5b 0d EOR $0d5b ; (seed + 1)
0be7 : 85 1c __ STA ACCU + 1 
; 654, "E:/Projects/C64Repo/oscar64/include/stdlib.c"
0be9 : 4a __ __ LSR
0bea : 45 1b __ EOR ACCU + 0 
; 655, "E:/Projects/C64Repo/oscar64/include/stdlib.c"
0bec : 8d 5a 0d STA $0d5a ; (seed + 0)
; 656, "E:/Projects/C64Repo/oscar64/include/stdlib.c"
0bef : 85 1b __ STA ACCU + 0 
; 655, "E:/Projects/C64Repo/oscar64/include/stdlib.c"
0bf1 : 45 1c __ EOR ACCU + 1 
0bf3 : 8d 5b 0d STA $0d5b ; (seed + 1)
; 656, "E:/Projects/C64Repo/oscar64/include/stdlib.c"
0bf6 : 85 1c __ STA ACCU + 1 
.s1001:
0bf8 : 60 __ __ RTS
--------------------------------------------------------------------
seed:
0d5a : __ __ __ BYT 00 7a                                           : .z
--------------------------------------------------------------------
joy_poll:
.s0:
;   8, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0bf9 : aa __ __ TAX
0bfa : bd 00 dc LDA $dc00,x 
;  10, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0bfd : a8 __ __ TAY
;  24, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0bfe : 29 10 __ AND #$10
0c00 : f0 04 __ BEQ $0c06 ; (joy_poll.s1005 + 0)
.s1006:
0c02 : a9 00 __ LDA #$00
0c04 : f0 02 __ BEQ $0c08 ; (joy_poll.s1007 + 0)
.s1005:
0c06 : a9 01 __ LDA #$01
.s1007:
0c08 : 9d 9a 0d STA $0d9a,x ; (joyb + 0)
;  10, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0c0b : 98 __ __ TYA
0c0c : 4a __ __ LSR
0c0d : b0 1d __ BCS $0c2c ; (joy_poll.s2 + 0)
.s1:
;  11, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0c0f : a9 ff __ LDA #$ff
.s15:
;  15, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0c11 : 9d 96 0d STA $0d96,x ; (joyy + 0)
;  17, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0c14 : 98 __ __ TYA
0c15 : 29 04 __ AND #$04
0c17 : d0 06 __ BNE $0c1f ; (joy_poll.s8 + 0)
.s7:
;  18, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0c19 : a9 ff __ LDA #$ff
.s1001:
;  22, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0c1b : 9d 98 0d STA $0d98,x ; (joyx + 0)
0c1e : 60 __ __ RTS
.s8:
;  19, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0c1f : 98 __ __ TYA
0c20 : 29 08 __ AND #$08
0c22 : f0 04 __ BEQ $0c28 ; (joy_poll.s1011 + 0)
.s1012:
0c24 : a9 00 __ LDA #$00
0c26 : f0 f3 __ BEQ $0c1b ; (joy_poll.s1001 + 0)
.s1011:
0c28 : a9 01 __ LDA #$01
0c2a : d0 ef __ BNE $0c1b ; (joy_poll.s1001 + 0)
.s2:
;  12, "E:/Projects/C64Repo/oscar64/include/c64/joystick.c"
0c2c : 98 __ __ TYA
0c2d : 29 02 __ AND #$02
0c2f : f0 04 __ BEQ $0c35 ; (joy_poll.s1008 + 0)
.s1009:
0c31 : a9 00 __ LDA #$00
0c33 : f0 dc __ BEQ $0c11 ; (joy_poll.s15 + 0)
.s1008:
0c35 : a9 01 __ LDA #$01
0c37 : d0 d8 __ BNE $0c11 ; (joy_poll.s15 + 0)
--------------------------------------------------------------------
joyy:
0d96 : __ __ __ BSS	2
--------------------------------------------------------------------
joyx:
0d98 : __ __ __ BSS	2
--------------------------------------------------------------------
joyb:
0d9a : __ __ __ BSS	2
--------------------------------------------------------------------
char_get_pix:
.s0:
;  50, "E:/Projects/C64Repo/sprcoltut/sprcoltut4.c"
0c39 : a5 0f __ LDA P2 
0c3b : 85 43 __ STA T0 + 0 
0c3d : a5 10 __ LDA P3 
0c3f : 4a __ __ LSR
0c40 : 66 43 __ ROR T0 + 0 
0c42 : 4a __ __ LSR
0c43 : 66 43 __ ROR T0 + 0 
0c45 : 4a __ __ LSR
0c46 : 66 43 __ ROR T0 + 0 
0c48 : 49 10 __ EOR #$10
0c4a : 38 __ __ SEC
0c4b : e9 10 __ SBC #$10
0c4d : 85 44 __ STA T0 + 1 
0c4f : a5 43 __ LDA T0 + 0 
0c51 : 0a __ __ ASL
0c52 : 85 1b __ STA ACCU + 0 
0c54 : a5 44 __ LDA T0 + 1 
0c56 : 2a __ __ ROL
0c57 : 06 1b __ ASL ACCU + 0 
0c59 : 2a __ __ ROL
0c5a : aa __ __ TAX
0c5b : 18 __ __ CLC
0c5c : a5 1b __ LDA ACCU + 0 
0c5e : 65 43 __ ADC T0 + 0 
0c60 : 85 43 __ STA T0 + 0 
0c62 : 8a __ __ TXA
0c63 : 65 44 __ ADC T0 + 1 
0c65 : 06 43 __ ASL T0 + 0 
0c67 : 2a __ __ ROL
0c68 : 06 43 __ ASL T0 + 0 
0c6a : 2a __ __ ROL
0c6b : 06 43 __ ASL T0 + 0 
0c6d : 2a __ __ ROL
0c6e : 85 44 __ STA T0 + 1 
0c70 : a5 0e __ LDA P1 
0c72 : 4a __ __ LSR
0c73 : 66 0d __ ROR P0 
0c75 : 4a __ __ LSR
0c76 : 66 0d __ ROR P0 
0c78 : 4a __ __ LSR
0c79 : 66 0d __ ROR P0 
0c7b : 49 10 __ EOR #$10
0c7d : 38 __ __ SEC
0c7e : e9 10 __ SBC #$10
0c80 : aa __ __ TAX
0c81 : 18 __ __ CLC
0c82 : a5 0d __ LDA P0 
0c84 : 65 43 __ ADC T0 + 0 
0c86 : 85 43 __ STA T0 + 0 
0c88 : 8a __ __ TXA
0c89 : 65 44 __ ADC T0 + 1 
0c8b : 18 __ __ CLC
0c8c : 69 04 __ ADC #$04
0c8e : 85 44 __ STA T0 + 1 
0c90 : a0 00 __ LDY #$00
0c92 : b1 43 __ LDA (T0 + 0),y 
.s1001:
0c94 : 60 __ __ RTS
--------------------------------------------------------------------
divmod:
0c95 : a5 1c __ LDA ACCU + 1 
0c97 : d0 31 __ BNE $0cca ; (divmod + 53)
0c99 : a5 04 __ LDA WORK + 1 
0c9b : d0 1e __ BNE $0cbb ; (divmod + 38)
0c9d : 85 06 __ STA WORK + 3 
0c9f : a2 04 __ LDX #$04
0ca1 : 06 1b __ ASL ACCU + 0 
0ca3 : 2a __ __ ROL
0ca4 : c5 03 __ CMP WORK + 0 
0ca6 : 90 02 __ BCC $0caa ; (divmod + 21)
0ca8 : e5 03 __ SBC WORK + 0 
0caa : 26 1b __ ROL ACCU + 0 
0cac : 2a __ __ ROL
0cad : c5 03 __ CMP WORK + 0 
0caf : 90 02 __ BCC $0cb3 ; (divmod + 30)
0cb1 : e5 03 __ SBC WORK + 0 
0cb3 : 26 1b __ ROL ACCU + 0 
0cb5 : ca __ __ DEX
0cb6 : d0 eb __ BNE $0ca3 ; (divmod + 14)
0cb8 : 85 05 __ STA WORK + 2 
0cba : 60 __ __ RTS
0cbb : a5 1b __ LDA ACCU + 0 
0cbd : 85 05 __ STA WORK + 2 
0cbf : a5 1c __ LDA ACCU + 1 
0cc1 : 85 06 __ STA WORK + 3 
0cc3 : a9 00 __ LDA #$00
0cc5 : 85 1b __ STA ACCU + 0 
0cc7 : 85 1c __ STA ACCU + 1 
0cc9 : 60 __ __ RTS
0cca : a5 04 __ LDA WORK + 1 
0ccc : d0 1f __ BNE $0ced ; (divmod + 88)
0cce : a5 03 __ LDA WORK + 0 
0cd0 : 30 1b __ BMI $0ced ; (divmod + 88)
0cd2 : a9 00 __ LDA #$00
0cd4 : 85 06 __ STA WORK + 3 
0cd6 : a2 10 __ LDX #$10
0cd8 : 06 1b __ ASL ACCU + 0 
0cda : 26 1c __ ROL ACCU + 1 
0cdc : 2a __ __ ROL
0cdd : c5 03 __ CMP WORK + 0 
0cdf : 90 02 __ BCC $0ce3 ; (divmod + 78)
0ce1 : e5 03 __ SBC WORK + 0 
0ce3 : 26 1b __ ROL ACCU + 0 
0ce5 : 26 1c __ ROL ACCU + 1 
0ce7 : ca __ __ DEX
0ce8 : d0 f2 __ BNE $0cdc ; (divmod + 71)
0cea : 85 05 __ STA WORK + 2 
0cec : 60 __ __ RTS
0ced : a9 00 __ LDA #$00
0cef : 85 05 __ STA WORK + 2 
0cf1 : 85 06 __ STA WORK + 3 
0cf3 : 84 02 __ STY $02 
0cf5 : a0 10 __ LDY #$10
0cf7 : 18 __ __ CLC
0cf8 : 26 1b __ ROL ACCU + 0 
0cfa : 26 1c __ ROL ACCU + 1 
0cfc : 26 05 __ ROL WORK + 2 
0cfe : 26 06 __ ROL WORK + 3 
0d00 : 38 __ __ SEC
0d01 : a5 05 __ LDA WORK + 2 
0d03 : e5 03 __ SBC WORK + 0 
0d05 : aa __ __ TAX
0d06 : a5 06 __ LDA WORK + 3 
0d08 : e5 04 __ SBC WORK + 1 
0d0a : 90 04 __ BCC $0d10 ; (divmod + 123)
0d0c : 86 05 __ STX WORK + 2 
0d0e : 85 06 __ STA WORK + 3 
0d10 : 88 __ __ DEY
0d11 : d0 e5 __ BNE $0cf8 ; (divmod + 99)
0d13 : 26 1b __ ROL ACCU + 0 
0d15 : 26 1c __ ROL ACCU + 1 
0d17 : a4 02 __ LDY $02 
0d19 : 60 __ __ RTS
--------------------------------------------------------------------
bitshift:
0d5c : __ __ __ BYT 00 00 00 00 00 00 00 00 01 02 04 08 10 20 40 80 : ............. @.
0d6c : __ __ __ BYT 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 : ................
0d7c : __ __ __ BYT 80 40 20 10 08 04 02 01 00 00 00 00 00 00 00 00 : .@ .............
0d8c : __ __ __ BYT 00 00 00 00 00 00 00 00                         : ........
