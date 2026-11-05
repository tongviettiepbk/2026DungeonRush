using UnityEngine;
using UnityEngine.SceneManagement;

// Scene khởi động đầu tiên (bootstrap). Cấu hình app, tạo các manager sống xuyên suốt
// (giống StickIdle: manager tự DontDestroyOnLoad trong Awake của chính nó), nạp static data
// rồi chuyển sang Login.
// Đặt Root là scene index 0 trong Build Settings.
//
// Bản rút gọn so với StickIdle: bỏ ProtectedConst / DOTween.Init
// (DOTween tự init khi dùng lần đầu). Thêm lại khi có các phần tương ứng.
public class Root : MonoBehaviour
{
    public bool runInBackground = true;

    private void Awake()
    {
#if UNITY_EDITOR
        Application.runInBackground = runInBackground;
#else
        Application.runInBackground = true;
#endif
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Chạm .Instance để tạo sớm và persist qua Login -> MainGame.
        _ = EventDispatcher.Instance;
        _ = AudioManager.Instance;
        _ = AutoSaveManager.Instance;
        _ = AnalyticsTracker.Instance;
        _ = LocalPushController.Instance;
    }

    private void Start()
    {
        GameData.staticData.Load();

        FirebaseServices.Init();
        AppsflyerHelper.Init();
        // Tạo lớp SDK trước khi ai chạm MediationAds/PurchaseController.Instance, không thì Singleton tự tạo lớp base.
        // IapController cần static data để dựng danh mục sản phẩm nên phải sau Load.
        new GameObject(nameof(MediationAds)).AddComponent<MaxAdController>();
        new GameObject(nameof(PurchaseController)).AddComponent<IapController>();

        SceneManager.LoadScene(StaticValue.SCENE_LOGIN);
    }
}
