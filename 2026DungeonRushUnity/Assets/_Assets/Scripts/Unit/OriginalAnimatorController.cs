using UnityEngine;

// Điều khiển Animator theo CharacterAnimatorController GỐC (rig cũ "Soldier/Body/...", VD rồng Dragon Dungeon)
// qua bề mặt AnimationController mà BaseUnit đã gọi sẵn. Controller gốc chuyển state bằng 3 bool:
//   Idle → Attack (IsAttacking), Attack → Idle (IsIdle), * → Die (IsDying).
// Chỉ đổi HÌNH: sát thương vẫn ra theo mốc nhịp của BaseUnit. Clip đánh bắn OnAttackAnimationCompleted
// ở cuối → về Idle.
public class OriginalAnimatorController : AnimationController
{
    private static readonly int ANIM_IS_IDLE = Animator.StringToHash("IsIdle");
    private static readonly int ANIM_IS_ATTACKING = Animator.StringToHash("IsAttacking");
    private static readonly int ANIM_IS_DYING = Animator.StringToHash("IsDying");

    public Animator animator;

    private bool isAttacking;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            return;
        }

        // Event clip gốc bắn vào GameObject chứa Animator → relay chuyển về đây.
        UnitAnimationEventRelay relay = animator.GetComponent<UnitAnimationEventRelay>();
        if (relay == null)
        {
            relay = animator.gameObject.AddComponent<UnitAnimationEventRelay>();
        }
        relay.AttackAnimationCompleted = SetIdle;
    }

    public override void Renew()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(ANIM_IS_DYING, false);
        SetIdle();
    }

    // Đang vung đòn thì để clip đánh chạy hết (event Completed tự về Idle).
    public override void PlayAnimIdle()
    {
        if (isAttacking == false)
        {
            SetIdle();
        }
    }

    public override void PlayAnimMove()
    {
        PlayAnimIdle();
    }

    public override void PlayAnimAttack()
    {
        if (animator == null)
        {
            return;
        }

        isAttacking = true;
        animator.SetBool(ANIM_IS_IDLE, false);
        animator.SetBool(ANIM_IS_ATTACKING, true);
    }

    public override void PlayAnimDie()
    {
        if (animator == null)
        {
            return;
        }

        isAttacking = false;
        animator.SetBool(ANIM_IS_IDLE, false);
        animator.SetBool(ANIM_IS_ATTACKING, false);
        animator.SetBool(ANIM_IS_DYING, true);
    }

    private void SetIdle()
    {
        isAttacking = false;
        if (animator == null)
        {
            return;
        }

        animator.SetBool(ANIM_IS_ATTACKING, false);
        animator.SetBool(ANIM_IS_IDLE, true);
    }
}
