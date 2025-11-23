using UnityEngine;

public class PreferencesObject : MonoBehaviour
{

    // Sahneler arasinda tasinmasini istedigimiz degerleri tutan tasimaya yardimci script.Ayarlar veya secilen aracin bir sonraki mapte spawnlanabilmesi vs.
    // gibi bazi datalari leveller arasi tasir
    public int SelectedCarIndex;
    public int SelectedRaceModeIndex;
   

    // ------------------------------------
    public float GameSound;
    public float MenuVolume;
    public float SfxVolume;
    public int GraphicsPreference;
    public bool FpsPreference;

    // ------------------------------------
    public AudioSource menuSound;

    public static PreferencesObject instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    
}
