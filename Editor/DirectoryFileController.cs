using System.IO;
public class DirectoryFileController
{
    public static void IsExistFolder(string path)
    {
        if (Directory.Exists(path) == false)
            Directory.CreateDirectory(path);
    }
    public static (string, FileSystemInfo[]) ReturnDirFile(string folderPath)//���ϻӸ� �ƴ϶� ���� ������ ������
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
        if (!directoryInfo.Exists) return ("", null);

        string folderName = directoryInfo.Name;
        FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos();
        return (folderName, fileSystemInfos);
    }
    public static void EmptyFolder()
    {
        string backPath = Path.Combine(DataController.defaultPath, "BackUp");
        string[] preFiles = Directory.GetFiles(backPath);

        //������ backup ���� ����
        foreach (string preFile in preFiles) File.Delete(preFile);

        //���� �Ǿ��ִ� ���� �ű��
        string[] targetFiles = Directory.GetFiles(DataController.defaultPath);
        foreach (string file in targetFiles)
        {
            string newPath = Path.Combine(backPath, Path.GetFileName(file));
            File.Move(file, newPath);
        }
    }
    public static void MoveToCurrentFolder()
    {
        string temPath = Path.Combine(DataController.defaultPath, "TemporaryFolder");
        string[] temFiles = Directory.GetFiles(temPath);
        foreach (string file in temFiles)
        {
            string newPath = Path.Combine(DataController.defaultPath, Path.GetFileName(file));
            File.Move(file, newPath);
        }
    }
    
    public static void MoveTemporary(string[] filePaths, string[] newNames)
    {
        string path = Path.Combine(DataController.defaultPath, "TemporaryFolder");
        IsExistFolder(path);
        if(newNames == null)
        {
            foreach (string file in filePaths)
            {
                string newPath = Path.Combine(path, Path.GetFileName(file));
                File.Copy(file, newPath, true);
            }
            return;
        }
        //Not null
        if(filePaths.Length != newNames.Length) 
        {
            UnityEngine.Debug.LogError("filePaths.Length != newNames.Length");
            return; 
        }
        for(int i = 0; i < filePaths.Length; i++)
        {
            string newPath = Path.Combine(path, newNames[i]);
            File.Copy(filePaths[i], newPath, true);
        }
    }
}
