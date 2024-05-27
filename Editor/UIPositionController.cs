using System;
using UnityEngine;

public class UIPositionController : MonoBehaviour
{
    [Header("Unit width : Screen.width / 50, height : Screen.height / 50")]
    [SerializeField] private UIPosition[] uiArr;
    void Start()
    {
        Invoke("RePositionUI", 1f);
    }
    void RePositionUI()
    {
        float widthUnit = Screen.width / 50f;
        float heightUnit = Screen.height / 50f;

        foreach (UIPosition ui in uiArr)
        {
            ui.target.position = new Vector3(ui.widthInt * widthUnit, ui.heightInt * heightUnit, 0);
        }
    }
}
[System.Serializable]
public class UIPosition
{
    public Transform target;
    public int widthInt;
    public int heightInt;
}