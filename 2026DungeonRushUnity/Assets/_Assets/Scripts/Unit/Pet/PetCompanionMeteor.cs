using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Companion Meteor (type 11) — Star Feather: "Summons meteors from the sky that deal <Value> Area Damage."
// Logic GỐC (reverse strategy `lb`):
//   • MeteorBombCount điểm rơi: enemy thứ (i % số enemy) + lệch ngẫu nhiên (insideUnitCircle ×
//     Random(0.5, MeteorOffsetRange)).
//   • Coroutine: lần lượt mỗi MeteorBombDelay thả 1 thiên thạch từ độ cao MeteorDropHeight, rơi với
//     BombProjectileSpeed; chạm đất → FX nổ (BombRadius, BombExplosionDuration) + damage chuẩn cho
//     mọi enemy trong BombRadius (+ cháy nếu BurnEnabled) — dùng chung phần nổ với Bomber.
public class PetCompanionMeteor : PetCompanionBomber
{
    private const float MIN_OFFSET = 0.5f;

    protected override void ReleaseAbility()
    {
        List<BaseUnit> enemies = GetEnemiesAroundOwner();
        if (enemies.Count == 0)
        {
            return;
        }

        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < companionData.meteorBombCount; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(MIN_OFFSET, Mathf.Max(MIN_OFFSET, companionData.meteorOffsetRange));
            points.Add(enemies[i % enemies.Count].Transform.position + (Vector3)(dir * dist));
        }

        StartCoroutine(RoutineDropMeteors(points));
        PlayShootSfx();
    }

    private IEnumerator RoutineDropMeteors(List<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 point = points[i];
            Vector3 start = point + Vector3.up * companionData.meteorDropHeight;

            CompanionBullet meteor = SpawnCompanionBullet(projectilePrefab, companionData.bombProjectileSpeed);
            if (meteor != null)
            {
                meteor.ActivePoint(start, point, this, Explode);
            }
            else
            {
                Explode(point);
            }

            yield return WaitBattleTime(companionData.meteorBombDelay);
        }
    }
}
