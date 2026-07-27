using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Port từ StickIdle. Đã lược bớt các region Bullets/Enemies/GoldDrop vì DungeonRush
// chưa có BaseBullet/BaseEnemy/GoldDrop/PrefabUtils; sẽ thêm lại khi các hệ thống đó được port.
public class PoolingController : Singleton<PoolingController>
{
    [Header("Groups")]
    public Transform groupFx;
    public Transform groupTextDamage;

    public ObjectPooling<CombatText> poolTextDamage = new ObjectPooling<CombatText>();

    public Dictionary<int, ObjectPooling<BaseFx>> poolFx = new Dictionary<int, ObjectPooling<BaseFx>>();
    public Dictionary<int, int> activeFxInstances = new Dictionary<int, int>();

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
}
