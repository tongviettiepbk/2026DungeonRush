using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Công cụ test (chỉ Editor): bảng chọn đồ mặc lên hero ngay trong Game view.
// Đặt ở scene Root → DontDestroyOnLoad, sống qua Login → MainGame.
// Bấm nút "Đồ" góc dưới-trái để mở bảng; mỗi slot bấm < > để lướt lần lượt từng món
// (vị trí đầu = trống/cởi). Đổi là mặc + lưu save ngay, hero/lobby vẽ lại qua EquipmentChanged.
// Rarity lấy theo catalog món, level dùng ô Level chung, substat để trống.
// Cape chỉ ghi vào slot CAPE của UserEquipmentData (không đụng kho cape UserCapeData).
public class EquipmentTester : MonoBehaviour
{
    public bool showPanel;
    public int level = 1;

    private static EquipmentTester instance;

    private static readonly GearSlotType[] SLOTS =
    {
        GearSlotType.WEAPON, GearSlotType.HELMET, GearSlotType.GLOVES, GearSlotType.RING,
        GearSlotType.NECKLACE, GearSlotType.BACKPACK, GearSlotType.WING, GearSlotType.CAPE,
    };

    private class Option
    {
        public string id;
        public string name;
        public Rarity rarity;
    }

    private Dictionary<GearSlotType, List<Option>> options;

#if UNITY_EDITOR
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnGUI()
    {
        // Chỉ ở MainGame: RefreshLobby chạm GameController.Instance — ở Login Singleton sẽ tự tạo GameController.
        if (SceneManager.GetActiveScene().name != StaticValue.SCENE_MAINGAME)
            return;
        if (GameData.userData == null || GameData.userData.equipment == null)
            return;

        // Game view dọc 1080x2160 → phóng IMGUI theo chiều ngang 540.
        float scale = Screen.width / 540f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float height = Screen.height / scale;

        if (GUI.Button(new Rect(4, height - 34, 50, 30), showPanel ? "X" : "Đồ"))
        {
            showPanel = !showPanel;
            if (showPanel)
                options = null;
        }

        if (showPanel == false)
            return;

        if (options == null)
            BuildOptions();

        GUILayout.BeginArea(new Rect(4, 60, 532, 420), GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Level", GUILayout.Width(50));
        string levelText = GUILayout.TextField(level.ToString(), GUILayout.Width(60));
        if (int.TryParse(levelText, out int newLevel))
            level = Mathf.Max(1, newLevel);
        if (GUILayout.Button("Áp level", GUILayout.Width(80)))
            ApplyLevelAll();
        if (GUILayout.Button("Cởi hết", GUILayout.Width(80)))
            UnequipAll();
        GUILayout.EndHorizontal();

        for (int i = 0; i < SLOTS.Length; i++)
            DrawSlot(SLOTS[i]);

        GUILayout.EndArea();
    }

    private void DrawSlot(GearSlotType slot)
    {
        List<Option> list = options[slot];
        int index = FindIndex(list, GameData.userData.equipment.GetEquipped(slot));

        GUILayout.BeginHorizontal();
        GUILayout.Label(slot.ToString(), GUILayout.Width(80));
        if (GUILayout.Button("<", GUILayout.Width(30)))
            Select(slot, list, index - 1);
        if (GUILayout.Button(">", GUILayout.Width(30)))
            Select(slot, list, index + 1);

        string text = index < 0
            ? "(trống)"
            : $"{index + 1}/{list.Count}  {list[index].name}  [{list[index].rarity}]";
        GUILayout.Label(text);
        GUILayout.EndHorizontal();
    }

    // index -1 = trống; vòng quanh: trống → món 1 → ... → món cuối → trống.
    private void Select(GearSlotType slot, List<Option> list, int index)
    {
        if (index < -1)
            index = list.Count - 1;
        else if (index >= list.Count)
            index = -1;

        if (index < 0)
            GameData.userData.equipment.Unequip(slot);
        else
            GameData.userData.equipment.Equip(slot, list[index].id, list[index].rarity, level, null);

        OnChanged(slot);
        DebugCustom.Log("[EquipmentTester] " + slot + " = " + (index < 0 ? "(trống)" : list[index].id));
    }

    private void ApplyLevelAll()
    {
        UserEquipmentData equipment = GameData.userData.equipment;
        for (int i = 0; i < SLOTS.Length; i++)
        {
            EquippedItemData rec = equipment.GetRecord(SLOTS[i]);
            if (rec != null)
                equipment.Equip(SLOTS[i], rec.equipId, rec.rarity, level, rec.subStats);
        }
        OnChanged(null);
    }

    private void UnequipAll()
    {
        for (int i = 0; i < SLOTS.Length; i++)
            GameData.userData.equipment.Unequip(SLOTS[i]);
        OnChanged(null);
    }

    // slot null = đổi nhiều slot → bắn event từng slot (HeroVisual chỉ vẽ lại slot có trong param).
    private void OnChanged(GearSlotType? slot)
    {
        GameData.Save();
        if (slot.HasValue)
        {
            this.PostEvent(EventID.EquipmentChanged, slot.Value);
        }
        else
        {
            for (int i = 0; i < SLOTS.Length; i++)
                this.PostEvent(EventID.EquipmentChanged, SLOTS[i]);
        }
        ShopUIUtil.RefreshLobby();
    }

    private static int FindIndex(List<Option> list, string id)
    {
        if (string.IsNullOrEmpty(id))
            return -1;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].id == id)
                return i;
        }
        return -1;
    }

    // Chỉ đồ người chơi (bỏ đồ quái/boss), xếp theo rarity.
    private void BuildOptions()
    {
        options = new Dictionary<GearSlotType, List<Option>>();
        for (int i = 0; i < SLOTS.Length; i++)
            options[SLOTS[i]] = new List<Option>();

        foreach (WeaponData weapon in GameData.staticData.weapons.weapons)
        {
            if (weapon.isMonsterWeapon == false)
                options[GearSlotType.WEAPON].Add(new Option { id = weapon.assetName, name = weapon.displayName, rarity = weapon.rarity });
        }

        foreach (GearItemData gear in GameData.staticData.gears.all)
        {
            if (gear.isMonsterGear == false && options.ContainsKey(gear.slot))
                options[gear.slot].Add(new Option { id = gear.assetName, name = gear.displayName, rarity = gear.rarity });
        }

        foreach (WingData wing in GameData.staticData.wings.wings)
            options[GearSlotType.WING].Add(new Option { id = wing.wingId.ToString(), name = wing.wingName, rarity = wing.rarity });

        foreach (CapeData cape in GameData.staticData.capes.capes)
            options[GearSlotType.CAPE].Add(new Option { id = cape.capeId.ToString(), name = cape.capeName, rarity = cape.rarity });

        foreach (List<Option> list in options.Values)
            list.Sort((a, b) => a.rarity.CompareTo(b.rarity));
    }
#endif
}
