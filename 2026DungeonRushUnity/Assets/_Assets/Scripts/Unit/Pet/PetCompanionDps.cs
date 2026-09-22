using UnityEngine;

// Companion_1_Common_DPS_Single — "Ember Fist" (CompanionType.DPS).
// Kỹ năng: bắn 1 viên đạn lửa vào enemy gần nhất trong tầm; trúng đích gây sát thương companion
// (đơn mục tiêu) + spawn Fx va chạm (impactAttack). Mô tả gốc: "Launches flames that deal X Damage."
//
// Đạn dùng chung hạ tầng BaseBullet (straight/parabol + impact FX). Tốc độ đạn lấy từ
// CompanionData.projectileSpeed. Thiếu prefab đạn → fallback cận chiến ở PetUnit.ReleaseAbility().
public class PetCompanionDps : PetUnit
{
    [Header("DPS (Ember Fist)")]
    [Tooltip("Prefab đạn lửa (gắn BaseBullet). Trống → đánh cận chiến tạm.")]
    [SerializeField] private BaseBullet projectilePrefab;

    [Tooltip("Âm thanh khi bắn (ShootSound của companion gốc). Có thể để trống.")]
    [SerializeField] private AudioClip sfxShoot;

    protected override void ReleaseAbility()
    {
        if (target == null || projectilePrefab == null)
        {
            // Không có prefab đạn → dùng đòn cận chiến mặc định để vẫn ra damage.
            base.ReleaseAbility();
            return;
        }

        BaseBullet bullet = PoolingController.Instance.GetBullet(projectilePrefab);
        if (bullet != null)
        {
            if (companionData != null && companionData.projectileSpeed > 0.01f)
            {
                bullet.speed = companionData.projectileSpeed;
            }

            bullet.Active(firePoint, this, target, GetAbilityAttackData());
        }

        if (sfxShoot != null)
        {
            PlaySfx(sfxShoot);
        }
    }
}
