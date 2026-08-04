using DG.Tweening;
using UnityEngine;

public class BaseUI : MonoBehaviour
{
    public bool isOverlay { get; set; }
    public bool isLoadFromResources { get; set; } = false;
    public bool isBackable { get; set; } = true;
    public bool isPoolingWhenClose { get; set; } = false;
    public bool isPopup;

    protected virtual void Awake() { }

    protected virtual void OnEnable() { }

    public virtual void Initialize() { }

    public virtual void ListenOnClose(object obj)
    {
        try
        {
            if (gameObject.activeInHierarchy)
            {
                Close();
            }
        }
        catch { }
    }

    public virtual void Close()
    {
        if (this != null && this.gameObject != null)
        {
            gameObject.SetActive(false);

            if (isLoadFromResources)
            {
                UIManager.Instance.HideUI(this);
            }
        }
    }

    public virtual void Back()
    {
        if (IsBackable())
        {
            Close();
        }
    }

    public virtual bool IsBackable()
    {
        return isBackable && gameObject.activeInHierarchy;
    }

    public virtual bool CheckNotify()
    {
        return false;
    }

    public virtual void RegisterListeners() { }

    public virtual void RemoveListeners() { }

    public virtual void OnAnimationDone() { }
}
