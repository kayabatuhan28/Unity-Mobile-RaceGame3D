using System;
using System.Collections;
using UnityEngine;

public class AiManager : MonoBehaviour
{
    [Header("--- DÝKKAT VE KONTROL ---")]
    // Hizlanma esnasinda maksimum hizin ne kadar kullanilabilecegini belirler. Deger cok yüksek olursa arac mevcut maksimum hizi sonuna kadar kullanmaya calisir.
    [SerializeField]
    [Range(0, 1)] float maxSpeedAttentionFactor;

    // Virajlari ne kadar dikkatli donecegidir. Degger arttikca dikkat artar düstükce düser.
    [SerializeField]
    [Range(0, 180)] float maximumCorneringAngle;

    // Frenleme, dönüþ, hedefleme iþlemlerinde kontrole baþlanacagi maksimum mesafedir.Deðer arttikça bir sonraki hedef noktaya ulaþmadan daha çok kontrol saðla
    // yacaðý için yol tutuþ ve iþlemler çok daha garanti olur.
    [SerializeField] float maximumControlDistance;

    // Yz nin kendi anlýk acisini, hizini kontrol ederken ne kadar dikkatli olmasi gerektigi. Virajlara girdiginde kendi acisinida kontrol ederek dönmesi gereken
    // aciya gore daha smooth bir hesaplama islemi icin.
    [SerializeField] float angularVelocityControlFactor;


    float randomMovementFactor; // Aracýn robotik gozukmesini engellemek icin aracin arada yalpalamasi, çizgiyi tam takip etmemesi vs.
    float collisionAvoidanceTime; // Yakýn zamanda çarpýþýlan araçtan kaçýnmak için gereken süre
    float avoidanceDecelerationMultiplier; // Baþka bir arabayla carpismadan kacinirken ne kadar yavaslamasi gerektigi
    float vehicleAvoidanceDirection; // kaçýnma yaparken hangi yöne kacinacagini belirler

    [Header("--- HASSASÝYETLER ---")]
    [SerializeField] float turnSensitivity;
    [SerializeField] float accelerationSensitivity;
    [SerializeField] float brakeSensitivity;

    //
    [Header("--- YANAL ISLEMLER ---")]
    [SerializeField]
    [Range(0, 1)] float lateralTravelDistance;
    [SerializeField]
    [Range(0, 1)] float sideSwaySpeed;

    [Header("--- IVMELENME KONTROL ---")]
    // ivmelenme sapmasi 0 olmasi halinde arac normal sekilde ivmelenir bu degeri arttirmamiz halinde yaptigimiz hesaba gore ivmelenmeyi etkiler.
    [SerializeField]
    [Range(0, 1)] float accelerationDeviation;

    // Arabalarin ivmelenmesinde ne kadar hizli dalgalanacagini belirtir.
    [SerializeField] float accelerationSpeedOscillation;

    [Header("--- KONTROLLER VE ISLEMLER ---")]
    // Bazi yapay zeka araclarin virajlara giriste frenleyip, yavaslamasinin istenip istenmedigini belirtir.
    public bool IsBrakeControlEnabled;
    public bool IsDrivingActive;
    public AiCarControl _AiCarControl;

    [Header("--- POZÝSYONLAR VE ROTA ---")]
    [SerializeField] Transform currentRoute;
    public Transform[] Routes;
    int routeIndex;
    public int WhichRoute;

    Coroutine moveBackwardCoroutine;
    Coroutine fixCarRotationCoroutine;


    void Start()
    {
        randomMovementFactor = UnityEngine.Random.value * 100;
        
        //WhichRoute = UnityEngine.Random.Range(1, 3);
        switch (WhichRoute)
        {
            case 1:
                Routes = GameManager.instance.Route1;
                break;
            case 2:
                Routes = GameManager.instance.Route2;
                break;
            case 3:
                Routes = GameManager.instance.Route3;
                break;
            case 4:
                Routes = GameManager.instance.Route4;
                break;
        }

        currentRoute = Routes[routeIndex];
        IsDrivingActive = true;
        

        // 2.yol direk hardcoded rota verme
        /*
        Routes = GameManager.instance.Route1;
        currentRoute = Routes[routeIndex];
        IsDrivingActive = true;
        */


    }

    void SetNewRoutePosition()
    {
        if (routeIndex != Routes.Length - 1)
        {
            routeIndex++;
            currentRoute = Routes[routeIndex];
        }
        else
        {
            //Debug.Log("On Reach Finish");
        }
    }

   
    private void FixedUpdate()
    {
        // Editorde test amaçlý
        /*
        if (ApplyBrake)
        {
            _AiCarControl.CarMovement(0, 0, 0);
            _AiCarControl.IsBraking = true;
            return;
        }
        else
        {
            _AiCarControl.IsBraking = false;
        }

        if (MoveBackward)
        {
            _AiCarControl.CarMovement(0, 0, -1f);

            return;
        }
        */

        if (!GameManager.instance._RaceManager.IsGameStart)
        {
            return;
        }

        if (currentRoute == null || !IsDrivingActive)
        {
            // Arac fren
            //_AiCarControl.CarMovement(0, 0, -1f);
        }
        else
        {       
            // Mevcut rota pointine ulastiysa
            if (Vector3.Distance(transform.position, currentRoute.position) < 9f)
            {              
                SetNewRoutePosition();
            }
            
            Vector3 fwd = transform.forward;
            if (_AiCarControl.Rb.linearVelocity.magnitude >_AiCarControl.MaximumSpeed * 0.1f)
            {
                // araci yavaslatarak maksimum hiza geri indiririz hizi
                fwd = _AiCarControl.Rb.linearVelocity;
            }

            float targetSpeed = _AiCarControl.MaximumSpeed;

            if (IsBrakeControlEnabled)
            {
                Vector3 Delta = currentRoute.position - transform.position;
                
                float DistanceAttentionFactor = Mathf.InverseLerp(maximumControlDistance, 0, Delta.magnitude);

                float AngularControl = _AiCarControl.Rb.angularVelocity.magnitude * angularVelocityControlFactor;

                float AttentionLevel = Mathf.Max(Mathf.InverseLerp(0, maximumCorneringAngle, AngularControl), DistanceAttentionFactor);

                targetSpeed = Mathf.Lerp(_AiCarControl.MaximumSpeed, _AiCarControl.MaximumSpeed * maxSpeedAttentionFactor, AttentionLevel);
            }

            // ------ Araclardan kacinma ---------
            Vector3 currentRoutePos = currentRoute.position;
            if (Time.time < collisionAvoidanceTime)
            {
                targetSpeed *= avoidanceDecelerationMultiplier;
                currentRoutePos += currentRoute.right * vehicleAvoidanceDirection;
            }
            else
            {
                // perlinnoise rastgele hareket saðlar. 2 nokta arasinda giderken daha randomizasyon saðlar.
                currentRoutePos += (Mathf.PerlinNoise(Time.time * sideSwaySpeed, randomMovementFactor) * 2 - 1) * lateralTravelDistance * currentRoute.right;
            }


            // ------ Hareket --------
            float accelerationBrakeSensitivity = (targetSpeed < _AiCarControl.currentSpeed) ? brakeSensitivity : accelerationSensitivity;
            float Acceleration = Mathf.Clamp((targetSpeed - _AiCarControl.currentSpeed) * accelerationBrakeSensitivity, -1, 1);
            
            Acceleration *= (1 - accelerationDeviation) + (Mathf.PerlinNoise(Time.time
                * accelerationSpeedOscillation, randomMovementFactor) * accelerationDeviation);

            // Verilen noktaya dogru yönelmeyi saglayacak olan bir vector belirler
            Vector3 LocalTarget = transform.InverseTransformPoint(currentRoutePos);

            // Virajlari donerken dim direk deðilde 2 nokta arasinda hafif açili eðri gibi gidebilmesi için açiyi aliriz.
            float TargetAngle = Mathf.Atan2(LocalTarget.x, LocalTarget.z) * Mathf.Rad2Deg;

            // Sign Pozitif veya 0 olduðunda dönüs degeri 1, negatifse -1 döndürür. Mevcut hizimiz bazen azalip bazen artabilecegi icin ileri dogru bir hareket
            // olmasi icin 
            float Steering = Mathf.Clamp(TargetAngle * turnSensitivity, -1, 1) * Mathf.Sign(_AiCarControl.currentSpeed);

            _AiCarControl.CarMovement(Steering, Acceleration, Acceleration);


        }
    }


    private void OnCollisionStay(Collision collision)
    {
        if (collision.rigidbody != null)
        {
            var OtherCar = collision.gameObject;

            if (OtherCar != null)
            {
                collisionAvoidanceTime = Time.time + 1;

                // Carpisma sirasinda bizmi, yoksa carptigimiz arabami onde -- arkadaysak fren.
                if (Vector3.Angle(transform.forward, OtherCar.transform.position - transform.position) < 90)
                {
                    // Diger yapay zeka aracý önde
                    avoidanceDecelerationMultiplier = .5f;
                }
                else
                {
                    // Carpan yapay zeka önde
                    avoidanceDecelerationMultiplier = 1;
                }

                var OtherCarLocalPosition = transform.InverseTransformPoint(OtherCar.transform.position);
               
                // Verilen aciya göre araclardan kacinma yonu belirlenir
                var TempAngle = Mathf.Atan2(OtherCarLocalPosition.x, OtherCarLocalPosition.z);
                vehicleAvoidanceDirection = lateralTravelDistance * -Mathf.Sign(TempAngle);
            }
        }    
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Barrier"))
        {
            if (_AiCarControl.currentSpeed < 1)
            {
                moveBackwardCoroutine ??= StartCoroutine(MoveBackWard());
            }
        }

        if (other.CompareTag("Road"))
        {
            fixCarRotationCoroutine ??= StartCoroutine(FixCarRotation());
        }
    }

    

    IEnumerator MoveBackWard()
    {
        IsDrivingActive = false;
        yield return new WaitForSeconds(0.2f);
        _AiCarControl.CarMovement(0, 0, -1f);       
        yield return new WaitForSeconds(3f);
        moveBackwardCoroutine = null;
        IsDrivingActive = true;
    }

    // Ai arac carpisma vs. ile takla atip ters donerse araci düzeltme iþlemi
    IEnumerator FixCarRotation()
    {
        IsDrivingActive = false;
        Debug.Log("Arac ters döndü düzeltiliyor!");
        yield return new WaitForSeconds(1f);
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        transform.position = currentRoute.position;
        yield return new WaitForSeconds(3f);
        fixCarRotationCoroutine = null;
        IsDrivingActive = true;
    }


}
