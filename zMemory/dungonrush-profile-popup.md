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

**Còn "coming soon" (`listButtonComingSoon`, giữ nguyên bố cục gốc):** Redeem Code (`Prefabs/UI/Settings/RedeemCodePopup`), Discord, Country (`CountrySelectPopup`, cờ ở `Art/2_Icons/Flags/Language`), Report Bug, Restore Purchase (đang ẩn như rip). Chưa có: nhạc nền (musicVolume mới lưu), rung thật, PowerButton chưa có hành động, User ID rỗng khi chưa đăng nhập Firebase, `UIMainLobby.txtNamePlayer` vẫn chưa gán tên.
**Language (2026-10-17):** nút Language mở UILanguageSelectPopup, hiện cờ + tên ngôn ngữ đang dùng — [[dungonrush-localization]].
Xem [[dungonrush-boss-rush-model]] (popup xem người khác + avatar), [[dungonrush-uinew-prefab-folder]].
**Reskin (2026-10-18):** 4 popup Profile/Avatar/Nickname/Player đã reskin theo mockup — [[dungonrush-reskin-plan]]. KHÔNG chạy lại ProfilePrefabBuilder (ghi đè reskin).
**2026-11-12:** dòng Discord + Country ẨN trong prefab (link Discord gốc là game khác → const URL để trắng); quốc gia mặc định VN (`PvPController.DEFAULT_COUNTRY_CODE`, chat cũng gửi VN), cờ nút Country = Art/2_Icons/Flags/VN.png. Khung Content căn giữa (pos 0) + `UIFitInParent` (Scripts/UI/Common) tự thu nhỏ cho vừa màn thấp (iPad 4:3 → ×0.80), không phóng to.
**Tên ở lobby (2026-11-12):** UIMainLobby.txtNamePlayer trước đó KHÔNG được ghi (hiện chữ mẫu "Nicknames") → thêm LoadName() lúc Start + nghe EventID.NicknameChanged. UserData.Load cấp "PlayerXXXXXX" cho MỌI save có userName rỗng (save Editor từng có userName null sau Sign Out, chưa rõ gốc rễ).
