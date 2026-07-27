using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Màn loading: nạp toàn bộ GameData rồi hiện nút "Tap To Play" để vào Lobby.
//
// Bản rút gọn so với StickIdle: bỏ Firebase / RemoteConfig / server time / UIManager.
// Việc nạp dữ liệu (config tĩnh + save người chơi) gói gọn trong GameData.Init().
// Khi có phần UI/GameConfig sẽ thay text/version bằng UIManager + GameConfig.Instance.
public class Login : MonoBehaviour
{
    public Button btTapToPlay;
    public Text txLoading;
    public Text txVersion;

    private bool isGoLobby;

    private void Awake()
    {
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

        // Nạp config tĩnh + save người chơi — 1 lần duy nhất cho cả phiên chơi.
        GameData.Init();
        yield return null; // nhường 1 frame để UI kịp vẽ trạng thái loading

        OnLoadDone();
    }

    private void OnLoadDone()
    {
        SetLoading(string.Empty);

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
        GameData.Save(true);
        SceneManager.LoadScene(StaticValue.SCENE_LOBBY);
    }
}
