using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using System.IO.Compression;
using System.IO;
using UnityEngine;
using GLTFast;
using System.Threading.Tasks;
using System;
using System.Text;
using LightmapAnalysis;
public class CompressController : MonoBehaviour
{   
    //0: objectTrans, 1:lightTrans, 2: bakedData
    
    public List<byte[]> bytesList = new List<byte[]>();

    [SerializeField] private Transform foundationPrefab;
    [SerializeField] private Transform pointPrefab;
    [SerializeField] private Transform spotPrefab;
    [SerializeField] private Transform saveLoadPanel;

    Transform loadedObjects, gltfGround, lightGround;
    string defaultPath = DataController.defaultPath;
    string uploadedName = "";
    int gltfStartIndex = 0;

    void Start()
    {
        loadedObjects = FindTransform.FindSceneRoot("LoadedObjects");
        gltfGround = FindTransform.FindChild(loadedObjects, "GLTFs");
        lightGround = FindTransform.FindChild(loadedObjects, "Lights");
    }
    public void UploadWebBtn(string upFileName)
    {
        uploadedName = upFileName;
        SaveCompressFile();
    }
    public void PreviewWeb()
    {
        LoadCompressData();
    }
    void SaveCompressFile()
    {
        string filePath = Path.Combine(defaultPath, "CloudFolder", "storagefile");
        WaitingPanel.Instance.PanelStart(1, new string[] {"Compressing File"});
        
        List<string> pathList = new List<string>();
        SetPath(pathList);

        TaskCallBack.RunTaskMain(() => 
        {
            ReadBytesToFiles(pathList.ToArray());
        },
        () => 
        {
            SaveCompressedFile(bytesList.ToArray(), filePath);
        });
    }
    void ReadBytesToFiles(string[] pathArr)
    {
        bytesList.Clear();

        string path = Path.Combine(defaultPath, "TransformData", "transforms");
        string json = File.ReadAllText(path);

        TransformData preData = JsonUtility.FromJson<TransformData>(json);
        TransformData newData = new TransformData(preData.nameArray.Length);

        byte[][] byteArrArr = new byte[pathArr.Length][];
        string[] newGltfPathArr = new string[pathArr.Length - gltfStartIndex];
        for (int i = 0; i < pathArr.Length; i++)
        {
            RelocateTrans(preData, newData, pathArr[i], i - gltfStartIndex);
            byteArrArr[i] = File.ReadAllBytes(pathArr[i]);
        }

        //TransData추가
        string newJson = JsonUtility.ToJson(newData);//Encoding.UTF8.GetBytes
        byte[] transBytes = Encoding.UTF8.GetBytes(newJson);
        bytesList.Add(transBytes);

        //byte[][] 할당
        bytesList.AddRange(byteArrArr);
    }
    void SetPath(List<string> list) 
    {
        string lightTransPath = Path.Combine(defaultPath, "LightData", "LightTransData");
        string bakedPath = Path.Combine(defaultPath, "LightData", "bakedData");

        list.Add(lightTransPath); list.Add(bakedPath);

        gltfStartIndex = list.Count;
        list.AddRange(Directory.GetFiles(defaultPath));
    }
    //path배열 순서대로 데이터를 정렬
    void RelocateTrans(TransformData preData, TransformData newData, string path, int gltfIndex)
    {
        string extens = Path.GetExtension(path);
        if (extens != ".glb") return;

        string name = Path.GetFileNameWithoutExtension(path);
        int index = Array.IndexOf(preData.nameArray, name);
        if (index == -1) return;

        newData.nameArray[gltfIndex] = preData.nameArray[index];
        newData.posArray[gltfIndex] = preData.posArray[index];
        newData.rotArray[gltfIndex] = preData.rotArray[index];
        newData.scaleArray[gltfIndex] = preData.scaleArray[index];
    }
    public void SaveCompressedFile(byte[][] bytesArrArr, string compressedFile)
    {
        FileUploader fileUploader = new FileUploader();
        TaskCallBack.RunTaskMain(() => 
        {
            using (FileStream compressedFileStream = File.Create(compressedFile))
            {
                using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                {
                    // 각 바이트 배열의 길이와 바이트 배열을 압축 스트림에 쓰기를 수행합니다.
                    foreach (byte[] byteArray in bytesArrArr)
                    {
                        //데이터 저장 전 바이트 배열 길이 저장
                        compressionStream.Write(BitConverter.GetBytes(byteArray.Length), 0, sizeof(int));
                        compressionStream.Write(byteArray, 0, byteArray.Length);
                    }
                }
                WaitingPanel.Instance.CompleteTask();
            }
        },
        () => 
        {
            StartCoroutine(fileUploader.UploadFile(uploadedName));
        });
    }

    //-----------------Load------------------
    public void LoadCompressData()
    {
        LoadCompressFile();
    }
    void LoadCompressFile()
    {
        bytesList.Clear();
        TaskCallBack.RunTaskMain(() => 
        {
            string filePath = Path.Combine(DataController.defaultPath, "CloudFolder", "storagefile");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning("File does not exist: " + filePath);
                return;
            }

            using (FileStream compressedFileStream = File.OpenRead(filePath))
            {
                using (GZipStream decompressionStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
                {
                    while (true)
                    {
                        // 바이트 배열의 길이를 읽습니다.
                        byte[] lengthBuffer = new byte[sizeof(int)];
                        int bytesRead = decompressionStream.Read(lengthBuffer, 0, sizeof(int));
                        if (bytesRead == 0)
                            break; // 파일의 끝에 도달하면 종료합니다.
                        if (bytesRead != sizeof(int))
                        {
                            Debug.LogError("Failed to read the length of the byte array.");
                            return;
                        }

                        // 바이트 배열의 길이를 가져옵니다.
                        int length = BitConverter.ToInt32(lengthBuffer, 0);

                        // 실제 바이트 배열을 읽습니다.
                        byte[] byteArray = new byte[length];
                        bytesRead = decompressionStream.Read(byteArray, 0, length);
                        if (bytesRead != length)
                        {
                            Debug.LogError("Failed to read the byte array.");
                            return;
                        }

                        // 바이트 배열을 리스트에 추가합니다.
                        bytesList.Add(byteArray);
                    }
                }
            }
        },
        () => 
        {
            LoadFile();
        });
    }
    public void LoadFile()
    {
        List<Transform> gltfTransform = new List<Transform>();

        int startIndex = 3;////0: objectTrans, 1:lightTrans, 2: bakedData, 3 ~ : gltfData
        CreateObject(bytesList[0], gltfTransform);
        CreateRealLight(bytesList[1]);
        ImportBakedLightMap(bytesList[2]);
        ImportGLTFData(bytesList, gltfTransform, startIndex);
    }
    void CreateObject(byte[] bytes, List<Transform> list)
    {
        string json = Encoding.UTF8.GetString(bytes);
        TransformData data = JsonUtility.FromJson<TransformData>(json);

        int count = data.nameArray.Length;
        for(int i = 0; i < count; i++)
        {
            Transform foundation = Instantiate(foundationPrefab, gltfGround);
            foundation.name = data.nameArray[i];
            foundation.localPosition = data.posArray[i];
            foundation.localEulerAngles = data.rotArray[i];
            foundation.localScale = data.scaleArray[i];

            list.Add(foundation);
        }
    }
    void CreateRealLight(byte[] bytes)
    {
        string json = Encoding.UTF8.GetString(bytes);
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
    void ImportBakedLightMap(byte[] bytes)
    {
        try
        {
            LightmapAnalyzer lightmapAnalyzer = LightmapAnalyzer.DefaultInstance;
            Transform room = GameObject.FindWithTag("Room").transform;
            room.GetComponent<PathToBeLoaded>().paths.Add("Web:current");

            string json = Encoding.UTF8.GetString(bytes);
            lightmapAnalyzer.SetDataDictionary("Web:current", json);
            lightmapAnalyzer.Import();
        }
        catch (Exception e) { Debug.LogException(e); }
    }
    void ImportGLTFData(List<byte[]> list, List<Transform> transList, int startIndex)
    {
        for (int i = startIndex; i < list.Count; i++) ImportGLTF(list[i], transList[i - startIndex]);
    }
    async void ImportGLTF(byte[] data, Transform target)
    {
        var gltf = new GltfImport();
        bool success = await gltf.LoadGltfBinary(data, null);
        if (success) await gltf.InstantiateMainSceneAsync(target);
        else Debug.LogError("glTF 로드 실패!");
    }
}