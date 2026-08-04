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

    public void Show(string content)
    {
        if (isAnimating)
        {
            return;
        }

        isAnimating = true;
        textContent.text = content;
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    public override void OnAnimationDone()
    {
        textContent.text = string.Empty;
        isAnimating = false;
    }
}
