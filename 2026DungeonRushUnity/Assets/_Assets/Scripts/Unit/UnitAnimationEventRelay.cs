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
}
