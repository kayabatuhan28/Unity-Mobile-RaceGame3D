using RacingGame;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Modification : MonoBehaviour
{

    // Modifiye edilebilir arac icin veri sisteminden veriyi okuyup arabayla ilgili butun degerleri alip islem yapar.
    public GameObject[] _WheelsObject;
    public List<CarCustomization> _CarCustomization;
    BinaryFormatter _Bf = new();
    FileStream _File;

    void Start()
    {
        GetVehicleCustomizationDataFromTxt();
        SetNewCustomWheel();
    }

    void GetVehicleCustomizationDataFromTxt()
    {
        if (File.Exists(Application.persistentDataPath + "/VehicleCustomization.gd"))
        {
            _File = File.Open(Application.persistentDataPath + "/VehicleCustomization.gd", FileMode.Open);
            _CarCustomization = (List<CarCustomization>)_Bf.Deserialize(_File);
            _File.Close();
        }
    }

    void SetNewCustomWheel()
    {
        int SavedWheelIndex = _CarCustomization[0]._Cars[0].CurrentWheelIndex;
        _WheelsObject[SavedWheelIndex].SetActive(true);

        // Araba secme leveli degilse
        if (SceneManager.GetActiveScene().buildIndex != 1)
        {
            for (int i = 0; i < _WheelsObject[SavedWheelIndex].transform.childCount; i++)
            {
                GetComponent<CarControl>().Wheels[i].WheelModel = _WheelsObject[SavedWheelIndex].transform.GetChild(i).gameObject;
            }
        }

        
    }

    
   
}
