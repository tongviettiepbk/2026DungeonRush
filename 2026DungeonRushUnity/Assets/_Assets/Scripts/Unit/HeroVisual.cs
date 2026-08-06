using Spine.Unity;
using UnityEngine;

// Hình ảnh trang bị của Hero. Ref các node trên rig được gán sẵn trong prefab (đơn giản hơn
// resolve theo tên). Sprite/Spine lấy TRỰC TIẾP từ data gear qua EquipVisualResolver.
//   - 5 slot sprite: Weapon/Helmet/Gloves/Backpack/Wing → set SpriteRenderer.
//   - Cape: Spine — gán skeletonData + đổi skin lấy từ CapeData.
//
// Quy tắc hiển thị: CHỈ bật (SetActive) object của slot khi MẶC THÀNH CÔNG (có hình). Riêng
// bàn tay (handLeft/handRight) LUÔN bật vì có sprite mặc định — mặc găng thì đè sprite lên,
// cởi găng thì trả lại sprite mặc định (đã cache lúc đầu).
public class HeroVisual : MonoBehaviour
{
    public HeroUnit heroController;

    [Space(20)]
    public SpriteRenderer handLeft;
    public SpriteRenderer handRight;
    public SpriteRenderer weapon;
    public SpriteRenderer helmet;
    public SpriteRenderer backPack;
    public SpriteRenderer wing1;
    public SpriteRenderer wing2;
    public SkeletonAnimation cape;

    [Header("Test")]
    [Tooltip("Sprite thay thế khi data gear CHƯA gán art — để test được logic bật object.")]
    public Sprite debugSprite;

    // Sprite mặc định của bàn tay (bare hand) để cởi găng thì trả lại.
    private Sprite defaultHandLeft;
    private Sprite defaultHandRight;
    private bool defaultsCached;

    // Dựng lại toàn bộ hình từ save. Gọi lúc Hero khởi tạo và sau khi người chơi đổi đồ.
    public void RefreshAll()
    {
        CacheHandDefaults();

        UserEquipmentData data = GameData.userData != null ? GameData.userData.equipment : null;
        if (data == null)
        {
            return;
        }

        WearEquipment(GearSlotType.WEAPON, data.GetEquipped(GearSlotType.WEAPON));
        WearEquipment(GearSlotType.HELMET, data.GetEquipped(GearSlotType.HELMET));
        WearEquipment(GearSlotType.GLOVES, data.GetEquipped(GearSlotType.GLOVES));
        WearEquipment(GearSlotType.BACKPACK, data.GetEquipped(GearSlotType.BACKPACK));
        WearEquipment(GearSlotType.WING, data.GetEquipped(GearSlotType.WING));
        WearEquipment(GearSlotType.CAPE, data.GetEquipped(GearSlotType.CAPE));
    }

    // Mặc 1 slot. id rỗng = cởi (ẩn object, riêng găng thì trả sprite mặc định).
    public void WearEquipment(GearSlotType slot, string id)
    {
        if (slot == GearSlotType.CAPE)
        {
            WearCape(id);
            return;
        }

        Sprite sprite = EquipVisualResolver.GetBodySprite(slot, id);
        switch (slot)
        {
            case GearSlotType.WEAPON:
                ApplySlot(weapon, sprite);
                break;
            case GearSlotType.HELMET:
                ApplySlot(helmet, sprite);
                break;
            case GearSlotType.GLOVES:
                ApplyGloves(sprite);
                break;
            case GearSlotType.BACKPACK:
                ApplySlot(backPack, sprite);
                break;
            case GearSlotType.WING:
                ApplySlot(wing1, sprite);
                ApplySlot(wing2, sprite);
                break;
            default:
                Debug.LogWarning($"Unsupported equip slot: {slot}");
                break;
        }
    }

    // Slot thường: chỉ bật object khi mặc thành công (có sprite), không thì tắt.
    private static void ApplySlot(SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null)
        {
            return;
        }

        if (sprite != null)
        {
            sr.sprite = sprite;
        }
        sr.gameObject.SetActive(sprite != null);
    }

    // Găng tay: bàn tay LUÔN bật. Mặc găng đè sprite, cởi thì trả sprite mặc định.
    private void ApplyGloves(Sprite sprite)
    {
        CacheHandDefaults();
        SetHand(handLeft, sprite != null ? sprite : defaultHandLeft);
        SetHand(handRight, sprite != null ? sprite : defaultHandRight);
    }

    private static void SetHand(SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null)
        {
            return;
        }

        sr.gameObject.SetActive(true);
        sr.sprite = sprite;
    }

    // Cache sprite tay mặc định 1 lần, TRƯỚC khi có thao tác đè găng đầu tiên.
    private void CacheHandDefaults()
    {
        if (defaultsCached)
        {
            return;
        }

        if (handLeft != null) defaultHandLeft = handLeft.sprite;
        if (handRight != null) defaultHandRight = handRight.sprite;
        defaultsCached = true;
    }

    // Cape = Spine skin. Cape gốc chia 3 skeleton (short/mid/long) nên đổi cape có thể phải
    // đổi cả skeletonData rồi re-init; đổi trong cùng skeleton thì chỉ SetSkin. Chỉ bật object
    // khi mặc thành công.
    private void WearCape(string id)
    {
        if (cape == null)
        {
            return;
        }

        CapeData data = EquipVisualResolver.GetCapeData(id);
        if (data == null || data.skeletonData == null || string.IsNullOrEmpty(data.skinName))
        {
            cape.gameObject.SetActive(false);
            return;
        }

        cape.gameObject.SetActive(true);

        if (cape.Skeleton == null || cape.skeletonDataAsset != data.skeletonData)
        {
            cape.skeletonDataAsset = data.skeletonData;
            cape.initialSkinName = data.skinName;
            cape.Initialize(true);
        }
        else
        {
            cape.Skeleton.SetSkin(data.skinName);
            cape.Skeleton.SetSlotsToSetupPose();
        }
    }

    // ===== TEST (chạy ở Play mode: chuột phải component trong Inspector) =====
    // Mặc thử món ĐẦU TIÊN của mỗi loại lấy trực tiếp từ static data — chỉ đổi HÌNH, KHÔNG
    // đụng save. Slot nào data CHƯA có art thì fallback debugSprite để vẫn thấy object bật lên.
    // Log rõ từng slot lấy hình từ đâu để biết data nào còn thiếu.
    [ContextMenu("TEST: Mặc thử mỗi slot 1 món")]
    public void TestWearFirstEachSlot()
    {
        if (GameData.staticData == null)
        {
            Debug.LogWarning("[HeroVisual] TEST cần Play mode (GameData.staticData chưa load).");
            return;
        }

        TestApplySprite(GearSlotType.WEAPON, FirstWeaponId());
        TestApplySprite(GearSlotType.HELMET, FirstGearId(GearSlotType.HELMET));
        TestApplySprite(GearSlotType.GLOVES, FirstGearId(GearSlotType.GLOVES));
        TestApplySprite(GearSlotType.BACKPACK, FirstGearId(GearSlotType.BACKPACK));
        TestApplySprite(GearSlotType.WING, FirstWingId());

        string capeId = FirstCapeId();
        WearEquipment(GearSlotType.CAPE, capeId);
        Debug.Log($"[HeroVisual] TEST Cape: capeId='{capeId}' (spine).");
    }

    // Áp hình test cho 1 slot sprite: ưu tiên art trong data, thiếu thì dùng debugSprite.
    private void TestApplySprite(GearSlotType slot, string id)
    {
        Sprite fromData = EquipVisualResolver.GetBodySprite(slot, id);
        Sprite s = fromData != null ? fromData : debugSprite;
        Debug.Log($"[HeroVisual] TEST {slot}: id='{id}', " +
                  (fromData != null ? $"dùng art data ({fromData.name})"
                   : s != null ? "data CHƯA có art → dùng debugSprite"
                   : "data CHƯA có art & debugSprite TRỐNG → object sẽ ẩn"));

        if (slot == GearSlotType.GLOVES)
        {
            ApplyGloves(s);
        }
        else if (slot == GearSlotType.WING)
        {
            ApplySlot(wing1, s);
            ApplySlot(wing2, s);
        }
        else if (slot == GearSlotType.WEAPON)
        {
            ApplySlot(weapon, s);
        }
        else if (slot == GearSlotType.HELMET)
        {
            ApplySlot(helmet, s);
        }
        else if (slot == GearSlotType.BACKPACK)
        {
            ApplySlot(backPack, s);
        }
    }

    // Cởi hết (kiểm tra object tắt đúng, tay trả sprite mặc định).
    [ContextMenu("TEST: Cởi hết")]
    public void TestUnwearAll()
    {
        WearEquipment(GearSlotType.WEAPON, null);
        WearEquipment(GearSlotType.HELMET, null);
        WearEquipment(GearSlotType.GLOVES, null);
        WearEquipment(GearSlotType.BACKPACK, null);
        WearEquipment(GearSlotType.WING, null);
        WearEquipment(GearSlotType.CAPE, null);
        Debug.Log("[HeroVisual] TEST: đã cởi hết.");
    }

    private static string FirstWeaponId()
    {
        var list = GameData.staticData.weapons != null ? GameData.staticData.weapons.weapons : null;
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].isMonsterWeapon == false) return list[i].assetName;
        }
        return null;
    }

    private static string FirstGearId(GearSlotType slot)
    {
        if (GameData.staticData.gears == null) return null;
        var list = GameData.staticData.gears.GetBySlot(slot);
        return list.Count > 0 ? list[0].assetName : null;
    }

    private static string FirstWingId()
    {
        var list = GameData.staticData.wings != null ? GameData.staticData.wings.wings : null;
        return list != null && list.Count > 0 ? list[0].wingId.ToString() : null;
    }

    private static string FirstCapeId()
    {
        var list = GameData.staticData.capes != null ? GameData.staticData.capes.capes : null;
        return list != null && list.Count > 0 ? list[0].capeId.ToString() : null;
    }
}
