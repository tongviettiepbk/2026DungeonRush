using UnityEngine;
using System.Collections;

// Port từ StickIdle (GamePlay/HealthBar.cs). Thanh máu 2 lớp: 'hp' (giá trị hiện tại) +
// 'visual' (lớp trắng trượt theo sau tạo hiệu ứng mất máu). Sprite dùng DrawMode Sliced nên
// đổi size.x = co giãn thanh máu theo phần trăm.
public class HealthBar : MonoBehaviour
{
    public SpriteRenderer hp;
    public SpriteRenderer visual;
    public float speedVisual = 0.3f;
    public Color32 colorTeamA;
    public Color32 colorTeamB;

    private BaseUnit unit;
    private float maxSizeBar;
    private IEnumerator coroutineVisual;
    private const float VISUAL_HP_TIME_OUT_ANIMATE = 0.5f;
    private static WaitForEndOfFrame waitEndFrame = new WaitForEndOfFrame();
    private bool isHpBarActiveBeforePause;
    private bool flagAnimate;

    private void OnDisable()
    {
        StopAnimate();
    }

    private void Update()
    {
        if (flagAnimate)
        {
            float visualHpValue = visual.size.x;
            float mainHpValue = hp.size.x;
            visualHpValue = Mathf.MoveTowards(visualHpValue, mainHpValue, Time.deltaTime * speedVisual);

            Vector2 v = visual.size;
            v.x = visualHpValue;
            visual.size = v;

            if (visualHpValue <= mainHpValue)
            {
                flagAnimate = false;
            }
        }
    }

    public void Init(BaseUnit unit)
    {
        this.unit = unit;
        maxSizeBar = hp.size.x;
        gameObject.SetActive(false);
    }

    public void Reset()
    {
        hp.color = unit.isTeamA ? colorTeamA : colorTeamB;
        SetMainHp(1f);
        SetVisualHp(1f);
    }

    public void Pause()
    {
        isHpBarActiveBeforePause = gameObject.activeInHierarchy;
        gameObject.SetActive(false);
    }

    public void Resume()
    {
        if (unit.isDead == false && isHpBarActiveBeforePause)
        {
            gameObject.SetActive(true);
        }
    }

    public void Deactive()
    {
        StopAllCoroutines();
        StopAnimate();
        gameObject.SetActive(false);
    }

    public void UpdateHealthBar(float percent)
    {
        if (percent <= 0f || unit.crowdController.isKnockedDown || unit.crowdController.isKnockedUp || unit.crowdController.isHex)
        {
            gameObject.SetActive(false);
            return;
        }

        SetMainHp(percent);

        float visualHpValue = visual.size.x;
        float mainHpValue = hp.size.x;
        float percentLost = Mathf.Clamp01((visualHpValue - mainHpValue) / maxSizeBar);
        if (percentLost >= 0.15f)
        {
            flagAnimate = true;
        }
        else
        {
            flagAnimate = false;
            Vector2 v = visual.size;
            v.x = hp.size.x;
            visual.size = v;
        }

        gameObject.SetActive(true);
    }

    private void SetMainHp(float percent)
    {
        float newSize = maxSizeBar * percent;
        Vector2 v = hp.size;
        v.x = newSize;
        hp.size = v;
    }

    private void SetVisualHp(float percent)
    {
        float newSize = maxSizeBar * percent;
        Vector2 v = visual.size;
        v.x = newSize;
        visual.size = v;
    }

    private void StopAnimate()
    {
        if (coroutineVisual != null)
        {
            StopCoroutine(coroutineVisual);
        }

        coroutineVisual = null;
        SetVisualHp(unit.GetHpPercent());
    }

    private void StartAnimate(float startHpPercent, float endHpPercent)
    {
        if (coroutineVisual == null)
        {
            coroutineVisual = RoutineVisualHp(startHpPercent, endHpPercent);
            StartCoroutine(coroutineVisual);
        }
    }

    private IEnumerator RoutineVisualHp(float startHpPercent, float endHpPercent)
    {
        float timer = 0;
        float valueToChange = endHpPercent - startHpPercent;

        while (timer < VISUAL_HP_TIME_OUT_ANIMATE)
        {
            float percent = Mathf.Clamp01(timer / VISUAL_HP_TIME_OUT_ANIMATE);
            float tmpValue = startHpPercent + valueToChange * percent;
            yield return waitEndFrame;
            timer += Time.deltaTime;
            SetVisualHp(tmpValue);
        }

        SetVisualHp(endHpPercent);
    }
}
