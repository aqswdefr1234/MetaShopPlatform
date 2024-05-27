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
        StartCoroutine(RequestFile(RoomFileDownload.roomFileName));//roomFileName���� ����ũ����� ���� �������*/
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
                    // ����Ʈ �迭�� ���̸� �н��ϴ�.
                    byte[] lengthBuffer = new byte[sizeof(int)];
                    int bytesRead = decompressionStream.Read(lengthBuffer, 0, sizeof(int));
                    if (bytesRead == 0)
                        break; // ������ ���� �����ϸ� �����մϴ�.
                    if (bytesRead != sizeof(int))
                    {
                        Debug.LogError("Failed to read the length of the byte array.");
                        return;
                    }

                    // ����Ʈ �迭�� ���̸� �����ɴϴ�.
                    int length = BitConverter.ToInt32(lengthBuffer, 0);

                    // ���� ����Ʈ �迭�� �н��ϴ�.
                    byte[] byteArray = new byte[length];
                    bytesRead = decompressionStream.Read(byteArray, 0, length);
                    if (bytesRead != length)
                    {
                        Debug.LogError("Failed to read the byte array.");
                        return;
                    }

                    // ����Ʈ �迭�� ����Ʈ�� �߰��մϴ�.
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
        else Debug.LogError("glTF �ε� ����!");
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
                        //���̰� int������ �ۼ��Ǿ� �����Ƿ� 4����Ʈ
                        byte[] lengthBuffer = new byte[sizeof(int)];
                        //decompressionStream ���� lengthBuffer�� 4����Ʈ ��ŭ ����
                        int bytesRead = decompressionStream.Read(lengthBuffer, 0, sizeof(int));
                        //����Ʈ �迭�� ������ Ǯ�� ����Ʈ ��. ��Ʈ���� ���� ������ ��쿡�� 0 �Ǵ� ���� ����Ʈ ���� ��ȯ
                        //��, 0�̸� ���� ���� ����. 4�� �ƴ϶�� ���� ���� ����Ʈ �迭�� ���� ���� �ƴ� �ٸ� ���� ���� ��.
                        if (bytesRead == 0)
                            break;
                        if (bytesRead != sizeof(int))//4
                        {
                            Debug.LogError("Failed to read the length of the byte array.");
                            return;
                        }
                        //�ǹٸ� ���̶�� 4����Ʈ ���� ���� �о� ���� �� ����
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
            int fileSize = (int)Math.Truncate((double)info.Length / (1024 * 1024));//�Ҽ��� ����
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
