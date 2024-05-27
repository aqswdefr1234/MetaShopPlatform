using System.Collections;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using System.IO.Compression;
using System.IO;
using UnityEngine;
using GLTFast;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Networking;
using LightmapAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine.XR;

public class WebDataController : MonoBehaviour
{
    List<byte[]> bytesList = new List<byte[]>();
    [SerializeField] private Transform foundationPrefab;
    [SerializeField] private Transform pointPrefab;
    [SerializeField] private Transform spotPrefab;
    [SerializeField] private GameObject watingPanel;
    Transform loadedObjects, gltfGround, lightGround;
    MeshMerger merger = new MeshMerger();

    void Start()
    {
        CreateFolder();
        loadedObjects = FindTransform.FindSceneRoot("LoadedObjects");
        gltfGround = FindTransform.FindChild(loadedObjects, "GLTFs");
        lightGround = FindTransform.FindChild(loadedObjects, "Lights");
        Debug.Log(RoomFileDownload.roomFileName);
        StartCoroutine(RequestFile(RoomFileDownload.roomFileName));//roomFileName에는 파일크기까지 같이 들어있음*/
    }
    void CreateFolder()
    {
        string createPath = Path.Combine(DataController.defaultPath, "LightData", "RoomData");
        DirectoryFileController.IsExistFolder(createPath);

        createPath = Path.Combine(DataController.defaultPath, "BackUp");
        DirectoryFileController.IsExistFolder(createPath);

        createPath = Path.Combine(DataController.defaultPath, "TransformData");
        DirectoryFileController.IsExistFolder(createPath);

        createPath = Path.Combine(DataController.defaultPath, "TemporaryFolder");
        DirectoryFileController.IsExistFolder(createPath);

        createPath = Path.Combine(DataController.defaultPath, "CloudFolder");
        DirectoryFileController.IsExistFolder(createPath);
    }
    public void LoadCompressData(string filePath)
    {
        TaskCallBack.RunTaskMain(() =>
        {
            LoadCompressFile(filePath);
        },
        () =>
        {
            LoadFile();
        });
    }
    void LoadCompressFile(string filePath)
    {
        bytesList.Clear();
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
        WaitingPanel.Instance.CompleteTask();
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
        for (int i = 0; i < count; i++)
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
        LightmapAnalyzer lightmapAnalyzer = LightmapAnalyzer.DefaultInstance;
        Transform room = GameObject.FindWithTag("Room").transform;
        room.GetComponent<PathToBeLoaded>().paths.Add("Web:current");

        string json = Encoding.UTF8.GetString(bytes);
        lightmapAnalyzer.SetDataDictionary("Web:current", json);
        lightmapAnalyzer.Import();
    }
    void ImportGLTFData(List<byte[]> list, List<Transform> transList, int startIndex)
    {
        int allCount = list.Count - startIndex;
        WaitingPanel.Instance.PanelStart(allCount, WaitingPanel.Instance.RepeatItem(allCount, "Loading Mesh"));
        for (int i = startIndex; i < list.Count; i++) ImportGLTF(list[i], transList[i - startIndex]);
        bytesList.Clear();
    }
    async void ImportGLTF(byte[] data, Transform target)
    {
        var gltf = new GltfImport();
        bool success = await gltf.LoadGltfBinary(data, null);
        if (success) await gltf.InstantiateMainSceneAsync(target);
        else Debug.LogError("glTF 로드 실패!");
        merger.MergeMesh(target);
        WaitingPanel.Instance.CompleteTask();
    }

#if UNITY_WEBGL
    IEnumerator RequestFile(string fileData)
    {
        WaitingPanel.Instance.PanelStart(2, new string[] { "Loading Web file", "DeCompressing" });
        string[] fileNameSize = fileData.Split(" : ");
        string fileName = fileNameSize[0];
        int fileMB = Convert.ToInt32(fileNameSize[1]);
        string url = Path.Combine("https://www.ksjdatadomain.p-e.kr/Room", fileName);


        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error downloading: " + www.error);
                yield break;
            }
            byte[] compressedData = www.downloadHandler.data;
            WaitingPanel.Instance.CompleteTask();

            LoadCompressWebGL(compressedData);
        }
    }
    public void LoadCompressWebGL(byte[] compressedData)
    {
        DeCompressWebGL(compressedData);
        WaitingPanel.Instance.CompleteTask();

        LoadFile();
    }
    void DeCompressWebGL(byte[] compressedData)
    {
        try
        {
            using (MemoryStream compressedMemoryStream = new MemoryStream(compressedData))
            {
                using (GZipStream decompressionStream = new GZipStream(compressedMemoryStream, CompressionMode.Decompress))
                {
                    while (true)
                    {
                        //길이가 int형으로 작성되어 있으므로 4바이트
                        byte[] lengthBuffer = new byte[sizeof(int)];
                        //decompressionStream 에서 lengthBuffer에 4바이트 만큼 저장
                        int bytesRead = decompressionStream.Read(lengthBuffer, 0, sizeof(int));
                        //바이트 배열에 압축이 풀린 바이트 수. 스트림의 끝에 도달한 경우에는 0 또는 읽은 바이트 수가 반환
                        //즉, 0이면 파일 끝에 도달. 4도 아니라면 다음 오는 바이트 배열의 길이 값이 아닌 다른 값을 읽은 것.
                        if (bytesRead == 0)
                            break;
                        if (bytesRead != sizeof(int))//4
                        {
                            Debug.LogError("Failed to read the length of the byte array.");
                            return;
                        }
                        //옳바른 값이라면 4바이트 에서 값을 읽어 길이 값 추출
                        int length = BitConverter.ToInt32(lengthBuffer, 0);
                        byte[] byteArray = new byte[length];
                        bytesRead = decompressionStream.Read(byteArray, 0, length);
                        if (bytesRead != length)
                        {
                            Debug.LogError("Failed to read the byte array.");
                            return;
                        }

                        bytesList.Add(byteArray);
                    }
                }
            }

        }
        catch (Exception ex) { Debug.LogException(ex); }
    }
#else
    IEnumerator RequestFile(string fileData)
    {
        WaitingPanel.Instance.PanelStart(2, new string[] { "Loading Web file", "DeCompressing" });
        
        string[] fileNameSize = fileData.Split(" : ");
        string fileName = fileNameSize[0];
        int fileMB = Convert.ToInt32(fileNameSize[1]);
        string path = Path.Combine(DataController.defaultPath, "CloudFolder", fileName);
        string url = Path.Combine("https://www.ksjdatadomain.p-e.kr/Room", fileName);

        if (File.Exists(path))
        {
            FileInfo info = new FileInfo(path);
            int fileSize = (int)Math.Truncate((double)info.Length / (1024 * 1024));//소수점 버림
            if(fileMB == fileSize)
            {
                WaitingPanel.Instance.CompleteTask();
                LoadCompressData(path);
                yield break;
            }
        }

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.downloadHandler = new DownloadHandlerFile(path);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error downloading: " + www.error);
            yield break;
        }
        WaitingPanel.Instance.CompleteTask();
        LoadCompressData(path);
    }
#endif
}
