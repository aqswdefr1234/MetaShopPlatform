using System.Collections.Generic;
using UnityEngine;

public class FillCollection
{
    public static void FillComponent<T>(Transform target, List<T> list) where T : UnityEngine.Component
    {
        list.Add(target.GetComponent<T>());
        foreach (Transform child in target) FillComponent<T>(child, list);
    }
    public static void FillTransform(Transform target, List<Transform> list)
    {
        list.Add(target.transform);
        foreach (Transform child in target) FillTransform(child, list);
    }
    public static void FillTransformHierarchy(Transform target, int floor, List<(int, Transform)> list)
    {
        list.Add((floor, target.transform));
        foreach (Transform child in target) FillTransformHierarchy(child, floor + 1, list);
    }
}
