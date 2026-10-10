using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Bảng kết quả khi mua gói rương (ChestBundleRevealPopup gốc, prefab ChestRewardsPopup): các ô hiện lần lượt mỗi
// RevealInterval giây, danh sách tự cuộn theo; hiện xong thì chạm bất kỳ để đóng (chạm sớm = hiện hết ngay).
// Rút gọn so với gốc: không ảo hoá lưới (VirtualizedGridScroller) — tạo đủ ô, gói lớn nhất 180 món.
public class UIChestBundleRevealPopup : BaseUI
{
    public ScrollRect ScrollRect;
    public ChestRewardElementUI CellPrefab;
    public float ScrollFollowAnchor;
    public float ScrollSmoothTime;
    public float RevealInterval;

    private List<ChestOutcome> outcomes;
    private Coroutine revealRoutine;
    private int revealedCount;

    // fmm + eip gốc.
    public void Setup(List<ChestOutcome> list)
    {
        outcomes = list;
        revealedCount = 0;
        Transform content = ScrollRect.content;
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(Reveal());
    }

    // fmn gốc.
    private IEnumerator Reveal()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(RevealInterval);
        while (revealedCount < outcomes.Count)
        {
            AddNext();
            ScrollRect.verticalNormalizedPosition = 0f;
            yield return wait;
        }
        revealRoutine = null;
    }

    private void AddNext()
    {
        Instantiate(CellPrefab, ScrollRect.content).Setup(outcomes[revealedCount]);
        revealedCount++;
    }

    private void Update()
    {
        if (outcomes == null || Input.GetMouseButtonUp(0) == false) return;

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
            while (revealedCount < outcomes.Count) AddNext();
            return;
        }
        // Đang kéo danh sách thì không tính là chạm để đóng.
        if (ScrollRect.velocity.sqrMagnitude < 1f) Close();
    }

    public override void Close()
    {
        outcomes = null;
        base.Close();
        ShopUIUtil.RefreshLobby();
    }
}
