using UnityEngine;

// 1 khối đất/đá/quặng trên lưới mỏ (MiningBlock gốc). Pool do MiningGrid giữ.
public class MiningBlock : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer damageOverlay;
    public SpriteRenderer clickOverlay;

    public MineOreType OreType { get; private set; }
    public bool IsCollectible { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public int HitsRequired { get; private set; }
    public int CurrentHits { get; private set; }

    private MiningBlockHpUI hpUI;

    public bool HasHpUI => hpUI != null;

    // ixh: gán loại quặng + sprite, scale sprite vừa ô (size = CellSize × BlockScale), tắt overlay.
    public void Setup(MineOreType ore, bool collectible, int hitsRequired, Sprite sprite, Vector2Int position, float size)
    {
        OreType = ore;
        IsCollectible = collectible;
        HitsRequired = hitsRequired;
        CurrentHits = 0;
        GridPosition = position;

        spriteRenderer.sprite = sprite;
        if (sprite != null)
        {
            float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            if (spriteSize > 0f)
            {
                float scale = size / spriteSize;
                transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        if (damageOverlay != null) damageOverlay.enabled = false;
        if (clickOverlay != null) clickOverlay.enabled = false;
    }

    // ixj/ixk: ô sáng lên khi giữ tay (OnPointerDown/Up của popup).
    public void ShowClickOverlay()
    {
        if (clickOverlay != null) clickOverlay.enabled = true;
    }

    public void HideClickOverlay()
    {
        if (clickOverlay != null) clickOverlay.enabled = false;
    }

    // ixl: nạp số nhát đã đập từ save.
    public void SetCurrentHits(int hits)
    {
        CurrentHits = hits;
        if (damageOverlay != null) damageOverlay.enabled = true;
    }

    // ixm: đập 1 nhát. true = vỡ.
    public bool Hit()
    {
        CurrentHits++;
        if (CurrentHits >= HitsRequired)
        {
            return true;
        }

        if (damageOverlay != null) damageOverlay.enabled = true;
        if (hpUI != null) hpUI.SetValue(1f - (float)CurrentHits / HitsRequired);
        return false;
    }

    // ixn
    public void SetHpUI(MiningBlockHpUI ui)
    {
        hpUI = ui;
    }

    // ixo: trả thanh máu về pool.
    public void ReleaseHpUI()
    {
        if (hpUI != null)
        {
            Destroy(hpUI.gameObject);
            hpUI = null;
        }
    }

    // OnSpawn/OnDespawn
    public void OnDespawn()
    {
        ReleaseHpUI();
        CurrentHits = 0;
        HideClickOverlay();
        if (damageOverlay != null) damageOverlay.enabled = false;
    }
}
