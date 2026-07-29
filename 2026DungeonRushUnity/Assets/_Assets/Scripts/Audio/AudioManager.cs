using UnityEngine;

// Bản rút gọn so với StickIdle (Audio/AudioManager.cs ~377 dòng). Đủ bề mặt cho BaseUnit.
// TODO(follow-stick): thay bằng hệ audio đầy đủ (nhạc nền, mixer, settings) khi port.
public class AudioManager : Singleton<AudioManager>
{
    public bool isMuteSfx;

    [SerializeField] private AudioSource sfxSource;

    public void PlaySfx(AudioClip clip)
    {
        if (isMuteSfx || clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }
}
