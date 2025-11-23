using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using RacingGame;


public class CarControl : MonoBehaviour
{
    float currentTorque;
    float currentRotateAngle;
    int lastSpeed;
    bool isMovingReverse;
    bool isFrictionEnable;
    bool isReachMaxSpeed;
    float oldCarRotationPosition; 
   
    Coroutine nitroUseRoutine;

    
    int currentGear;
    int gearRange; 
    int higherGear; 
    int lowerGear; 

    public float currentSpeed { get { return rb.linearVelocity.magnitude * 2.23693629f; } }  // 2.23693629f günümüzüe gore ideal kabul edilen bir degerdir.

    InputAction movementAction;
    InputAction brakeAction;
    InputAction handBrakeAction;
    InputAction nitroAction;
    Vector2 movementValue;

    [SerializeField] DriveType driveType = DriveType.AWD;
    public List<Wheels> Wheels;

    [Header("-- Technical Data --")]
    [SerializeField] float maximumSpeed;
    [SerializeField] float brakePower;
    [SerializeField] float handBrakePower;
    [SerializeField] float maximumRotateAngle;
    [SerializeField] int maximumReverseSpeed;
    [SerializeField] float wheelTorque;
    [SerializeField] float reverseTorque;

    // bazi asset arac modellerinin tekerlerinin default rotu farkli oldugu icin olusturuldu
    [SerializeField] float wheelOfsetX;
    [SerializeField] float wheelOfsetY;
    [SerializeField] float wheelOfsetZ;

    [SerializeField] Rigidbody rb;
    [SerializeField] Vector3 centerOfMass;

    [Header("-- Effects --")]
    [SerializeField] TrailRenderer leftBackWheelBrakeTrail;
    [SerializeField] TrailRenderer rightBackWheelBrakeTrail;
    [SerializeField] ParticleSystem[] Effects;

    [Header("-- Car Lights --")]
    [SerializeField] Material brakeLight;
    [SerializeField] Material reverseGearLight;

    [Header("-- Sounds --")]
    [SerializeField] AudioSource brakeAudio;
    [SerializeField] AudioSource changeGearAudio;
    [SerializeField] AudioSource carAudio;
    public float pitchValue; 


    [Header("-- YOL TUTUCU VE YARDIMCILARI  --")]
    [Range(0, 1)]
    [SerializeField] float trackionControl; // 0 çekiþ kontrolü yok, 1 tam müdahale
    [Range(0, 1)]
    [SerializeField] float rotationAssistMultiplier; // Arttikca donuslere müdahaleyi arttirarak donusleri kisitlar
    [Range(0, 0.5f)]
    [SerializeField] float linearFrictionRate;
    [Range(0, 0.3f)]
    [SerializeField] float angularFrictionRate;

    [SerializeField] float downForce; // Yere basma kuvveti
   
    public bool CanSpin;
    [SerializeField] float spinLimit;

    
    [Header("-- Camera --")]
    public GameObject[] cameras;
    int cameraIndex;

    float currentAccelerationValue;
    float currentBrakeValue;

    private void Awake()
    {
        gearRange = CalculateGearRange();
        
    }


    void Start()
    {
        rb.centerOfMass = centerOfMass;

        movementAction = InputSystem.actions.FindAction("Navigate");
        brakeAction = InputSystem.actions.FindAction("Brake");
        handBrakeAction = InputSystem.actions.FindAction("HandBrake");
        nitroAction = InputSystem.actions.FindAction("Nitro");

        currentTorque = wheelTorque - (trackionControl * wheelTorque);
        GameManager.instance._CarControl = this; 
    }

    
    void Update()
    {
        if (!GameManager.instance._RaceManager.IsGameStart)
        {
            return;
        }

        movementValue = movementAction.ReadValue<Vector2>();

        if (handBrakeAction.IsPressed())
        {           
            CarMovement(movementValue.x, movementValue.y, movementValue.y, 1); 
        }
        else
        {
            CarMovement(movementValue.x, movementValue.y, movementValue.y, 0);
        }
    
        lastSpeed = (int)currentSpeed;
        GameManager.instance.CurrentSpeedText.text = lastSpeed.ToString();

        StartNitroUse();
        Brake();       
        SpeedControl();       
        ReverseGear();
        GearControl();

        // Sürüþ yardimci fonksiyonlari
        HandleCarRotation();
        HandleLinearFriction();
        HandleAngularFriction();
        HandlingAssist();
        TractionControl();

        if (CanSpin)
        {
            HandleCarSpin();
        }
       

    }

    void SpeedControl()
    {
        if (currentSpeed > maximumSpeed)
        {
            // Dogrusal velocity hizlanmasini durdurup normallestirir.Her zaman maksimum hizi gecemeyecek sekilde kitler.
            rb.linearVelocity = (maximumSpeed / 2.23693629f) * rb.linearVelocity.normalized;
            isReachMaxSpeed = true; 
        }
        else
        {
            isReachMaxSpeed = false; 
        }
    }

   
    void ReverseGear()
    {
        // Araba hizli giderken s ye basarsak -> fren lambasi yanar
        // Araba durmaya yakin hizda s ye basarak geri gidersek -> geri lambasi yanar
        if (currentBrakeValue == 1)
        {
            if (currentSpeed > 5)
            {
                if (isMovingReverse)
                {
                    brakeLight.SetColor("_EmissionColor", Color.red * Mathf.Pow(2, 5));
                }
            }
            else if (currentSpeed > maximumReverseSpeed) // Aracin geri geri giderken cikabilecegi maks hizi limitler.
            {
                brakeLight.SetColor("_EmissionColor", Color.red * Mathf.Pow(2, 3));
            }
            else
            {
                isMovingReverse = true;
                brakeLight.SetColor("_EmissionColor", Color.red * Mathf.Pow(2, 3));

                if (reverseGearLight != null)
                {
                    reverseGearLight.EnableKeyword("_EMISSION");                                     
                }
            }

            // Geri gitme hizini limitler
            if (isMovingReverse)
            {
                if (currentSpeed > maximumReverseSpeed)
                {
                    rb.linearVelocity = (maximumReverseSpeed / 2.23693629f) * rb.linearVelocity.normalized;
                }
            }

        }

        // Geri gitme birakildiysa
        if (currentBrakeValue == 0)
        {
            /*
            foreach(var item in Wheels)
            {
                item.WheelCollider.motorTorque = 0;
            }
            */

            brakeLight.SetColor("_EmissionColor", Color.red * Mathf.Pow(2, 3));

            if (reverseGearLight != null)
            {
                reverseGearLight.DisableKeyword("_EMISSION");
            }

            isMovingReverse = false;
            isFrictionEnable = true;

        }
        
    }

    int CalculateGearRange()
    {
        return Mathf.RoundToInt((maximumSpeed + 3) / 5); 
    }

    
    void GearControl()
    {
        // Toplam hiz / 5 ten vites araliklarini belirleriz farkli araclarda farkli hizlar olabilir.
        

        if (currentSpeed <= .9f) // duruyorsa
        {
            higherGear = gearRange;
            currentGear = 0;
            GameManager.instance.CurrentGearText.text = "D"; // Bosta, 0.vites
        }

        if (currentSpeed >= 1 && currentSpeed <= gearRange) // 1.viteste, ilk harekete baþlandigi yer
        {
            higherGear = gearRange;
            currentGear = 1;
            GameManager.instance.CurrentGearText.text = currentGear.ToString(); 
        }
        else if (currentSpeed >= 1 && currentSpeed <= gearRange) // 1.viteste, ilk harekete baþlandigi yer
        {          
            currentGear = 0;
            GameManager.instance.CurrentGearText.text = "R";
        }

        if (currentSpeed > higherGear) // vitesteki max sinira ulasildi, arac vites atmasi gerekmekte.
        {
            // 180 / 5 ten 2.vitese gecis hizi 35 se burada uzerine tekrar ekleyerek yeni siniri 70 e ceker. Bu þekilde son vitese kadar kontrol saglariz.
            lowerGear = higherGear;           
            higherGear += gearRange; 

            if (currentGear < 5) // sonuncu viteste degilsek
            {
                currentGear++;
                if (currentGear != 1)
                {
                    Effects[0].gameObject.SetActive(true);
                }

                changeGearAudio.Play();
            }

            GameManager.instance.CurrentGearText.text = currentGear.ToString();
        }
        else if (currentSpeed < lowerGear) // Mevcut hizimiz alt vitesten kucukse arac vites dusurur
        {
            higherGear -= gearRange;

            if (lowerGear != 0)
            {
                lowerGear -= gearRange;
            }

            if (currentGear != 1)
            {
                currentGear--;
            }
            GameManager.instance.CurrentGearText.text = currentGear.ToString();
        }

    }

    void CarMovement(float SteeringWheel, float Acceleration, float BrakeValue, float HandBrakeValue)
    {
        SteeringWheel = Mathf.Clamp(SteeringWheel, -1, 1);             
        Acceleration = Mathf.Clamp(Acceleration, 0, 1);    
        BrakeValue = -1 * Mathf.Clamp(BrakeValue, -1, 0);  
        HandBrakeValue = Mathf.Clamp(HandBrakeValue, 0, 1);

        // Tekerlerin saga sola donus acisini sinirlandiririz maks 30 derece idealdir. On tekerlerin saga sola donmesi
        currentRotateAngle = SteeringWheel * maximumRotateAngle;
        Wheels[0].WheelCollider.steerAngle = currentRotateAngle;
        Wheels[1].WheelCollider.steerAngle = currentRotateAngle;

        // Model olan tekerlerin ileri geri donebilmesi icin
        foreach (var item in Wheels)
        {
            
            item.WheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);

            // Rotation düzeltmesi
            Quaternion fixedRot = rot * Quaternion.Euler(item.RotationOffset);

            item.WheelModel.transform.SetPositionAndRotation(pos, fixedRot);
        }

        DriveMode(Acceleration, BrakeValue);
        HandBrake(HandBrakeValue);

        currentAccelerationValue = Acceleration;
        currentBrakeValue = BrakeValue;
     
        if (Acceleration == 1)
        {         
            if (rb.linearDamping != 0)
            {
                rb.linearDamping = 0;
                isFrictionEnable = false;
            }
            
            CarSound(true);
        }
       
        if (Acceleration == 0 && currentSpeed > 2f)
        {                     
            isFrictionEnable = true;
        }
    }

    void Brake()
    {
        if (brakeAction.IsPressed())
        {
            BrakeSystemControl(true);
        }
      
        if (brakeAction.WasReleasedThisFrame())
        {
            BrakeSystemControl(false);
        }
    }

    void HandBrake(float HandBrakeValue)
    {
        if (handBrakeAction.IsPressed())
        {
            if (HandBrakeValue > 0f)
            {              
                var brakeTorque = HandBrakeValue * handBrakePower;
                BrakeSystemControl(true, brakeTorque);
            }
        }
      
        if (handBrakeAction.WasReleasedThisFrame()) 
        {
            BrakeSystemControl(false);
        }
    }

    void BrakeSystemControl(bool situtation, float HandBrakeValue = 0)
    {
        
        if (situtation)
        {
            if (HandBrakeValue == 0) // normal fren
            {
                Wheels[2].WheelCollider.brakeTorque = brakePower;
                Wheels[3].WheelCollider.brakeTorque = brakePower;
            }
            else // el freni
            {
                Wheels[0].WheelCollider.brakeTorque = HandBrakeValue;
                Wheels[1].WheelCollider.brakeTorque = HandBrakeValue;
                Wheels[2].WheelCollider.brakeTorque = HandBrakeValue;
                Wheels[3].WheelCollider.brakeTorque = HandBrakeValue;
            }
          
            brakeLight.SetColor("_EmissionColor", Color.red * Mathf.Pow(2, 5));

            
            if (currentSpeed > 1f)
            {
                // wheel colliderdaki get ground hit ile tekerin yere degip degmediginin checki
                Wheels[2].WheelCollider.GetGroundHit(out WheelHit LeftBackHit);

                // Teker yere degiyorsa
                if (LeftBackHit.normal != Vector3.zero)
                {
                    // Sol arka tekerde Fren sirasinda duman vfxi
                    if (!Effects[3].isPlaying)
                    {
                        Effects[3].gameObject.SetActive(true);
                        Effects[3].Play();
                    }

                    if (leftBackWheelBrakeTrail != null)
                    {                        
                        leftBackWheelBrakeTrail.emitting = situtation;
                    } 
                }

                Wheels[3].WheelCollider.GetGroundHit(out WheelHit RightBackHit);
              
                if (RightBackHit.normal != Vector3.zero)
                {
                    // Sag arka tekerde fren sirasinda duman vfxi
                    if (!Effects[4].isPlaying)
                    {
                        Effects[4].gameObject.SetActive(true);
                        Effects[4].Play();
                    }

                    if (rightBackWheelBrakeTrail != null)
                    {
                        rightBackWheelBrakeTrail.emitting = situtation;
                    }
                }
              
                if (!brakeAudio.isPlaying)
                {
                    brakeAudio.Play();
                }
            }
            else // Araba hareket etmiyorsa veya tam durmak uzereye yakinsa fren izi ve sesi durdur.
            {
                leftBackWheelBrakeTrail.emitting = situtation;
                rightBackWheelBrakeTrail.emitting = situtation;
                if (brakeAudio.isPlaying)
                {
                    brakeAudio.Stop();
                }
            }                    
        }
        else
        {
            // Tekerlerdeki frenlemeyi serbest birakir.
            Wheels[0].WheelCollider.brakeTorque = 0;
            Wheels[1].WheelCollider.brakeTorque = 0;
            Wheels[2].WheelCollider.brakeTorque = 0;
            Wheels[3].WheelCollider.brakeTorque = 0;
            brakeLight.SetColor("_EmissionColor", Color.red * Mathf.Pow(2, 3)); // 3 varsayilan deger

            leftBackWheelBrakeTrail.emitting = situtation;
            rightBackWheelBrakeTrail.emitting = situtation;

            Effects[4].Stop();
            Effects[3].Stop();
            if (brakeAudio.isPlaying)
            {
                brakeAudio.Stop();
            }
        }
    }

    void DriveMode(float SteeringWheel, float BrakeValue)
    {
        float Torque;
        switch (driveType)
        {
            case DriveType.AWD:
                // Eðer 4 ceker ise torque bütün tekerlere yayarýz.
                Torque = SteeringWheel * (currentTorque / 4f);
                foreach(var item in Wheels)
                {
                    item.WheelCollider.motorTorque = Torque;
                }
                break;
            case DriveType.FWD:
                // Onden cekerde sadece on tekerler hareket eder gelen degeri 2 ye boleriz.
                Torque = SteeringWheel * (currentTorque / 2f);
                // 0. ve 1. indekste ondeki 2 teker yer aldigi icin bu 2 tekere torku uylariz.
                Wheels[0].WheelCollider.motorTorque = Wheels[1].WheelCollider.motorTorque = Torque;
                break;
            case DriveType.BWD:
                // Arkadan cekerde sadece arka 2 tekere tork uygulanir. 2'ye boleriz.
                Torque = SteeringWheel * (currentTorque / 2f);
                // 2. ve 3. indekste arkadaki 2 teker oldugu icin arka tekerlere tork uygular.
                Wheels[2].WheelCollider.motorTorque = Wheels[3].WheelCollider.motorTorque = Torque;
                break;
        }

        // Brake mekanigi
        foreach(var item in Wheels)
        {
            // Araba biraz hareket ediyorsa
            if (currentSpeed > 5 && Vector3.Angle(transform.forward, rb.linearVelocity) < 50f)
            {
                item.WheelCollider.brakeTorque = brakePower * BrakeValue;
            }
            else if (BrakeValue > 0)
            {
                item.WheelCollider.brakeTorque = 0;
                // Ters torkun mantýgý tekerlere tersine bir güc vererek daha smooth iyi durmasini saglar.
                item.WheelCollider.motorTorque = -reverseTorque * BrakeValue;
            }
        }

    }

    IEnumerator UseNitro()
    {       
        if ((maximumSpeed - currentSpeed) >= 10)
        {
            Effects[1].gameObject.SetActive(true);
            Effects[2].gameObject.SetActive(true);

            if (!GameManager.instance.NitroSound.isPlaying)
            {
                GameManager.instance.NitroSound.Play();
            }
        }

        while ((maximumSpeed - currentSpeed) >= 10)
        {
            rb.linearVelocity += 0.7f * rb.linearVelocity.normalized;
            GameManager.instance.NitroValue -= 5f;
            GameManager.instance.NitroSlider.value = GameManager.instance.NitroValue;

            if (GameManager.instance.NitroValue <= 0 || currentSpeed >= maximumSpeed)
            {
                Effects[1].gameObject.SetActive(false);
                Effects[2].gameObject.SetActive(false);
                GameManager.instance.NitroValue = 0;
                GameManager.instance.NitroUsed();

                if (nitroUseRoutine != null)
                {
                    StopCoroutine(nitroUseRoutine);
                    nitroUseRoutine = null;
                }
                break;
            }
            yield return new WaitForSeconds(0.2f);
            
        }

        Effects[1].gameObject.SetActive(false);
        Effects[2].gameObject.SetActive(false);
        if (GameManager.instance.NitroSound.isPlaying)
        {
            GameManager.instance.NitroSound.Stop();
        }

    }

    void StartNitroUse()
    {
        if (nitroAction.IsPressed())
        {
            if (!GameManager.instance.NitroStatus && nitroUseRoutine == null)
            {               
                if ((maximumSpeed - currentSpeed) >= 10)
                {
                    nitroUseRoutine = StartCoroutine(UseNitro());
                }
            }
        }

        if (nitroAction.WasReleasedThisFrame())
        {
            Effects[1].gameObject.SetActive(false);
            Effects[2].gameObject.SetActive(false);

            if (nitroUseRoutine != null)
            {
                StopCoroutine(nitroUseRoutine);
                nitroUseRoutine = null;
            }
            GameManager.instance.NitroUsed();
        }

    }

    public void ChangeCamera()
    {
        cameras[cameraIndex].SetActive(false);
        if (cameraIndex != cameras.Length - 1)
        {
            cameraIndex++;
        }
        else
        {
            cameraIndex = 0;
        }
        cameras[cameraIndex].SetActive(true);
    }

  
    void CarSound(bool IsCarMoving)
    {
        float TempPitch = pitchValue * (int)currentSpeed + 1;

        if (IsCarMoving)
        {
            if (!isReachMaxSpeed)
            {
                carAudio.pitch = TempPitch;
            }
        }
        else
        {
            carAudio.pitch = TempPitch;
            if (carAudio.pitch < 1) // 1 asagisi pitch degerinde ses slow motion gibi olup kotu durmakta
            {              
                carAudio.pitch = 1; 
            }
        }
    }

    // ----------------------------------------------------- Arac sürüs yardimci sistemler ---------------------------------------------------------------
    // Bu sistemler opsiyonel sistemlerdir degiskenleri 0 yaparak etkilerini kaldirabiliriz.

    // Arac cok hizli giderken ani manevralarda aracin dengesi bozulabilmekte buna gimble kitlenmesi denir.Ana mantik donerken her zaman bir onceki
    // rotasyonu kontrol ederek farkin belli bir seviyeden daha büyük olmamasini saglariz.
    // (Opsiyonel) Eðer ani dönüslerde savrulma yalpalama vs. isteniyorsa bu fonksiyonu kullanma 
    void HandleCarRotation()
    {
       
        foreach(var item in Wheels)
        {
            item.WheelCollider.GetGroundHit(out WheelHit wheelHit);
            if (wheelHit.normal == Vector3.zero)
            {
                return;
            }
        }

        if (Mathf.Abs(oldCarRotationPosition - transform.eulerAngles.y) < 10f)
        {
            var smoothRotate = (transform.eulerAngles.y - oldCarRotationPosition) * rotationAssistMultiplier;
            Quaternion lastVelocityRotation = Quaternion.AngleAxis(smoothRotate, Vector3.up);
            rb.linearVelocity = lastVelocityRotation * rb.linearVelocity;
        }

        oldCarRotationPosition = transform.eulerAngles.y;




    }

    // W basilmadigi zamanda sürtünmeyi arttirarak araci yavaslatir.
    void HandleLinearFriction()
    {
        // Araba gitmiyorsa, gazi biraktiysak
        if (currentAccelerationValue == 0)
        {
            isFrictionEnable = true;
        }

        if (isFrictionEnable)
        {
            CarSound(false);
            if (currentSpeed > 0)
            {
                rb.linearDamping = linearFrictionRate;
            }
            else
            {
                rb.linearDamping = 0;
            }
        }
    }

    // Aracin donusunu hiza bagli olarak limitler (Cok hizli giden bir aracin ani rotate yapmasini sert bir sekilde engeller)
    void HandleAngularFriction()
    {
        if (currentSpeed > 0)
        {
            rb.angularDamping = currentSpeed * angularFrictionRate;
        }
    }

    // Tekerlerin rigidbodysine kuvvet uygulayarak aracin biraz daha yere basmasini saglar
    void HandlingAssist()
    {
        foreach(var item in Wheels)
        {
            // Tekerlerin yukarisindan asagi dogru bir force uygular
            Vector3 calculatedForceValue = item.WheelCollider.attachedRigidbody.linearVelocity.magnitude * downForce * -transform.up;
            item.WheelCollider.attachedRigidbody.AddForce(calculatedForceValue);
        }
    }

    // Aracin belirli bir hizin üstüne ciktiktan sonraki dönüslerdeki kayma kisimlarina yonelik islemler (ses, kayma vfx vs.)
    void HandleCarSpin()
    {
        if (currentSpeed > 50)
        {
            // Sol Arka teker
            Wheels[2].WheelCollider.GetGroundHit(out WheelHit hit);
            if (Mathf.Abs(hit.forwardSlip) >= spinLimit || Mathf.Abs(hit.sidewaysSlip) >= spinLimit)
            {
                if (!brakeAudio.isPlaying)
                {
                    brakeAudio.Play();
                }

                if (leftBackWheelBrakeTrail != null)
                {
                    leftBackWheelBrakeTrail.emitting = true;
                }
            }
            else 
            {
                if (brakeAudio.isPlaying)
                {
                    brakeAudio.Stop();
                }

                if (leftBackWheelBrakeTrail != null)
                {
                    leftBackWheelBrakeTrail.emitting = false;
                }
            }


            // Sað Arka teker
            Wheels[3].WheelCollider.GetGroundHit(out WheelHit hit2);
            if (Mathf.Abs(hit2.forwardSlip) >= spinLimit || Mathf.Abs(hit2.sidewaysSlip) >= spinLimit)
            {
                if (!brakeAudio.isPlaying)
                {
                    brakeAudio.Play();
                }

                if (rightBackWheelBrakeTrail != null)
                {
                    rightBackWheelBrakeTrail.emitting = true;
                }
            }
            else
            {
                if (brakeAudio.isPlaying)
                {
                    brakeAudio.Stop();
                }

                if (rightBackWheelBrakeTrail != null)
                {
                    rightBackWheelBrakeTrail.emitting = false;
                }
            }
        }
    }

    void TractionControl()
    {
        WheelHit wheelHit;
        switch (driveType)
        {
            case DriveType.AWD:                            
                foreach (var item in Wheels)
                {
                    item.WheelCollider.GetGroundHit(out wheelHit);
                    SetTorque(wheelHit.forwardSlip);
                }
                break;
            case DriveType.FWD:
                Wheels[2].WheelCollider.GetGroundHit(out wheelHit);
                SetTorque(wheelHit.forwardSlip);               
                Wheels[3].WheelCollider.GetGroundHit(out wheelHit);
                SetTorque(wheelHit.forwardSlip);
                break;
            case DriveType.BWD:
                Wheels[0].WheelCollider.GetGroundHit(out wheelHit);
                SetTorque(wheelHit.forwardSlip);
                Wheels[1].WheelCollider.GetGroundHit(out wheelHit);
                SetTorque(wheelHit.forwardSlip);
                break;
        }
    }

    void SetTorque(float ForwardSlip)
    {
        // arac gidiyorsa mevcut bir torksa ve arac kayiyorsa
        if (ForwardSlip >= spinLimit && currentTorque >= 0)
        {
            currentTorque -= 10 * trackionControl;
        }
        else
        {
            currentTorque += 10 * trackionControl;
            if (currentTorque > wheelTorque)
            {
                currentTorque = wheelTorque;
            }
        }
    }

    public void SetSfxVolume(float NewVolumeLevel)
    {
        brakeAudio.volume = NewVolumeLevel;
        changeGearAudio.volume = NewVolumeLevel;
        carAudio.volume = NewVolumeLevel;
    }


}
