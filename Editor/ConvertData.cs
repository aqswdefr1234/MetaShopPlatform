using System.IO;
using UnityEngine;

public class ConvertData
{
    public static (Vector3, Vector3, Vector3) TransToVec(Transform target)
    {
        Vector3 pos = target.localPosition;
        Vector3 rot = target.localEulerAngles;
        Vector3 size = target.localScale;
        return (pos, rot, size);
    }
    public static void VecToTrans(Transform target, (Vector3, Vector3, Vector3) item)
    {
        target.localPosition = item.Item1;
        target.localEulerAngles = item.Item2;
        target.localScale = item.Item3;
    }
}
