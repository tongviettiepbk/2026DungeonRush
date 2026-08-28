using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Port từ StickIdle. Đã lược bớt các region Enemies/GoldDrop vì DungeonRush
// chưa có BaseEnemy/GoldDrop/PrefabUtils; sẽ thêm lại khi các hệ thống đó được port.
public class PoolingController : Singleton<PoolingController>
{
    [Header("Groups")]
    public Transform groupFx;
    public Transform groupTextDamage;
    public Transform groupBullet;

    public ObjectPooling<CombatText> poolTextDamage = new ObjectPooling<CombatText>();

    public Dictionary<int, ObjectPooling<BaseFx>> poolFx = new Dictionary<int, ObjectPooling<BaseFx>>();
    public Dictionary<int, int> activeFxInstances = new Dictionary<int, int>();

    public Dictionary<int, ObjectPooling<BaseBullet>> poolBullets = new Dictionary<int, ObjectPooling<BaseBullet>>();
    private Dictionary<string, int> bulletIdByNames = new Dictionary<string, int>();
    private int idBullet;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

    }

    #region Fx
    public void StoreFx(BaseFx fx)
    {
        int id = fx.idFx;

        if (id == 0)
        {
            //DebugCustom.LogFormat("[StoreFx] id={0}, type={1}", fx.idFx);
            return;
        }

        if (!poolFx.ContainsKey(id))
        {
            poolFx[id] = new ObjectPooling<BaseFx>();
        }

        if (!poolFx[id].Contains(fx))
        {
            poolFx[id].Store(fx);
        }

        fx.transform.parent = groupFx;
        transform.position = Vector3.zero;
        HideFxInScene(id);
    }

    public BaseFx GetEffectFromPool(int id)
    {
        if (poolFx.ContainsKey(id) == false)
        {
            poolFx[id] = new ObjectPooling<BaseFx>();
        }

        BaseFx fx = poolFx[id].New();
        return fx;
    }

    public int GetActiveInstances(int id)
    {
        int current = 0;

        if (activeFxInstances.ContainsKey(id))
        {
            current = activeFxInstances[id];
        }

        return current;
    }

    public void AddFxInScene(int id)
    {
        int current = 0;

        if (activeFxInstances.ContainsKey(id))
        {
            current = activeFxInstances[id];
        }

        current++;
        activeFxInstances[id] = current;
    }

    public void HideFxInScene(int id)
    {
        int current = 0;

        if (activeFxInstances.ContainsKey(id))
        {
            current = activeFxInstances[id];
        }

        current--;
        activeFxInstances[id] = current;

        if (current < 0)
        {
            activeFxInstances.Remove(id);
        }
    }
    #endregion

    #region Bullets
    // Lấy 1 đạn từ pool theo prefab (khoá theo tên prefab). Hết pool thì Instantiate mới.
    public BaseBullet GetBullet(BaseBullet prefab)
    {
        int id = 0;
        string prefabName = prefab.name;
        if (bulletIdByNames.ContainsKey(prefabName))
        {
            id = bulletIdByNames[prefabName];
        }
        else
        {
            idBullet++;
            id = idBullet;
            bulletIdByNames[prefabName] = idBullet;
        }

        if (!poolBullets.ContainsKey(id))
        {
            poolBullets[id] = new ObjectPooling<BaseBullet>();
        }

        BaseBullet bullet = poolBullets[id].New();
        if (bullet == null)
            bullet = Instantiate(prefab);

        bullet.poolingId = id;
        return bullet;
    }

    // Trả đạn về pool theo poolingId đã gán khi lấy ra.
    public void StoreBullet(BaseBullet bullet)
    {
        int id = bullet.poolingId;

        if (poolBullets.ContainsKey(id))
        {
            if (poolBullets[id].Contains(bullet) == false)
                poolBullets[id].Store(bullet);

            return;
        }

        DebugCustom.LogError("Bullet type not found=" + id);
    }
    #endregion
}
