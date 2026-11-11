using UnityEngine;
using UnityEngine.UI;

public enum MessageErrorType
{
    DEFAULT = 0,

    NETWORK_CONNECTION_FAILED,
    INSUFFICIENT_RESOURCES,
    NOT_ENOUGH_GEMS,
    NOT_ENOUGH_GOLD,
    NOT_ENOUGH_ADS_TODAY,
    INVALID_NAME,
    SUSPENDED_ACCOUNT,
    UPDATE_NEW_VERSION,
    SUSPENDED_ACCOUNT_AND_BLOCK_DEVICE,
    SERVER_MAINTANCE,
    INVENTORY_FULL,
}

public enum MessageTemplateType
{
    PURCHASE_SUCCESS,
    COMEBACK_TOMORROW,
    COMING_SOON,
}

public class PopupToastMessage : BaseUI
{
    public Text textContent;

    private bool isAnimating = false;
    private float showTime;

    // Thời lượng clip toast_message ~2.08s; quá mốc này mà isAnimating vẫn true = event cuối clip bị lỡ.
    private const float STUCK_TIMEOUT = 3f;

    protected override void Awake()
    {
        base.Awake();
        // Toast chỉ để đọc, không nhận bấm. BG nằm ở canvas Overlay 1000 (trên mọi popup), phủ ngang đáy màn;
        // nếu event OnAnimationDone ở khung cuối bị lỡ, BG đứng lại ở alpha 0 nhưng vẫn chặn raycast
        // → vùng đáy màn (Sell/Equip, tab bar) bấm không ăn mà không thấy gì (gặp trên BlueStacks).
        foreach (var cg in GetComponentsInChildren<CanvasGroup>(true))
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        foreach (var g in GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = false;
        }
    }

    public void Show(string content)
    {
        // Bỏ qua khi toast đang chạy; nhưng nếu bị kẹt (object đã tắt, hoặc event cuối bị lỡ) thì cho hiện lại.
        if (isAnimating && gameObject.activeSelf && Time.unscaledTime - showTime < STUCK_TIMEOUT)
        {
            return;
        }

        isAnimating = true;
        showTime = Time.unscaledTime;
        textContent.text = content;
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    public override void OnAnimationDone()
    {
        textContent.text = string.Empty;
        isAnimating = false;
        // Tắt thông báo xong → ẩn hẳn object (Show bật lại khi có toast mới).
        gameObject.SetActive(false);
    }
}
