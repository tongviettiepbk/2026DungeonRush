using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum CombatTextType
{
    Default,
    Heal,
    DamageA,
    DamageB,
    Status
}

public class CombatText : MonoBehaviour
{
    public Canvas canvas;
    public RectTransform canvasRect;
    public TextMeshProUGUI textMesh;
    public Animation anim;
    public RectTransform contentRect;

    private Material materialDefault;

    [Header("Heal")]
    public string animHeal;
    public Color32 colorHeal;
    public Color32 colorHealOutline;
    private Material materialHeal;

    [Header("Damage")]
    public TMP_SpriteAsset spriteAssetCrit;
    public TMP_SpriteAsset spriteAssetIgnoreDef;
    public string animHitNormal;
    public string animHitCrit;
    public Color32 colorHit_A;
    public Color32 colorHitOutline_A;
    public Color32 colorHit_B;
    public Color32 colorHitOutline_B;

    private Material materialDamageA;
    private Material materialDamageB;

    [Header("Status")]
    public string animStatus;
    public Color32 colorStatus;
    public Color32 colorStatusOutline;
    private Material materialStatus;

    private const string FORMAT_ICON_DAMAGE = "<sprite=0> {0}";
    private const string FORMAT_HEAL = "+{0}";

    private const string KEY_UNDERLAY = "UNDERLAY_ON";
    private const string PROPERTY_UNDERLAY = "_UnderlayColor";

    private bool isInitialized;

    private void Awake()
    {
        EventDispatcher.Instance.RegisterListener(EventID.ResetMode, OnResetMode);

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            try
            {
                Deactive();
            }
            catch { }
        };

        materialDefault = new Material(textMesh.fontMaterial);
        materialHeal = new Material(textMesh.fontMaterial);
        materialDamageA = new Material(textMesh.fontMaterial);
        materialDamageB = new Material(textMesh.fontMaterial);
        materialStatus = new Material(textMesh.fontMaterial);
    }

    public void Initialize()
    {
        if (isInitialized == false)
        {
            isInitialized = true;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main; // TODO(follow-stick): đổi sang CameraController khi port.
            canvas.sortingLayerName = StaticValue.SORTING_LAYER_UI;
            canvas.sortingOrder = 4;
        }
    }

    public void ShowDamage(Vector3 position, double damage, bool isCrit, bool isTeamA)
    {
        string content = damage >= 10_000 ? damage.ToLetter() : ((int)damage).ToString();

        if (isCrit)
        {
            content = string.Format(FORMAT_ICON_DAMAGE, content);
            textMesh.spriteAsset = isCrit ? spriteAssetCrit : spriteAssetIgnoreDef;
            anim.Play(animHitCrit);
        }
        else
        {
            anim.Play(animHitNormal);
        }

        CombatTextType typeText = isTeamA ? CombatTextType.DamageA : CombatTextType.DamageB;
        Color32 colorText = isCrit ? colorHit_A : isTeamA ? colorHit_A : colorHit_B;
        Color32 colorOutline = isCrit ? colorHitOutline_A : isTeamA ? colorHitOutline_A : colorHitOutline_B;
        SetColor(typeText, colorText, colorOutline);

        Show(content, position);
    }

    public void ShowHeal(Vector3 position, double hp)
    {
        string content = string.Format(FORMAT_HEAL, (int)hp);
        SetColor(CombatTextType.Heal, colorHeal, colorHealOutline);
        anim.Play(animHeal);
        Show(content, position);
    }

    public void ShowStatus(Vector3 position, string content)
    {
        SetColor(CombatTextType.Status, colorStatus, colorStatusOutline);
        anim.Play(animStatus);
        Show(content, position);
    }

    public void SetColor(CombatTextType type, Color32 colorText, Color32 colorOutline)
    {
        textMesh.color = colorText;
        Material material = materialDefault;

        switch (type)
        {
            case CombatTextType.DamageA: material = materialDamageA; break;
            case CombatTextType.DamageB: material = materialDamageB; break;
            case CombatTextType.Heal: material = materialHeal; break;
            case CombatTextType.Status: material = materialStatus; break;
        }

        textMesh.fontMaterial = material;
        textMesh.fontMaterial.EnableKeyword(KEY_UNDERLAY);
        textMesh.fontMaterial.SetColor(PROPERTY_UNDERLAY, colorOutline);
    }

    public void Show(string content, Vector3 position)
    {
        //return;
        textMesh.text = content;
        canvasRect.transform.SetParent(null);

        //Vector2 viewPortPosition = CameraController.Instance.Camera.WorldToViewportPoint(position);
        //Vector2 screenPosition;
        //screenPosition.x = (viewPortPosition.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f);
        //screenPosition.y = (viewPortPosition.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f);
        //contentRect.anchoredPosition = screenPosition;

        Vector2 screenPosition;
        screenPosition.x = position.x;
        screenPosition.y = position.y;
        canvasRect.anchoredPosition = screenPosition;
        canvasRect.transform.SetParent(PoolingController.Instance.transform);

        gameObject.SetActive(true);
    }

    public void Deactive()
    {
        canvasRect.transform.SetParent(PoolingController.Instance.groupTextDamage);
        canvasRect.anchoredPosition = Vector2.zero;
        PoolingController.Instance.poolTextDamage.Store(this);

        gameObject.SetActive(false);
    }

    private void OnResetMode(object obj)
    {
        try
        {
            if (gameObject.activeInHierarchy)
            {
                Deactive();
            }
        }
        catch { }
    }
}
