using UnityEngine;
using UnityEngine.SceneManagement;

// Scene khởi động đầu tiên (bootstrap). Chỉ cấu hình app rồi chuyển sang Login.
// Đặt Root là scene index 0 trong Build Settings.
//
// Bản rút gọn so với StickIdle: bỏ Firebase.Init / ProtectedConst / DOTween.Init
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
    }

    private void Start()
    {
        SceneManager.LoadScene(StaticValue.SCENE_LOGIN);
    }
}
