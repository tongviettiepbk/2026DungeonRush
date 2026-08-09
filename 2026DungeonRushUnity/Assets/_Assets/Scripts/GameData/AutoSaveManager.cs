using System.Collections;
using UnityEngine;

// Tự động lưu save mỗi 60s (chỉ ghi module nào isDataChanged, xem UserData.Save).
// Sống xuyên suốt qua các scene (tạo ở Root, giống EventDispatcher/AudioManager).
public class AutoSaveManager : Singleton<AutoSaveManager>
{
    private const float SAVE_INTERVAL = 60f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(SAVE_INTERVAL);

        while (true)
        {
            yield return wait;
            GameData.Save();
        }
    }

    // Phòng app bị đóng/pause trước khi tới mốc 60s.
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            GameData.Save(true);
        }
    }

    private void OnApplicationQuit()
    {
        GameData.Save(true);
    }
}
