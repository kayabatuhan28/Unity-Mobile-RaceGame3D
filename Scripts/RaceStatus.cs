using UnityEngine;

// Araçla ilgili yarýþ durumlarýný tutan scripttir. Aracýn checkpointlerde yarýþta kaçýncý oldugunu vs. belirler.
public class RaceStatus : MonoBehaviour
{

    int TotalPoints; // toplam checkpoint sayisi
    public int currentPointIndex; // kacinci checkpointte
    public int NextPointIndex;
    public float DistanceToNextWaypoint;
    public Transform NextWaypoint;

    public bool IsPlayerCar;

    void Start()
    {
        TotalPoints = GameManager.instance._RaceManager.Waypoints.Length;

        // Yaris baslangicinda ilk pointi checkpoint olarak ayarlanmasý
        NextWaypoint = GameManager.instance._RaceManager.Waypoints[currentPointIndex].transform;
        GameManager.instance._RaceManager.AddCar(this, IsPlayerCar);
        NextPointIndex = 1;

    }

    
    void Update()
    {      
        if (NextWaypoint != null)
        {
            DistanceToNextWaypoint = Vector3.Distance(transform.position, NextWaypoint.position);
        }       
    }

    public float GetVehicleProgress()
    {
        // 2 waypoint arasinda birden cok arac olmasi durumunda birbirlerinin onlerine gecmedede siralama guncellenebilmesi icin 2 waypoint arasinda 
        // gecis noktasina daha yakin olanlari cneck edebilmek için daha hassas bir hesap iþlemi yapmak gerekmekte aksi halde sadece checkpointlerden 
        // gecis anindaki siralama baz alinir. 
        return currentPointIndex * 1000f - DistanceToNextWaypoint;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Waypoint"))
        {
            if (NextPointIndex == other.gameObject.GetComponent<WayPoint>().WaypointIndex)
            {
                currentPointIndex++;
                NextPointIndex++;
            
                if (currentPointIndex == TotalPoints)
                {                  
                    GameManager.instance._RaceManager.OnRaceFinished();
                }
                else
                {
                    if (currentPointIndex != TotalPoints)
                    {
                        NextWaypoint = GameManager.instance._RaceManager.Waypoints[currentPointIndex].transform;
                    }
                }

                // yz araclarinin checkpointleri gecince uyariyi kapatmamasi icin
                if (IsPlayerCar) 
                {                   
                    if (GameManager.instance.ReturnWarningImage.activeInHierarchy)
                    {
                        GameManager.instance.ReturnWarningImage.SetActive(false);
                    }
                }             
            }
            else
            {
                if (IsPlayerCar)
                {
                    //Debug.Log("Wrong Way, Turn Back!");
                    GameManager.instance.ReturnWarningImage.SetActive(true);
                }
               
            }
        }       
    }
}
