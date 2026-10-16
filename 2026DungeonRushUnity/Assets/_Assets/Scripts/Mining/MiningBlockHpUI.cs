using UnityEngine;
using UnityEngine.UI;

// Thanh máu khối nhiều nhát (MiningBlockHpUI gốc) — nằm trên canvas world-space "HpUICanvas" của MiningGrid.
// Bản gốc mọi quặng HitsRequired = 1 nên thực tế không hiện; giữ để đúng luồng.
public class MiningBlockHpUI : MonoBehaviour
{
    public Slider hpSlider;
    public Vector3 offset;

    private Transform target;

    // ixp
    public void Follow(Transform t)
    {
        target = t;
        if (hpSlider != null) hpSlider.value = 1f;
        UpdatePosition();
    }

    // ixq
    public void SetValue(float value)
    {
        if (hpSlider != null) hpSlider.value = value;
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    // ixr
    private void UpdatePosition()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}
