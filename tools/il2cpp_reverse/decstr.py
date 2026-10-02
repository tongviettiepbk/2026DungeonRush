# Giải mã chuỗi bị obfuscate của game gốc (<PrivateImplementationDetails>{88089A81...}.a): byte[] ở metadata @0x7AD340, len 0x70B2F,
# XOR (i & 0xFF) ^ 0xAA; mỗi getter a$$xxx gọi a_(index, offset, len). Ra decstr.json {VA getter: [tên, chuỗi]}. Chạy cạnh disasm.py.
import json, struct
import disasm as D
from capstone import Cs, CS_ARCH_ARM64, CS_MODE_ARM
md=open('global-metadata.dat','rb').read()
N=0x70b2f
blob=bytearray(md[0x7AD340:0x7AD340+N])
for i in range(N): blob[i]^=(i&0xff)^0xaa
cs=Cs(CS_ARCH_ARM64,CS_MODE_ARM)
res={}
pre='<PrivateImplementationDetails>{88089A81-E4A1-48AB-B1F4-35A2039BBC0B}.a$$'
for m in D.sm:
    if not m['Name'].startswith(pre): continue
    va=m['Address']; o=D.va_to_off(va); code=D.raw[o:o+0xb0]
    regs={}
    for ins in cs.disasm(code,va):
        if ins.mnemonic=='mov' and ins.op_str.startswith(('w0,','w1,','w2,')):
            r,v=ins.op_str.split(', ')
            try: regs[r]=int(v.lstrip('#'),16)
            except: pass
        if ins.mnemonic=='b' and len(regs)==3:
            s=bytes(blob[regs['w1']:regs['w1']+regs['w2']]).decode('utf8','replace')
            res[hex(va)]=(m['Name'][len(pre):],s); break
json.dump(res,open('decstr.json','w'),ensure_ascii=False,indent=0)
print(len(res))
