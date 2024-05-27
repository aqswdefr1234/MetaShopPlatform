using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadLocalFileController : MonoBehaviour
{
    [SerializeField] private Transform localFilePanel;
    [SerializeField] private Transform btnPrefab;

    List<string> pathList = new List<string>();
    string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    TMP_Text folderName; Button returnBtn; Transform fileContent;

    void Start()
    {
        ConnectTransform();
    }
    void ConnectTransform()
    {
        folderName = FindTransform.FindChild(localFilePanel, "FolderName").GetComponent<TMP_Text>();
        fileContent = FindTransform.FindChild(localFilePanel, "Content");
        returnBtn = FindTransform.FindChild(localFilePanel, "ReturnButton").GetComponent<Button>();
        returnBtn.onClick.AddListener(OnClick_ReturnButton);

        LoadLocalFolder(defaultPath, false);
    }
    void OnClick_ReturnButton()
    {
        if (pathList.Count <= 1) return;
        pathList.RemoveAt(pathList.Count - 1);
        LoadLocalFolder(pathList[pathList.Count - 1], true);
    }
    void LoadLocalFolder(string path, bool isReturn)//폴더에 들어가거나, 뒤로가기 버튼을 누를때 실행된다.
    {
        ClearView();
        (string dirName, FileSystemInfo[] fileSystemInfos) = DirectoryFileController.ReturnDirFile(path);
        if (!isReturn) pathList.Add(path);

        folderName.text = dirName;
        foreach (FileSystemInfo fsi in fileSystemInfos) InstantiateButton(fsi);
    }
    void InstantiateButton(FileSystemInfo fsi)
    {
        string fsiName = fsi.Name;
        if (fsi is FileInfo file)
        {
            if (file.Extension != ".glb") return;
            Color32 btnColor = new Color32(255, 255, 255, 255);//white
            CopyButton(fsiName, btnColor, () => transform.GetComponent<GLTFLoader>().LoadLocal(fsi.FullName));
        }
        else if (fsi is DirectoryInfo directory)
        {
            Color32 btnColor = new Color32(255, 255, 0, 255);//yellow
            CopyButton(fsiName, btnColor, () => LoadLocalFolder(fsi.FullName, false));
        }
    }
    void CopyButton(string fsiName, Color32 btnColor, Action action)
    {
        Transform btn = Instantiate(btnPrefab, fileContent);

        if (fsiName.Length <= 10) btn.GetComponentInChildren<TMP_Text>().text = fsiName;
        else btn.GetComponentInChildren<TMP_Text>().text = fsiName.Substring(0, 5) + "..." + fsiName.Substring(5, 4);
        btn.GetComponentInChildren<TMP_Text>().text = fsiName;
        btn.GetComponent<UnityEngine.UI.Image>().color = btnColor;
        btn.GetComponent<Button>().onClick.AddListener(() => action());//UnityAction => System.Action
    }
    void ClearView()
    {
        foreach (Transform btn in fileContent) Destroy(btn.gameObject);
    }
}
