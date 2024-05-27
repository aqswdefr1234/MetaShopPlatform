using UnityEngine;

public class Disappearance : MonoBehaviour
{
    void Start()
    {
        Invoke("Disappear", 15f);
    }
    void Disappear()
    {
        Destroy(gameObject);
    }
}
