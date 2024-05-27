using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;
using GLTFast;

public class DataController : MonoBehaviour
{
#if UNITY_ANDROID
    public static string defaultPath = Path.Combine(Application.persistentDataPath, "MetaShopData");
#else
    public static string defaultPath = Path.Combine(UnityEngine.Application.dataPath, "../", "MetaShopData");
#endif
    string bakedMapPath, lightTransPath, objectTransPath;

    [SerializeField] private Transform foundationPrefab;
    [SerializeField] private Transform pointPrefab;
    [SerializeField] private Transform spotPrefab;
    [SerializeField] private GameObject waitPanel;
    Transform gltfGround, lightGround;

    void Awake()
    {
        bakedMapPath = Path.Combine(defaultPath, "LightData", "bakedData");
        lightTransPath = Path.Combine(defaultPath, "LightData", "LightTransData");
        objectTransPath = Path.Combine(defaultPath, "TransformData", "transforms");
    }
    void Start()
    {
        gltfGround = LoadedPlace.gltfsPlace;
        lightGround = LoadedPlace.lightsPlace;
        CreateFolder();
    }
    
    void CreateFolder()
    {
        string[] createFolders = new string[5] 
        { 
            "LightData/RoomData", "BackUp", "TransformData", "TemporaryFolder", "CloudFolder"
        };
        foreach(string path in createFolders)
        {
            string createPath = Path.Combine(defaultPath, path);
            DirectoryFileController.IsExistFolder(createPath);
        }
    }
    public void SaveScene()
    {
        SaveLights();
        SaveObjects();
    }
    public void LoadScene()
    {
        RemoveInstantiate();
        LoadLights();
        LoadObjects();
    }
    void SaveObjects()
    {
        List<(Vector3, Vector3, Vector3)> transDataList = new List<(Vector3, Vector3, Vector3)>();
        List<string> pathList = new List<string>();
        List<string> nameList = new List<string>();

        foreach (Transform child in gltfGround)
        {
            string path = child.GetComponent<GltfAsset>().Url;
            transDataList.Add(ConvertData.TransToVec(child));
            pathList.Add(path);
            nameList.Add(child.name);
        }

        WaitingPanel.Instance.PanelStart(2, new string[] { "Saving Objects", "Optimizing Texture" });
        TaskToSave(transDataList, pathList, nameList);
    }
    //Transform 데이터 Json 저장 및 GLB파일 텍스처 최적화 후 저장
    void TaskToSave(List<(Vector3, Vector3, Vector3)> transDataList, List<string> pathList, List<string> nameList)
    {
        string[] newNames = null;
        TaskCallBack.RunTaskMain(() => 
        {
            int count = transDataList.Count;
            newNames = new string[count];

            SaveTransform(transDataList.ToArray(), nameList.ToArray());
            for (int i = 0; i < count; i++)
            {
                newNames[i] = nameList[i] + Path.GetExtension(pathList[i]);
            }
            SaveFiles(pathList.ToArray(), newNames);
            WaitingPanel.Instance.CompleteTask();
        }, 
        () => 
        {
            GLBTextureOptimization optimization = transform.GetComponent<GLBTextureOptimization>();
            int count = pathList.Count;

            for (int i = 0; i < count; i++)
            {
                //디폴트경로에 존재하지 않는다면(이전에 저장돼있던 파일이 아니라면)
                if (pathList[i].IndexOf(defaultPath) == -1)
                {
                    string desPath = Path.Combine(defaultPath, newNames[i]);
                    optimization.LowTextureExport(pathList[i], desPath);
                }
            }
            WaitingPanel.Instance.CompleteTask();
        });
    }
    void SaveTransform((Vector3, Vector3, Vector3)[] items, string[] nameArr)
    {
        TransformData data = new TransformData(items, nameArr);
        string path = objectTransPath;
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);
    }
    
    void SaveFiles(string[] targetPaths, string[] newNames)
    {
        DirectoryFileController.MoveTemporary(targetPaths, newNames);
        DirectoryFileController.EmptyFolder();
        DirectoryFileController.MoveToCurrentFolder();
    }
    void SaveLights()
    {
        string lightFolder = Path.Combine(defaultPath, "LightData");
        SaveBakedLightMap(lightFolder);
        SaveRealLight(lightFolder);
    }
    void SaveBakedLightMap(string lightFolder)
    {
        string bakedPath = bakedMapPath;
        if(RoomController.currentPath != bakedPath)
            File.Copy(RoomController.currentPath, bakedPath, true);
        transform.GetComponent<RoomController>().InitializeBakedData();
    }
    void SaveRealLight(string lightFolder)
    {
        string lightTransPath = Path.Combine(lightFolder, "LightTransData");
        List<Light> lightList = new List<Light>();
        FillCollection.FillComponent<Light>(lightGround, lightList);
        LightsParser lightsData = new LightsParser(lightList);
        string json = JsonUtility.ToJson(lightsData);
        File.WriteAllText(lightTransPath, json);
    }
    
    void RemoveInstantiate()
    {
        foreach (Transform child in gltfGround) Destroy(child.gameObject);
        foreach (Transform child in lightGround) Destroy(child.gameObject);
    }
    void LoadObjects()
    {
        string[] files = null; string[] transName = null; TransformData data = null; int length = 0;
        TaskCallBack.RunTaskMain(() => 
        {
            data = LoadTransform();
            files = Directory.GetFiles(defaultPath);
            length = files.Length;
            transName = new string[length];
            for (int i = 0; i < length; i++)
            {
                transName[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
        }, 
        () => 
        {
            for (int i = 0; i < length; i++)
            {
                Transform foundation = Instantiate(foundationPrefab, gltfGround);
                foundation.GetComponent<GltfAsset>().Url = files[i];
                foundation.name = transName[i];
                (Vector3, Vector3, Vector3) item = FindTransData(data, transName[i]);
                ConvertData.VecToTrans(foundation, item);
            }
        });
    }
    TransformData LoadTransform()
    {
        string path = objectTransPath;
        string json = File.ReadAllText(path);
        TransformData data = JsonUtility.FromJson<TransformData>(json);
        return data;
    }
    (Vector3, Vector3, Vector3) FindTransData(TransformData data, string transName)
    {
        int length = data.nameArray.Length;
        for(int i = 0; i < length; i++)
        {
            if (data.nameArray[i] == transName)
                return (data.posArray[i], data.rotArray[i], data.scaleArray[i]);
        }
        return (new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0));
    }
    void LoadLights()
    {
        string lightFolder = Path.Combine(defaultPath, "LightData");
        LoadBakedLightMap();
        LoadRealLight();
    }
    void LoadBakedLightMap()
    {
        string path = bakedMapPath;
        if (!File.Exists(path)) return;
        RoomController.currentPath = path;
        transform.GetComponent<RoomController>().LoadBakedMap(path, "bakedData");
    }
    void LoadRealLight()
    {
        string path = lightTransPath;
        if (!File.Exists(path)) return;
        
        string json = File.ReadAllText(lightTransPath);
        LoadRealLightJson(json);
    }
    public void LoadRealLightJson(string json)
    {
        LightsParser lightsData = JsonUtility.FromJson<LightsParser>(json);

        int count = lightsData.lightType.Count;
        for (int i = 0; i < count; i++)
        {
            Transform newLight = null;
            if (lightsData.lightType[i] == "Spot") newLight = Instantiate(spotPrefab, lightGround);
            else if (lightsData.lightType[i] == "Point") newLight = Instantiate(pointPrefab, lightGround);

            if (newLight == null) continue;
            newLight.position = lightsData.posList[i];
            newLight.eulerAngles = lightsData.rotList[i];

            Light light = newLight.GetComponent<Light>();
            light.color = lightsData.colorList[i];
            light.intensity = lightsData.intensityList[i];
        }
    }
}
[System.Serializable]
public class TransformData
{
    public string[] nameArray;
    public Vector3[] posArray;
    public Vector3[] rotArray;
    public Vector3[] scaleArray;
    public TransformData((Vector3, Vector3, Vector3)[] items, string[] names) 
    {
        nameArray = names;

        int count = items.Length;
        posArray = new Vector3[count];
        rotArray = new Vector3[count];
        scaleArray = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            (posArray[i], rotArray[i], scaleArray[i]) = items[i];
        }
    }
    public TransformData(int count) 
    {
        nameArray = new string[count];
        posArray = new Vector3[count];
        rotArray = new Vector3[count];
        scaleArray = new Vector3[count];
    }
}
[System.Serializable]
public class LightsParser
{
    public List<string> lightType = new List<string>();
    public List<Vector3> posList = new List<Vector3>();
    public List<Vector3> rotList = new List<Vector3>();
    public List<Color32> colorList = new List<Color32>();
    public List<int> intensityList = new List<int>();
    public LightsParser(List<Light> lightList)
    {
        foreach (Light light in lightList)
        {
            if (light == null) continue;
            lightType.Add(light.type.ToString());
            posList.Add(light.transform.position);
            rotList.Add(light.transform.eulerAngles);
            colorList.Add(light.color);
            intensityList.Add((int)light.intensity);
        }
    }
}