# Bắt traffic server Dungeon Rush trên LDPlayer 14 (Android 14, rooted)

Mục tiêu: dump các API response JSON (stat quái theo wave, PvP, reward, remote config số...)
mà game tải từ server — phần KHÔNG có trong APK tĩnh.

Đã xác nhận: LDPlayer instance 0 chạy, adb `127.0.0.1:5555`, root uid=0, Android 14, x86_64,
game đã cài. mitmproxy 12.2.3 đã cài trên host.

Biến dùng chung (PowerShell hoặc Git Bash trên host):
```
ADB="/c/LDPlayer/LDPlayer14/adb.exe -s 127.0.0.1:5555"
MITM="/c/Users/admin/AppData/Local/Programs/Python/Python313/Scripts/mitmdump.exe"
OUT="E:/00Work/00Project/2026DungOnRush/DecodedData/network"
```

## Bước 1 — Chạy mitmdump (terminal riêng, để nguyên)
```
"$MITM" --listen-port 8080 -w "$OUT/flows.mitm" --set stream_large_bodies=5m
```
Lần đầu chạy sẽ sinh CA cert ở `~/.mitmproxy/mitmproxy-ca-cert.cer`.

## Bước 2 — Route traffic emulator qua proxy (adb reverse)
```
$ADB reverse tcp:8080 tcp:8080
$ADB shell settings put global http_proxy 127.0.0.1:8080
```

## Bước 3 — Cài mitmproxy CA vào system store (Android 14 APEX bind-mount)
App hiện đại không tin user-CA, phải cài system-CA. Android 14 để cacerts trong APEX read-only,
dùng kỹ thuật bind-mount:
```
# hash tên file cert theo chuẩn Android
HASH=$(openssl x509 -inform PEM -subject_hash_old -in ~/.mitmproxy/mitmproxy-ca-cert.cer -noout)
cp ~/.mitmproxy/mitmproxy-ca-cert.cer /tmp/$HASH.0
$ADB push /tmp/$HASH.0 /data/local/tmp/$HASH.0

$ADB shell su -c '
  # tạo bản copy có thể ghi của cacerts rồi bind-mount đè lên apex
  mkdir -p /data/local/tmp/cacerts
  cp /apex/com.android.conscrypt/cacerts/* /data/local/tmp/cacerts/ 2>/dev/null
  cp /system/etc/security/cacerts/* /data/local/tmp/cacerts/ 2>/dev/null
  mv /data/local/tmp/'$HASH'.0 /data/local/tmp/cacerts/
  chmod 644 /data/local/tmp/cacerts/*
  chcon u:object_r:system_file:s0 /data/local/tmp/cacerts/*
  mount --bind /data/local/tmp/cacerts /apex/com.android.conscrypt/cacerts
  # bind vào mọi namespace zygote để app con nhìn thấy
  for pid in 1 $(pgrep zygote) $(pgrep zygote64); do
     nsenter --mount=/proc/$pid/ns/mnt -- \
       mount --bind /data/local/tmp/cacerts /apex/com.android.conscrypt/cacerts 2>/dev/null
  done
'
```
Nếu bản LDPlayer vẫn dùng `/system/etc/security/cacerts` (một số build), chỉ cần:
```
$ADB shell su -c 'mount -o rw,remount /system; cp /data/local/tmp/'$HASH'.0 /system/etc/security/cacerts/; chmod 644 /system/etc/security/cacerts/'$HASH'.0'
```

## Bước 4 — Khởi động lại game & thao tác
```
$ADB shell am force-stop com.lavalabs.dungeonrush
$ADB shell monkey -p com.lavalabs.dungeonrush -c android.intent.category.LAUNCHER 1
```
Vào game, mở lần lượt: màn chơi (wave), PvP/Arena, Boss Rush, Shop/Offer, Clan, mở rương —
mỗi màn kích hoạt API tương ứng. mitmdump sẽ ghi vào `flows.mitm`.

## Bước 5 (nếu vẫn không thấy traffic = có cert pinning) — frida unpin
frida 17.16.4 đã có trên host. Cần frida-server **x86_64** trong emulator:
```
# tải frida-server-<ver>-android-x86_64 khớp version 17.16.4, đẩy vào:
$ADB push frida-server /data/local/tmp/frida-server
$ADB shell su -c 'chmod 755 /data/local/tmp/frida-server; /data/local/tmp/frida-server &'
# chạy game qua script gỡ pinning (dùng script "frida-multiple-unpinning")
frida -U -f com.lavalabs.dungeonrush -l unpin.js
```

## Bước 6 — Gỡ proxy sau khi xong
```
$ADB shell settings delete global http_proxy
$ADB reverse --remove tcp:8080
```

## Sau khi có flows.mitm
Báo tôi — tôi sẽ parse `flows.mitm` (đọc bằng mitmproxy io), lọc host game
(`*.run.app`, `*.cloudfunctions.net`, `firebaseremoteconfig.googleapis.com`, Firestore),
tách JSON response thành bảng data như các bảng trong `tables/`.
