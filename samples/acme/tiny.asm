		!to "tiny.prg", cbm	; set output file and format
* = $0801

        !wo boot
        !wo 1 ; line number
        !by $9e ; SYS
        !TEXT "2061",0
boot   !wo 0
 
        * = 2061

		;* = $c000		; set program counter

	CLEAR = 147		; a global symbol definition
!addr	basout = $ffd2		; another one, marked as an address

		; a string output loop:
start	ldx #0
		lda .string
		beq +			; enter loop

-			jsr basout	; output character
			inx		; advance pointer
+			lda .string, x	; get character
			bne -		; check whether last
		rts
.string		!pet "Dumb example", 13, 0