using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;

//Glb ���� �� ���� �ػ� �ؽ�ó ���̴� ��ũ��Ʈ
public class GLBTextureOptimization : MonoBehaviour
{
    public void LowTextureExport(string targetPath, string destination)
    {
        byte[] glbData = File.ReadAllBytes(targetPath);
        ChangeGLBFile(glbData, destination);
    }


    //Glb ����
    //header       : 12 bytes

    //Json Length  : 4  bytes
    //"JSON" Str   : 4  bytes
    //Json + Padding(0x20)

    //Binary Length: 4  bytes
    //"BIN" + null : 4  bytes
    //Binary

    //���� ���� �������� �׻� 4����Ʈ ����� �����ؾ��Ѵ�.
    //�ش� ���� �䰡 ���� ��, ������ + ���̰� 4�� ����� �ȵȴٸ� ���ʿ� �е��� �߰��ؾ��Ѵ�. null(0x00)
    //�׷��� ���� ���� �䰡 4�� ��� �������� ������.

    public void ChangeGLBFile(byte[] glbData, string destination)
    {
        string desPath = destination;
        
        uint jsonChunkLength = BitConverter.ToUInt32(glbData, 12);
        string jsonStr = Encoding.UTF8.GetString(glbData, 20, (int)jsonChunkLength);

        // �Ľ�
        JObject glbJson = JObject.Parse(jsonStr);
        JArray images = (JArray)glbJson["images"];
        if (images == null || images.Count == 0) return;

        //�������� �̹��� ���ۺ� ������ : <�ε���, (����, ������)>
        Dictionary<int, (uint, uint)> oriBufferViewDict = new Dictionary<int, (uint, uint)>();
        List<int> jpgIndex = new List<int>();
        AllocateList(oriBufferViewDict, glbJson, jpgIndex);

        //���ο� �ؽ�ó ������
        Dictionary<int, byte[]> imageDataDict = new Dictionary<int, byte[]>();
        ChangeTexture(glbData, oriBufferViewDict, imageDataDict, jpgIndex, glbJson, jsonChunkLength);

        //���ۺκ� ����Ʈ�� ��ȯ. "BIN " �ں���
        uint count = (uint)(glbData.Length - (20 + jsonChunkLength + 8));
        byte[] temArray = new byte[count];
        Array.Copy(glbData, (long)(20 + jsonChunkLength + 8), temArray, 0, (long)count);
        List<byte> oriBufferList = new List<byte>();
        oriBufferList.AddRange(temArray);

        //glb���� ������ ����
        byte[] changeData = ChangeData(glbData, glbJson, oriBufferViewDict, imageDataDict, oriBufferList);
        WriteBinary(changeData, desPath);
    }
    void AllocateList(Dictionary<int, (uint, uint)> oriBufferViewDict, JObject glbJson, List<int> jpgIndex)
    {
        JArray bufferViews = (JArray)glbJson["bufferViews"];
        JArray images = (JArray)glbJson["images"];

        AllocateJPGIndex(images, jpgIndex);
        List<int> oriBufferIndexList = ReturnImageIndex(images);
        oriBufferIndexList.Sort();

        //�̹����� ���Ǵ� ���� �ε����� �̿��Ͽ� BufferViews�� �ش� �ε��� �ȿ� ���̿� �������� ���Ѵ�.
        foreach (int index in oriBufferIndexList)
        {
            uint length = (uint)bufferViews[index]["byteLength"];
            uint offset = (uint)bufferViews[index]["byteOffset"];
            oriBufferViewDict[index] = (length, offset);
        }
    }
    void ChangeTexture(byte[] glbData, Dictionary<int, (uint, uint)> oriBufferViewDict, Dictionary<int, byte[]> imageDataDict, List<int> jpgIndex, JObject glbJson, uint jsonChunkLength)
    {
        //< �ε���, (����, ������) >
        foreach (KeyValuePair<int, (uint, uint)> pair in oriBufferViewDict)
        {
            uint length = pair.Value.Item1;
            uint offset = pair.Value.Item2;
            byte[] textureData = new byte[length];
            Array.Copy(glbData, (long)(28 + jsonChunkLength + offset), textureData, 0, (long)length);
            Texture2D tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, textureData);

            if (jpgIndex.Contains(pair.Key))
            {
                byte[] bytes = CompressTexture(tex, "JPG");
                imageDataDict[pair.Key] = bytes;
            }
            else
            {
                byte[] bytes = CompressTexture(tex, "PNG");
                imageDataDict[pair.Key] = bytes;
            }
        }
    }
    byte[] ChangeData(byte[] glbData, JObject glbJson, Dictionary<int, (uint, uint)> oriBufferViewDict, Dictionary<int, byte[]> imageDataDict, List<byte> bufferList)
    {
        JArray bufferViews = (JArray)glbJson["bufferViews"];
        JArray images = (JArray)glbJson["images"];

        List<int> oriBufferIndexList = ReturnImageIndex(images);
        oriBufferIndexList.Sort();
        //�̹����� ���Ǵ� ���� �ε����� �̿��Ͽ� BufferViews�� �ش� �ε��� �ȿ� ���̿� �������� ���Ѵ�.
        foreach (int index in oriBufferIndexList)
        {
            uint length = (uint)bufferViews[index]["byteLength"];
            uint offset = (uint)bufferViews[index]["byteOffset"];
            oriBufferViewDict[index] = (length, offset);
        }
        //���� �κ� ���� ����
        foreach (KeyValuePair<int, byte[]> pair in imageDataDict)
        {
            int index = pair.Key;//���ۺ� �ε���
            uint length = (uint)pair.Value.Length;//���ο� ����
            bufferViews[index]["byteLength"] = length;
        }
        //������ �κ� ����
        //���ۺ��� �������� �� �� ���ۺ��� ���� + ������ + (�е�)
        for (int i = 1; i < bufferViews.Count; i++)
        {
            uint preLength = (uint)bufferViews[i - 1]["byteLength"];
            uint preOffset = (uint)bufferViews[i - 1]["byteOffset"];
            int prePaddingCount = CalculatePadding(preLength, preOffset);
            bufferViews[i]["byteOffset"] = preLength + preOffset + prePaddingCount;
        }

        //���ο� �̹��� ���� ������
        //�ں��� �����͸� �ٲ�� �ǹٸ��� �۵�
        oriBufferIndexList.Reverse();
        for (int i = 0; i < oriBufferIndexList.Count; i++)
        {
            int currentIndex = oriBufferIndexList[i];
            uint startIndex = oriBufferViewDict[currentIndex].Item2;
            uint count = oriBufferViewDict[currentIndex].Item1;
            ModifyByteArray(bufferList, imageDataDict[currentIndex], startIndex, count);
        }

        //���� �ڿ� ���� ���� ���۰� 4����Ʈ����� �������� ���缭 �߰�: null(0x00)
        //���� ���� ����
        while (bufferList.Count > 0 && bufferList[bufferList.Count - 1] == 0)
        {
            bufferList.RemoveAt(bufferList.Count - 1);
        }
        uint bufferLength = (uint)bufferList.Count;
        int addNullCount = Convert.ToInt32(4 - ((uint)bufferLength % 4));
        byte[] padding = new byte[addNullCount];
        bufferList.AddRange(padding);

        //json���� ���� ���� �� ����
        int newBufferCount = bufferList.Count;
        glbJson["buffers"][0]["byteLength"] = newBufferCount;


        //����Ʈ �迭�� ���� ���ο� .glb ���� �����ϱ�
        //(���) + (json����Ʈ����) + ("JSON" ���ڿ� ����Ʈ) + (Json) + (Buffer ����Ʈ ����) + ("BIN " ���ڿ� ����Ʈ) + (Buffer)
        List<byte> newGLB = new List<byte>();

        string json = JsonConvert.SerializeObject(glbJson, Formatting.None, new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
        });
        //���� ���� �� ����Ʈ �迭�� ��ȯ

        //Json
        byte[] jsonBytes = ModifyPadding(json);
        //Json ����Ʈ ����
        byte[] jsonCountBytes = BitConverter.GetBytes(jsonBytes.Length);
        //Json �տ� "JSON" ���ڿ� ����Ʈ
        byte[] jsonStrBytes = Encoding.UTF8.GetBytes("JSON");
        //Buffer ����Ʈ ����
        byte[] bufferCountBytes = BitConverter.GetBytes(newBufferCount);
        //"BIN" ���ڿ� ����Ʈ. BIN�ڿ� null�� ����. 16���� "00". �����ϰ�� �ٸ� ���� �����Ƿ� ����
        byte[] binStrBytes = new byte[4];
        Buffer.BlockCopy(Encoding.UTF8.GetBytes("BIN"), 0, binStrBytes, 0, 3);
        //Buffer : bufferList

        //���ϱ�. ���� ���� �������(�������� �Է¶����� ����� �������� ��������)
        newGLB.AddRange(jsonCountBytes); newGLB.AddRange(jsonStrBytes);//(���) + (json����Ʈ����) + ("JSON" ���ڿ� ����Ʈ) + (Json)
        newGLB.AddRange(jsonBytes); newGLB.AddRange(bufferCountBytes); newGLB.AddRange(binStrBytes);//(Json) + (Buffer ����Ʈ ����) + ("BIN " ���ڿ� ����Ʈ)
        newGLB.AddRange(bufferList);//(Buffer)

        //newGLB.AddRange(padding);
        //���
        byte[] headerBytes = new byte[12];
        //��� ���� �� 8����Ʈ�� �״�� �ۼ�. ����� ���̳ʸ� ��ü ���̴� ���� �Է�
        Buffer.BlockCopy(glbData, 0, headerBytes, 0, 8);
        //�������� ����(����� ���� �κ�)
        byte[] bytes = BitConverter.GetBytes((uint)newGLB.Count + 12);
        Buffer.BlockCopy(bytes, 0, headerBytes, 8, 4);
        newGLB.InsertRange(0, headerBytes);

        return newGLB.ToArray();
    }
    byte[] CompressTexture(Texture2D oriTex, string imageType)
    {
        (int newWidth, int newHeight) = CalculateSize(oriTex.width, oriTex.height);
        Texture2D newTex = ResizeTexture(oriTex, newWidth, newHeight);
        if(imageType == "JPG")
        {
            byte[] bytes = ImageConversion.EncodeToJPG(newTex);
            return bytes;
        }
        else
        {
            byte[] bytes = ImageConversion.EncodeToPNG(newTex);
            return bytes;
        }
    }
    (int, int) CalculateSize(int width, int height)
    {
        int newWidth = width;
        int newHeight = height;
        while (true)
        {
            if (newWidth % 2 == 0 && newHeight % 2 == 0 && newWidth >= 1024 && newHeight >= 1024)
            {
                newWidth = newWidth / 2; newHeight = newHeight / 2;
                continue;
            }
            return (newWidth, newHeight);
        }
    }
    Texture2D ResizeTexture(Texture2D oriTex, int targetX, int targetY)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetX, targetY, 24);
        Graphics.Blit(oriTex, rt);

        RenderTexture.active = rt;
        Texture2D result = new Texture2D(targetX, targetY);
        result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
        result.Apply();

        RenderTexture.ReleaseTemporary(rt);
        Destroy(oriTex);

        return result;
    }
    
    List<int> ReturnImageIndex(JArray images)
    {
        List<int> oriBufferIndexList = new List<int>();
        for (int i = 0; i < images.Count; i++)
        {
            if ((string)images[i]["mimeType"] != "image/png" && (string)images[i]["mimeType"] != "image/jpeg") continue;
            int index = (int)images[i]["bufferView"];
            oriBufferIndexList.Add(index);
        }
        return oriBufferIndexList;
    }
    List<int> AllocateJPGIndex(JArray images, List<int> jpgIndexList)
    {
        for (int i = 0; i < images.Count; i++)
        {
            if ((string)images[i]["mimeType"] != "image/jpeg") continue;
            int index = (int)images[i]["bufferView"];
            jpgIndexList.Add(index);
        }
        return jpgIndexList;
    }
    void ModifyByteArray(List<byte> oriByteList, byte[] newBytes, uint oriStartIndex, uint count)
    {
        //���� ������ ���� ���� �����±��� �����ؾ� ������ �����
        int oriPadding = CalculatePadding(count, oriStartIndex);

        //(��ŸƮ �ε���, ������ ����)
        oriByteList.RemoveRange((int)oriStartIndex, (int)count + oriPadding);

        //���ο� ����Ʈ �迭 ����. ���̿� �ε����� ���� 4����Ʈ�� ���� ����������. �ƴ϶�� �������
        int paddingCount = CalculatePadding((uint)newBytes.Length, oriStartIndex);//4 - ((newBytes.Length + index) % 4);
        oriByteList.InsertRange((int)oriStartIndex, newBytes);

        for (int i = 0; i < paddingCount; i++)
        {
            oriByteList.Insert((int)oriStartIndex + newBytes.Length, 0);
        }
    }
    byte[] ModifyPadding(string json)//JSON�� ����Ʈ�� ��ȯ ���� �� 4�� ����� �ǵ��� ������ �߰����־����.
    {
        //���� ��������
        int lastIndex = json.Length - 1;
        while (lastIndex >= 0 && json[lastIndex] == ' ')
        {
            lastIndex--;
        }

        // ���ŵ� ���ڿ��� ���ο� ���ڿ��� �Ҵ�
        string trimmedJsonStr = json.Substring(0, lastIndex + 1);

        //���Ӱ� ���� ����Ʈ ������ �°� ���� �߰�
        int byteCount = Encoding.ASCII.GetByteCount(trimmedJsonStr);
        int padding = 4 - (byteCount % 4);
        if (padding != 4)
        {
            trimmedJsonStr += new string(' ', padding);
        }
        return Encoding.UTF8.GetBytes(trimmedJsonStr);
    }
    void WriteBinary(byte[] bytes, string savedPath)
    {
        try
        {
            // BinaryWriter�� ����Ͽ� ���Ͽ� ���̳ʸ� �����͸� ���� ���� FileStream ����
            using (FileStream fs = new FileStream(savedPath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // ����Ʈ �迭�� ���Ͽ� ���ϴ�.
                writer.Write(bytes);
            }

            Notification.notiList.Add("������ ���������� ����Ǿ����ϴ�.");
        }
        catch (Exception ex)
        {
            Notification.notiList.Add($"���� ���� �� ������ �߻��߽��ϴ�: {ex.Message}");
        }
    }
    int CalculatePadding(uint length, uint offset)
    {
        int paddingCount = (int)(4 - ((length + offset) % 4));
        if (paddingCount == 4) return 0;
        return paddingCount;
    }
}