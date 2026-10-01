---
name: dungonrush-mediation-ads
description: Lớp quảng cáo MediationAds (copy từ StickIdle) — mọi chỗ xem ads gọi MediationAds.Instance; SDK thật sau này viết class kế thừa
metadata:
  type: project
  modified: 2026-09-30
---

`_Assets/Scripts/Ads/MediationAds.cs` copy nguyên từ StickIdle (kèm .meta, guid 4d8f024e...): `Singleton<MediationAds>` + enum `ShowResultADS` + class `Keys`. Chỉ lấy lớp BASE — KHÔNG lấy MaxAdController/IronSourceAdController (cần SDK Max/IronSource/AppsFlyer/Firebase chưa có).

Chỗ gọi: `MediationAds.Instance.ShowRewardedVideoAd(placement, result => { if (result == ShowResultADS.Finished) ... })`. Đã nối: CompanionUI.OnClickSummonAds (placement "summon_companion"). UIDungeonPopupStart.OnClickAds (placement "dungeon_key", +1 bonus key).

**Why:** User muốn chỗ gọi ads cố định; sau chỉ viết class kế thừa (vd MaxAdController : MediationAds, bọc #if MAX) đặt trên GameObject trong scene để Singleton FindObjectOfType trả về lớp con.

**How to apply:** Lớp base chỉ gọi callback Finished trong UNITY_EDITOR; build thiết bị mà chưa có lớp con → callback KHÔNG chạy (nút ads không làm gì). Placement đặt theo kiểu StickIdle: snake_case "<feature>_<action>". Xem [[stickidle-architecture]], [[dungonrush-companion-todo]].
