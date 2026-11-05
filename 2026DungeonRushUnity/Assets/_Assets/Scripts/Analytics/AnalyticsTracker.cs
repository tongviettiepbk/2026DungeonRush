using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Bắt mốc vòng đời cho Analytics: vào MainGame, xuống nền, thoát app, nhịp 1 giây. Tạo ở Root, sống xuyên scene.
public class AnalyticsTracker : Singleton<AnalyticsTracker>
{
    private const float TICK_SECONDS = 1f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(TickRoutine());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static bool IsMainGame => SceneManager.GetActiveScene().name == StaticValue.SCENE_MAINGAME;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == StaticValue.SCENE_MAINGAME)
        {
            Analytics.BeginSession(isResume: false);
        }
    }

    private IEnumerator TickRoutine()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(TICK_SECONDS);

        while (true)
        {
            yield return wait;
            Analytics.Tick();
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused == false)
        {
            if (IsMainGame)
            {
                Analytics.BeginSession(isResume: true);
            }

            return;
        }

        // Quảng cáo toàn màn hình và hộp thoại thanh toán làm app pause: vẫn là cùng một phiên.
        if (MediationAds.Instance.IsFullscreenShowing == false && PurchaseController.IsPurchasing == false)
        {
            Analytics.EndSession();
        }
    }

    private void OnApplicationQuit()
    {
        Analytics.EndSession();
    }
}
