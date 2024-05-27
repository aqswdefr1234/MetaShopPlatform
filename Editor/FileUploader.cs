using System.IO;
using UnityEngine.Networking;
using System.Collections;

public class FileUploader
{
    string serverUrl = "https://www.ksjdatadomain.p-e.kr/upload/"; // ������ ������ ���� URL�� ������ �����ϼ���.
    string localPath = Path.Combine(DataController.defaultPath, "CloudFolder", "storagefile"); // ������ ������ ��θ� ������ �����ϼ���.
    
    public IEnumerator UploadFile(string uploadedName)
    {
        if (uploadedName == "") { yield break; }
        string filePath = localPath;
        string url = Path.Combine(serverUrl, uploadedName);
        WaitingPanel.Instance.PanelStart(1, new string[] { "Uploading" });

        // ������ �б� ���� FileStream ����
        byte[] fileBytes = File.ReadAllBytes(filePath);

        // UnityWebRequest�� �����Ͽ� ����Ʈ ������ ����
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(fileBytes);
        request.SetRequestHeader("Content-Type", "application/octet-stream");

        // ��û ���� �� ���� ���
        yield return request.SendWebRequest();

        // ��û�� �Ϸ�Ǿ����� Ȯ��
        if (request.result != UnityWebRequest.Result.Success)
        {
            Notification.notiList.Add($"Failed: {request.error}");
        }
        else
        {
            Notification.notiList.Add($"Success!");
        }

        WaitingPanel.Instance.CompleteTask();
    }
}
