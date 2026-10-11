using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Popup chọn avatar (AvatarSelectPopup gốc): lưới toàn bộ avatar, ô đang dùng có dấu tích. Bấm 1 ô → lưu avatar,
// báo AvatarChanged (lobby đổi hình) + gửi lại hồ sơ Boss Rush để người khác thấy, rồi đóng.
public class UIAvatarSelectPopup : BaseUI
{
    public Button btClose;
    public Transform gridContent;
    public ElementAvatarUI elementPrefab;

    private readonly List<ElementAvatarUI> elements = new List<ElementAvatarUI>();

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Draw();
    }

    private void Draw()
    {
        List<Sprite> sprites = AvatarConfig.Sprites;
        int current = GameData.userData.profile.avatarId;
        for (int i = 0; i < sprites.Count; i++)
        {
            if (i >= elements.Count)
            {
                elements.Add(Instantiate(elementPrefab, gridContent));
            }
            elements[i].Show(i, sprites[i], i == current, OnClickAvatar);
        }
    }

    private void OnClickAvatar(int avatarId)
    {
        UserProfileData profile = GameData.userData.profile;
        if (profile.avatarId != avatarId)
        {
            profile.avatarId = avatarId;
            profile.isDataChanged = true;
            GameData.Save();
            this.PostEvent(EventID.AvatarChanged);
            BossRushController.Instance.UpdatePlayer();
        }
        Close();
    }
}
