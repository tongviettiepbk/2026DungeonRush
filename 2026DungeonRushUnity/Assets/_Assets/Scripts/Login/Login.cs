using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Màn loading: nạp save người chơi (static data đã nạp ở Root), chạy thanh % rồi TỰ vào MainGame
// khi xong (không còn nút "Tap To Play").
//
// Bản rút gọn so với StickIdle: bỏ RemoteConfig / server time / UIManager.
public class Login : MonoBehaviour
{
    // Chờ giờ server tối đa ngần này giây rồi vào game (quá thì dùng giờ máy).
    private const float SERVER_TIME_TIMEOUT = 3f;

    // Mốc % thanh loading sau từng bước; phần còn lại là nạp scene MainGame.
    private const float PROGRESS_USER_LOAD = 0.3f;
    private const float PROGRESS_ACCOUNT = 0.6f;
    private const float PROGRESS_SERVER_TIME = 0.8f;

    // Tốc độ thanh chạy tối đa (phần/giây) để thanh trôi mượt, không nhảy cóc.
    private const float FILL_SPEED = 1.2f;

    // Ảnh fill 9-slice neo mép trái; kéo dài chiều rộng để đầu bo tròn không bị cắt.
    public RectTransform rtProgressFill;
    public TMP_Text txProgress;
    public Text txVersion;

    private float targetProgress;
    private float shownProgress;
    private float fillFullWidth;

    private void Awake()
    {
        GameData.Init();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void Start()
    {
        DebugCustom.LogFormat("<color=yellow>DeviceId={0}</color>", SystemInfo.deviceUniqueIdentifier);

        if (txVersion != null)
        {
            txVersion.text = "v" + Application.version;
        }

        if (rtProgressFill != null)
        {
            fillFullWidth = rtProgressFill.sizeDelta.x;
        }

        ShowProgress(0f);
        StartCoroutine(LoadRoutine());
    }

    private void Update()
    {
        if (shownProgress < targetProgress)
        {
            ShowProgress(Mathf.MoveTowards(shownProgress, targetProgress, FILL_SPEED * Time.unscaledDeltaTime));
        }
    }

    private IEnumerator LoadRoutine()
    {
        // Static data đã nạp ở Root; ở đây chỉ nạp save người chơi.
        GameData.userData.Load();
        Analytics.MarkUserLoaded();
        Analytics.BootStep("user_load");
        targetProgress = PROGRESS_USER_LOAD;
        yield return null; // nhường 1 frame để UI kịp vẽ trạng thái loading

        // Đăng nhập (lần đầu: tự tạo tài khoản khách) + tài khoản email thì đối chiếu save cloud.
        yield return AccountController.Instance.BootRoutine();

        FirebaseServices.SetUserId(FirebaseManager.Instance.Uid);
        AppsflyerHelper.SetCustomerUserId(FirebaseManager.Instance.Uid);
        Analytics.BootStep("account");
        targetProgress = PROGRESS_ACCOUNT;

        // Giờ server cho mốc reset key Dungeon / vé Events.
        yield return ServerTime.SyncRoutine(SERVER_TIME_TIMEOUT);
        Analytics.BootStep("server_time");
        targetProgress = PROGRESS_SERVER_TIME;

        Analytics.LogBootComplete();
        yield return EnterGameRoutine();
    }

    // Nạp ngầm MainGame, thanh chạy tới 100% rồi mới chuyển scene.
    private IEnumerator EnterGameRoutine()
    {
        // Giữ tên step cũ để query 06_boot_drop vẫn chạy; giờ mọi người đều tự vào.
        Analytics.BootStep("tap_to_play");
        GameData.Save(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(StaticValue.SCENE_MAINGAME);
        op.allowSceneActivation = false;

        // Unity dừng progress ở 0.9 khi allowSceneActivation = false.
        while (op.progress < 0.9f)
        {
            targetProgress = Mathf.Lerp(PROGRESS_SERVER_TIME, 1f, op.progress / 0.9f);
            yield return null;
        }

        targetProgress = 1f;
        while (shownProgress < 1f)
        {
            yield return null;
        }

        op.allowSceneActivation = true;
    }

    private void ShowProgress(float value)
    {
        shownProgress = value;

        if (rtProgressFill != null)
        {
            // Ngắn nhất = chiều cao thanh để 2 đầu bo vẫn tròn.
            float minWidth = rtProgressFill.sizeDelta.y;
            rtProgressFill.gameObject.SetActive(value > 0f);
            rtProgressFill.sizeDelta = new Vector2(Mathf.Lerp(minWidth, fillFullWidth, value), rtProgressFill.sizeDelta.y);
        }

        if (txProgress != null)
        {
            txProgress.text = Mathf.FloorToInt(value * 100f) + "%";
        }
    }
}
