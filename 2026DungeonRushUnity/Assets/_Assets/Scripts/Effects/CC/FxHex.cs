using System.Collections;
using UnityEngine;


public class FxHex : BaseFx
{
    public GameObject objFrog;
    public GameObject objShadow;
    public ParticleSystem fxDust;

    public override void Active(Vector3 position, float scale = 1)
    {
        base.Active(position, scale);
        objFrog.SetActive(false);
        objFrog.SetActive(true);

        fxDust.transform.SetParent(transform);
        fxDust.transform.localPosition = Vector3.zero;
        fxDust.Play();

        Vector3 v = objFrog.transform.position;
        v.z = v.y;
        objFrog.transform.position = v;

        Vector3 x = objShadow.transform.position;
        x.z = x.y;
        objShadow.transform.position = x;
    }

    public override void Deactive(bool isNulifyParent = false)
    {
        base.Deactive();
        objFrog.SetActive(false);

        fxDust.transform.SetParent(null);
        fxDust.Play();

        FxController.Instance.StartDelayAction(0.5f, () =>
        {
            fxDust.transform.SetParent(transform);
            fxDust.transform.localPosition = Vector3.zero;
        });
    }
}
