using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Nhân vật đào trong mỏ (MiningCharacter gốc). Nhìn từ trên xuống: cả transform xoay theo hướng đi/đập
// (góc = atan2(dy_world, dx) − 90°, tween 0.15s OutQuad). Mặc Mũ/Ba lô/Găng theo đồ đang mặc của Hero,
// tay cầm dụng cụ đang chọn (MineToolRenderer). Đập: Animator bool IsAttacking/IsIdle + animation event
// OnAttackAnimationHit/OnAttackAnimationCompleted; có hẹn giờ dự phòng 0.5s (hit) + 0.3s (xong).
public class MiningCharacter : MonoBehaviour
{
    public Action AttackAnimationHit;
    public Action AttackAnimationCompleted;

    public Animator animator;
    public Transform bodyTransform;
    public SpriteRenderer helmetRenderer;
    public SpriteRenderer backpackRenderer;
    public SpriteRenderer glovesRendererLeft;
    public SpriteRenderer glovesRendererRight;
    public SpriteRenderer mineToolRenderer;

    public Vector2Int GridPosition { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsAttacking { get; private set; }

    private const float ROTATE_DURATION = 0.15f;
    private const float FALLBACK_HIT_DELAY = 0.5f;
    private const float FALLBACK_COMPLETE_DELAY = 0.3f;
    private const string ANIM_IS_ATTACKING = "IsAttacking";
    private const string ANIM_IS_IDLE = "IsIdle";

    private MiningGrid grid;
    private float moveSpeed;
    private Coroutine moveRoutine;
    private Coroutine attackRoutine;
    private RuntimeAnimatorController baseController;
    private AnimatorOverrideController overrideController;
    private Tween rotateTween;
    private Action onHit;
    private Action onComplete;
    private bool hitFired;

    // iys
    public void Init(MiningGrid miningGrid, float speed, Vector2Int position)
    {
        grid = miningGrid;
        moveSpeed = speed;
        if (animator != null && baseController == null)
        {
            baseController = animator.runtimeAnimatorController;
        }

        GridPosition = position;
        transform.position = grid.GridToWorld(position);
        transform.rotation = Quaternion.Euler(0f, 0f, -180f);
        IsMoving = false;
        IsAttacking = false;
        ApplyEquipment();
    }

    // iyt/iyu: Mũ / Ba lô / Găng (gốc ItemType 1/2/3) lấy hình đồ Hero đang mặc.
    private void ApplyEquipment()
    {
        UserEquipmentData equipment = GameData.userData.equipment;
        ApplySprite(helmetRenderer, EquipVisualResolver.GetBodySprite(GearSlotType.HELMET, equipment.GetEquipped(GearSlotType.HELMET)));
        ApplySprite(backpackRenderer, EquipVisualResolver.GetBodySprite(GearSlotType.BACKPACK, equipment.GetEquipped(GearSlotType.BACKPACK)));
        Sprite gloves = EquipVisualResolver.GetBodySprite(GearSlotType.GLOVES, equipment.GetEquipped(GearSlotType.GLOVES));
        if (gloves != null)
        {
            ApplySprite(glovesRendererLeft, gloves);
            ApplySprite(glovesRendererRight, gloves);
        }
    }

    // iyv: gán sprite, không có hình thì tắt renderer.
    private static void ApplySprite(SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null)
        {
            return;
        }
        sr.sprite = sprite;
        sr.enabled = sprite != null;
    }

    // iyw: đổi dụng cụ cầm tay + bộ animation của dụng cụ.
    public void SetTool(Sprite toolSprite, AnimatorOverrideController toolAnimator)
    {
        ApplySprite(mineToolRenderer, toolSprite);
        ApplyAnimator(toolAnimator);
    }

    // iyx: bọc override của dụng cụ lên controller gốc của nhân vật.
    private void ApplyAnimator(AnimatorOverrideController toolAnimator)
    {
        if (animator == null)
        {
            return;
        }

        if (toolAnimator == null)
        {
            animator.runtimeAnimatorController = baseController;
        }
        else
        {
            if (overrideController == null || overrideController.runtimeAnimatorController != toolAnimator.runtimeAnimatorController)
            {
                overrideController = new AnimatorOverrideController(toolAnimator.runtimeAnimatorController);
            }

            List<KeyValuePair<AnimationClip, AnimationClip>> overrides =
                new List<KeyValuePair<AnimationClip, AnimationClip>>(toolAnimator.overridesCount);
            toolAnimator.GetOverrides(overrides);
            overrideController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = overrideController;
        }

        animator.Rebind();
        animator.Update(0f);
        SetIdleAnim();
    }

    // izf
    private void SetIdleAnim()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }
        animator.SetBool(ANIM_IS_ATTACKING, false);
        animator.SetBool(ANIM_IS_IDLE, true);
    }

    // izg
    private void SetAttackAnim()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }
        animator.SetBool(ANIM_IS_IDLE, false);
        animator.SetBool(ANIM_IS_ATTACKING, true);
    }

    // iyy/iza: đi theo đường (bỏ ô đầu = ô đang đứng), mỗi ô quay mặt rồi MoveTowards.
    public void MoveAlongPath(List<Vector2Int> path, Action onArrived)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }
        moveRoutine = StartCoroutine(MoveAlongPathRoutine(path, onArrived));
    }

    private IEnumerator MoveAlongPathRoutine(List<Vector2Int> path, Action onArrived)
    {
        IsMoving = true;
        SetIdleAnim();
        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int cell = path[i];
            Vector3 target = grid.GridToWorld(cell);
            Face(cell - GridPosition, null);
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = target;
            GridPosition = cell;
        }

        IsMoving = false;
        moveRoutine = null;
        SetIdleAnim();
        onArrived?.Invoke();
    }

    // iyz: dừng di chuyển.
    public void StopMoving()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
        IsMoving = false;
        KillRotate();
        SetIdleAnim();
    }

    // izi
    public void KillRotate()
    {
        if (rotateTween != null)
        {
            rotateTween.Kill();
            rotateTween = null;
        }
    }

    // izb: vung dụng cụ 1 lần. Không có animator thì gọi luôn 2 callback.
    public void PlayAttack(Action hit, Action complete)
    {
        IsAttacking = true;
        hitFired = false;
        if (animator == null)
        {
            hit?.Invoke();
            IsAttacking = false;
            complete?.Invoke();
            return;
        }

        onHit = hit;
        onComplete = complete;
        SetAttackAnim();
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }
        attackRoutine = StartCoroutine(AttackFallbackRoutine());
    }

    // izc (sd): hẹn giờ dự phòng khi clip không có animation event.
    private IEnumerator AttackFallbackRoutine()
    {
        yield return new WaitForSeconds(FALLBACK_HIT_DELAY);
        FireHit();
        yield return new WaitForSeconds(FALLBACK_COMPLETE_DELAY);
        attackRoutine = null;
        FireComplete();
    }

    // Animation event (clip đập).
    public void OnAttackAnimationHit()
    {
        AttackAnimationHit?.Invoke();
        FireHit();
    }

    // Animation event (clip đập).
    public void OnAttackAnimationCompleted()
    {
        AttackAnimationCompleted?.Invoke();
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        FireHit();
        FireComplete();
    }

    // izd: hit chỉ 1 lần mỗi nhát.
    private void FireHit()
    {
        if (hitFired)
        {
            return;
        }
        hitFired = true;
        Action cb = onHit;
        onHit = null;
        cb?.Invoke();
    }

    // ize
    private void FireComplete()
    {
        if (IsAttacking == false)
        {
            return;
        }
        IsAttacking = false;
        SetIdleAnim();
        Action cb = onComplete;
        onComplete = null;
        cb?.Invoke();
    }

    // izh: quay mặt theo hướng (ô lưới) rồi gọi onDone. Hướng 0 → gọi luôn.
    public void Face(Vector2Int direction, Action onDone)
    {
        if (direction == Vector2Int.zero)
        {
            onDone?.Invoke();
            return;
        }

        float angle = Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion target = Quaternion.Euler(0f, 0f, angle);
        KillRotate();

        float dot = Mathf.Min(Mathf.Abs(Quaternion.Dot(target, transform.rotation)), 1f);
        if (dot > 0.999999f)
        {
            transform.rotation = target;
            onDone?.Invoke();
            return;
        }

        rotateTween = transform.DORotateQuaternion(target, ROTATE_DURATION).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                rotateTween = null;
                onDone?.Invoke();
            });
    }

    // izj: dịch tức thời (bị kẹt khi độ sâu tăng).
    public void Teleport(Vector2Int cell)
    {
        GridPosition = cell;
        transform.position = grid.GridToWorld(cell);
    }

    // izk (sf): đi thẳng tới 1 ô với tốc độ cho trước (khoan / cuốc vàng dùng PowerUpMoveSpeed).
    public IEnumerator MoveToCell(Vector2Int cell, float speed)
    {
        Face(cell - GridPosition, null);
        GridPosition = cell;
        Vector3 target = grid.GridToWorld(cell);
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    private void OnDestroy()
    {
        KillRotate();
    }
}
