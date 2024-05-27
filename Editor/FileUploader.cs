using System.IO;
using UnityEngine.Networking;
using System.Collections;

public class FileUploader
{
    string serverUrl = "https://www.ksjdatadomain.p-e.kr/upload/"; // 파일을 전송할 서버 URL을 적절히 수정하세요.
    string localPath = Path.Combine(DataController.defaultPath, "CloudFolder", "storagefile"); // 전송할 파일의 경로를 적절히 수정하세요.
    
    public IEnumerator UploadFile(string uploadedName)
    {
        if (uploadedName == "") { yield break; }
        string filePath = localPath;
        string url = Path.Combine(serverUrl, uploadedName);
        WaitingPanel.Instance.PanelStart(1, new string[] { "Uploading" });

        // 파일을 읽기 위한 FileStream 열기
        byte[] fileBytes = File.ReadAllBytes(filePath);

        // UnityWebRequest를 생성하여 바이트 데이터 전송
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(fileBytes);
        request.SetRequestHeader("Content-Type", "application/octet-stream");

        // 요청 수행 및 응답 대기
        yield return request.SendWebRequest();

        // 요청이 완료되었는지 확인
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
