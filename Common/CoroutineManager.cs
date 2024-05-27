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
    //MonoBehaviour가 없는 스크립트의 IEnumerator을 실행시키기 위한 메소드
    public void RunCoroutine(IEnumerator method)
    {
        if (method != null)
            StartCoroutine(method);
    }
    //여러 IEnumerator들을 순차 시작하는 메소드. 코드를 간결하게 하기 위한 메소드
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
