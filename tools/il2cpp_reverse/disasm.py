import json, sys, os, struct
import lief
from capstone import Cs, CS_ARCH_ARM64, CS_MODE_ARM

SP = os.path.dirname(os.path.abspath(__file__))
SO = os.path.join(SP, "libil2cpp.so")
SCRIPT = os.path.join(SP, "dump", "script.json")

# --- load ELF for VA->fileoffset + reading bytes/consts ---
elf = lief.parse(SO)
segs = [(s.virtual_address, s.virtual_address + s.virtual_size, s.file_offset, s)
        for s in elf.segments if s.type == lief.ELF.Segment.TYPE.LOAD]
raw = open(SO, "rb").read()

def va_to_off(va):
    for vs, ve, fo, s in segs:
        if vs <= va < ve:
            return fo + (va - vs)
    return None

def read_at_va(va, n):
    o = va_to_off(va)
    if o is None: return None
    return raw[o:o+n]

def read_f32(va):
    b = read_at_va(va, 4)
    return struct.unpack("<f", b)[0] if b else None
def read_u64(va):
    b = read_at_va(va, 8)
    return struct.unpack("<Q", b)[0] if b else None

# --- method address table from script.json ---
sm = json.load(open(SCRIPT, encoding="utf-8"))["ScriptMethod"]
addrs = sorted(set(m["Address"] for m in sm))
name_by_addr = {}
for m in sm:
    name_by_addr.setdefault(m["Address"], m["Name"])

import bisect
def func_size(va):
    i = bisect.bisect_right(addrs, va)
    if i < len(addrs):
        return addrs[i] - va
    return 0x200

md = Cs(CS_ARCH_ARM64, CS_MODE_ARM)
md.detail = False

# track adrp page per register for const resolution
def disasm(va, size=None, maxins=400):
    if size is None:
        size = min(func_size(va), 0x1200)
    o = va_to_off(va)
    code = raw[o:o+size]
    page = {}   # reg -> adrp page base
    out = []
    n = 0
    for ins in md.disasm(code, va):
        n += 1
        line = f"0x{ins.address:X}: {ins.mnemonic}\t{ins.op_str}"
        note = ""
        m = ins.mnemonic
        ops = ins.op_str
        if m == "adrp":
            try:
                reg, imm = ops.split(", ")
                page[reg] = int(imm, 16)
            except: pass
        elif m in ("ldr","ldrsw") and "]" in ops and "#" in ops:
            # ldr sN, [xR, #off]  -> const if xR is an adrp page
            try:
                dst, mem = ops.split(", [", 1)
                base = mem.split(",")[0].strip()
                off = 0
                if "#" in mem:
                    off = int(mem.split("#")[1].rstrip("]!"), 16)
                if base in page:
                    ca = page[base] + off
                    if dst.startswith("s"):
                        note = f"  ; =float {read_f32(ca)} @0x{ca:X}"
                    elif dst.startswith("d"):
                        b = read_at_va(ca,8);
                        note = f"  ; =double {struct.unpack('<d',b)[0]} @0x{ca:X}" if b else ""
                    else:
                        note = f"  ; =0x{(read_u64(ca) or 0):X} @0x{ca:X}"
            except Exception as e:
                pass
        elif m == "add" and "#" in ops:
            # add xR, xR, #imm following adrp -> final page addr
            try:
                parts = [p.strip() for p in ops.split(",")]
                if len(parts)==3 and parts[1] in page and parts[2].startswith("#"):
                    page[parts[0]] = page[parts[1]] + int(parts[2][1:],16)
            except: pass
        elif m == "bl":
            tgt = ops.strip()
            if tgt.startswith("#"):
                ta = int(tgt[1:],16)
                nm = name_by_addr.get(ta)
                if nm: note = f"  ; -> {nm}"
        elif m in ("fmov",) and "#" in ops:
            note = "  ; imm fmov"
        out.append(line+note)
        if n >= maxins:
            out.append("... (truncated)")
            break
    return "\n".join(out)

if __name__ == "__main__":
    for a in sys.argv[1:]:
        va = int(a,16)
        print(f"\n########## FUNC @0x{va:X}  size=0x{func_size(va):X}  {name_by_addr.get(va,'?')} ##########")
        print(disasm(va))
