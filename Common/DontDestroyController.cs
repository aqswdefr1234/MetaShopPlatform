using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroyController : MonoBehaviour
{
    [SerializeField] private Transform[] targets;
    public static DontDestroyController Instance = null;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }
        DestroyArray();
    }
    void Start()
    {
        foreach(Transform target in targets) DontDestroyOnLoad(target.gameObject);
    }
    void DestroyArray()
    {
        Destroy(gameObject);
        foreach (Transform target in targets) Destroy(target.gameObject);
    }
}
