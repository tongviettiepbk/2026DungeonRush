using System;
using UnityEngine;

// Nhận AnimationEvent của clip đánh gốc (OnAttackAnimationHit / OnAttackAnimationCompleted — Character gốc).
// Event bắn vào GameObject chứa Animator (node "Soldier"), còn logic unit nằm ở root → component này chuyển tiếp.
public class UnitAnimationEventRelay : MonoBehaviour
{
    public Action AttackAnimationHit;
    public Action AttackAnimationCompleted;

    public void OnAttackAnimationHit()
    {
        AttackAnimationHit?.Invoke();
    }

    public void OnAttackAnimationCompleted()
    {
        AttackAnimationCompleted?.Invoke();
    }

    // Event của clip đánh unit_all (strike_atk/gun_atk...). Hiện sát thương ra theo mốc nhịp
    // (BaseUnit.ScheduleAttackHits) → chỉ nhận để Unity không báo "AnimationEvent has no receiver".
    public void AE_Attack() { }
    public void AE_AttackStep() { }
    public void AE_AttackWindowBegin() { }
    public void AE_AttackWindowEnd() { }
}
