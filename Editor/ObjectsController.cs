using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectsController : MonoBehaviour
{
    Transform gltfGround, objectsPanel;
    Transform selectedTrans; Transform camTrans;
    Transform rotationPanel, lightPanel, modelPanel, loadedLights;
    Dictionary<string, TMP_InputField> inputDict = new Dictionary<string, TMP_InputField>();
    
    void Start()
    {
        ConnectTransform();
        ConnnectButton();
    }
    void ConnectTransform()
    {
        gltfGround = LoadedPlace.gltfsPlace;
        loadedLights = LoadedPlace.lightsPlace;

        Transform canvasTrans = FindTransform.FindSceneRoot("Canvas");
        objectsPanel = FindTransform.FindChild(canvasTrans, "ObjectsPanel");
        camTrans = FindTransform.FindSceneRoot("Main Camera");

        //TMP_InputField 연결하기
        List<Transform> targetList = new List<Transform>();

        rotationPanel = FindTransform.FindChild(objectsPanel, "RotationPanel"); targetList.Add(rotationPanel);
        lightPanel = FindTransform.FindChild(objectsPanel, "LightSettingPanel"); targetList.Add(lightPanel);
        modelPanel = FindTransform.FindChild(objectsPanel, "ModelSettingPanel"); targetList.Add(modelPanel);
        Transform nameField = FindTransform.FindChild(objectsPanel, "NameInputField"); targetList.Add(nameField);
        
        foreach (Transform target in targetList) ConnectInputField(target);
    }
    void ConnnectButton()
    {
        UnityEngine.UI.Button allChangeBtn = FindTransform.FindChild(objectsPanel, "AllChangeButton").GetComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Button removeBtn = FindTransform.FindChild(objectsPanel, "RemoveButton").GetComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Button resetViewBtn = FindTransform.FindChild(objectsPanel, "ResetViewButton").GetComponent<UnityEngine.UI.Button>();
        
        allChangeBtn.onClick.AddListener(OnClick_AllChange);
        removeBtn.onClick.AddListener(OnClick_Remove);
        resetViewBtn.onClick.AddListener(OnClick_ResetView);
    }
    void ConnectInputField(Transform target)
    {
        List<TMP_InputField> list = new List<TMP_InputField>();
        FillCollection.FillComponent<TMP_InputField>(target, list);

        //Dictionary 할당
        int count = list.Count;
        for (int i = count - 1; i >= 0 ; i--)
        {
            TMP_InputField input = list[i];
            if (input == null) { list.RemoveAt(i); continue; }
            inputDict.Add(input.transform.name, input);
        }
        
        //특성별 Event 할당
        if (target.name == "NameInputField")
        {
            OnEndEditEvent(list, () => ChangeName(selectedTrans));
            return;
        }
        if (target.name == "RotationPanel")
        {
            OnEndEditEvent(list, () => ChangeRotation(selectedTrans));
            return;
        }
        if (target.name == "LightSettingPanel")
        {
            OnEndEditEvent(list, () => 
            { 
                (Color32 color, int intens) = ReadInputField();
                ChangeLight(color, intens);
            });
            return;
        }
        if (target.name == "ModelSettingPanel")
        {
            OnEndEditEvent(list, () => ChangeScale(selectedTrans));
            return;
        }
    }
    void OnEndEditEvent(List<TMP_InputField> list, Action action)
    {
        foreach (TMP_InputField input in list)
        {
            input.onEndEdit.AddListener((string newString) =>
            {
                if (selectedTrans == null) return;
                action();
            });
        }
    }
    void ChangeName(Transform target)
    {
        string newName = SameNameChanger.Change_NumInParenthesis(gltfGround, target, inputDict["NameInputField"].text);
        target.name = newName;
    }
    void ChangeRotation(Transform target)
    {
        float x = (float)Convert.ToInt32(inputDict["RotX"].text);
        float y = (float)Convert.ToInt32(inputDict["RotY"].text);
        float z = (float)Convert.ToInt32(inputDict["RotZ"].text);
        target.eulerAngles = new Vector3(x, y, z);
        inputDict["RotX"].text = x.ToString(); inputDict["RotY"].text = y.ToString(); inputDict["RotZ"].text = z.ToString();
    }
    void ChangeScale(Transform target)
    {
        float x = (float)Math.Round(Convert.ToSingle(inputDict["ScaleX"].text), 3);
        float y = (float)Math.Round(Convert.ToSingle(inputDict["ScaleY"].text), 3);
        float z = (float)Math.Round(Convert.ToSingle(inputDict["ScaleZ"].text), 3);
        target.localScale = new Vector3(x, y, z);
        inputDict["ScaleX"].text = x.ToString(); inputDict["ScaleY"].text = y.ToString(); inputDict["ScaleZ"].text = z.ToString();
    }
    void ChangeLight(Color32 color, int intens)
    {
        selectedTrans.GetComponent<Light>().color = color;
        selectedTrans.GetComponent<Light>().intensity = intens;
    }
    (Color32, int) ReadInputField()
    {
        Color32 color = new Color32();
        color.r = (byte)Convert.ToInt32(inputDict["R"].text);
        color.g = (byte)Convert.ToInt32(inputDict["G"].text);
        color.b = (byte)Convert.ToInt32(inputDict["B"].text);
        int intens = Convert.ToInt32(inputDict["Intensity"].text);
        return (color, intens);
    }
    void OnClick_AllChange()
    {
        List<Light> lightList = new List<Light>();
        FillCollection.FillComponent<Light>(loadedLights, lightList);
        (Color32 color, int intens) = ReadInputField();
        foreach (Light light in lightList)
        {
            if (light == null) continue;
            light.color = color; light.intensity = intens;
        }
    }
    void OnClick_Remove()
    {
        if (selectedTrans == null) return;
        Destroy(selectedTrans.gameObject);
    }
    void OnClick_ResetView()
    {
        camTrans.position = new Vector3(0, 0, 0);
    }
    
    //대상 타겟 변경될 시 인풋필드에 타겟 Transform 데이터 입력하기
    public void LoadData(Transform changedTrans)
    {
        if (changedTrans == null) return;
        selectedTrans = changedTrans;
        inputDict["NameInputField"].text = changedTrans.name;
        WriteInputField(changedTrans);
    }
    void WriteInputField(Transform target)
    {
        if (target.tag == "LightObject")
        {
            lightPanel.gameObject.SetActive(true);
            modelPanel.gameObject.SetActive(false);
            Light light = target.GetComponent<Light>();
            inputDict["Intensity"].text = light.intensity.ToString();
            Color32 color = light.color;
            inputDict["R"].text = color.r.ToString();
            inputDict["G"].text = color.g.ToString();
            inputDict["B"].text = color.b.ToString();
        }
        else
        {
            modelPanel.gameObject.SetActive(true);
            lightPanel.gameObject.SetActive(false);
            inputDict["ScaleX"].text = Math.Round(target.localScale.x, 3).ToString();
            inputDict["ScaleY"].text = Math.Round(target.localScale.y, 3).ToString();
            inputDict["ScaleZ"].text = Math.Round(target.localScale.z, 3).ToString();
        }
        //Rotation is Common elements
        inputDict["RotX"].text = Math.Round(target.eulerAngles.x).ToString();
        inputDict["RotY"].text = Math.Round(target.eulerAngles.y).ToString();
        inputDict["RotZ"].text = Math.Round(target.eulerAngles.z).ToString();
    }
}