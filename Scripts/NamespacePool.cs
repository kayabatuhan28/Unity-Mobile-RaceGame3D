using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;

namespace RacingGame
{
    [System.Serializable]
    public class Wheels
    {
        public WheelCollider WheelCollider;
        public GameObject WheelModel;
        public Vector3 RotationOffset; // Farkli arac modellerinde tekerlerin pivot rotationlari farkli oldugu icin eklendi offset icin
    }

    enum DriveType
    {
        FWD, 
        BWD, 
        AWD, 
    }

    // Data System
    [System.Serializable]
    public class SettingsData
    {
        public float GameSound;
        public float MenuVolume;
        public float SfxVolume;
        public int GraphicsPreference;
        public bool FpsPreference;
    }

    [System.Serializable]
    public class GeneralData
    {
        public int CurrentMoney;
    }

    public class DataSystem
    {
        
        
        BinaryFormatter _Bf = new();
        FileStream _File;

        
        void CreateDataFile()
        {          
            if (!File.Exists(Application.persistentDataPath + "/Settings.gd"))
            {              
                _File = File.Create(Application.persistentDataPath + "/Settings.gd");
                _Bf.Serialize(_File, MainMenuManager.Instance.SettingsData);
                _File.Close();
            }

            if (!File.Exists(Application.persistentDataPath + "/GeneralData.gd"))
            {
                _File = File.Create(Application.persistentDataPath + "/GeneralData.gd");
                _Bf.Serialize(_File, MainMenuManager.Instance.GeneralData);
                _File.Close();
            }

            
            if (!File.Exists(Application.persistentDataPath + "/VehicleCustomization.gd"))
            {
                _File = File.Create(Application.persistentDataPath + "/VehicleCustomization.gd");
                _Bf.Serialize(_File, MainMenuManager.Instance._CarCustomization);
                _File.Close();
            }
            
        }
        public void CheckFile()
        {
            // Oyuncu oyunu ilk kez açýyorsa
            if (!PlayerPrefs.HasKey("IsFirstSetup"))
            {
                CreateDataFile();
                PlayerPrefs.SetInt("IsFirstSetup", 1);
            }
        }

        public void SaveSettings()
        {
            if (File.Exists(Application.persistentDataPath + "/Settings.gd"))
            {
                _File = File.OpenWrite(Application.persistentDataPath + "/Settings.gd");
                _Bf.Serialize(_File, MainMenuManager.Instance.SettingsData);
                _File.Close();
            }
        }
        public async void LoadSettings()
        {
            if (File.Exists(Application.persistentDataPath + "/Settings.gd"))
            {
                _File = File.Open(Application.persistentDataPath + "/Settings.gd", FileMode.Open);
                MainMenuManager.Instance.SettingsData = (List<SettingsData>)_Bf.Deserialize(_File);
                _File.Close();
            }
            await Task.Delay(2000);
        }

        public void SaveGeneralData(string FromWhere = "Map")
        {
            if (File.Exists(Application.persistentDataPath + "/GeneralData.gd"))
            {
                _File = File.OpenWrite(Application.persistentDataPath + "/GeneralData.gd");

                // Yaris maplerindeki gameManagerdaki data saveleri icin Map kullanilmakta, Ozellestirme ekrani kismi icinse else blogu
                if (FromWhere == "Map")
                {
                    _Bf.Serialize(_File, GameManager.instance._GeneralData);
                }
                else
                {
                    _Bf.Serialize(_File, VehicleCustomizationManager.instance._GeneralData);
                }
                  
                _File.Close();
            }
        }
        public async void LoadGeneralData(string FromWhere = "Map")
        {
            if (File.Exists(Application.persistentDataPath + "/GeneralData.gd"))
            {
                _File = File.Open(Application.persistentDataPath + "/GeneralData.gd", FileMode.Open);

                // Yaris maplerindeki gameManagerdaki datalari yuklemek icin Map kullanilmakta, Ozellestirme ekrani kismi icinse else blogu
                if (FromWhere == "Map")
                {
                    GameManager.instance._GeneralData = (List<GeneralData>)_Bf.Deserialize(_File);
                }
                else
                {
                    VehicleCustomizationManager.instance._GeneralData = (List<GeneralData>)_Bf.Deserialize(_File);
                }

                _File.Close();
            }
            await Task.Delay(2000);
        }

        public void SaveVehicleCustomizationSettings()
        {
            if (File.Exists(Application.persistentDataPath + "/VehicleCustomization.gd"))
            {
                _File = File.OpenWrite(Application.persistentDataPath + "/VehicleCustomization.gd");
                _Bf.Serialize(_File, VehicleCustomizationManager.instance._CarCustomization);
                _File.Close();
            }
        }
        public async void LoadVehicleCustomizationSettings()
        {
            if (File.Exists(Application.persistentDataPath + "/VehicleCustomization.gd"))
            {
                _File = File.Open(Application.persistentDataPath + "/VehicleCustomization.gd", FileMode.Open);
                VehicleCustomizationManager.instance._CarCustomization = (List<CarCustomization>)_Bf.Deserialize(_File);
                _File.Close();
            }
            await Task.Delay(2000);
        }

       
    }

    // --------------- CAR CUSTOMIZATION -----------------
    [System.Serializable]
    public class Cars
    {
        public int CarID;
        public int CurrentWheelIndex;
        public List<WheelsType> _WheelsType;
    }

    [System.Serializable]
    public class WheelsType
    {
        public int WheelTypeID;
        public int WheelTypeCost;
        public bool IsPurchased;
    }

    [System.Serializable]
    public class CarCustomization
    {
        public List<Cars> _Cars;
    }

    [System.Serializable]
    public class CarsWheels
    {
        public List<WheelsObject> _WheelsObject;
    }

    [System.Serializable]
    public class WheelsObject
    {
        public GameObject WheelObject;
    }
    // --------------- CAR CUSTOMIZATION -----------------

    enum MapsMode
    {
        Race,
        Checkpoint          
    }

}
