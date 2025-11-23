using NUnit.Framework;
using RacingGame;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("--- General ---")]
    public static GameManager instance;
    public CarControl _CarControl;   // Active Car Data
    [SerializeField] MapsMode mapsMode = MapsMode.Race;
    public RaceManager _RaceManager;
    public CheckPointManager _CheckPointManager;
    public AudioSource GameSound;

    public TextMeshProUGUI CurrentSpeedText;
    public TextMeshProUGUI CurrentGearText;
    [SerializeField] GameObject RacePositionPanel;
    public GameObject ReturnWarningImage;
    public GameObject ExitPanel;


    [Header("--- Nitro ---")]
    public Slider NitroSlider;
    public float NitroValue = 0;
    public bool NitroStatus = true; // true ise nitro regeni aktif etmek icin kullanabiliriz.
    public Animator NitroVisualAnim;
    public AudioSource NitroSound;
    Coroutine fillUpNitroRoutine;
    public float NitroFillRate = 2f;
    [SerializeField] GameObject NitroPanel;

    [Header("--- Route System ---")]
    // Ai araclarin rotalari
    public Transform[] Route1;
    public Transform[] Route2;
    public Transform[] Route3;
    public Transform[] Route4;

    // ---------------------------
    [Header("--- Starting Game ---")]
    public GameObject[] PlayerCars;
    [SerializeField] Transform playerCarSpawnPoint;

    public GameObject[] AiCars;
    [SerializeField] Transform[] aiCarsSpawnPoints;

    [SerializeField] CinemachineCamera cinemachineCamera;
    [SerializeField] GameObject mainCameraObject;

    [Header("--- Begin Countdown ---")]
    [SerializeField] GameObject BeginCountdownPanel;
    [SerializeField] TextMeshProUGUI BeginCountdownText;
    int Countdown = 3;

    [Header("--- Data System ---")]
    public System.Collections.Generic.List<GeneralData> _GeneralData;
    DataSystem _DataSystem = new();
    [SerializeField] TextMeshProUGUI MoneyText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject); // doðru nesneyi yok eder
            return;
        }
        InitialSetup();
    }

    void Start()
    {
        if (mapsMode == MapsMode.Race)
        {
            fillUpNitroRoutine = StartCoroutine(NitroSystem());
            Debug.Log("Buraya girdi RACEEEE!");
        }
        else
        {
            // Checkpoint modunda nitro ve sira panel gerekmemekte. Genel olarak sayac 0'a ulasmadan ilgili checkpointlere ulasýp sureyi arttirmaya yonelik
            // bir oynanýs modu.
            NitroPanel.SetActive(false);
            RacePositionPanel.SetActive(false);
            Debug.Log("Buraya girdi cHECKPOÝNT!");
        }

        _DataSystem.LoadGeneralData();
        if (MoneyText != null)
        {
            MoneyText.text = _GeneralData[0].CurrentMoney.ToString();
        }    
    }


    public void NitroUsed()
    {
        fillUpNitroRoutine ??= StartCoroutine(NitroSystem());
    }

    IEnumerator NitroSystem()
    {
        NitroVisualAnim.SetBool("FillUpNitro", true);
        NitroStatus = true;
        while (true)
        {
            yield return new WaitForSeconds(.3f);

            if (NitroStatus)
            {
                NitroValue += NitroFillRate;
                NitroSlider.value = NitroValue;

                if (NitroValue >= 100)
                {
                    NitroVisualAnim.SetBool("FillUpNitro", false);
                    NitroValue = 100;
                    NitroStatus = false;
                    StopCoroutine(fillUpNitroRoutine);
                    fillUpNitroRoutine = null;
                }
            }
        }
    }

    public void CameraSwitchRequested()
    {
        _CarControl.ChangeCamera(); 
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < Route1.Length; i++)
        {
            if (i != Route1.Length - 1)
            {
                // Arac sürüs path pointleri arasinda debug cizer
                Gizmos.DrawLine(Route1[i].position, Route1[i + 1].position);

                // Arac sürüs path pointlerinin success gelme radiusunu debuglar. (hedefe ulasma testi vs. icin)
                //Gizmos.DrawWireSphere(Route1[i].position, 9);
            }
        }

        for (int i = 0; i < Route2.Length; i++)
        {
            if (i != Route2.Length - 1)
            {
                // Arac sürüs path pointleri arasinda debug cizer
                Gizmos.DrawLine(Route2[i].position, Route2[i + 1].position);
                
            }
        }

        for (int i = 0; i < Route3.Length; i++)
        {
            if (i != Route3.Length - 1)
            {
                // Arac sürüs path pointleri arasinda debug cizer
                Gizmos.DrawLine(Route3[i].position, Route3[i + 1].position);

            }
        }

        for (int i = 0; i < Route4.Length; i++)
        {
            if (i != Route4.Length - 1)
            {
                // Arac sürüs path pointleri arasinda debug cizer
                Gizmos.DrawLine(Route4[i].position, Route4[i + 1].position);

            }
        }

    }

    // bütün oyun kurulum ve ayar islemlerinin yapildigi fonksiyon
    void InitialSetup()
    {
        PreferencesObject.instance.menuSound.mute = true;

        // -------------- PLAYER ARABA ISLEMLERI -------------- 
        
        GameObject Car = Instantiate(PlayerCars[PreferencesObject.instance.SelectedCarIndex], playerCarSpawnPoint.position, Quaternion.identity);      
        _CarControl = Car.GetComponent<CarControl>();      
        Car.SetActive(true);     
        cinemachineCamera.Follow = Car.transform;    
        _CarControl.SetSfxVolume(PreferencesObject.instance.SfxVolume);
        _CarControl.cameras[0] = mainCameraObject;

        if (mapsMode != MapsMode.Race)
        {
            // Yaris modu olmayan modlarda race manager (Ailarla yarista kacinci vs.) gerekmemekte, checkpoint modunda ai araclar mevcut degil.
            Car.GetComponent<RaceStatus>().enabled = false;

            // Race modunda game start coroutine ile sayac bitince yaris baslayacak sekilde ayarlý, checkpoint modunda direk baslamasi gerekmekte.
            _RaceManager.IsGameStart = true;
        }

        if (mapsMode == MapsMode.Race)
        {
            // -------------- YAPAY ZEKA ARABA ISLEMLERI --------------
            for (int i = 0; i < 4; i++)
            {
                int CarIndex = Random.Range(0, AiCars.Length);
                GameObject aiCar = Instantiate(AiCars[CarIndex], aiCarsSpawnPoints[i].position, Quaternion.identity);

                aiCar.GetComponent<AiManager>().WhichRoute = i + 1;

                aiCar.GetComponent<AiCarControl>().SetSfxVolume(PreferencesObject.instance.SfxVolume);

                aiCar.SetActive(true);
            }
        }

        
        // -------------- TEKNIK ISLEMLER --------------
        
        if (PreferencesObject.instance.FpsPreference) // fps i 30'a kitleme secili ise
        {
            // Bu islemi yapmazsak iframeyi kitleyemeyiz.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
        }

        // 2.parametre ilgili secilen grafik ayarinda performans isteyen, iþlemesi zor olan seçenekleride dikkate almayý temsil eder.
        QualitySettings.SetQualityLevel(PreferencesObject.instance.GraphicsPreference, true);



        // -------------- GENEL SES ISLEMLERI --------------
        NitroSound.volume = PreferencesObject.instance.SfxVolume;
        GameSound.volume = PreferencesObject.instance.GameSound;

        if (mapsMode == MapsMode.Race)
        {
            // Araclarin countdown sayaci boyunca hareketsiz kalip sonradan yarisa baslamasi kismi.
            StartCoroutine(BeginCountdown());
        }     
    }

    IEnumerator BeginCountdown()
    {
        BeginCountdownPanel.SetActive(true);
        BeginCountdownText.text = Countdown.ToString();

        while (true)
        {
            yield return new WaitForSeconds(1);
            Countdown--;
            BeginCountdownText.text = Countdown.ToString();

            if (Countdown < 0)
            {
                BeginCountdownPanel.SetActive(false);
                _RaceManager.IsGameStart = true;
            }
        }
    }

    public void AddMoney(int EarnedMoney)
    {
        _GeneralData[0].CurrentMoney += EarnedMoney;
        MoneyText.text = _GeneralData[0].CurrentMoney.ToString() + " (+" + EarnedMoney + ")";

        _DataSystem.SaveGeneralData();

        
    }

    public void ReturnToMainMenu(string OperationType)
    {
        if (OperationType == "FirstTimePanelOpen")
        {
            ExitPanel.SetActive(true);
            Time.timeScale = 0;
        }
        else if (OperationType == "No")
        {
            ExitPanel.SetActive(false);
            Time.timeScale = 1;
        }
        else // yes
        {
            SceneManager.LoadScene(0);
            Time.timeScale = 1;
        }      
    }

    // En son gecilen checkpointe araci düzeltip isinlamak amaciyla olusturuldu
    public void FixRotationAndMoveToLastCheckpoint()
    {
        int TempCurrentWaypoint = _CarControl.GetComponent<RaceStatus>().currentPointIndex;      

        // current 3.indekste isek en son 2.checkpointi gecmis oluruz pozisyonlama 2 ye gore yapilir.
        if (TempCurrentWaypoint != 0)
        {
            _CarControl.transform.SetPositionAndRotation(
                _RaceManager.Waypoints[TempCurrentWaypoint - 1].transform.position + new Vector3(0, 0, 8), 
                _RaceManager.Waypoints[TempCurrentWaypoint - 1].rotation);



        }
    }

}
