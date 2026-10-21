using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Damage 1 người trong trận Boss Rush (hero chính hoặc ghost). order = thứ tự vào trận, dùng khi bằng damage.
public class BossRushDamageEntry
{
    public string playerName;
    public int avatarId;
    public bool isMine;
    public long damage;
    public int order;
}

// Bảng damage trong trận Boss Rush trên tấm phủ EventBlocker (thay vòng xoay "Battle in progress"):
// mỗi người 1 dòng, tự sắp lại theo damage giảm dần, dòng trượt tới chỗ mới. Thanh đỏ = damage / tổng damage đội.
// PvP dùng chung tấm phủ → Hide() trả lại vòng xoay + chữ.
public class UIBossRushDamageBoard : MonoBehaviour
{
    public RectTransform content;              // dòng neo đỉnh, xếp từ trên xuống
    public ElementBossRushDamageUI rowTemplate;
    public float rowSpacing = 72f;             // khoảng cách đỉnh 2 dòng liền nhau
    public float moveDuration = 0.25f;
    public GameObject[] objInProgress;         // vòng xoay + chữ "Battle in progress" của tấm phủ

    private readonly List<BossRushDamageEntry> entries = new List<BossRushDamageEntry>();
    private readonly List<ElementBossRushDamageUI> rows = new List<ElementBossRushDamageUI>();
    private readonly List<ElementBossRushDamageUI> pool = new List<ElementBossRushDamageUI>();
    private readonly Dictionary<BossRushDamageEntry, ElementBossRushDamageUI> rowByEntry = new Dictionary<BossRushDamageEntry, ElementBossRushDamageUI>();

    public void Show(List<BossRushDamageEntry> players)
    {
        Clear();
        gameObject.SetActive(true);
        SetActiveInProgress(false);
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);

        for (int i = 0; i < players.Count; i++)
        {
            ElementBossRushDamageUI row = GetRow();
            row.Show(players[i]);
            entries.Add(players[i]);
            rows.Add(row);
            rowByEntry[players[i]] = row;
        }
        Refresh(false);
    }

    public void Hide()
    {
        Clear();
        gameObject.SetActive(false);
        SetActiveInProgress(true);
    }

    // Sắp lại theo damage (bằng nhau giữ thứ tự vào trận), cập nhật số + thanh, dòng đổi hạng thì trượt tới chỗ mới.
    public void Refresh(bool animate = true)
    {
        entries.Sort((a, b) => a.damage != b.damage ? b.damage.CompareTo(a.damage) : a.order.CompareTo(b.order));

        long total = 0;
        for (int i = 0; i < entries.Count; i++) total += entries[i].damage;

        for (int i = 0; i < entries.Count; i++)
        {
            ElementBossRushDamageUI row = rowByEntry[entries[i]];
            row.SetDamage(i + 1, entries[i].damage, total > 0 ? (float)((double)entries[i].damage / total) : 0f);

            float y = -i * rowSpacing;
            RectTransform rt = row.RectTransform;
            if (Mathf.Approximately(rt.anchoredPosition.y, y)) continue;
            rt.DOKill();
            if (animate) rt.DOAnchorPosY(y, moveDuration).SetEase(Ease.OutQuad);
            else rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
        }
    }

    private ElementBossRushDamageUI GetRow()
    {
        ElementBossRushDamageUI row;
        if (pool.Count > 0)
        {
            row = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
        }
        else
        {
            row = Instantiate(rowTemplate, content);
        }
        row.gameObject.SetActive(true);
        row.RectTransform.anchoredPosition = new Vector2(row.RectTransform.anchoredPosition.x, 0f);
        return row;
    }

    private void Clear()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].RectTransform.DOKill();
            rows[i].gameObject.SetActive(false);
            pool.Add(rows[i]);
        }
        rows.Clear();
        entries.Clear();
        rowByEntry.Clear();
    }

    private void SetActiveInProgress(bool isOn)
    {
        if (objInProgress == null) return;
        for (int i = 0; i < objInProgress.Length; i++)
        {
            if (objInProgress[i] != null) objInProgress[i].SetActive(isOn);
        }
    }
}
