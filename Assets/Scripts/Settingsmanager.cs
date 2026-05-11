using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string KEY_SENSITIVITY = "sensitivity";
    private const string KEY_MASTER_VOL  = "vol_master";
    private const string KEY_SFX_VOL     = "vol_sfx";
    private const string KEY_MUSIC_VOL   = "vol_music";

    private const float DEFAULT_SENSITIVITY = 0.10f;
    private const float DEFAULT_MASTER_VOL  = 1.00f;
    private const float DEFAULT_SFX_VOL     = 1.00f;
    private const float DEFAULT_MUSIC_VOL   = 0.80f;

    public float Sensitivity  { get; private set; }
    public float MasterVolume { get; private set; }
    public float SFXVolume    { get; private set; }
    public float MusicVolume  { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        Apply();
    }

    public void SetSensitivity(float value)
    {
        Sensitivity = Mathf.Clamp(value, 0.01f, 1f);
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, Sensitivity);
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
        AudioListener.volume = MasterVolume;
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
        ApplyToAudioSources("SFX", SFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
        ApplyToAudioSources("Music", MusicVolume);
    }

    public void ResetToDefaults()
    {
        SetSensitivity(DEFAULT_SENSITIVITY);
        SetMasterVolume(DEFAULT_MASTER_VOL);
        SetSFXVolume(DEFAULT_SFX_VOL);
        SetMusicVolume(DEFAULT_MUSIC_VOL);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        Sensitivity  = PlayerPrefs.GetFloat(KEY_SENSITIVITY, DEFAULT_SENSITIVITY);
        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL,  DEFAULT_MASTER_VOL);
        SFXVolume    = PlayerPrefs.GetFloat(KEY_SFX_VOL,     DEFAULT_SFX_VOL);
        MusicVolume  = PlayerPrefs.GetFloat(KEY_MUSIC_VOL,   DEFAULT_MUSIC_VOL);
    }

    private void Apply()
    {
        AudioListener.volume = MasterVolume;
    }

    // Finds all AudioSources tagged with the given tag and sets their volume
    private void ApplyToAudioSources(string tag, float volume)
    {
        foreach (var go in GameObject.FindGameObjectsWithTag(tag))
        {
            var src = go.GetComponent<AudioSource>();
            if (src != null) src.volume = volume;
        }
    }
}