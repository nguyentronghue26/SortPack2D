using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip winMusic;
    [SerializeField] private AudioClip loseMusic;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private float musicFadeDuration = 1f;

    [Header("Item Sounds")]
    [SerializeField] private AudioClip pickUpSound;
    [SerializeField] private AudioClip dropSound;
    [SerializeField] private AudioClip invalidDropSound;

    [Header("Merge Sounds")]
    [SerializeField] private AudioClip mergeSound;
    [SerializeField] private AudioClip[] comboSounds;

    [Header("Cell Sounds")]
    [SerializeField] private AudioClip cellSpawnSound;
    [SerializeField] private AudioClip cellFlyAwaySound;

    [Header("Lock Sounds")]
    [SerializeField] private AudioClip unlockSound;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip winSoundUI;
    [SerializeField] private AudioClip loseSoundUI;
    [SerializeField] private AudioClip warningSound;

    [Header("Star / Reward Sounds")]
    [SerializeField] private AudioClip starGainSound;

    [Header("Settings")]
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private bool enableSFX = true;
    [SerializeField] private bool enableMusic = true;

    [Header("Haptic")]
    [SerializeField] private bool enableHaptic = true;
    [SerializeField] private long lightVibrationMs = 20;
    [SerializeField] private long mediumVibrationMs = 40;
    [SerializeField] private long heavyVibrationMs = 80;

    [Header("Pitch Variation")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;
    [SerializeField] private bool randomizePitch = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        LoadSettings();
    }

    void Start()
    {
        if (playMusicOnStart && backgroundMusic != null)
            PlayBackgroundMusic();
    }

    public void PlayPickUp()
    {
        PlaySFX(pickUpSound);
        VibrateLight();
    }

    public void PlayDrop()
    {
        PlaySFX(dropSound);
        VibrateLight();
    }

    public void PlayInvalidDrop()
    {
        PlaySFX(invalidDropSound);
        VibrateHeavy();
    }

    public void PlayMerge(int comboCount = 1)
    {
        if (GameUIManager.Instance != null)
            //GameUIManager.Instance.AddStar(2);

        if (comboSounds != null && comboSounds.Length > 0 && comboCount > 1)
        {
            int index = Mathf.Min(comboCount - 2, comboSounds.Length - 1);
            PlaySFX(comboSounds[index], 1f + (comboCount - 1) * 0.1f);
        }
        else
        {
            PlaySFX(mergeSound);
        }

        if (comboCount >= 3) VibrateHeavy();
        else VibrateMedium();

        PlayStarGain();
    }

    public void PlayCellSpawn() => PlaySFX(cellSpawnSound);
    public void PlayCellFlyAway() => PlaySFX(cellFlyAwaySound);
    public void PlayUnlock()
    {
        PlaySFX(unlockSound);
        VibrateMedium();
    }

    public void PlayButtonClick() => PlaySFX(buttonClickSound, 1f, false);
    public void PlayWin() => PlaySFX(winSoundUI, 1f, false);
    public void PlayLose() => PlaySFX(loseSoundUI, 1f, false);
    public void PlayWarning() => PlaySFX(warningSound);

    public void PlayStarGain()
    {
        if (starGainSound != null)
            PlaySFX(starGainSound, 1f, false);
    }

    public void PlaySFX(AudioClip clip, float pitchMultiplier = 1f, bool randomPitch = true)
    {
        if (!enableSFX || clip == null || sfxSource == null) return;

        float pitch = pitchMultiplier;

        if (randomPitch && randomizePitch)
            pitch *= Random.Range(minPitch, maxPitch);

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (!enableSFX || clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume);
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusic == null) return;
        PlayMusic(backgroundMusic, true);
    }

    public void PlayMenuMusic()
    {
        if (menuMusic != null) PlayMusic(menuMusic, true);
        else PlayBackgroundMusic();
    }

    public void PlayWinMusic()
    {
        if (winMusic != null) PlayMusic(winMusic, false);
    }

    public void PlayLoseMusic()
    {
        if (loseMusic != null) PlayMusic(loseMusic, false);
    }

    public void PlayMusic(AudioClip music, bool loop = true)
    {
        if (musicSource == null || music == null) return;
        StartCoroutine(FadeAndPlayMusic(music, loop));
    }

    private System.Collections.IEnumerator FadeAndPlayMusic(AudioClip newMusic, bool loop)
    {
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < musicFadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (musicFadeDuration / 2f));
                yield return null;
            }

            musicSource.Stop();
        }

        musicSource.clip = newMusic;
        musicSource.loop = loop;
        musicSource.volume = 0f;

        if (enableMusic)
        {
            musicSource.Play();
            float elapsed = 0f;

            while (elapsed < musicFadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / (musicFadeDuration / 2f));
                yield return null;
            }

            musicSource.volume = musicVolume;
        }
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            StartCoroutine(FadeOutMusic());
    }

    private System.Collections.IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume;
    }

    public void PauseMusic(bool pause)
    {
        if (musicSource == null) return;
        if (pause) musicSource.Pause();
        else musicSource.UnPause();
    }

    public void DuckMusic(bool duck, float duckVolume = 0.3f)
    {
        if (musicSource == null) return;
        float targetVolume = duck ? musicVolume * duckVolume : musicVolume;
        StartCoroutine(FadeMusicVolume(targetVolume, 0.5f));
    }

    private System.Collections.IEnumerator FadeMusicVolume(float targetVolume, float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public enum HapticType { Light, Medium, Heavy }

    public void TriggerHaptic(HapticType type)
    {
        if (!enableHaptic) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidVibrate(type);
#elif UNITY_IOS && !UNITY_EDITOR
        IOSVibrate(type);
#endif
    }

    public void VibrateLight() => TriggerHaptic(HapticType.Light);
    public void VibrateMedium() => TriggerHaptic(HapticType.Medium);
    public void VibrateHeavy() => TriggerHaptic(HapticType.Heavy);

#if UNITY_ANDROID
    private void AndroidVibrate(HapticType type)
    {
        long duration = type switch
        {
            HapticType.Light => lightVibrationMs,
            HapticType.Medium => mediumVibrationMs,
            HapticType.Heavy => heavyVibrationMs,
            _ => mediumVibrationMs
        };

        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (GetAndroidSDKLevel() >= 26)
                {
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                            "createOneShot", duration, GetVibrationAmplitude(type));
                        vibrator.Call("vibrate", vibrationEffect);
                    }
                }
                else
                {
                    vibrator.Call("vibrate", duration);
                }
            }
        }
        catch
        {
            Handheld.Vibrate();
        }
    }

    private int GetAndroidSDKLevel()
    {
        using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
            return buildVersion.GetStatic<int>("SDK_INT");
    }

    private int GetVibrationAmplitude(HapticType type)
    {
        return type switch
        {
            HapticType.Light => 50,
            HapticType.Medium => 128,
            HapticType.Heavy => 255,
            _ => 128
        };
    }
#endif

#if UNITY_IOS
    private void IOSVibrate(HapticType type)
    {
        Handheld.Vibrate();
    }
#endif

    public void SetHapticEnabled(bool enabled)
    {
        enableHaptic = enabled;
        PlayerPrefs.SetInt("HapticEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsHapticEnabled() => enableHaptic;

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SaveSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null) musicSource.volume = musicVolume;
        SaveSettings();
    }

    public void SetSFXEnabled(bool enabled)
    {
        enableSFX = enabled;
        SaveSettings();
    }

    public void SetMusicEnabled(bool enabled)
    {
        enableMusic = enabled;
        if (musicSource != null)
        {
            if (enabled) musicSource.UnPause();
            else musicSource.Pause();
        }
        SaveSettings();
    }

    public float GetSFXVolume() => sfxVolume;
    public float GetMusicVolume() => musicVolume;
    public bool IsSFXEnabled() => enableSFX;
    public bool IsMusicEnabled() => enableMusic;

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetInt("SFXEnabled", enableSFX ? 1 : 0);
        PlayerPrefs.SetInt("MusicEnabled", enableMusic ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        enableSFX = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
        enableMusic = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;

        if (musicSource != null)
            musicSource.volume = musicVolume;
    }
}
