using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Mảnh EXP rơi trên map campaign — port phần ExperienceShard của LootDropController gốc (reverse il2cpp):
//   • Enemy chết → rơi Random[min..max] mảnh (gốc 1..3, ×boost exp trần ×5), mỗi mảnh nảy ra theo
//     cung Bezier quanh xác rồi nằm trên map (hsm / hsn / hsq).
//   • Thắng → mọi mảnh trên map lần lượt bay về hero, chạm hero thì biến mất kèm tiếng nhặt (hsv / hsx / hsy).
// Chỉ là hiệu ứng: exp thật vẫn cộng 1 lần ở CampaignMode.CalculateResult (round(49 + level)).
// Giá trị mặc định = GameResources + LootDropController trong scene gốc.
public class LootDropController : MonoBehaviour
{
    [Header("Prefab (âm thanh = AudioConfig.LootCollectSound)")]
    public GameObject expShardPrefab;

    [Header("Số mảnh mỗi quái (ExperienceShardDropCountMin/Max)")]
    public int dropCountMin = 1;
    public int dropCountMax = 3;

    [Header("Nảy ra khi quái chết")]
    public float scatterRadius = 1f;
    public float scatterDuration = 0.3f;
    public float bezierHeightMin = 1.6f;
    public float bezierHeightMax = 2f;

    [Header("Bay về hero khi thắng")]
    public float collectStartDelay = 0.8f;
    public float collectDelayMin = 0f;
    public float collectDelayMax = 0.7f;
    public float collectDurationMin = 0.2f;
    public float collectDurationMax = 0.5f;

    // Gốc CollectingLootData: chờ delay rồi mới bắt đầu bay, bay trong duration giây.
    private class CollectingShard
    {
        public Transform shard;
        public float delay;
        public float duration;
        public float elapsed;
        public bool isMoving;
        public Vector3 startPosition;
        public float moveElapsed;
    }

    private readonly List<Transform> drops = new List<Transform>();
    private readonly List<CollectingShard> collecting = new List<CollectingShard>();
    private Transform target;

    // Đang thu mảnh về hero (CampaignMode chờ xong mới dựng màn mới).
    public bool IsCollecting { get; private set; }

    // ===== RƠI =====

    public void DropExp(Vector3 position)
    {
        if (expShardPrefab == null)
        {
            return;
        }

        int count = Random.Range(dropCountMin, dropCountMax + 1);

        // Gốc: đang boost exp thì rơi nhiều mảnh hơn = round(min(boost, 5) × count), tối thiểu 1.
        float multiplier = ShopService.GetExpMultiplier();
        if (multiplier > 1f)
        {
            count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(multiplier, 5f) * count));
        }

        for (int i = 0; i < count; i++)
        {
            SpawnShard(position);
        }
    }

    private void SpawnShard(Vector3 position)
    {
        Transform shard = Instantiate(expShardPrefab, position, Quaternion.identity, transform).transform;
        drops.Add(shard);

        // Điểm rơi: hướng ngẫu nhiên, cách xác [R/2, R], kẹp trong biên map.
        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector3 end = position + (Vector3)(dir * Random.Range(scatterRadius * 0.5f, scatterRadius));
        end = MapController.Instance.ClampPointInMap(end);

        // Điểm điều khiển Bezier = giữa đường + nhấc lên → mảnh nảy thành vòng cung.
        Vector3 control = (position + end) * 0.5f + Vector3.up * Random.Range(bezierHeightMin, bezierHeightMax);

        Vector3 scale = shard.localScale;
        shard.localScale = Vector3.zero;

        DOTween.Sequence()
            .Append(shard.DOScale(scale, scatterDuration * 0.4f).SetEase(Ease.OutBack))
            .Join(DOVirtual.Float(0f, 1f, scatterDuration, t => shard.position = Bezier(position, control, end, t)).SetEase(Ease.OutQuad))
            .SetTarget(shard);
    }

    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    // ===== THU VỀ HERO =====

    public void CollectAll(Transform collectTarget)
    {
        target = collectTarget;
        IsCollecting = true;

        for (int i = 0; i < drops.Count; i++)
        {
            if (drops[i] == null)
            {
                continue;
            }

            collecting.Add(new CollectingShard
            {
                shard = drops[i],
                delay = collectStartDelay + Random.Range(collectDelayMin, collectDelayMax),
                duration = Random.Range(collectDurationMin, collectDurationMax),
            });
        }
        drops.Clear();
    }

    private void Update()
    {
        for (int i = collecting.Count - 1; i >= 0; i--)
        {
            CollectingShard c = collecting[i];
            if (c.shard == null)
            {
                collecting.RemoveAt(i);
                continue;
            }

            c.elapsed += Time.deltaTime;
            if (c.elapsed < c.delay)
            {
                continue;
            }

            // Mất hero → bỏ mảnh luôn.
            if (target == null)
            {
                DestroyShard(c.shard);
                collecting.RemoveAt(i);
                continue;
            }

            if (!c.isMoving)
            {
                c.isMoving = true;
                c.startPosition = c.shard.position;
            }

            // Gốc: lerp theo t^4 → khởi đầu chậm rồi hút mạnh vào hero.
            c.moveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(c.moveElapsed / c.duration);
            c.shard.position = Vector3.Lerp(c.startPosition, target.position, t * t * t * t);

            if (t >= 1f)
            {
                AudioManager.Instance.PlayLootCollect();
                DestroyShard(c.shard);
                collecting.RemoveAt(i);
            }
        }

        if (IsCollecting && collecting.Count == 0)
        {
            IsCollecting = false;
            target = null;
        }
    }

    // Dọn mọi mảnh (dựng lại màn / thua).
    public void Clear()
    {
        for (int i = 0; i < drops.Count; i++)
        {
            if (drops[i] != null)
            {
                DestroyShard(drops[i]);
            }
        }
        for (int i = 0; i < collecting.Count; i++)
        {
            if (collecting[i].shard != null)
            {
                DestroyShard(collecting[i].shard);
            }
        }

        drops.Clear();
        collecting.Clear();
        target = null;
        IsCollecting = false;
    }

    private static void DestroyShard(Transform shard)
    {
        DOTween.Kill(shard);
        Destroy(shard.gameObject);
    }
}
