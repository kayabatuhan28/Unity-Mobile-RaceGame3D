using UnityEngine;
using UnityEngine.SceneManagement;
using RacingGame;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{

    public static MainMenuManager Instance;

    public List<SettingsData> SettingsData;
    public List<GeneralData> GeneralData;
    public List<CarCustomization> _CarCustomization;
    DataSystem _DataSystem = new();

    // -- Settings
    [SerializeField] GameObject SettingsPanel;
    [SerializeField] Slider GameSoundSlider;
    [SerializeField] Slider MenuVolumeSlider;
    [SerializeField] Slider SfxVolumeSlider;
    [SerializeField] TMP_Dropdown GraphicPreferencesDropDown;
    [SerializeField] Toggle FpsPreferences;

    [Header("---------------------------")]
    [SerializeField] GameObject ExitPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance);
        }

        _DataSystem.CheckFile();
    }

    private void Start()
    {       
        LoadSettingsData();
        PreferencesObject.instance.menuSound.mute = false;
    }

    public void SettingsChanged(string ChangedSetting)
    {
        switch (ChangedSetting)
        {
            case "GameSound":
                SettingsData[0].GameSound = GameSoundSlider.value;
                PreferencesObject.instance.GameSound = GameSoundSlider.value;
                break;
            case "MenuVolume":
                SettingsData[0].MenuVolume = MenuVolumeSlider.value;
                PreferencesObject.instance.MenuVolume = MenuVolumeSlider.value;
                PreferencesObject.instance.menuSound.volume = MenuVolumeSlider.value; 
                break;
            case "SfxVolume":
                SettingsData[0].SfxVolume = SfxVolumeSlider.value;
                PreferencesObject.instance.SfxVolume = SfxVolumeSlider.value;
                break;
            case "Graphic":
                SettingsData[0].GraphicsPreference = GraphicPreferencesDropDown.value;
                PreferencesObject.instance.GraphicsPreference = GraphicPreferencesDropDown.value;
                break;
            case "FPS":
                SettingsData[0].FpsPreference = FpsPreferences.isOn;
                PreferencesObject.instance.FpsPreference = FpsPreferences.isOn;
                break;
        }
    }

    void LoadSettingsData()
    {
        _DataSystem.LoadSettings();
        GameSoundSlider.value = SettingsData[0].GameSound;
        MenuVolumeSlider.value = SettingsData[0].MenuVolume;
        SfxVolumeSlider.value = SettingsData[0].SfxVolume;
        GraphicPreferencesDropDown.value = SettingsData[0].GraphicsPreference;
        FpsPreferences.isOn = SettingsData[0].FpsPreference;

        PreferencesObject.instance.GameSound = SettingsData[0].GameSound;
        
        PreferencesObject.instance.MenuVolume = SettingsData[0].MenuVolume;
        PreferencesObject.instance.menuSound.volume = SettingsData[0].MenuVolume;
       
        PreferencesObject.instance.SfxVolume = SettingsData[0].SfxVolume;
        PreferencesObject.instance.GraphicsPreference = SettingsData[0].GraphicsPreference;
        PreferencesObject.instance.FpsPreference = SettingsData[0].FpsPreference;
    }

    public void MainMenuButtonFunctions(string buttonType)
    {
        switch (buttonType)
        {
            case "StartGame":
                SceneManager.LoadScene(1);
                break;

            case "VehicleCustomization":
                SceneManager.LoadScene(5);
                break;

            case "Settings":
                SettingsPanel.SetActive(true);              
                break;

            case "CloseSettings":
                // veri kaydetme - panel kapatma
                _DataSystem.SaveSettings();
                SettingsPanel.SetActive(false);
                break;

            case "ExitGame": 
                ExitPanel.SetActive(true);
                break;

            case "ExitGameYes": 
                Application.Quit();
                break;

            case "ExitGameNo":
                ExitPanel.SetActive(false);
                break;

        }
    }
}
