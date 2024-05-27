using UnityEngine;

public class RealtimeLightController : MonoBehaviour
{
    [SerializeField] private GameObject pointLightPrefab;
    [SerializeField] private GameObject spotLightPrefab;

    public void CreatePointLight() 
    {
        Transform lightsTrans = LoadedPlace.lightsPlace;
        if (lightsTrans.childCount > 29) return;
        Instantiate(pointLightPrefab, lightsTrans).transform.name = $"Point{lightsTrans.childCount}";
    }
    public void CreateSpotLight()
    {
        Transform lightsTrans = LoadedPlace.lightsPlace;
        if (lightsTrans.childCount > 29) return;
        Instantiate(spotLightPrefab, lightsTrans).transform.name = $"Spot{lightsTrans.childCount}"; ;
    }
}
