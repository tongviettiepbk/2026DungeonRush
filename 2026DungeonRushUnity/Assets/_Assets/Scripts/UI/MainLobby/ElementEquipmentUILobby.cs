using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElementEquipmentUILobby : MonoBehaviour
{
    public GearSlotType typeEquipment;
    public Button btEquipment;
    public TMP_Text txtLevel;
    public TMP_Text txtLevelEnchantment;
    public Image imgEquipment;

    public Button btEnchantmentInfo;

    // UIMainLobby gán: bấm nút enchantment → mở trang Enchantment.
    public Action<GearSlotType> onClickEnchantment;

    // UIMainLobby gán: bấm ô Wing → mở trang Wing (ô Wing không mở popup info gear).
    public Action onClickWing;

    private Sprite defaultSprite;
    private bool isDefaultSpriteSaved;

    private LootResult dataGear;

    // Đồ đang mặc ở slot này (null nếu chưa được set layout trong phiên chơi này).
    public LootResult GetDataGear()
    {
        return dataGear;
    }

    private void Start()
    {
        btEquipment.onClick.AddListener(ClickBtInfoGear);
        if (btEnchantmentInfo != null)
            btEnchantmentInfo.onClick.AddListener(ClickBtEnchantment);
    }

    private void OnEnable()
    {
        EventDispatcher.Instance.RegisterListener(EventID.EnchantmentChanged, OnEnchantmentChanged);
        RefreshEnchantment();
    }

    private void OnDisable()
    {
        EventDispatcher.Instance.RemoveListener(EventID.EnchantmentChanged, OnEnchantmentChanged);
    }

    // Gắn item vừa loot vào slot: icon + level. Null-guard từng ref vì prefab có thể chưa wire hết.
    public void SetLayout(LootResult result)
    {
        this.dataGear = result;
        RefreshEnchantment();

        if (result == null)
            return;

        if (imgEquipment != null)
        {
            DebugCustom.Log("set layout");

            imgEquipment.sprite = result.icon;
            //imgEquipment.enabled = result.icon != null; // ẩn Image nếu item không có icon.
        }

        string levelText = "Lv." + result.level;
        if (txtLevel != null)
            txtLevel.text = levelText;
    }

    private void OnEnchantmentChanged(object param)
    {
        RefreshEnchantment();
    }

    // Nhãn "+tier" relic đang đeo ở slot — hiện khi tier >= 1, không xét slot có đồ hay không (ItemElementUI.ict gốc).
    private void RefreshEnchantment()
    {
        if (GameData.userData == null)
            return;

        if (btEnchantmentInfo != null)
            btEnchantmentInfo.gameObject.SetActive(EnchantmentService.IsUnlocked());

        if (txtLevelEnchantment == null)
            return;

        int tier = GameData.userData.enchantments.GetEquipped(typeEquipment);
        bool isShow = tier >= 1;
        txtLevelEnchantment.gameObject.SetActive(isShow);
        if (isShow)
            txtLevelEnchantment.text = "+" + tier;
    }

    private void ClickBtEnchantment()
    {
        onClickEnchantment?.Invoke(typeEquipment);
    }

    // Ô Wing: icon + "Lv.N" wing đang mặc; null → trả về hình mặc định của ô.
    public void SetWing(WingData wing, int level)
    {
        if (imgEquipment != null)
        {
            if (isDefaultSpriteSaved == false)
            {
                defaultSprite = imgEquipment.sprite;
                isDefaultSpriteSaved = true;
            }
            imgEquipment.sprite = wing != null ? wing.icon : defaultSprite;
        }

        if (txtLevel != null)
            txtLevel.text = wing != null ? "Lv." + level : string.Empty;
    }

    private void ClickBtInfoGear()
    {
        if (typeEquipment == GearSlotType.WING)
        {
            onClickWing?.Invoke();
            return;
        }

        if (this.dataGear == null)
        {
            UIManager.Instance.ShowToastMessage("Chưa có trang bị", isLocalize: false);
        }
        else
        {
            UIGearInfo uiGearInfo = UIManager.Instance.LoadUI(UIKey.InfoGear) as UIGearInfo;
            if (uiGearInfo != null)
                uiGearInfo.Show(this.dataGear, GameData.userData.enchantments.GetEquipped(typeEquipment));
        }
    }
}
