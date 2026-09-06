import zipfile, os, io, sys

XAPK = r"E:\Project\2026DungeonRush\Dungeon+Rush_41_APKPure.xapk"
OUT = os.path.dirname(os.path.abspath(__file__))

def find_in_zip(zf, pred):
    return [n for n in zf.namelist() if pred(n)]

with zipfile.ZipFile(XAPK) as outer:
    print("=== APKs trong xapk ===")
    apks = [n for n in outer.namelist() if n.endswith(".apk")]
    for a in apks:
        print(" ", a, outer.getinfo(a).file_size)

    got_so = got_meta = False
    for a in apks:
        data = outer.read(a)
        with zipfile.ZipFile(io.BytesIO(data)) as inner:
            # libil2cpp.so (arm64)
            for n in inner.namelist():
                if n.endswith("arm64-v8a/libil2cpp.so") and not got_so:
                    p = os.path.join(OUT, "libil2cpp.so")
                    open(p, "wb").write(inner.read(n))
                    print("SO   <-", a, "!", n, os.path.getsize(p))
                    got_so = True
                if n.endswith("global-metadata.dat") and not got_meta:
                    p = os.path.join(OUT, "global-metadata.dat")
                    open(p, "wb").write(inner.read(n))
                    print("META <-", a, "!", n, os.path.getsize(p))
                    got_meta = True
    print("done", got_so, got_meta)
