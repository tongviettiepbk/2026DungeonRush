using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Bản rút gọn so với StickIdle (UI/UIManager.cs ~672 dòng).
// Giữ base cốt lõi: stack UI (LoadUI/HideUI/Back), Notice, Toast, Waiting, Fade, Tooltip.
// Đã lược bỏ (liên kết nhiều — dev sau): Rewards (PopupReward/RewardData/TooltipRewards),
// Recommendation (LRecommendation/PackageId/GameConfig), BattlePowerChange, NavigationResources,
// OpenGear/OpenHeroShow. Các chỗ này giữ region trống + TODO(follow-stick) để port lại.

public enum FadeColor
{
    White,
    Black
}

public class UIManager : Singleton<UIManager>
{
    public string UI_PREFAB_PATH = "UI/";
    public PopupNotice notice;
    public PopupToastMessage toast;
    public Tooltips tooltips;
    public PopupWaiting waiting;
    public GameObject shieldUI;
    public Image imgFade;
    public RectTransform groupScreenOverlayUI;

    private bool isFading;
    // Đang có fade chạy (Fade hoặc FadeOut/FadeIn chuyển màn campaign) → Fade() mới sẽ bị bỏ qua.
    public bool IsFading => isFading;
    private Image imgFadeHalf;   // tấm đang dùng cho FadeOut/FadeIn
    private Dictionary<string, BaseUI> cachedUIs = new Dictionary<string, BaseUI>();
    private Stack<BaseUI> activeUIs = new Stack<BaseUI>();

    private const int DEFAULT_SORTING_ORDER_OVERLAY = 1000;
    private const int DEFAULT_SORTING_ORDER = 5;
    private const int SORTING_ORDER_STEP = 20;

    protected void Awake()
    {
        DontDestroyOnLoad(this);
    }

    #region Stack UI
    public BaseUI LoadUI(string key, bool isBackable = true, bool isPoolingWhenClose = false, bool isOverlay = false)
    {
        BaseUI obj = null;

        if (cachedUIs.ContainsKey(key))
        {
            obj = cachedUIs[key];
            obj.transform.SetParent(null);
        }
        else
        {
            BaseUI prefab = Resources.Load<BaseUI>(UI_PREFAB_PATH + key);

            if (prefab == null)
            {
                DebugCustom.LogError("UI key not found=" + key);
                return null;
            }
            else
            {
                obj = Instantiate(prefab);
                obj.gameObject.name = key;
                cachedUIs.Add(key, obj);
            }
        }

        if (activeUIs.Contains(obj) == false)
            activeUIs.Push(obj);

        Canvas canvas = obj.GetComponent<Canvas>();
        if (canvas != null)
        {
            if (isOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                List<BaseUI> remainingUIs = activeUIs.Where(x => x != null && x.isOverlay).ToList();
                canvas.sortingOrder = DEFAULT_SORTING_ORDER_OVERLAY + ((remainingUIs.Count + 1) * SORTING_ORDER_STEP);
            }
            else
            {
                // Bản rút gọn: StickIdle gắn canvas vào camera combat + sorting layer overlay riêng.
                // TODO(follow-stick): nối CameraController combat + SORTING_LAYER_OVERLAY khi port đủ.
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.sortingLayerName = StaticValue.SORTING_LAYER_UI;

                List<BaseUI> remainingUIs = activeUIs.Where(x => x != null && x.isOverlay == false).ToList();
                canvas.sortingOrder = DEFAULT_SORTING_ORDER + ((remainingUIs.Count + 1) * SORTING_ORDER_STEP);
            }
        }

        obj.isOverlay = isOverlay;
        obj.isBackable = isBackable;
        obj.isPoolingWhenClose = isPoolingWhenClose;
        obj.isLoadFromResources = true;
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void HideUI(BaseUI uiObject)
    {
        if (!cachedUIs.ContainsKey(uiObject.gameObject.name) && uiObject.isPoolingWhenClose)
        {
            cachedUIs.Add(uiObject.gameObject.name, uiObject);
        }

        BaseUI lastestPopup = activeUIs.Peek();
        if (lastestPopup != null)
        {
            if (lastestPopup == uiObject)
            {
                activeUIs.Pop();

                if (!uiObject.isPoolingWhenClose)
                {
                    if (cachedUIs.ContainsKey(uiObject.gameObject.name))
                    {
                        cachedUIs.Remove(uiObject.gameObject.name);
                    }

                    Destroy(uiObject.gameObject);
                }
                else
                {
                    uiObject.transform.parent = groupScreenOverlayUI;
                }
            }
            else
            {
                DebugCustom.Log(string.Format("HideUI={0}, LastestUI={1}", uiObject.name, lastestPopup.name));

                // Port fix StickIdle: đóng popup không nằm trên đỉnh (VD OfflineEarning bị LevelPopup tự bật đè lên)
                // → dựng lại stack bỏ uiObject; không thì entry đã Destroy còn kẹt, GetActiveUI văng MissingReference.
                if (activeUIs.Contains(uiObject))
                {
                    List<BaseUI> remaining = activeUIs.Where(x => x != uiObject).ToList();
                    activeUIs.Clear();
                    for (int i = remaining.Count - 1; i >= 0; i--)
                    {
                        activeUIs.Push(remaining[i]);
                    }
                }

                if (cachedUIs.ContainsKey(uiObject.gameObject.name))
                {
                    cachedUIs.Remove(uiObject.gameObject.name);
                }

                Destroy(uiObject.gameObject);
            }
        }
    }

    public bool Back()
    {
        if (isFading || waiting.gameObject.activeInHierarchy)
        {
            return false;
        }

        List<BaseUI> overlayUIs = activeUIs.Where(x => x.isOverlay).OrderByDescending(x => x.GetComponent<Canvas>()?.sortingOrder).ToList();
        if (overlayUIs.Count > 0)
        {
            BaseUI backUI = activeUIs.Peek();
            if (backUI == overlayUIs[0])
            {
                backUI.Back();
            }

            return true;
        }

        if (notice.IsBackable())
        {
            notice.Back();
            return true;
        }

        if (activeUIs.Count > 0)
        {
            BaseUI popup = activeUIs.Peek();
            if (popup != null)
            {
                popup.Back();
                return true;
            }
        }

        return false;
    }

    public void HideAllActiveUI()
    {
        List<BaseUI> popups = activeUIs.ToList();
        for (int i = 0; i < popups.Count; i++)
        {
            var p = popups[i];
            if (p != null)
            {
                p.Close();
            }
        }
    }

    public BaseUI GetActiveUI(string key)
    {
        return activeUIs.FirstOrDefault(x => x.gameObject.name == key);
    }

    public BaseUI GetLastestUI()
    {
        BaseUI ui = null;
        activeUIs.TryPeek(out ui);
        return ui;
    }

    public bool IsAnyActivePopup() // ignore isPopup
    {
        return activeUIs != null && activeUIs.Count(x => !x.isPopup) > 0;
    }

    public bool IsAnyActiveUI()
    {
        return activeUIs.Count > 0;
    }

    public void ActiveShield(bool isOn, float delayDeactive = 0f)
    {
        shieldUI.SetActive(isOn);

        if (isOn)
        {
            float timeDelayDeactive = delayDeactive > 0f ? delayDeactive : 10f;
            this.StartDelayAction(timeDelayDeactive, () =>
            {
                if (shieldUI.activeInHierarchy)
                {
                    shieldUI.SetActive(false);
                }
            });
        }
    }
    #endregion

    #region Message
    public void ShowNotice(string content, bool isLocalizeContent = true, PopupNoticeType popupType = PopupNoticeType.YesNo, bool isBackable = true,
        TextAlignmentOptions textAnchor = TextAlignmentOptions.Center, string title = "Notice", string labelYes = "Common.Confirm", string labelNo = "Common.Cancel", bool titleToUpper = true,
        UnityAction yesCallback = null, UnityAction noCallback = null, UnityAction closeCallback = null)
    {
        // Chuỗi là key bảng gốc thì dịch; chữ thường (đã dịch sẵn / tên riêng) giữ nguyên.
        if (isLocalizeContent)
        {
            content = Translate(content);
        }

        title = Translate(title);
        if (titleToUpper && title != null)
        {
            title = title.ToUpper();
        }
        labelYes = Translate(labelYes);
        labelNo = Translate(labelNo);

        notice.Show(content, popupType, isBackable, textAnchor, title, labelYes, labelNo, yesCallback, noCallback, closeCallback);
    }

    public void ShowToastMessage(string content, bool isLocalize = true)
    {
        if (isLocalize)
        {
            content = Translate(content);
        }

        toast.Show(content);
    }

    private static string Translate(string text)
    {
        return LocalizeManager.Instance.HasKey(text) ? LocalizeManager.Instance.GetLocalizeText(text) : text;
    }

    public void ShowToastMessageError(string content, bool isLocalize = true)
    {
        // TODO(follow-stick): thêm SFX lỗi khi hệ audio nhận SoundType (hiện AudioManager chỉ nhận AudioClip).
        ShowToastMessage(content, isLocalize: isLocalize);
    }
    #endregion.

    #region Waiting
    public void ShowWaiting(bool isOn, bool isTimeOut = true)
    {
        if (isOn)
        {
            int timeOut = isTimeOut ? 20 : 0;
            waiting.Show(timeOut);
        }
        else
        {
            waiting.Close();
        }
    }
    #endregion

    #region Fade
    public void FadeToLoadScene(string sceneName = null, UnityAction actionBeforeLoad = null)
    {
        Fade(color: FadeColor.Black, toMaxCallback: () =>
        {
            if (actionBeforeLoad != null)
                actionBeforeLoad();

            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        });
    }

    public void Fade(FadeColor color = FadeColor.White, float fadingSpeedToMax = 7f, float fadingSpeedBackToMin = 1f,
        UnityAction toMaxCallback = null, UnityAction toMinCallback = null)
    {
        if (isFading == false)
        {
            isFading = true;
            ActiveShield(true);
            StartCoroutine(CoroutineFade(color, fadingSpeedToMax, fadingSpeedBackToMin, toMaxCallback, toMinCallback));
        }
    }

    private IEnumerator CoroutineFade(FadeColor color, float fadingSpeedToMax, float fadingSpeedBackToMin,
        UnityAction toMaxCallback, UnityAction toMinCallback)
    {
        imgFade.color = color == FadeColor.White ? Color.white : Color.black;
        Color c = imgFade.color;
        c.a = 0f;
        imgFade.color = c;
        imgFade.gameObject.SetActive(true);
        bool isFadingToMax = true;

        while (isFading)
        {
            if (isFadingToMax)
            {
                c.a = Mathf.MoveTowards(c.a, 1f, fadingSpeedToMax * Time.deltaTime);
                imgFade.color = c;

                if (c.a >= 0.95f)
                {
                    c.a = 1f;
                    imgFade.color = c;
                    isFadingToMax = false;

                    if (toMaxCallback != null)
                    {
                        yield return new WaitForEndOfFrame();
                        toMaxCallback();
                    }
                }
            }
            else
            {
                c.a = Mathf.MoveTowards(c.a, 0f, fadingSpeedBackToMin * Time.deltaTime);
                imgFade.color = c;

                if (c.a <= 0.05f)
                {
                    c.a = 0f;
                    imgFade.color = c;
                    isFading = false;

                    if (toMinCallback != null)
                    {
                        yield return new WaitForEndOfFrame();
                        toMinCallback();
                    }

                    ActiveShield(false);
                    imgFade.gameObject.SetActive(false);
                }
            }

            yield return null;
        }
    }

    // Fade tách 2 nửa (chuyển màn campaign — GameplayUI.kjr/kjs gốc): tối dần rồi GIỮ tối tới khi gọi FadeIn.
    // target: tấm phủ riêng (VD chỉ vùng map của lobby); null = imgFade toàn màn hình. FadeIn dùng lại tấm này.
    public void FadeOut(FadeColor color, float duration, UnityAction doneCallback = null, Image target = null)
    {
        isFading = true;
        ActiveShield(true);
        imgFadeHalf = target != null ? target : imgFade;
        imgFadeHalf.color = color == FadeColor.White ? Color.white : Color.black;
        StartCoroutine(CoroutineFadeAlpha(imgFadeHalf, 0f, 1f, duration, false, doneCallback));
    }

    public void FadeIn(float duration, UnityAction doneCallback = null)
    {
        StartCoroutine(CoroutineFadeAlpha(imgFadeHalf != null ? imgFadeHalf : imgFade, 1f, 0f, duration, true, doneCallback));
    }

    private IEnumerator CoroutineFadeAlpha(Image img, float from, float to, float duration, bool isEnd, UnityAction doneCallback)
    {
        img.gameObject.SetActive(true);
        Color c = img.color;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(from, to, t / duration);
            img.color = c;
            yield return null;
        }

        c.a = to;
        img.color = c;
        if (isEnd)
        {
            isFading = false;
            ActiveShield(false);
            img.gameObject.SetActive(false);
        }

        doneCallback?.Invoke();
    }
    #endregion

    #region Tooltips
    public void ShowTooltips(string content, Vector3 position, TextAnchor textAnchor = TextAnchor.MiddleCenter)
    {
        tooltips.Show(content, position, textAnchor);
    }
    #endregion

    // TODO(follow-stick): Rewards region — port PopupReward + RewardData khi có hệ reward.
    // TODO(follow-stick): Recommendation region — port LRecommendation + PackageId/GameConfig.
    // TODO(follow-stick): Common region — OpenGear/OpenHeroShow theo feature Gears/Heroes.
}
