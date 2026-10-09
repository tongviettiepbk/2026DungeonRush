using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô relic (EnchantmentElementUI gốc): nền theo tier + icon tier + "Lvl tier".
// Dùng cho kho relic ở trang Enchantment, kết quả summon (UISumonEnchantment) và popup Merge.
public class ElementEchantmentUI : MonoBehaviour
{
    public Button btChose;
    public TMP_Text textLvl;
    public Image imgIcon;
    public Image imgBgTier;

    private Action<ElementEchantmentUI> onClick;

    public int Tier { get; private set; }

    protected virtual void Awake()
    {
        if (btChose != null)
        {
            btChose.onClick.AddListener(OnClick);
        }
    }

    public void Init(Action<ElementEchantmentUI> onClick)
    {
        this.onClick = onClick;
    }

    // EnchantmentElementUI.hxh gốc: nền = GameResources.jgg(max(tier,1)); tier <= 0 = ô trống
    // (ẩn icon + chữ); có relic thì icon = EnchantmentTierIcons[tier-1], chữ "Lvl {tier}".
    public void SetData(int tier)
    {
        Tier = tier;
        StaticEnchantmentData data = GameData.staticData.enchantments;
        bool hasRelic = tier > 0;

        if (imgBgTier != null)
        {
            imgBgTier.sprite = data.GetTierBackground(Mathf.Max(tier, 1));
        }
        if (textLvl != null)
        {
            textLvl.gameObject.SetActive(hasRelic);
            textLvl.text = "Lvl " + tier;
        }
        if (imgIcon != null)
        {
            imgIcon.gameObject.SetActive(hasRelic);
            if (hasRelic)
            {
                imgIcon.sprite = data.GetTierIcon(tier);
            }
        }
    }

    // EnchantmentElementUI.hxi gốc.
    public void SetInteractable(bool isOn)
    {
        if (btChose != null)
        {
            btChose.interactable = isOn;
        }
    }

    private void OnClick()
    {
        onClick?.Invoke(this);
    }
}
