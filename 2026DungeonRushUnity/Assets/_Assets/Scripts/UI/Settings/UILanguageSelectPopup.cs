using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Popup chọn ngôn ngữ (LanguageSelectPopup gốc), mở từ nút Language ở trang Settings của UIProfilePopup.
// Lưới 10 ngôn ngữ (danh sách + cờ nằm trong prefab, đúng thứ tự gốc), ô đang dùng có dấu tích.
// Bấm ngôn ngữ khác → LocalizeManager.ChangeLanguage: lưu mã, đóng mọi popup, nạp lại màn chơi chính (như gốc).
public class UILanguageSelectPopup : BaseUI
{
    [Serializable]
    public class LanguageOption
    {
        public string code;
        public string displayName;
        public Sprite flag;
    }

    public Button btClose;
    public Transform gridContent;
    public ElementLanguageUI elementPrefab;
    public List<LanguageOption> languages = new List<LanguageOption>();

    private readonly List<ElementLanguageUI> elements = new List<ElementLanguageUI>();
    private readonly List<string> elementCodes = new List<string>();
    private string currentCode;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
    }

    // eip + kmh gốc.
    protected override void OnEnable()
    {
        base.OnEnable();
        currentCode = LocalizeManager.Instance.curLanguage.ToCode();
        for (int i = 0; i < languages.Count; i++)
        {
            LanguageOption option = languages[i];
            if (option == null || string.IsNullOrEmpty(option.code))
                continue;
            ElementLanguageUI element = Instantiate(elementPrefab, gridContent);
            element.Show(option.code, option.displayName, option.flag, option.code == currentCode, OnClickLanguage);
            elements.Add(element);
            elementCodes.Add(option.code);
        }
    }

    // ein gốc: ẩn popup là xoá các ô.
    private void OnDisable()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] != null)
                Destroy(elements[i].gameObject);
        }
        elements.Clear();
        elementCodes.Clear();
    }

    // kmi gốc.
    private void OnClickLanguage(string code)
    {
        if (code == currentCode)
            return;

        currentCode = code;
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].SetSelected(elementCodes[i] == code);
        }

        LanguageID id;
        if (LanguageCode.TryParse(code, out id))
        {
            LocalizeManager.Instance.ChangeLanguage(id);
        }
    }

    // krj gốc (ProfilePopup đọc danh sách của popup này để hiện cờ + tên ngôn ngữ đang dùng).
    public static LanguageOption GetOption(string code)
    {
        UILanguageSelectPopup prefab = Resources.Load<UILanguageSelectPopup>(UIManager.Instance.UI_PREFAB_PATH + UIKey.LanguageSelectPopup);
        if (prefab == null)
            return null;
        return prefab.languages.Find(x => x != null && x.code == code);
    }
}
