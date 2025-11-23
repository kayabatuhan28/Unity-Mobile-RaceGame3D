using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using RacingGame; 

// Playerin kontrol etmediði yanimizda yariþan baþka yapay zeka arac classlari icin olusturuldu.

public class AiCarControl : MonoBehaviour
{
    float currentTorque;
    float currentRotateAngle;
    bool isMovingReverse;
    bool isFrictionEnable; // sürtünmeyi temsil eder.
    bool isReachMaxSpeed;
    float oldCarRotationPosition; // Ani donusleri limitlemek icin
    
    float currentAccelerationValue;
    float currentBrakeValue;
    public bool IsBraking;

    public float currentSpeed { get { return Rb.linearVelocity.magnitude * 2.23693629f; } }  // 2.23693629f günümüzüe gore ideal kabul edilen bir degerdir.

    [SerializeField] DriveType driveType = DriveType.AWD;
    public List<Wheels> Wheels;

    [Header("-- Technical Data --")]
    public float MaximumSpeed;
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

    public Rigidbody Rb;
    [SerializeField] Vector3 centerOfMass;

    [Header("-- Effects --")]
    [SerializeField] TrailRenderer leftBackWheelBrakeTrail;
    [SerializeField] TrailRenderer rightBackWheelBrakeTrail;
    [SerializeField] ParticleSystem[] Effects;

    // frene basinca fren lambalarinin yanmasi icin
    [Header("-- Car Lights --")]
    [SerializeField] Material brakeLight;
    [SerializeField] Material reverseGearLight;

    [Header("-- Sounds --")]
    [SerializeField] AudioSource brakeAudio;
    [SerializeField] AudioSource carAudio;
    public float pitchValue; // Arac hareket ederken yavaslama hizlanma gibi kisimlarda


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


    void Start()
    {
        Rb.centerOfMass = centerOfMass;

        currentTorque = wheelTorque - (trackionControl * wheelTorque);
       
    }

    
    void Update()
    {
        
        SpeedControl();
        ReverseGear();

        

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
        if (currentSpeed > MaximumSpeed)
        {
            // Dogrusal velocity hizlanmasini durdurup normallestirir.Her zaman maksimum hizi gecemeyecek sekilde kitler.
            Rb.linearVelocity = (MaximumSpeed / 2.23693629f) * Rb.linearVelocity.normalized;
            isReachMaxSpeed = true; 
        }
        else
        {
            isReachMaxSpeed = false; 
        }
    }

    // Arabanin geri gidip gitmedigini check eder.Geri gitmesi halinde geri vites lambasi vs. yakmak icin
    void ReverseGear()
    {
        // Ai arac fren yapiyorsa, geriye dogru gidiliyorsa
        if (currentBrakeValue == -1)
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
                    Rb.linearVelocity = (maximumReverseSpeed / 2.23693629f) * Rb.linearVelocity.normalized;
                }
            }

        }

        // Fren býrakýldýysa
        if (currentBrakeValue == 0)
        {
            foreach(var item in Wheels)
            {
                item.WheelCollider.motorTorque = 0;
            }

            brakeLight.SetColor("_EmissionColor", Color.red * Mathf.Pow(2, 3));

            if (reverseGearLight != null)
            {
                reverseGearLight.DisableKeyword("_EMISSION");
            }

            isMovingReverse = false;
            isFrictionEnable = true;

        }
        
    }

    public void CarMovement(float SteeringWheel, float Acceleration, float BrakeValue)
    {

        // Direksiyon degerini 2 deger arasinda sikistiririz. Gelmeme ihtimaline karsi bir kontrol yapariz.
        SteeringWheel = Mathf.Clamp(SteeringWheel, -1, 1);
        
        // Araba ilerimi gidiyor, duruyor mu
        Acceleration = Mathf.Clamp(Acceleration, 0, 1);

        // 
        BrakeValue = -1 * Mathf.Clamp(BrakeValue, -1, 0);

        currentAccelerationValue = Acceleration;
        currentBrakeValue = BrakeValue;

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

        if (IsBraking)
        {
            BrakeSystemControl(true);
        }
        else
        {
            BrakeSystemControl(false);
        }


        if (Acceleration > 0.7f)
        {
            // Gaza basinca sürtünmeyi sifirlama
            if (Rb.linearDamping != 0)
            {
                Rb.linearDamping = 0;
                isFrictionEnable = false;
            }

            CarSound(true);
        }
        
        if (Acceleration < 0.4f && currentSpeed > 2f)
        {                     
            isFrictionEnable = true;
        }
    }

    void BrakeSystemControl(bool situtation)
    {
        // Fren olayi arka tekerlerde olacagi icin 2 ve 3.wheelslara yani arka tekerlere uyguladik.
        if (situtation)
        {
            Wheels[2].WheelCollider.brakeTorque = brakePower;
            Wheels[3].WheelCollider.brakeTorque = brakePower;

            // Fren yapildigi anda fren lambasinin yanmasi kismi
            brakeLight.SetColor("_EmissionColor", Color.red * Mathf.Pow(2, 5));

            // Brake Effect
            if (currentSpeed > 1f)
            {
                // wheel colliderdaki get ground hit ile tekerin yere degip degmedigini algilayabiliriz.
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

                // fren sesi calmiyorsa fren cal
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
            IsBraking = false;
            // Tekerlerdeki frenlemeyi serbest birakir.         
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
        foreach (var item in Wheels)
        {
            // Araba biraz hareket ediyorsa
            if (currentSpeed > 5 && Vector3.Angle(transform.forward, Rb.linearVelocity) < 50f)
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

    // Arac hizlandikca pitch degeri arttirilarak daha smooth bir araba sesi ayarlamasi
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
            Rb.linearVelocity = lastVelocityRotation * Rb.linearVelocity;
        }

        oldCarRotationPosition = transform.eulerAngles.y;




    }

    // W basilmadigi zamanda sürtünmeyi arttirarak araci yavaslatir. 
    void HandleLinearFriction()
    {
        // Ai arac gaz vermiyorsa
        if (currentAccelerationValue <= 0)
        {
            isFrictionEnable = true;
        }

        if (isFrictionEnable)
        {
            CarSound(false);
            if (currentSpeed > 0)
            {
                Rb.linearDamping = linearFrictionRate;
            }
            else
            {
                Rb.linearDamping = 0;
            }
        }
    }

    // Aracin donusunu hiza bagli olarak limitler (Cok hizli giden bir aracin ani rotate yapmasini sert bir sekilde engeller)
    void HandleAngularFriction()
    {
        if (currentSpeed > 0)
        {
            Rb.angularDamping = currentSpeed * angularFrictionRate;
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
        carAudio.volume = NewVolumeLevel;
    }


}
