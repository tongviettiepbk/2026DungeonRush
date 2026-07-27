using UnityEngine;

// Bản rút gọn so với StickIdle (GamePlay/HealthBar.cs). Đủ bề mặt cho BaseUnit.
// TODO(follow-stick): nối UI thanh máu thật khi port hệ HUD combat.
public class HealthBar : MonoBehaviour
{
    private BaseUnit owner;

    public void Init(BaseUnit unit)
    {
        owner = unit;
    }

    public void Reset() { }
    public void Pause() { }
    public void Resume() { }
    public void Deactive() { }

    public void UpdateHealthBar(float percent) { }
}
