using UnityEngine;

public class ToastMsgAnimation : MonoBehaviour
{
    private PopupToastMessage popupToastMsg;

    private void Awake()
    {
        popupToastMsg = transform.parent.gameObject.GetComponent<PopupToastMessage>();
    }

    public void OnAnimationDone()
    {
        if (popupToastMsg != null)
        {
            popupToastMsg.OnAnimationDone();
        }
    }
}
