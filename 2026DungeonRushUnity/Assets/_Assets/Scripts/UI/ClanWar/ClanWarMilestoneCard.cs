using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 mốc thưởng cá nhân (ClanWarMilestoneCard gốc): ngưỡng điểm, thưởng, nút Claim; màu theo trạng thái.
public class ClanWarMilestoneCard : MonoBehaviour
{
    public enum State
    {
        Unreached = 0,
        ReachedClaimable = 1,
        ReachedInBundle = 2,
        Collected = 3,
    }

    public TMP_Text ThresholdText;
    public ClanWarRewardsView RewardsView;
    public Button ClaimButton;
    public Image BackgroundImage;
    public ClanLoadingView ClaimLoading;
    public Color UnreachedColor = Color.gray;
    public Color ReachedColor = new Color(1f, 0.85f, 0.4f);
    public Color CollectedColor = new Color(0.298f, 0.686f, 0.314f);
    // Reskin: có sprite thì đổi SPRITE theo trạng thái (đạt/đã nhận = xanh dương, chưa đạt = xám) thay cho tô màu.
    public Sprite ReachedSprite;
    public Sprite UnreachedSprite;
    // Reskin Đợt 8: mốc nhận được hiện nút Claim xanh lá + chấm "!" (cả thẻ vẫn là nút), đã nhận hiện tick.
    public GameObject ClaimTag;
    public GameObject CollectedTag;

    private Action onClaim;

    private void Awake()
    {
        if (ClaimButton != null) ClaimButton.onClick.AddListener(() => onClaim?.Invoke());
    }

    // gcn gốc.
    public void Set(ClanWarMilestoneRowDTO row, State state, Action onClaim)
    {
        this.onClaim = onClaim;
        // Reskin Đợt 8: chỉ số mốc (mốc lớn "200,000 pts" không vừa cột).
        if (ThresholdText != null) ThresholdText.text = ClanWarController.FormatNumber(row.threshold);
        if (RewardsView != null) RewardsView.SetRewards(row.ToRewards());
        // ClaimButton nằm ngay trên thẻ (bấm cả thẻ để nhận) nên chỉ khoá bấm, không tắt GameObject.
        if (ClaimButton != null) ClaimButton.interactable = state == State.ReachedClaimable;
        if (ClaimTag != null) ClaimTag.SetActive(state == State.ReachedClaimable);
        if (CollectedTag != null) CollectedTag.SetActive(state == State.Collected);
        if (BackgroundImage != null)
        {
            if (ReachedSprite != null && UnreachedSprite != null)
            {
                BackgroundImage.sprite = state == State.Unreached ? UnreachedSprite : ReachedSprite;
                BackgroundImage.color = Color.white;
            }
            else BackgroundImage.color = ColorOf(state);
        }
        SetBusy(false);
    }

    // gco gốc.
    public void SetBusy(bool busy)
    {
        if (ClaimLoading == null) return;
        if (busy) ClaimLoading.ShowLoading();
        else ClaimLoading.Hide();
    }

    // gcp gốc.
    private Color ColorOf(State s)
    {
        switch (s)
        {
            case State.Collected: return CollectedColor;
            case State.ReachedClaimable:
            case State.ReachedInBundle: return ReachedColor;
            default: return UnreachedColor;
        }
    }
}
