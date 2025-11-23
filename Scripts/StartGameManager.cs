using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGameManager : MonoBehaviour
{

    [SerializeField] GameObject[] cars;
    int selectedCarIndex;

    [SerializeField] Transform CarSpawnLocation;

    [SerializeField] GameObject[] Panels;

    [SerializeField] Slider LoadingSceneSlider;
    [SerializeField] TextMeshProUGUI LoadingRateText;

    [SerializeField] GameObject[] raceMaps;
    [SerializeField] GameObject[] checkpointMaps;

    

    public void SelectCar(string Direction)
    {
        cars[selectedCarIndex].SetActive(false);

        if (Direction == "Forward")
        {
            if (selectedCarIndex != cars.Length - 1)
            {
                selectedCarIndex++;
            }
            else
            {
                selectedCarIndex = 0;
            }
        }
        else
        {
            if (selectedCarIndex != 0)
            {
                selectedCarIndex--;
            }
            else
            {
                selectedCarIndex = cars.Length - 1;
            }
        }

        cars[selectedCarIndex].transform.position = CarSpawnLocation.transform.position;
        cars[selectedCarIndex].SetActive(true);
    }

    // select car ve main menu butonlarda acilan panellerdeki button islemleri
    public void ButtonFunctions(string buttonType)
    {
        switch (buttonType)
        {
            case "SelectCar":
                PreferencesObject.instance.SelectedCarIndex = selectedCarIndex;
                Panels[0].SetActive(true);              
                break;

            case "RaceMode":
                PreferencesObject.instance.SelectedRaceModeIndex = 0;
                Panels[0].SetActive(false);
                Panels[1].SetActive(true);

                // Onceden acilmis checkpoint haritalari varsa onlari kapatir.
                foreach (var item in checkpointMaps)
                {
                    item.SetActive(false);
                }
              
                foreach (var item in raceMaps)
                {
                    item.SetActive(true);
                }
                break;

            case "CheckPointMode":
                PreferencesObject.instance.SelectedRaceModeIndex = 1;
                Panels[0].SetActive(false);
                Panels[1].SetActive(true);

                // Onceden acilmis yaris haritalari varsa onlari kapatir.
                foreach (var item in raceMaps)
                {
                    item.SetActive(false);
                }
              
                foreach (var item in checkpointMaps)
                {
                    item.SetActive(true);
                }
                break;

            case "Map1Race":              
                Panels[1].SetActive(false);
                StartCoroutine(LoadingScenePanel(2));
                break;

            case "Map1Checkpoint":               
                Panels[1].SetActive(false);
                StartCoroutine(LoadingScenePanel(3));
                break;
            case "Map2Race":
                Panels[1].SetActive(false);
                StartCoroutine(LoadingScenePanel(4));
                break;

            case "ReturnToSelection":
                foreach(var item in Panels)
                {
                    item.SetActive(false);
                }
                break;

            case "ReturnMainMenu":
                PreferencesObject.instance.SelectedCarIndex = 0;              
                PreferencesObject.instance.SelectedRaceModeIndex = 0;
                SceneManager.LoadScene(0);
                break;

        }
    }

    IEnumerator LoadingScenePanel(int MapId)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(MapId);
        Panels[2].SetActive(true);

        while (!op.isDone)
        {          
            LoadingRateText.text = (op.progress * 100 + "%").ToString();
            LoadingSceneSlider.value = op.progress;

            yield return null;
        }

    }


}
