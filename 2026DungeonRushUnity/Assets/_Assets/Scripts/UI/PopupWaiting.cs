using System.Collections;

public class PopupWaiting : BaseUI
{
    private int timeOut;

    public void Show(int timeOut = 20)
    {
        StopAllCoroutines();
        CancelInvoke();
        this.timeOut = timeOut;
        UIManager.Instance.ActiveShield(true);
        Invoke("DelayShow", 2f);
    }

    public override void Close()
    {
        gameObject.SetActive(false);
        StopAllCoroutines();
        CancelInvoke();
        UIManager.Instance.ActiveShield(false);
    }

    private void DelayShow()
    {
        gameObject.SetActive(true);
        UIManager.Instance.ActiveShield(false);

        if (timeOut > 0)
        {
            StartCoroutine(TimeOut());
        }
    }

    private IEnumerator TimeOut()
    {
        while (timeOut > 0)
        {
            yield return Yielder.Get(1f);
            timeOut--;
        }

        Close();
    }
}
