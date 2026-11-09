using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Nhân vật đào trong mỏ. LỆCH GỐC có chủ ý (user chốt 2026-11-09): gốc nhìn từ trên xuống, xoay cả người
// theo hướng đi; ở đây dùng VỊT NHÌN NGANG như Hero campaign (rig 00Hero + unit_all.controller):
//   - Không xoay, chỉ lật trái/phải (visualRoot.localScale.x) khi hướng đi/đập có thành phần ngang.
//   - Đứng → UnitIdle, đi → UnitRun, đập → strike_atk (tua cho vừa nhịp gốc 0.5s/nhát).
//   - Mặc Mũ/Kính(Wing)/Tẩu(Cape)/Ba lô/Áo(Gloves) theo đồ đang mặc của Hero, tay cầm dụng cụ đang chọn.
public class MiningCharacter : MonoBehaviour
{
    public Transform visualRoot;
    public Animator animator;
    public SpriteRenderer helmetRenderer;
    public SpriteRenderer glassRenderer;
    public SpriteRenderer pipeRenderer;
    public SpriteRenderer backpackRenderer;
    public SpriteRenderer clothesRenderer;
    public SpriteRenderer mineToolRenderer;

    public Vector2Int GridPosition { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsAttacking { get; private set; }

    // Nhịp gốc (Weapon_Hammer_Attack): hết nhát 0.5s. Clip strike_atk dài 0.6667s, AE_Attack ở 0.4167s
    // → tua nhanh cho vừa 0.5s, ra đòn đúng khung chạm (62.5% nhát).
    private const float ATTACK_DURATION = 0.5f;
    private const float STRIKE_CLIP_LENGTH = 0.6666667f;
    private const float STRIKE_HIT_TIME = 0.41666666f;
    private const float HIT_DELAY = ATTACK_DURATION * STRIKE_HIT_TIME / STRIKE_CLIP_LENGTH;

    private static readonly int HASH_IDLE = Animator.StringToHash("UnitIdle");
    private static readonly int HASH_RUN = Animator.StringToHash("UnitRun");
    private static readonly int HASH_ATTACK = Animator.StringToHash("strike_atk");

    // Góc cầm dụng cụ: cuốc dựng đứng → nghiêng như vũ khí cận chiến Hero; khoan mũi xuống → chĩa ra trước.
    private const float PICKAXE_ANGLE = -45f;
    private const float DRILL_ANGLE = 90f;

    private MiningGrid grid;
    private float moveSpeed;
    private Coroutine moveRoutine;
    private Coroutine attackRoutine;
    private int currentAnim;

    // iys
    public void Init(MiningGrid miningGrid, float speed, Vector2Int position)
    {
        grid = miningGrid;
        moveSpeed = speed;
        GridPosition = position;
        transform.position = grid.GridToWorld(position);
        transform.rotation = Quaternion.identity;
        SetFacing(1f);
        IsMoving = false;
        IsAttacking = false;
        ApplyEquipment();
        PlayAnim(HASH_IDLE);
    }

    // Mặc đồ theo save như HeroVisual: có hình mới bật object, Cape ẩn khi tắt "Show Cloak".
    private void ApplyEquipment()
    {
        UserEquipmentData equipment = GameData.userData.equipment;
        ApplySlot(helmetRenderer, EquipVisualResolver.GetBodySprite(GearSlotType.HELMET, equipment.GetEquipped(GearSlotType.HELMET)));
        ApplySlot(glassRenderer, EquipVisualResolver.GetBodySprite(GearSlotType.WING, equipment.GetEquipped(GearSlotType.WING)));
        ApplySlot(backpackRenderer, EquipVisualResolver.GetBodySprite(GearSlotType.BACKPACK, equipment.GetEquipped(GearSlotType.BACKPACK)));
        ApplySlot(clothesRenderer, EquipVisualResolver.GetBodySprite(GearSlotType.GLOVES, equipment.GetEquipped(GearSlotType.GLOVES)));
        bool showCloak = GameData.userData.capes == null || GameData.userData.capes.showCloak;
        ApplySlot(pipeRenderer, showCloak ? EquipVisualResolver.GetBodySprite(GearSlotType.CAPE, equipment.GetEquipped(GearSlotType.CAPE)) : null);
    }

    // Gán sprite, không có hình thì tắt object.
    private static void ApplySlot(SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null)
        {
            return;
        }
        sr.sprite = sprite;
        sr.gameObject.SetActive(sprite != null);
    }

    // iyw: đổi dụng cụ cầm tay.
    public void SetTool(Sprite toolSprite, MiningToolType tool)
    {
        ApplySlot(mineToolRenderer, toolSprite);
        if (mineToolRenderer != null)
        {
            float angle = tool == MiningToolType.Drill ? DRILL_ANGLE : PICKAXE_ANGLE;
            mineToolRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // Controller unit_all không có parameter/transition → chuyển state bằng Play.
    private void PlayAnim(int hash, bool restart = false)
    {
        if (animator == null || (currentAnim == hash && restart == false))
        {
            return;
        }
        currentAnim = hash;
        animator.speed = hash == HASH_ATTACK ? STRIKE_CLIP_LENGTH / ATTACK_DURATION : 1f;
        animator.Play(hash, 0, 0f);
    }

    // Vịt gốc quay mặt sang phải: x > 0 → phải, x < 0 → trái.
    private void SetFacing(float sign)
    {
        if (visualRoot == null)
        {
            return;
        }
        Vector3 s = visualRoot.localScale;
        s.x = Mathf.Abs(s.x) * Mathf.Sign(sign);
        visualRoot.localScale = s;
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
        if (path.Count > 1)
        {
            PlayAnim(HASH_RUN);
        }
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
        PlayAnim(HASH_IDLE);
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
        PlayAnim(HASH_IDLE);
    }

    // izb: vung dụng cụ 1 nhát: hit ở khung chạm, complete khi hết nhát.
    public void PlayAttack(Action hit, Action complete)
    {
        IsAttacking = true;
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }
        attackRoutine = StartCoroutine(AttackRoutine(hit, complete));
    }

    private IEnumerator AttackRoutine(Action hit, Action complete)
    {
        PlayAnim(HASH_ATTACK, restart: true);
        yield return new WaitForSeconds(HIT_DELAY);
        hit?.Invoke();
        yield return new WaitForSeconds(ATTACK_DURATION - HIT_DELAY);
        attackRoutine = null;
        IsAttacking = false;
        PlayAnim(HASH_IDLE);
        complete?.Invoke();
    }

    // izh: quay mặt theo hướng (ô lưới) rồi gọi onDone. Chỉ lật khi có thành phần ngang.
    public void Face(Vector2Int direction, Action onDone)
    {
        if (direction.x != 0)
        {
            SetFacing(direction.x);
        }
        onDone?.Invoke();
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
        PlayAnim(HASH_RUN);
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        PlayAnim(HASH_IDLE);
    }
}
