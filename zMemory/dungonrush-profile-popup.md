---
name: dungonrush-profile-popup
description: UIProfilePopup (hồ sơ của mình + Settings) dựng 2026-10-12 từ rip ProfilePopup, mở từ ô avatar lobby; hành vi gốc đã reverse + danh sách nút còn "coming soon"
metadata:
  type: project
---
Bấm ô avatar ở lobby (`UIMainLobby.btProfile`) → `UIProfilePopup` (UIKey.ProfilePopup, `Resources/Prefabs/UI/UIProfilePopup`, script `Scripts/UI/Profile/UIProfilePopup.cs`), dựng bằng `ProfilePrefabBuilder.BuildProfilePopup` (menu Tools/DungeonRush/Build Profile UI/Profile Popup — chạy lại là GHI ĐÈ prefab). Test Play OK 2026-10-12, CHƯA commit.

**Đã chạy:** tab Profile | Settings (`TabSelector` + prefab `UINew/Clan/TabSelectorItemUI`); avatar + nút bút → `UIAvatarSelectPopup`; Power; tên; slider Sound FX / Music lưu `UserSettingData.soundVolume/musicVolume` (0..1, thêm mới — `isSoundOn/isMusicOn` cũ của base không ai dùng); nút gạt rung `ToggleButton` (`Scripts/UI/Common`, port component gốc) lưu `isHapticOn`; User ID + nút chép; Version. `AudioManager.SfxVolume/isMuteSfx` giờ đọc thẳng từ save (BaseUnit cũng áp âm lượng).

**Hành vi gốc (reverse ProfilePopup il2cpp):** kqs = tay nắm slider ghi On khi value > 0 / Off khi = 0, màu DefaultButtonColor / DisabledButtonColor; kqm = tab 0 Profile, 1 Settings; kqj(int) = đặt tab mở đầu; nhãn tab `Popup.Profile.Tab.Profile/Settings`; "Version " + Application.version; "User ID: " + id, chép vào clipboard + toast `Popup.Profile.UserIdCopied`; Discord gốc `https://discord.gg/ErRm7dkAs4` (của game gốc — KHÔNG dùng). ToggleButton: núm ở ±(rộng khung/2 − Padding 40), chữ nằm đối xứng phía kia.

**Đã nối 2026-10-13 ([[dungonrush-account-system]]):** nút đổi tên → UINicknamePopup; mục Account (Sign In / Up ↔ Sign Out, Sync Now, dòng email · Last sync).

**Còn "coming soon" (`listButtonComingSoon`, giữ nguyên bố cục gốc):** Redeem Code (`Prefabs/UI/Settings/RedeemCodePopup`), Discord, Country (`CountrySelectPopup`, cờ ở `_ResourceGame/_CommingUse/Flags`), Language (`LanguageSelectPopup`), Report Bug, Restore Purchase (đang ẩn như rip). Chưa có: nhạc nền (musicVolume mới lưu), rung thật, PowerButton chưa có hành động, User ID rỗng khi chưa đăng nhập Firebase, `UIMainLobby.txtNamePlayer` vẫn chưa gán tên.
Xem [[dungonrush-boss-rush-model]] (popup xem người khác + avatar), [[dungonrush-uinew-prefab-folder]].
