using System.Collections.Generic;
using UnityEngine;

// Port AudioController gốc (reverse il2cpp v59 = v41):
//  • 1 AudioSource gameplay phát PlayOneShot mọi tiếng (UI + trận), volume source = slider Sound FX.
//  • 1 AudioSource nhạc: AudioConfig.Music lặp vô hạn, volume = AudioConfig.MusicVolume × slider Music.
//  • Chống trùng (xhh/djh/dji gốc): 1 clip vừa phát thì bỏ lần phát tiếp trong AudioReplayInterval giây
//    (scene gốc để 0 → chỉ chặn cùng 1 clip phát 2 lần trong cùng 1 frame).
// Số liệu clip/volume ở AudioConfig (Resources/Scriptable Objects/Audio/AudioConfig).
public class AudioManager : Singleton<AudioManager>
{
    // Âm lượng SFX lấy thẳng từ save (slider Sound FX ở UIProfilePopup).
    public float SfxVolume => GameData.userData.settings.soundVolume;
    public bool isMuteSfx => SfxVolume <= 0f;
    public float MusicSettingVolume => GameData.userData.settings.musicVolume;

    public AudioConfig Config => AudioConfig.Instance;

    public float audioReplayInterval = 0f;   // AudioController.AudioReplayInterval gốc (scene = 0)

    private AudioSource gameplaySource;      // GameplayAudioSource gốc
    private AudioSource musicSource;         // MusicAudioSource gốc
    private readonly Dictionary<int, float> playedTimes = new Dictionary<int, float>();
    private readonly List<int> expired = new List<int>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        gameplaySource = CreateSource("GameplayAudioSource");
        musicSource = CreateSource("MusicAudioSource");
    }

    private AudioSource CreateSource(string sourceName)
    {
        GameObject go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    // Update gốc: bỏ các clip đã quá AudioReplayInterval khỏi danh sách vừa phát.
    private void Update()
    {
        // Nhạc có thể bắt đầu trước khi save nạp xong → luôn bám slider Music trong save.
        if (musicSource.isPlaying && Config != null)
        {
            musicSource.volume = Config.MusicVolume * MusicSettingVolume;
        }

        if (playedTimes.Count == 0)
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        expired.Clear();
        foreach (KeyValuePair<int, float> pair in playedTimes)
        {
            if (now - pair.Value >= audioReplayInterval)
            {
                expired.Add(pair.Key);
            }
        }
        for (int i = 0; i < expired.Count; i++)
        {
            playedTimes.Remove(expired[i]);
        }
    }

    // ===== SFX =====

    // djd gốc. volumeScale = volume riêng của clip theo AudioConfig/WeaponData/CompanionData gốc.
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (isMuteSfx || clip == null || gameplaySource == null)
        {
            return;
        }

        int id = clip.GetInstanceID();
        if (playedTimes.ContainsKey(id))
        {
            return;
        }

        gameplaySource.volume = SfxVolume;
        gameplaySource.PlayOneShot(clip, volumeScale);
        playedTimes[id] = Time.realtimeSinceStartup;
    }

    // dje gốc: 1 clip ngẫu nhiên trong danh sách.
    public void PlayRandomSfx(List<AudioClip> clips, float volumeScale = 1f)
    {
        if (clips == null || clips.Count == 0)
        {
            return;
        }
        PlaySfx(clips[Random.Range(0, clips.Count)], volumeScale);
    }

    // diz gốc: kéo slider Sound FX → đổi volume source gameplay + tiếng hiệu ứng lặp của pet.
    public void SetSfxVolume(float value)
    {
        if (gameplaySource != null)
        {
            gameplaySource.volume = value;
        }
        PetUnit.RefreshEffectSfxVolume(value);
    }

    public void PlayButtonClick() { if (Config != null) PlaySfx(Config.ButtonClickSound, Config.ButtonClickVolume); }
    public void PlayItemEquip() { if (Config != null) PlaySfx(Config.ItemEquipSound, Config.ItemEquipVolume); }
    public void PlayItemSell() { if (Config != null) PlaySfx(Config.ItemSellSound, Config.ItemSellVolume); }
    public void PlayBoxPunch() { if (Config != null) PlaySfx(Config.BoxPunchSound, Config.BoxPunchVolume); }
    public void PlayBoxOpen() { if (Config != null) PlaySfx(Config.BoxOpenSound, Config.BoxOpenVolume); }
    public void PlayLevelUp() { if (Config != null) PlaySfx(Config.LevelUpSound, Config.LevelUpVolume); }
    public void PlayDoorOpen() { if (Config != null) PlaySfx(Config.DoorOpenSound, Config.DoorOpenVolume); }
    public void PlayLootCollect() { if (Config != null) PlaySfx(Config.LootCollectSound, Config.LootCollectVolume); }
    public void PlayUnarmedHit() { if (Config != null) PlayRandomSfx(Config.UnarmedHitAudios, Config.UnarmedHitAudioVolume); }

    // ===== Nhạc nền =====

    // djb gốc: phát AudioConfig.Music lặp (đang phát đúng bài thì thôi). Slider Music = 0 thì không phát.
    // Gốc đặt volume = MusicVolume (bỏ qua slider) khi bắt đầu phát; ở đây nhân thêm slider cho khớp diy.
    public void PlayMusic()
    {
        if (Config == null || Config.Music == null || musicSource == null || MusicSettingVolume <= 0f)
        {
            return;
        }

        if (musicSource.clip == Config.Music && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = Config.Music;
        musicSource.volume = Config.MusicVolume * MusicSettingVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    // diy gốc: kéo slider Music → volume = MusicVolume × value; > 0 mà chưa phát thì phát.
    public void SetMusicVolume(float value)
    {
        if (Config == null || musicSource == null)
        {
            return;
        }

        musicSource.volume = Config.MusicVolume * value;
        if (value > 0f && !musicSource.isPlaying)
        {
            PlayMusic();
        }
    }
}
