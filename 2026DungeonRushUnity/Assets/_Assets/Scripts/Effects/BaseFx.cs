using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class BaseFx : MonoBehaviour
{
    public float lifeTime;

    public int idFx { get; set; }

    private void Awake()
    {
        EventDispatcher.Instance.RegisterListener(EventID.ResetMode, OnResetMode);
    }

    public virtual void Active(Vector3 position, float scale = 1f)
    {
        gameObject.SetActive(false);
        gameObject.SetActive(true);

        transform.position = position;
        transform.localScale = Vector3.one * scale;

        if (lifeTime > 0f)
        {
            DelayDeactive(lifeTime);
        }
    }

    public virtual void DelayDeactive(float delay = 0f, bool isNulifyParent = false, UnityAction callback = null)
    {
        if (delay == 0f)
        {
            Deactive(isNulifyParent);

            if (callback != null)
                callback();
        }
        else
        {
            FxController.Instance.StartDelayAction(delay, () =>
            {
                Deactive(isNulifyParent);

                if (callback != null)
                    callback();
            });
        }
    }

    public virtual void Deactive(bool isNulifyParent = false)
    {
        try
        {
            gameObject.SetActive(false);

            if (isNulifyParent)
            {
                transform.SetParent(null);
            }

            PoolingController.Instance.StoreFx(this);
        }
        catch {}
    }

    protected virtual void OnResetMode(object obj)
    {
        try
        {
            if (gameObject.activeInHierarchy)
            {
                Deactive();
            }
        }
        catch { }
    }
}
