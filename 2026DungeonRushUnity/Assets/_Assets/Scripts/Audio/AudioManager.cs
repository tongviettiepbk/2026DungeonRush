using UnityEngine;

// Bản rút gọn so với StickIdle (Audio/AudioManager.cs ~377 dòng). Đủ bề mặt cho BaseUnit.
// TODO(follow-stick): thay bằng hệ audio đầy đủ (nhạc nền, mixer, settings) khi port.
public class AudioManager : Singleton<AudioManager>
{
    // Âm lượng SFX lấy thẳng từ save (slider Sound FX ở UIProfilePopup).
    public float SfxVolume => GameData.userData.settings.soundVolume;
    public bool isMuteSfx => SfxVolume <= 0f;

    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (isMuteSfx || clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, SfxVolume);
    }
}
