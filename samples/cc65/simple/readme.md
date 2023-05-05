# Build steps

```
cc65 -g hello.c -o out/hello.s
ca65 -g -t c64 -l out/hello.lst -o out/hello.o out/hello.s
ca65 -g -t c64 -l out/text.lst -o out/text.o text.s
ld65 -t c64 --dbgfile out/hello.dbg out/hello.o out/text.o c64.lib
```