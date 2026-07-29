using UnityEngine;

// UI gắn với một mode chơi — bám StickIdle (GameModes/UIModeBase.cs) nhưng lược `ClockTimer`
// (DungeonRush chưa có type này). Mỗi mode con có UIMode riêng kế thừa lớp này.
public class UIModeBase : MonoBehaviour
{
    public ModeType mode;

    // Bật/tắt các nút theo trạng thái mode. UI con override.
    public virtual void CheckButtons() { }

    // Hook cập nhật đồng hồ trận (BaseMode.RoutineTimer gọi qua mode con). UI con override.
    public virtual void UpdateBattleTime(float timer, int totalTime) { }
}
