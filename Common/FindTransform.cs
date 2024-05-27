using UnityEngine.SceneManagement;
using UnityEngine;

public class FindTransform
{
    public static Transform FindSceneRoot(string rootName)
    {
        foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (rootName == rootObject.transform.name) return rootObject.transform;
        }
        return null; // 찾지 못한 경우
    }
    public static Transform FindChild(Transform target, string name)
    {
        Transform[] findTrans = new Transform[1];
        Find(target, name, findTrans);
        return findTrans[0];
    }
    public static void Find(Transform target, string name, Transform[] findTrans)
    {
        if (target.name == name) { findTrans[0] = target; return; }
        foreach (Transform child in target)
        {
            Find(child, name, findTrans);
        }
    }
}