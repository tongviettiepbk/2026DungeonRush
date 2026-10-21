using System;
using System.Collections.Generic;
using UnityEngine;

// AudioConfig gốc (ScriptableObject "Paper War/Audio Config", số liệu đọc từ apk — v41 = v59).
// Đặt ở Resources/Scriptable Objects/Audio/AudioConfig.
//
// Âm của vũ khí (WeaponData.HitAudios/ShootAudios/ProjectileHitAudios gốc) và của pet (CompanionData
// .ShootSound/ProjectileHitSound/ProjectileImpactSound/EffectActiveSound gốc) gom vào đây, tra theo
// assetName — layer data WeaponData/CompanionData của project chỉ giữ stat cân bằng.
[CreateAssetMenu(fileName = "AudioConfig", menuName = "DungOnRush/Audio Config")]
public class AudioConfig : ScriptableObject
{
    public const string PATH = "Scriptable Objects/Audio/AudioConfig";

    private static AudioConfig instance;

    public static AudioConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<AudioConfig>(PATH);
            }
            return instance;
        }
    }

    public AudioClip ButtonClickSound;
    [Range(0f, 1f)] public float ButtonClickVolume = 1f;
    public AudioClip ItemEquipSound;
    [Range(0f, 1f)] public float ItemEquipVolume = 1f;
    public AudioClip ItemSellSound;
    [Range(0f, 1f)] public float ItemSellVolume = 1f;
    public AudioClip BoxPunchSound;
    [Range(0f, 1f)] public float BoxPunchVolume = 1f;
    public AudioClip BoxOpenSound;
    [Range(0f, 1f)] public float BoxOpenVolume = 1f;
    public AudioClip LevelUpSound;
    [Range(0f, 2f)] public float LevelUpVolume = 1f;
    public AudioClip DoorOpenSound;
    [Range(0f, 1f)] public float DoorOpenVolume = 1f;
    public AudioClip LootCollectSound;
    [Range(0f, 1f)] public float LootCollectVolume = 1f;
    public List<AudioClip> UnarmedHitAudios = new List<AudioClip>();
    [Range(0f, 1f)] public float UnarmedHitAudioVolume = 1f;
    public AudioClip Music;
    [Range(0f, 1f)] public float MusicVolume = 1f;

    [Header("Vũ khí (theo WeaponData.assetName)")]
    public List<WeaponAudio> Weapons = new List<WeaponAudio>();

    [Header("Pet (theo CompanionData.assetName)")]
    public List<CompanionAudio> Companions = new List<CompanionAudio>();

    public WeaponAudio GetWeapon(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            return null;
        }
        for (int i = 0; i < Weapons.Count; i++)
        {
            if (Weapons[i].AssetName == assetName)
            {
                return Weapons[i];
            }
        }
        return null;
    }

    public CompanionAudio GetCompanion(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            return null;
        }
        for (int i = 0; i < Companions.Count; i++)
        {
            if (Companions[i].AssetName == assetName)
            {
                return Companions[i];
            }
        }
        return null;
    }
}

[Serializable]
public class WeaponAudio
{
    public string AssetName;
    public List<AudioClip> HitAudios = new List<AudioClip>();
    public float HitAudioVolume = 1f;
    public List<AudioClip> ShootAudios = new List<AudioClip>();
    public float ShootAudioVolume = 1f;
    public List<AudioClip> ProjectileHitAudios = new List<AudioClip>();
    public float ProjectileHitAudioVolume = 1f;
}

[Serializable]
public class CompanionAudio
{
    public string AssetName;
    public AudioClip ShootSound;
    public float ShootVolume = 1f;
    public AudioClip ProjectileHitSound;
    public float ProjectileHitVolume = 1f;
    public AudioClip ProjectileImpactSound;
    public float ProjectileImpactVolume = 1f;
    public AudioClip EffectActiveSound;
    public float EffectActiveSoundVolume = 1f;
    public bool LoopEffectActiveSound;
}
