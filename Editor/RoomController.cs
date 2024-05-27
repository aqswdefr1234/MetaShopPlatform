using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using UnityEngine;
using LightmapAnalysis;
using UnityEngine.Networking;
using TMPro;
using System.Linq;

public class RoomController : MonoBehaviour
{
    public static string currentPath = "";

    [SerializeField] private Transform room;
    [SerializeField] private Transform roomPanel;
    [SerializeField] private Transform btnPrefab;

    string roomPath;
    Transform roomContent; Button refreshBtn; Button downloadBtn;
    LightmapAnalyzer lightmapAnalyzer;

    void Start()
    {
        CreateFastFolder();
        lightmapAnalyzer = LightmapAnalyzer.DefaultInstance;
        roomPath = Path.Combine(DataController.defaultPath, "LightData", "RoomData");
        ConnectTransform();
    }
    void CreateFastFolder()
    {
        string path = Path.Combine(UnityEngine.Application.dataPath, "../", "FastFolder");
        DirectoryFileController.IsExistFolder(path);
    }
    void ConnectTransform()
    {
        roomContent = FindTransform.FindChild(roomPanel, "Content");
        refreshBtn = FindTransform.FindChild(roomPanel, "RefreshButton").GetComponent<Button>();
        downloadBtn = FindTransform.FindChild(roomPanel, "LightmapDownButton").GetComponent<Button>();

        refreshBtn.onClick.AddListener(() => LoadFolder(roomPath));
        downloadBtn.onClick.AddListener(() => { StartCoroutine(DownloadMap()); });
        LoadFolder(roomPath);
    }
    void LoadFolder(string path)//������ ���ų�, �ڷΰ��� ��ư�� ������ ����ȴ�.
    {
        ClearView();
        string[] files = Directory.GetFiles(roomPath);
        InstantiateButton(files);
    }
    IEnumerator DownloadMap()
    {
        string listUrl = "https://www.ksjdatadomain.p-e.kr/maplist";
        string localPath = Path.Combine(DataController.defaultPath, "LightData", "RoomData");

        //����Ʈ ��û
        UnityWebRequest www = UnityWebRequest.Get(listUrl);
        yield return www.SendWebRequest();
        string json = www.downloadHandler.text;
        string newjson = json.Substring(2, json.Length - 4);//���� ["  ��  ���� "]  �߶󳻱�
        string[] fileArr = newjson.Split("\",\"");

        //�������ϰ� ��
        string[] localFilesPath = Directory.GetFiles(localPath);
        string[] localFilesName = new string[localFilesPath.Length];
        for (int i = 0; i < localFilesPath.Length; i++)
            localFilesName[i] = Path.GetFileName(localFilesPath[i]);

        //���� ���� ���
        string[] difference = fileArr.Except(localFilesName).ToArray();

        if (difference.Length == 0)
        {
            Notification.notiList.Add ("All files have already been downloaded.");
            yield break;
        }
            
        //���� ���� ��û
        foreach (string fileName in difference)
        {
            UnityEngine.Debug.Log(fileName);
            string mapDownUrl = Path.Combine("https://www.ksjdatadomain.p-e.kr/RoomLightMap", fileName);
            string filePath = Path.Combine(localPath, fileName);
            www = UnityWebRequest.Get(mapDownUrl);
            www.downloadHandler = new DownloadHandlerFile(filePath);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("Error downloading: " + www.error);
                continue;
            }
        }
    }
    void InstantiateButton(string[] files)
    {
        foreach (string file in files)
        {
            Transform btn = Instantiate(btnPrefab, roomContent);
            btn.GetComponentInChildren<TMP_Text>().text = Path.GetFileName(file);
            string fileName = Path.Combine("RoomData", Path.GetFileName(file));
            btn.GetComponent<Button>().onClick.AddListener(() => LoadBakedMap(file, fileName));
        }
    }
    public void LoadBakedMap(string filePath, string fileName)
    {
        if (room.GetComponent<PathToBeLoaded>().paths.Contains(fileName))
        {
            lightmapAnalyzer.ChangeLightmap(room, fileName);
            currentPath = filePath;
            return;
        }
        room.GetComponent<PathToBeLoaded>().paths.Add(fileName);
        lightmapAnalyzer.Import();
        lightmapAnalyzer.ChangeLightmap(room, fileName);
        currentPath = filePath;
    }
    void ClearView()
    {
        foreach (Transform btn in roomContent) Destroy(btn.gameObject);
    }
    public void InitializeBakedData() 
    {
        room.GetComponent<PathToBeLoaded>().paths.Remove("bakedData");
        lightmapAnalyzer.loadedDataDict.Remove("bakedData");
    }
    public void LoadJson(string key, string json)
    {
        if (room.GetComponent<PathToBeLoaded>().paths.Contains(key))
        {
            lightmapAnalyzer.ChangeLightmap(room, key);
            return;
        }  
        room.GetComponent<PathToBeLoaded>().paths.Add(key);
        lightmapAnalyzer.SetDataDictionary(key, json);
        lightmapAnalyzer.Import();
    }
}
