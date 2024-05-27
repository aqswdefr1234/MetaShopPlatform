using System.Collections;
using UnityEngine;

public class CoroutineManager : MonoBehaviour
{
    public static CoroutineManager instance = null;
    public static CoroutineManager Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("CoroutineManager").AddComponent<CoroutineManager>();
            return instance;
        }
    }
    //MonoBehaviour�� ���� ��ũ��Ʈ�� IEnumerator�� �����Ű�� ���� �޼ҵ�
    public void RunCoroutine(IEnumerator method)
    {
        if (method != null)
            StartCoroutine(method);
    }
    //���� IEnumerator���� ���� �����ϴ� �޼ҵ�. �ڵ带 �����ϰ� �ϱ� ���� �޼ҵ�
    public void StartSyncCoroutines(IEnumerator[] methodArr)
    {
        StartCoroutine(OperateSync(methodArr));
    }
    IEnumerator OperateSync(IEnumerator[] methodArr)
    {
        for (int i = 0; i < methodArr.Length; i++)
        {
            if (methodArr[i] == null) continue;
            yield return StartCoroutine(methodArr[i]);
        }
    }
}
