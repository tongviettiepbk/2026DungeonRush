using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Nút Weapon Offer ở lobby (OfferButton gốc, jjz..jkd): icon vũ khí đang chào, particle màu rarity,
// đồng hồ thời gian còn lại; hết hạn thì tự ẩn; bấm mở UIOfferPopup.
public class OfferButtonView : MonoBehaviour
{
    public Button Button;
    public Image WeaponIconImage;
    public ParticleSystem RarityParticles;
    public TMP_Text TimerText;

    private long lastSecondsLeft = -1;

    private void Awake()
    {
        Button.onClick.AddListener(UIOfferPopup.Show);
        WeaponOfferService.OfferChanged += Refresh;
    }

    private void OnDestroy()
    {
        WeaponOfferService.OfferChanged -= Refresh;
    }

    // jka gốc: cập nhật đồng hồ, hết giờ thì vẽ lại (ẩn).
    private void Update()
    {
        long left = WeaponOfferService.GetSecondsLeft();
        if (left == lastSecondsLeft)
            return;
        lastSecondsLeft = left;
        if (TimerText != null)
            TimerText.text = ShopUIUtil.FormatDuration(left);
        if (left <= 0)
            Refresh();
    }

    // jkc + jkd gốc.
    public void Refresh()
    {
        bool isVisible = WeaponOfferService.IsOfferVisible();
        gameObject.SetActive(isVisible);
        if (isVisible == false)
            return;

        WeaponData weapon = WeaponOfferService.GetWeapon();
        if (WeaponIconImage != null && weapon != null && weapon.icon != null)
            WeaponIconImage.sprite = weapon.icon;
        if (RarityParticles != null && weapon != null)
        {
            ParticleSystem.MainModule main = RarityParticles.main;
            main.startColor = UITabWing.GetRarityColor(weapon.rarity);
            if (RarityParticles.isPlaying == false)
                RarityParticles.Play();
        }
    }
}
