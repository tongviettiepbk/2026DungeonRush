using UnityEngine;

// Đơn vị enemy runtime — giữ stat sinh ra từ EnemySpawnGenerator.
// Combat thật (di chuyển/đánh nhau) sẽ bổ sung ở bước sau; hiện chỉ mang dữ liệu + vị trí.
public class EnemyUnit : MonoBehaviour
{
    public int level;
    public float health;
    public float maxHealth;
    public float attackPower;
    public float attackSpeed;
    public float moveSpeed;
    public bool isBoss;

    public void Init(EnemySpawnGenerator.EnemySpawnInfo info)
    {
        level = info.level;
        health = maxHealth = info.health;
        attackPower = info.attackPower;
        attackSpeed = info.attackSpeed;
        moveSpeed = info.moveSpeed;
        isBoss = info.isBoss;
    }
}
