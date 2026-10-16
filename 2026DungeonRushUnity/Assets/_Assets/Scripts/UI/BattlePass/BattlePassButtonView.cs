using UnityEngine;
using UnityEngine.UI;

// Nút Battle Pass ở lobby (BattlePassButton gốc, jtu/jtx): hiện từ playerLevel 4, chấm đỏ khi còn mốc nhận được.
public class BattlePassButtonView : MonoBehaviour
{
    public GameObject Root;
    public Button Button;
    public GameObject NotificationUI;

    private void Awake()
    {
        Button.onClick.AddListener(UIBattlePassPopup.Show);
        BattlePassService.Changed += Refresh;
    }

    private void OnDestroy()
    {
        BattlePassService.Changed -= Refresh;
    }

    public void Refresh()
    {
        bool isVisible = BattlePassService.IsUnlocked();
        GameObject target = Root != null ? Root : gameObject;
        if (target.activeSelf != isVisible)
            target.SetActive(isVisible);
        if (NotificationUI != null)
            NotificationUI.SetActive(isVisible && BattlePassService.HasClaimable());
    }
}
