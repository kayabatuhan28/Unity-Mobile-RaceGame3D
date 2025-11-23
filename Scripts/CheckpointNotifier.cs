using UnityEngine;

public class CheckpointNotifier : MonoBehaviour
{

    // Checkpoint direklerinde kullanilan script, Arac gecis aninda collider carpismasi ile bir takim islemleri tetikler

    public int CheckPointIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerCar"))
        {
            // Dogru checkpointten ilk kez gecmekte
            if (CheckPointIndex == GameManager.instance._CheckPointManager.NextCheckPointIndex)
            {             
                GameManager.instance._CheckPointManager.OnCheckpointPassed();
            }
            else
            {
                // Araba daha once bu checkpointten gecti - Ekstra bir uyari eklenebilir vs.
               
            }
        }
    }
}
