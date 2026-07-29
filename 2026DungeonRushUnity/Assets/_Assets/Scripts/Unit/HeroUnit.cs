// Hero (quân người chơi, TeamA). Hành vi CHỦ ĐỘNG đúng spec:
//   tự tìm enemy GẦN NHẤT trong map → đi tới vùng đánh → attack.
// Toàn bộ state machine (Idle→Move→Attack) đã có ở BaseUnit; Hero chỉ chỉnh cách chọn mục
// tiêu để không giới hạn tầm (luôn tìm khắp map) và không lọc theo biên camera.
//
// TODO(gear-visual): mặc vũ khí/găng/cánh/áo choàng hiển thị hình ảnh — cần wiring Spine slot
// trên prefab Hero (PreviewCharacter). Làm ở bước sau, không thuộc vòng lặp combat này.
public class HeroUnit : BaseUnit
{
    protected override void FindNearestTarget()
    {
        target = FindNearestEnemyAmong(GetAliveEnemies());
    }
}
