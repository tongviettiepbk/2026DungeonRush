using System;
using UnityEngine;

// Bản rút gọn so với StickIdle (Units/AnimationController.cs ~533 dòng, Spine-based).
// Chỉ giữ đúng bề mặt mà BaseUnit + BattleMechanic gọi tới, để foundation compile được.
// TODO(follow-stick): thay bằng bản Spine đầy đủ khi port hệ animation combat.
public class AnimationController : MonoBehaviour
{
    // Tên animation/event — sẽ nối vào Spine khi port đầy đủ.
    public string animAttack = "attack";
    public string animDie = "die";
    public string eventAttack = "event_attack";

    public bool isFacingRight { get; set; } = true;

    public virtual void Renew() { }
    public virtual void Reset() { }
    public virtual void UpdateSortingOrder() { }

    public virtual void PlayAnimIdle() { }
    public virtual void PlayAnimMove() { }
    public virtual void PlayAnimAttack() { }
    public virtual void PlayAnimHit() { }
    public virtual void ResetAnimHit() { }
    public virtual void PlayAnimDie() { }
    public virtual void PlayAnimKnockDown() { }
    public virtual void PlayAnimKnockUp() { }

    public virtual void PlayAnimGetUp(Action onComplete = null)
    {
        onComplete?.Invoke();
    }

    public virtual void Freeze(bool isOn) { }
    public virtual void SetDefaultColor() { }
    public virtual void FlashColor(Color color) { }
    public virtual void ActiveMeshRenderer(bool isOn) { }
    public virtual void ResetColorByCC() { }
    public virtual void SetTimeScaleAnimMoveBySpeed() { }

    public virtual void Pause() { }
    public virtual void Resume() { }
}
