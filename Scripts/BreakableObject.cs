using UnityEngine;

public class BreakableObject : MonoBehaviour
{

    [SerializeField] FixedJoint joint;
    [SerializeField] MeshCollider collider; 

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (joint == null)
        {
            // ilgili objeyi detach yapar, kopan tamponun vs. parenti takip etmemesi icin
            transform.SetParent(null);
            collider.enabled = true;
            enabled = false; // scripti pasifleþtirir.
        }
    }
}
