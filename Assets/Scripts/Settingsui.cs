using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private Slider          _sensitivitySlider;
    [SerializeField] private TextMeshProUGUI _sensitivityLabel;

    [Header("Volume")]
    [SerializeField] private Slider          _masterSlider;
    [SerializeField] private TextMeshProUGUI _masterLabel;

    [SerializeField] private Slider          _sfxSlider;
    [SerializeField] private TextMeshProUGUI _sfxLabel;

    [SerializeField] private Slider          _musicSlider;
    [SerializeField] private TextMeshProUGUI _musicLabel;

    [Header("Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _resetDefaultsButton;

    void OnEnable()
    {
        RefreshUI();
        _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        _masterSlider     .onValueChanged.AddListener(OnMasterChanged);
        _sfxSlider        .onValueChanged.AddListener(OnSFXChanged);
        _musicSlider      .onValueChanged.AddListener(OnMusicChanged);
        if (_resumeButton        != null) _resumeButton.onClick.AddListener(OnResume);
        if (_resetDefaultsButton != null) _resetDefaultsButton.onClick.AddListener(OnResetDefaults);
    }

    void OnDisable()
    {
        PlayerPrefs.Save();
        _sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        _masterSlider     .onValueChanged.RemoveListener(OnMasterChanged);
        _sfxSlider        .onValueChanged.RemoveListener(OnSFXChanged);
        _musicSlider      .onValueChanged.RemoveListener(OnMusicChanged);
        if (_resumeButton        != null) _resumeButton.onClick.RemoveListener(OnResume);
        if (_resetDefaultsButton != null) _resetDefaultsButton.onClick.RemoveListener(OnResetDefaults);
    }

    private void RefreshUI()
    {
        var s = SettingsManager.Instance;
        if (s == null) return;

        _sensitivitySlider.SetValueWithoutNotify(s.Sensitivity);
        _masterSlider     .SetValueWithoutNotify(s.MasterVolume);
        _sfxSlider        .SetValueWithoutNotify(s.SFXVolume);
        _musicSlider      .SetValueWithoutNotify(s.MusicVolume);

        SetLabel(_sensitivityLabel, "SENSITIVITY", s.Sensitivity,  isSensitivity: true);
        SetLabel(_masterLabel,      "MASTER",      s.MasterVolume, isSensitivity: false);
        SetLabel(_sfxLabel,         "SFX",         s.SFXVolume,    isSensitivity: false);
        SetLabel(_musicLabel,       "MUSIC",       s.MusicVolume,  isSensitivity: false);
    }

    private void OnSensitivityChanged(float v)
    {
        SettingsManager.Instance?.SetSensitivity(v);
        SetLabel(_sensitivityLabel, "SENSITIVITY", v, isSensitivity: true);
    }

    private void OnMasterChanged(float v)
    {
        SettingsManager.Instance?.SetMasterVolume(v);
        SetLabel(_masterLabel, "MASTER", v, isSensitivity: false);
    }

    private void OnSFXChanged(float v)
    {
        SettingsManager.Instance?.SetSFXVolume(v);
        SetLabel(_sfxLabel, "SFX", v, isSensitivity: false);
    }

    private void OnMusicChanged(float v)
    {
        SettingsManager.Instance?.SetMusicVolume(v);
        SetLabel(_musicLabel, "MUSIC", v, isSensitivity: false);
    }

    private void OnResume()
    {
        gameObject.SetActive(false);
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    private void OnResetDefaults()
    {
        SettingsManager.Instance?.ResetToDefaults();
        RefreshUI();
    }

    // Produces e.g.  "SENSITIVITY   0.22"  or  "MASTER   80 %"
    private static void SetLabel(TextMeshProUGUI label, string name, float value, bool isSensitivity)
    {
        if (label == null) return;
        string val = isSensitivity
            ? value.ToString("F2")
            : $"{Mathf.RoundToInt(value * 100)} %";
        label.text = $"{name}   {val}";
    }
}