using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LocalizeText : MonoBehaviour
{
    public bool isUpperCharacters;

    private Text textContent;
    private TMP_Text textMeshPro;

    private string defaultTextKey;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (SceneManager.GetActiveScene().name == StaticValue.SCENE_MAINGAME)
        {
            EventDispatcher.Instance.RegisterListener(EventID.ChangeLanguage, OnChangeLanguage);
        }

        ReloadText();
    }

    private void OnDisable()
    {
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
        if (textMeshPro != null)
        {
            if (!string.IsNullOrEmpty(textMeshPro.text) && LocalizeManager.Instance.defaultDictionary.ContainsKey(textMeshPro.text))
            {
                defaultTextKey = textMeshPro.text;
            }
        }
        else
        {
            if (textContent == null)
                textContent = GetComponent<Text>();

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
            }
        }
        catch { }
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
