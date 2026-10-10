using UnityEngine;

// Companion MirrorClone (type 14) — Halo Hare: "Summons a spectral clone possessing <Value>% of your Health that fights alongside you for <Value> seconds."
// Logic GỐC (reverse strategy `ld` + lt.gon + CompanionCloneTracker):
//   • Nhắm CHỦ. Bắn đạn về hero → tới nơi sinh BẢN SAO hero (cùng prefab + trang bị) ở ô trống gần hero
//     (dò tối đa CloneGridSearchCount ô).
//   • Máu tối đa bản sao = maxHp hero × (CloneHealthPercent + CloneHealthPercentScaler × level) / 100
//     (KHÔNG nhân bonus); sống CloneLifetime giây rồi biến mất. Hình mờ theo CloneAlpha/CloneBrightness.
public class PetCompanionClone : PetUnit
{
    protected override bool TargetsOwner => true;

    protected override void ReleaseAbility()
    {
        FireHoming(projectilePrefab, owner, companionData.projectileSpeed, _ => SpawnClone());
        PlayShootSfx();
    }

    private void SpawnClone()
    {
        BaseMode mode = GameController.Instance.mode;
        if (mode == null || mode.heroPrefab == null || owner == null || !owner.isTargetable)
        {
            return;
        }

        Vector3 pos = FindFreeCellNear(owner.Transform.position, companionData.cloneGridSearchCount);
        GameObject go = Instantiate(mode.heroPrefab, pos, Quaternion.identity, owner.Transform.parent);
        HeroUnit clone = go.GetComponent<HeroUnit>();
        if (clone == null)
        {
            Destroy(go);
            return;
        }

        float percent = companionData.cloneHealthPercent + companionData.cloneHealthPercentScaler * level;
        clone.SetupAsMirrorClone(percent);
        clone.CopyGhostFrom(owner as HeroUnit);
        clone.SpawnInBattle(owner.GetBaseStats(), owner.tag, pos);

        CompanionCloneTracker.Attach(clone, companionData.cloneLifetime, companionData.cloneAlpha, companionData.cloneBrightness);
    }

    // Ô đi được gần hero nhất (theo vòng lân cận), tối đa 'searchCount' ô thử; không có → cạnh hero.
    private static Vector3 FindFreeCellNear(Vector3 center, int searchCount)
    {
        MapController map = MapController.Instance;
        Vector2Int c = map.WorldToCell(center);
        Vector2Int[] offsets =
        {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        int tries = Mathf.Min(Mathf.Max(1, searchCount), offsets.Length);
        for (int i = 0; i < tries; i++)
        {
            Vector2Int cell = c + offsets[i];
            if (map.IsWalkable(cell))
            {
                return map.CellToWorld(cell);
            }
        }
        return map.ClampPointInMap(center + Vector3.right * 0.8f);
    }
}
