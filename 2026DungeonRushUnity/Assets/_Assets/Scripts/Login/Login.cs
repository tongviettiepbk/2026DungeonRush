using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Màn loading: nạp save người chơi (static data đã nạp ở Root) rồi hiện nút "Tap To Play"
// để vào MainGame.
//
// Bản rút gọn so với StickIdle: bỏ RemoteConfig / server time / UIManager.
// Khi có phần UI/GameConfig sẽ thay text/version bằng UIManager + GameConfig.Instance.
public class Login : MonoBehaviour
{
    // Chờ giờ server tối đa ngần này giây rồi vào game (quá thì dùng giờ máy).
    private const float SERVER_TIME_TIMEOUT = 3f;

    public Button btTapToPlay;
    public Text txLoading;
    public Text txVersion;

    private bool isGoLobby;

    private void Awake()
    {
        GameData.Init();

        if (btTapToPlay != null)
        {
            btTapToPlay.onClick.AddListener(ClickBtTapToPlay);
        }
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

        if (btTapToPlay != null)
        {
            btTapToPlay.gameObject.SetActive(false);
        }

        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        SetLoading("Loading game data...");

        // Static data đã nạp ở Root; ở đây chỉ nạp save người chơi.
        GameData.userData.Load();
        Analytics.MarkUserLoaded();
        Analytics.BootStep("user_load");
        yield return null; // nhường 1 frame để UI kịp vẽ trạng thái loading

        // Đăng nhập (lần đầu: tự tạo tài khoản khách) + tài khoản email thì đối chiếu save cloud.
        yield return AccountController.Instance.BootRoutine();

        FirebaseServices.SetUserId(FirebaseManager.Instance.Uid);
        AppsflyerHelper.SetCustomerUserId(FirebaseManager.Instance.Uid);
        Analytics.BootStep("account");

        // Giờ server cho mốc reset key Dungeon / vé Events.
        yield return ServerTime.SyncRoutine(SERVER_TIME_TIMEOUT);
        Analytics.BootStep("server_time");

        OnLoadDone();
    }

    private void OnLoadDone()
    {
        SetLoading(string.Empty);
        Analytics.LogBootComplete();

        // Người chưa qua màn nào vào thẳng game: Racing mất 32% người chỉ chơi 1 ngày ngay trong phút đầu.
        if (GameData.userData.campaign.passedStageId == 0)
        {
            ClickBtTapToPlay();
            return;
        }

        if (btTapToPlay != null)
        {
            btTapToPlay.gameObject.SetActive(true);
        }
    }

    private void SetLoading(string text)
    {
        if (txLoading == null)
        {
            return;
        }

        txLoading.text = text;
        txLoading.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    private void ClickBtTapToPlay()
    {
        if (isGoLobby)
        {
            return;
        }

        isGoLobby = true;
        Analytics.BootStep("tap_to_play");
        GameData.Save(true);
        SceneManager.LoadScene(StaticValue.SCENE_MAINGAME);
    }
}
