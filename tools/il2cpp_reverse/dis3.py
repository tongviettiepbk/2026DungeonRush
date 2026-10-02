# Disasm có chú thích: STR= string literal, DSTR= chuỗi đã giải mã (cần decstr.json). usage: dis3.py <VA hex | Class$$method> ...
import sys, json, re
import disasm as D
sl={int(e['address'],16):e['value'] for e in json.load(open('dump/stringliteral.json'))}
ds={int(k,16):v[1] for k,v in json.load(open('decstr.json')).items()}
def show(va):
    print(f"\n##### 0x{va:X} size=0x{D.func_size(va):X} {D.name_by_addr.get(va,'?')}")
    for line in D.disasm(va, maxins=1500).split("\n"):
        m=re.search(r'@0x([0-9A-F]+)',line)
        if m and int(m.group(1),16) in sl: line+='  STR='+repr(sl[int(m.group(1),16)][:120])
        m=re.search(r'bl\t#0x([0-9a-f]+)',line)
        if m and int(m.group(1),16) in ds: line+='  DSTR='+repr(ds[int(m.group(1),16)][:120])
        if 'PrivateImplementationDetails' in line: line=re.sub(r'; -> <Priv.*?\$\$\w+','',line)
        print(line)
if __name__=='__main__':
    for a in sys.argv[1:]:
        if a.startswith('0x') or re.fullmatch('[0-9A-Fa-f]+',a): show(int(a,16))
        else:
            for m in D.sm:
                if m['Name']==a: show(m['Address'])
