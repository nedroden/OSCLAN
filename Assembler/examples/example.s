.global _main
.align 8

_main:
	mov   x0, xzr
	mov   x16, #1
	svc   #0x80
