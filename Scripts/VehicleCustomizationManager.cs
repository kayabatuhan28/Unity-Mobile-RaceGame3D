using RacingGame;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VehicleCustomizationManager : MonoBehaviour
{
    public static VehicleCustomizationManager instance;

    [Header("--- Data System ---")]
    public System.Collections.Generic.List<GeneralData> _GeneralData;
    public List<CarCustomization> _CarCustomization;
    public List<CarsWheels> _CarsWheels;
    DataSystem _DataSystem = new();

    // kameralarin renk modu veya teker özellestirme moduna gore pozisyon degistirebilmesi icin
    bool cameraCanGoColorPosition;
    bool cameraCanGoWheelPosition;

    Vector3 RefPos;

    [SerializeField] GameObject[] panels;
    [SerializeField] Transform[] cameraPositions;
    [SerializeField] GameObject camera;

    [Header("---- Vehicle Color Settings ----")]
    [SerializeField] Color[] colors;
    [SerializeField] RawImage colorPreviewImg;
    [SerializeField] Material carMaterial;
    int selectedColorIndex;

    [Header("---- Wheel Settings ----")]
    [SerializeField] Button[] wheelsButton;
    [SerializeField] TextMeshProUGUI currentMoneyText;
    int selectedWheelIndex;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject); 
            return;
        }    
    }

    private void Start()
    {      
        LoadData();

        int savedWheelIndex = _CarCustomization[0]._Cars[0].CurrentWheelIndex;
        _CarsWheels[0]._WheelsObject[savedWheelIndex].WheelObject.SetActive(true);
        selectedWheelIndex = savedWheelIndex;

        wheelsButton[0].gameObject.SetActive(false);
        wheelsButton[1].gameObject.SetActive(false);

        Color newColor = carMaterial.GetColor("_SpecColor");
        newColor.a = 1f;
        colorPreviewImg.color = newColor;
        colorPreviewImg.SetAllDirty();

        currentMoneyText.text = _GeneralData[0].CurrentMoney.ToString();
        

    }


    void Update()
    {
        UpdateCameraPosition(); 
    }

    public void ChangeWheel(int WheelID)
    {
        _CarsWheels[0]._WheelsObject[selectedWheelIndex].WheelObject.SetActive(false);
        selectedWheelIndex = WheelID;
        _CarsWheels[0]._WheelsObject[selectedWheelIndex].WheelObject.SetActive(true);
     
        if (_CarCustomization[0]._Cars[0].CurrentWheelIndex != selectedWheelIndex)
        {        
            if (!_CarCustomization[0]._Cars[0]._WheelsType[WheelID].IsPurchased)
            {
                wheelsButton[0].gameObject.SetActive(true);
                wheelsButton[1].gameObject.SetActive(false);
                               
                if (_GeneralData[0].CurrentMoney >= _CarCustomization[0]._Cars[0]._WheelsType[WheelID].WheelTypeCost)
                {
                    wheelsButton[0].interactable = true;
                }
                else
                {
                    wheelsButton[0].interactable = false;
                }
            }
            else
            {
                wheelsButton[0].gameObject.SetActive(false);
                wheelsButton[1].interactable = true;
                wheelsButton[1].gameObject.SetActive(true);
            }
        }
        else
        {
            wheelsButton[0].gameObject.SetActive(false);
            wheelsButton[1].gameObject.SetActive(false);
        }
    }

    public void WheelOperations(string OperationType)
    {
        if (OperationType == "Buy")
        {
            _GeneralData[0].CurrentMoney -= _CarCustomization[0]._Cars[0]._WheelsType[selectedWheelIndex].WheelTypeCost;
            currentMoneyText.text = _GeneralData[0].CurrentMoney.ToString();

            wheelsButton[0].gameObject.SetActive(false);
            wheelsButton[1].interactable = true;
            wheelsButton[1].gameObject.SetActive(true);

            _CarCustomization[0]._Cars[0]._WheelsType[selectedWheelIndex].IsPurchased = true;
        }
        else 
        {
            // apply iþlemleri
            _CarCustomization[0]._Cars[0].CurrentWheelIndex = selectedWheelIndex;
            wheelsButton[1].gameObject.SetActive(false); 
        }
    }

    public void ChangeColor(string Direction)
    {
        if (Direction == "Forward")
        {
            if (selectedColorIndex != colors.Length - 1)
            {
                selectedColorIndex++;
            }
            else
            {
                selectedColorIndex = 0;
            }
        }
        else
        {
            if (selectedColorIndex != 0)
            {
                selectedColorIndex--;
            }
            else
            {
                selectedColorIndex = colors.Length - 1;
            }
        }

        Color newColor = colors[selectedColorIndex];
        newColor.a = 1f;
        colorPreviewImg.color = newColor;
        colorPreviewImg.SetAllDirty(); // UI'yý zorla güncelle

        carMaterial.SetColor("_SpecColor", colors[selectedColorIndex]);
    }

    public void MenuButtons(string Operation)
    {
        switch (Operation)
        {
            case "ChangeColor":
                cameraCanGoColorPosition = true;
                panels[0].SetActive(false);
                panels[1].SetActive(true);
                break;

            case "ChangeWheel":
                cameraCanGoWheelPosition = true;
                panels[0].SetActive(false);
                panels[2].SetActive(true);
                break;

            case "Back":
                cameraCanGoColorPosition = true;
                panels[1].SetActive(false);
                panels[2].SetActive(false);
                panels[0].SetActive(true);

                _CarsWheels[0]._WheelsObject[selectedWheelIndex].WheelObject.SetActive(false);
                _CarsWheels[0]._WheelsObject[_CarCustomization[0]._Cars[0].CurrentWheelIndex].WheelObject.SetActive(true);

                wheelsButton[0].gameObject.SetActive(false);
                wheelsButton[1].gameObject.SetActive(false);

                selectedWheelIndex = _CarCustomization[0]._Cars[0].CurrentWheelIndex;
                break;

            case "MainMenu":
                SaveCurrentData();
                SceneManager.LoadScene(0);
                break;
        }
    }

    void UpdateCameraPosition()
    {
        if (cameraCanGoColorPosition)
        {
            Vector3 CameraNewPosition = Vector3.SmoothDamp(camera.transform.position, cameraPositions[0].position, ref RefPos, .2f);
            Quaternion CameraNewRotation = Quaternion.Slerp(camera.transform.rotation, cameraPositions[0].rotation, 6f * Time.deltaTime);

            camera.transform.SetPositionAndRotation(CameraNewPosition, CameraNewRotation);
           
            // camera hedefe ulastiysa
            if (Vector3.Distance(camera.transform.position, cameraPositions[0].position) < 0.12f)
            {
                cameraCanGoColorPosition = false;
            }
        }

        if (cameraCanGoWheelPosition)
        {
            Vector3 CameraNewPosition = Vector3.SmoothDamp(camera.transform.position, cameraPositions[1].position, ref RefPos, .2f);
            Quaternion CameraNewRotation = Quaternion.Slerp(camera.transform.rotation, cameraPositions[1].rotation, 6f * Time.deltaTime);

            camera.transform.SetPositionAndRotation(CameraNewPosition, CameraNewRotation);

            // camera hedefe ulastiysa
            if (Vector3.Distance(camera.transform.position, cameraPositions[1].position) < 0.12f)
            {
                cameraCanGoWheelPosition = false;
            }
        }
    }

    void LoadData()
    {
        _DataSystem.LoadVehicleCustomizationSettings();
        _DataSystem.LoadGeneralData("Customization");
    }

    void SaveCurrentData()
    {
        _DataSystem.SaveVehicleCustomizationSettings();
        _DataSystem.SaveGeneralData("Customization");
    }

}
