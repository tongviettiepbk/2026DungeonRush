// Hero (quân người chơi, TeamA). Hành vi CHỦ ĐỘNG đúng spec:
//   tự tìm enemy GẦN NHẤT trong map → đi tới vùng đánh → attack.
// Toàn bộ state machine (Idle→Move→Attack) đã có ở BaseUnit; Hero chỉ chỉnh cách chọn mục
// tiêu để không giới hạn tầm (luôn tìm khắp map) và không lọc theo biên camera.
//
// Hình ảnh trang bị (mặc đồ): 5 slot dùng SpriteRenderer đã wiring qua HeroEquipmentVisual.
// Cape (Spine skin) làm ở bước sau.

public class HeroUnit : BaseUnit
{
    private HeroEquipmentVisual equipmentVisual;

    protected override void Awake()
    {
        base.Awake();

        equipmentVisual = GetComponent<HeroEquipmentVisual>();
        if (equipmentVisual == null)
        {
            equipmentVisual = gameObject.AddComponent<HeroEquipmentVisual>();
        }
        equipmentVisual.Bind(Transform);
        equipmentVisual.RefreshAll();
    }

    // Dựng lại hình trang bị từ save — gọi sau khi người chơi đổi đồ ở menu.
    public void RefreshEquipment()
    {
        equipmentVisual?.RefreshAll();
    }

    protected override void FindNearestTarget()
    {
        target = FindNearestEnemyAmong(GetAliveEnemies());
    }
}
