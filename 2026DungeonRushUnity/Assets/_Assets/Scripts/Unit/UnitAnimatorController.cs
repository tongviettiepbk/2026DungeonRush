using UnityEngine;

// Điều khiển Animator "unit_all.controller" (rig theo _ExportTemp/unit_base_all) qua bề mặt AnimationController
// mà BaseUnit đã gọi sẵn (PlayAnimIdle/Move/Attack/Die...). Controller gốc KHÔNG có parameter/transition
// → chuyển state bằng Animator.Play theo tên state.
//   - Vừa xuất hiện (Renew) → UnitShow, hết clip tự về UnitIdle.
//   - Đứng → UnitIdle, di chuyển → UnitRun.
//   - Đánh: vũ khí có đạn (đánh xa) → gun_atk, còn lại (đánh gần) → strike_atk.
public class UnitAnimatorController : AnimationController
{
    public Animator animator;

    [Header("State trong unit_all.controller")]
    public string stateShow = "UnitShow";
    public string stateIdle = "UnitIdle";
    public string stateRun = "UnitRun";
    public string stateMeleeAttack = "strike_atk";
    public string stateRangedAttack = "gun_atk";
    public string stateDie = "UnitDie";

    private int hashShow, hashIdle, hashRun, hashMelee, hashRanged, hashDie;
    private bool isShowing;      // đang chạy UnitShow → chờ hết clip mới về Idle
    private bool isShowPending;  // Renew lúc GameObject còn tắt → phát Show khi bật lên
    // State đã yêu cầu gần nhất. Không hỏi lại Animator vì ngay frame gọi Play nó vẫn trả state cũ.
    private int currentState;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            animator.enabled = true;
            // Clip đánh bắn AE_* vào GameObject chứa Animator → cần receiver để Unity không báo lỗi.
            if (animator.GetComponent<UnitAnimationEventRelay>() == null)
            {
                animator.gameObject.AddComponent<UnitAnimationEventRelay>();
            }
        }

        hashShow = Animator.StringToHash(stateShow);
        hashIdle = Animator.StringToHash(stateIdle);
        hashRun = Animator.StringToHash(stateRun);
        hashMelee = Animator.StringToHash(stateMeleeAttack);
        hashRanged = Animator.StringToHash(stateRangedAttack);
        hashDie = Animator.StringToHash(stateDie);
    }

    private void OnEnable()
    {
        if (isShowPending)
        {
            PlayShow();
        }
    }

    private void OnDisable()
    {
        currentState = 0;   // bật lại thì Animator về state mặc định
        isShowing = false;
    }

    private void Update()
    {
        if (!isShowing || animator == null)
        {
            return;
        }

        // Chỉ kết thúc khi Animator THẬT SỰ đang ở UnitShow và chạy hết clip.
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash == hashShow && info.normalizedTime >= 1f)
        {
            isShowing = false;
            Play(hashIdle);
        }
    }

    // Unit vừa xuất hiện (Active → Renew).
    public override void Renew()
    {
        isShowPending = true;
        if (isActiveAndEnabled)
        {
            PlayShow();
        }
    }

    private void PlayShow()
    {
        isShowPending = false;
        if (animator == null)
        {
            return;
        }

        isShowing = true;
        Play(hashShow, restart: true);
    }

    public override void PlayAnimIdle()
    {
        // Đang Show thì để Show chạy hết, Update sẽ tự về Idle.
        if (isShowing)
        {
            return;
        }

        Play(hashIdle);
    }

    public override void PlayAnimMove()
    {
        isShowing = false;
        Play(hashRun);
    }

    public override void PlayAnimAttack()
    {
        isShowing = false;
        Play(isRangedAttack ? hashRanged : hashMelee, restart: true);
    }

    public override void PlayAnimDie()
    {
        isShowing = false;
        Play(hashDie, restart: true);
    }

    // Lật art bằng localScale.x của node chứa Animator (Soldier). Clip chỉ animate main/... và shadow,
    // không đụng transform node này nên anim không đè mất hướng. Art mặc định quay sang PHẢI.
    public override bool isFacingRight
    {
        get { return base.isFacingRight; }
        set
        {
            base.isFacingRight = value;
            if (animator != null)
            {
                Transform art = animator.transform;
                Vector3 scale = art.localScale;
                scale.x = value ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                art.localScale = scale;
            }
        }
    }

    public override void Pause()
    {
        if (animator != null) animator.speed = 0f;
    }

    public override void Resume()
    {
        if (animator != null) animator.speed = 1f;
    }

    public override void Freeze(bool isOn)
    {
        if (isOn) Pause();
        else Resume();
    }

    // restart=false: state loop (Idle/Run) đang chạy đúng state rồi thì không phát lại từ đầu.
    private void Play(int stateHash, bool restart = false)
    {
        if (animator == null || !animator.isActiveAndEnabled)
        {
            return;
        }

        if (!restart && currentState == stateHash)
        {
            return;
        }

        currentState = stateHash;
        animator.Play(stateHash, 0, 0f);
    }
}
