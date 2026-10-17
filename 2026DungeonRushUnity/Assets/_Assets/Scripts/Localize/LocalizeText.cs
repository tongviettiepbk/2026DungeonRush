using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Nhãn tự dịch. 2 cách dùng:
//  - Có `key` (như LocalizedLabel gốc: Table + Key, VD "Common.Claim"): dịch lúc Awake + khi đổi ngôn ngữ, và CHỈ khi
//    chữ đang hiện vẫn là chữ mẫu của prefab (`sourceText`) hoặc bản dịch lần trước — chữ code đã tự gán thì để nguyên
//    (popup cha có thể vẽ ở OnEnable trước khi Awake của nhãn con chạy).
//  - Không có `key` (kiểu StickIdle cũ): lấy chính chữ đang hiện làm key, dịch lại mỗi lần OnEnable.
public class LocalizeText : MonoBehaviour
{
    public string key;
    public string sourceText;
    public bool isUpperCharacters;

    private Text textContent;
    private TMP_Text textMeshPro;

    private string defaultTextKey;
    private string lastAppliedText;
    private bool initialized;

    private bool HasKey => !string.IsNullOrEmpty(key);

    private void Awake()
    {
        Initialize();
        if (HasKey)
        {
            EventDispatcher.Instance.RegisterListener(EventID.ChangeLanguage, OnChangeLanguageKeyed);
            ReloadText();
        }
    }

    private void OnDestroy()
    {
        if (HasKey && EventDispatcher.IsNull == false)
        {
            EventDispatcher.Instance.RemoveListener(EventID.ChangeLanguage, OnChangeLanguageKeyed);
        }
    }

    private void OnEnable()
    {
        if (HasKey)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name == StaticValue.SCENE_MAINGAME)
        {
            EventDispatcher.Instance.RegisterListener(EventID.ChangeLanguage, OnChangeLanguage);
        }

        ReloadText();
    }

    private void OnDisable()
    {
        if (HasKey)
        {
            return;
        }

        try
        {
            if (SceneManager.GetActiveScene().name == StaticValue.SCENE_MAINGAME)
            {
                EventDispatcher.Instance.RemoveListener(EventID.ChangeLanguage, OnChangeLanguage);
            }
        }
        catch { }
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        defaultTextKey = string.Empty;

        textMeshPro = GetComponent<TMP_Text>();
        if (textContent == null)
            textContent = GetComponent<Text>();

        if (HasKey)
        {
            defaultTextKey = key;
        }
        else if (textMeshPro != null)
        {
            if (!string.IsNullOrEmpty(textMeshPro.text) && LocalizeManager.Instance.defaultDictionary.ContainsKey(textMeshPro.text))
            {
                defaultTextKey = textMeshPro.text;
            }
        }
        else
        {
            if (textContent != null && !string.IsNullOrEmpty(textContent.text) && LocalizeManager.Instance.defaultDictionary.ContainsKey(textContent.text))
            {
                defaultTextKey = textContent.text;
            }
        }

        initialized = true;
    }

    private void ReloadText()
    {
        try
        {
            Initialize();
            if (HasKey && IsShowingLabel() == false)
            {
                return;
            }
            string translatedText = LocalizeManager.Instance.GetLocalizeText(defaultTextKey, isUpperCharacters);

            if (string.IsNullOrEmpty(translatedText) == false)
            {
                if (textMeshPro != null)
                {
                    textMeshPro.text = translatedText;
                }
                else
                {
                    textContent.text = translatedText;
                }
                lastAppliedText = translatedText;
            }
        }
        catch { }
    }

    private bool IsShowingLabel()
    {
        string current = textMeshPro != null ? textMeshPro.text : (textContent != null ? textContent.text : null);
        return string.IsNullOrEmpty(sourceText) || current == sourceText || current == lastAppliedText;
    }

    private void OnChangeLanguageKeyed(object obj)
    {
        ReloadText();
    }

    private void OnChangeLanguage(object obj)
    {
        if (gameObject.activeInHierarchy)
        {
            bool isTextNeedTranslate = false;

            if (textMeshPro != null)
            {
                if (string.IsNullOrEmpty(textMeshPro.text) == false)
                {
                    isTextNeedTranslate = true;
                }
            }
            else
            {
                if (textContent != null && string.IsNullOrEmpty(textContent.text) == false)
                {
                    isTextNeedTranslate = true;
                }
            }

            if (isTextNeedTranslate)
            {
                LocalizeManager.Instance.StartDelayAction(0.2f, () =>
                {
                    ReloadText();
                });
            }
        }
    }
}
