using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Companion Blaster (type 10) — Echo Wing: "Releases sonic waves that deal <Value> Area Damage."
// Logic GỐC (reverse strategy `kh` + CompanionBlasterEffect):
//   • Nhắm enemy gần hero nhất, pet phải ở trong BlasterRange (tầm đánh = BlasterRange).
//   • Bắn BlasterWaveCount đợt sóng rải đều trong BlasterFireDuration (đợt đầu ngay lập tức).
//   • Mỗi đợt: mọi enemy trong BlasterRange quanh pet VÀ trong hình nón BlasterConeAngle quanh hướng
//     nhắm nhận TRỌN damage = (DamageBase + DamageScaler×lv)×bonus. Hướng nhắm bám mục tiêu hiện tại.
public class PetCompanionBlaster : PetUnit
{
    [SerializeField] private Color waveColor = new Color(0.55f, 0.9f, 1f, 0.9f);

    private const int ARC_SEGMENTS = 16;
    private LineRenderer waveLine;

    protected override BaseStats BuildCompanionStats(CompanionData data)
    {
        BaseStats s = base.BuildCompanionStats(data);
        if (data != null && data.blasterRange > 0f)
        {
            s.attackRange = data.blasterRange;
        }
        return s;
    }

    protected override void ReleaseAbility()
    {
        StartCoroutine(RoutineWaves(target, GetAbilityDamage()));
        PlayShootSfx();
    }

    private IEnumerator RoutineWaves(BaseUnit aim, double damage)
    {
        int waves = Mathf.Max(1, companionData.blasterWaveCount);
        float interval = companionData.blasterFireDuration / waves;
        Vector3 dir = aim != null ? (aim.Transform.position - Transform.position).normalized : Vector3.right;

        for (int w = 0; w < waves; w++)
        {
            if (aim != null && aim.isTargetable)
            {
                dir = (aim.Transform.position - Transform.position).normalized;
            }

            FireWave(dir, damage);
            yield return WaitBattleTime(interval);
        }

        if (waveLine != null)
        {
            waveLine.enabled = false;
        }
    }

    private void FireWave(Vector3 dir, double damage)
    {
        float range = companionData.blasterRange;
        float cosHalf = Mathf.Cos(companionData.blasterConeAngle * 0.5f * Mathf.Deg2Rad);
        Vector3 origin = Transform.position;

        List<BaseUnit> enemies = GetEnemiesInRadius(origin, range);
        for (int i = 0; i < enemies.Count; i++)
        {
            Vector3 to = (enemies[i].Transform.position - origin).normalized;
            if (Vector3.Dot(to, dir) >= cosHalf)
            {
                DealDamage(enemies[i], damage);
            }
        }

        DrawCone(origin, dir, range);
    }

    // Hình nón sóng âm: 2 cạnh + cung ở tầm tối đa.
    private void DrawCone(Vector3 origin, Vector3 dir, float range)
    {
        if (waveLine == null)
        {
            waveLine = CompanionFx.CreateLine(Transform, "BlasterWave", waveColor, 0.06f);
        }

        float half = companionData.blasterConeAngle * 0.5f;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        waveLine.positionCount = ARC_SEGMENTS + 3;
        waveLine.SetPosition(0, origin);
        for (int i = 0; i <= ARC_SEGMENTS; i++)
        {
            float a = (baseAngle - half + companionData.blasterConeAngle * i / ARC_SEGMENTS) * Mathf.Deg2Rad;
            waveLine.SetPosition(i + 1, origin + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * range);
        }
        waveLine.SetPosition(ARC_SEGMENTS + 2, origin);
        waveLine.enabled = true;
    }
}
