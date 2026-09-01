using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FxController : Singleton<FxController>
{
    public CombatText prefabTextDamage;
    public BaseFx fxEnemyDie;
    public BaseFx fxBossDie;
    public BaseFx fxExplodeBig;
    public BaseFx fxExplodeSmall;

    [Header("Buffs")]
    public BaseFx fxHeal;

    public BaseFx fxShieldAngelProtection;
    public BaseFx fxShieldDante;
    public BaseFx fxShieldSelena;

    public BaseFx fxBuffAttack;
    public BaseFx fxBuffAttackSpeed;
    public BaseFx fxBuffCrit;
    public BaseFx fxBuffDefense;

    [Header("Crowd Control")]
    public BaseFx fxStun;
    public BaseFx fxParalyze;
    public BaseFx fxRooted;
    public BaseFx fxCharm;
    public BaseFx fxSilent;
    public BaseFx fxDisarm;
    public BaseFx fxHex;
    public BaseFx fxFear;

    private Dictionary<string, int> fxIdByNames = new Dictionary<string, int>();
    private int idFx;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // FxController là Singleton tạo runtime (không có scene gán prefab) -> tự nạp prefab
        // text damage từ Resources nếu chưa gán trên Inspector.
        if (prefabTextDamage == null)
        {
            prefabTextDamage = Resources.Load<CombatText>("Prefabs/combat-text");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
    }

    #region Fx
    // Adapt cho DungeonRush: dùng EnemyUnit thay cho BaseEnemy của StickIdle.
    public BaseFx ShowFxDie(EnemyUnit enemy)
    {
        BaseFx fx = enemy.isBoss ? fxBossDie : fxEnemyDie;
        return SpawnFx(fx, enemy.transform.position);
    }

    public void ShowFxSpawnEnemy(Vector3 position, Action callback = null, float scale = 1f)
    {
        //BaseFx fx = SpawnFx(fxSpawnUnit, position, scale);
        //if (fx != null)
        //{
        //    float delay = fx.lifeTime - 1f;
        //    this.StartDelayAction(delay, () =>
        //    {
        //        if (callback != null)
        //            callback();
        //    });
        //}
        //else
        //{
        //    if (callback != null)
        //        callback();
        //}
    }

    public BaseFx SpawnFx(BaseFx fxPrefab, Vector3 position, float scale = 1f)
    {
        if (fxPrefab == null)
        {
            return null;
        }

        int id = 0;

        string prefabName = fxPrefab.name;

        if (fxIdByNames.ContainsKey(prefabName))
        {
            id = fxIdByNames[prefabName];
        }
        else
        {
            idFx++;
            id = idFx;
            fxPrefab.idFx = id;
            fxIdByNames[prefabName] = idFx;
        }

        if (id == 0)
        {
            DebugCustom.Log("[SpawnFx] NotFound");
            return null;
        }

        int currentActiveFx = PoolingController.Instance.GetActiveInstances(id);
        if (currentActiveFx > 20)
        {
            //DebugCustom.LogError("[SpawnFx] Too many active in scene=" + prefabName);
            return null;
        }

        BaseFx fx = PoolingController.Instance.GetEffectFromPool(id);
        if (fx == null)
        {
            fx = Instantiate(fxPrefab, PoolingController.Instance.groupFx);
        }

        if (fx != null)
        {
            fx.Active(position, scale);
            fx.idFx = id;
            PoolingController.Instance.AddFxInScene(id);
        }

        return fx;
    }
    #endregion

    #region CombatText
    public void ShowDamage(Vector3 position, double damage, bool isCrit, bool isTeamA)
    {
        CombatText textDamage = GetText();
        if (textDamage != null)
        {
            textDamage.ShowDamage(position, damage, isCrit, isTeamA);
        }
    }

    public void ShowHeal(Vector3 position, double hp)
    {
        CombatText textDamage = GetText();
        if (textDamage != null)
        {
            textDamage.ShowHeal(position, hp);
        }
    }

    public void ShowStatus(TextDamageStatus type, Vector3 position)
    {
        string msg = string.Empty;

        switch (type)
        {
            case TextDamageStatus.Miss: msg = "Miss"; break;
            case TextDamageStatus.Block: msg = "Block"; break;
            case TextDamageStatus.Immune: msg = "Immune"; break;
            case TextDamageStatus.Evade: msg = "Evade"; break;
        }

        if (string.IsNullOrEmpty(msg))
        {
            return;
        }

        // TODO(follow-stick): localize khi port LocalizeManager. Tạm dùng chuỗi gốc.
        //msg = LocalizeManager.Instance.GetLocalizeText(msg);

        CombatText textDamage = GetText();
        if (textDamage != null)
        {
            textDamage.ShowStatus(position, msg);
        }
    }

    public void ShowText(Vector3 position, string msg, Color32 colorText)
    {
        CombatText textDamage = GetText();
        if (textDamage != null)
        {
            textDamage.Show(msg, position);
            textDamage.SetColor(CombatTextType.Default, colorText, Color.black);
            textDamage.anim.Play(textDamage.animStatus);
        }
    }

    private CombatText GetText()
    {
        CombatText textDamage = PoolingController.Instance.poolTextDamage?.New();

        if (textDamage == null)
        {
            // Chưa gán prefab text damage → bỏ qua hiển thị (không Instantiate(null) gây crash).
            if (prefabTextDamage == null)
            {
                return null;
            }

            textDamage = Instantiate(prefabTextDamage);
            textDamage.Initialize();
        }

        return textDamage;
    }
    #endregion
}
