using UnityEngine;

public class LoadedPlace : MonoBehaviour
{
    public static Transform gltfsPlace;
    public static Transform lightsPlace;

    private void Awake()
    {
        Transform loadedObjects = FindTransform.FindSceneRoot("LoadedObjects");
        gltfsPlace = FindTransform.FindChild(loadedObjects, "GLTFs");
        lightsPlace = FindTransform.FindChild(loadedObjects, "Lights");
    }
}
