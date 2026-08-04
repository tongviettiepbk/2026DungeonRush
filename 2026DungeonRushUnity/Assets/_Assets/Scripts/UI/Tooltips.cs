using UnityEngine;
using UnityEngine.UI;

public class Tooltips : BaseUI
{
    public RectTransform rectTransform;
    public Image bgText;
    public Text textContent;

    private bool isShowDone;

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            if (isShowDone && gameObject.activeInHierarchy)
            {
                Close();
            }
        }
    }

    public void Show(string content, Vector3 position, TextAnchor textAnchor)
    {
        textContent.text = content;
        textContent.alignment = textAnchor;
        isShowDone = false;

        UIManager.Instance.StartActionEndOfFrame(() =>
        {
            textContent.gameObject.SetActive(false);
            textContent.gameObject.SetActive(true);
            bgText.transform.position = position;

            UIManager.Instance.StartActionEndOfFrame(() =>
            {
                ValidateShowPoint();
                isShowDone = true;
            });
        });

        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    private void ValidateShowPoint()
    {
        float halfContentWidth = bgText.rectTransform.rect.width / 2;
        float halfContentHeight = bgText.rectTransform.rect.height / 2;

        float maxX = (rectTransform.rect.width) / 2;
        float maxY = (rectTransform.rect.height) / 2;

        float margin = 10f;

        Vector3 v = bgText.rectTransform.anchoredPosition;

        // Tràn trái
        if (v.x < 0 && Mathf.Abs(v.x) + halfContentWidth > maxX)
        {
            v.x = -(maxX - halfContentWidth - margin);
        }

        // Tràn phải
        if (v.x > 0 && v.x + halfContentWidth > maxX)
        {
            v.x = maxX - halfContentWidth - margin;
        }

        // Tràn trên
        if (v.y > 0 && v.y + halfContentHeight > maxY)
        {
            v.y = maxY - halfContentHeight - margin;
        }

        // Tràn dưới
        if (v.y < 0 && Mathf.Abs(v.y) + halfContentHeight > maxY)
        {
            v.y = -(maxY - halfContentHeight - margin);
        }

        bgText.rectTransform.anchoredPosition = v;
    }
}
