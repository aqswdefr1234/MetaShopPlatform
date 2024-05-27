using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;

//Glb 파일 내 높은 해상도 텍스처 줄이는 스크립트
public class GLBTextureOptimization : MonoBehaviour
{
    public void LowTextureExport(string targetPath, string destination)
    {
        byte[] glbData = File.ReadAllBytes(targetPath);
        ChangeGLBFile(glbData, destination);
    }


    //Glb 구조
    //header       : 12 bytes

    //Json Length  : 4  bytes
    //"JSON" Str   : 4  bytes
    //Json + Padding(0x20)

    //Binary Length: 4  bytes
    //"BIN" + null : 4  bytes
    //Binary

    //버퍼 뷰의 오프셋은 항상 4바이트 배수로 시작해야한다.
    //해당 버퍼 뷰가 끝날 때, 오프셋 + 길이가 4의 배수가 안된다면 뒷쪽에 패딩을 추가해야한다. null(0x00)
    //그래야 다음 버퍼 뷰가 4의 배수 오프셋을 가진다.

    public void ChangeGLBFile(byte[] glbData, string destination)
    {
        string desPath = destination;
        
        uint jsonChunkLength = BitConverter.ToUInt32(glbData, 12);
        string jsonStr = Encoding.UTF8.GetString(glbData, 20, (int)jsonChunkLength);

        // 파싱
        JObject glbJson = JObject.Parse(jsonStr);
        JArray images = (JArray)glbJson["images"];
        if (images == null || images.Count == 0) return;

        //오리지널 이미지 버퍼뷰 데이터 : <인덱스, (길이, 오프셋)>
        Dictionary<int, (uint, uint)> oriBufferViewDict = new Dictionary<int, (uint, uint)>();
        List<int> jpgIndex = new List<int>();
        AllocateList(oriBufferViewDict, glbJson, jpgIndex);

        //새로운 텍스처 데이터
        Dictionary<int, byte[]> imageDataDict = new Dictionary<int, byte[]>();
        ChangeTexture(glbData, oriBufferViewDict, imageDataDict, jpgIndex, glbJson, jsonChunkLength);

        //버퍼부분 리스트로 변환. "BIN " 뒤부터
        uint count = (uint)(glbData.Length - (20 + jsonChunkLength + 8));
        byte[] temArray = new byte[count];
        Array.Copy(glbData, (long)(20 + jsonChunkLength + 8), temArray, 0, (long)count);
        List<byte> oriBufferList = new List<byte>();
        oriBufferList.AddRange(temArray);

        //glb파일 데이터 변경
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

        //이미지에 사용되는 버퍼 인덱스를 이용하여 BufferViews의 해당 인덱스 안에 길이와 오프셋을 구한다.
        foreach (int index in oriBufferIndexList)
        {
            uint length = (uint)bufferViews[index]["byteLength"];
            uint offset = (uint)bufferViews[index]["byteOffset"];
            oriBufferViewDict[index] = (length, offset);
        }
    }
    void ChangeTexture(byte[] glbData, Dictionary<int, (uint, uint)> oriBufferViewDict, Dictionary<int, byte[]> imageDataDict, List<int> jpgIndex, JObject glbJson, uint jsonChunkLength)
    {
        //< 인덱스, (길이, 오프셋) >
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
        //이미지에 사용되는 버퍼 인덱스를 이용하여 BufferViews의 해당 인덱스 안에 길이와 오프셋을 구한다.
        foreach (int index in oriBufferIndexList)
        {
            uint length = (uint)bufferViews[index]["byteLength"];
            uint offset = (uint)bufferViews[index]["byteOffset"];
            oriBufferViewDict[index] = (length, offset);
        }
        //길이 부분 먼저 변경
        foreach (KeyValuePair<int, byte[]> pair in imageDataDict)
        {
            int index = pair.Key;//버퍼뷰 인덱스
            uint length = (uint)pair.Value.Length;//새로운 길이
            bufferViews[index]["byteLength"] = length;
        }
        //오프셋 부분 변경
        //버퍼뷰의 오프셋은 그 전 버퍼뷰의 길이 + 오프셋 + (패딩)
        for (int i = 1; i < bufferViews.Count; i++)
        {
            uint preLength = (uint)bufferViews[i - 1]["byteLength"];
            uint preOffset = (uint)bufferViews[i - 1]["byteOffset"];
            int prePaddingCount = CalculatePadding(preLength, preOffset);
            bufferViews[i]["byteOffset"] = preLength + preOffset + prePaddingCount;
        }

        //새로운 이미지 버퍼 데이터
        //뒤부터 데이터를 바꿔야 옳바르게 작동
        oriBufferIndexList.Reverse();
        for (int i = 0; i < oriBufferIndexList.Count; i++)
        {
            int currentIndex = oriBufferIndexList[i];
            uint startIndex = oriBufferViewDict[currentIndex].Item2;
            uint count = oriBufferViewDict[currentIndex].Item1;
            ModifyByteArray(bufferList, imageDataDict[currentIndex], startIndex, count);
        }

        //버퍼 뒤에 후행 공백 버퍼가 4바이트배수로 끝나도록 맞춰서 추가: null(0x00)
        //기존 공백 제거
        while (bufferList.Count > 0 && bufferList[bufferList.Count - 1] == 0)
        {
            bufferList.RemoveAt(bufferList.Count - 1);
        }
        uint bufferLength = (uint)bufferList.Count;
        int addNullCount = Convert.ToInt32(4 - ((uint)bufferLength % 4));
        byte[] padding = new byte[addNullCount];
        bufferList.AddRange(padding);

        //json안의 버퍼 길이 값 변경
        int newBufferCount = bufferList.Count;
        glbJson["buffers"][0]["byteLength"] = newBufferCount;


        //바이트 배열들 합쳐 새로운 .glb 파일 구성하기
        //(헤더) + (json바이트길이) + ("JSON" 문자열 바이트) + (Json) + (Buffer 바이트 길이) + ("BIN " 문자열 바이트) + (Buffer)
        List<byte> newGLB = new List<byte>();

        string json = JsonConvert.SerializeObject(glbJson, Formatting.None, new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
        });
        //공백 수정 후 바이트 배열로 변환

        //Json
        byte[] jsonBytes = ModifyPadding(json);
        //Json 바이트 길이
        byte[] jsonCountBytes = BitConverter.GetBytes(jsonBytes.Length);
        //Json 앞에 "JSON" 문자열 바이트
        byte[] jsonStrBytes = Encoding.UTF8.GetBytes("JSON");
        //Buffer 바이트 길이
        byte[] bufferCountBytes = BitConverter.GetBytes(newBufferCount);
        //"BIN" 문자열 바이트. BIN뒤에 null이 있음. 16진수 "00". 공백하고는 다른 값을 가지므로 주의
        byte[] binStrBytes = new byte[4];
        Buffer.BlockCopy(Encoding.UTF8.GetBytes("BIN"), 0, binStrBytes, 0, 3);
        //Buffer : bufferList

        //더하기. 파일 구성 순서대로(최종길이 입력때문에 헤더는 마지막에 삽입해줌)
        newGLB.AddRange(jsonCountBytes); newGLB.AddRange(jsonStrBytes);//(헤더) + (json바이트길이) + ("JSON" 문자열 바이트) + (Json)
        newGLB.AddRange(jsonBytes); newGLB.AddRange(bufferCountBytes); newGLB.AddRange(binStrBytes);//(Json) + (Buffer 바이트 길이) + ("BIN " 문자열 바이트)
        newGLB.AddRange(bufferList);//(Buffer)

        //newGLB.AddRange(padding);
        //헤더
        byte[] headerBytes = new byte[12];
        //헤더 길이 전 8바이트는 그대로 작성. 헤더의 바이너리 전체 길이는 새로 입력
        Buffer.BlockCopy(glbData, 0, headerBytes, 0, 8);
        //최종길이 변경(헤더의 길이 부분)
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
        //현재 오프셋 부터 다음 오프셋까지 삭제해야 공백이 사라짐
        int oriPadding = CalculatePadding(count, oriStartIndex);

        //(스타트 인덱스, 삭제할 개수)
        oriByteList.RemoveRange((int)oriStartIndex, (int)count + oriPadding);

        //새로운 바이트 배열 삽입. 길이와 인덱스의 합이 4바이트로 나눠 떨어져야함. 아니라면 공백삽입
        int paddingCount = CalculatePadding((uint)newBytes.Length, oriStartIndex);//4 - ((newBytes.Length + index) % 4);
        oriByteList.InsertRange((int)oriStartIndex, newBytes);

        for (int i = 0; i < paddingCount; i++)
        {
            oriByteList.Insert((int)oriStartIndex + newBytes.Length, 0);
        }
    }
    byte[] ModifyPadding(string json)//JSON을 바이트로 변환 했을 때 4의 배수가 되도록 공백을 추가해주어야함.
    {
        //기존 공백제거
        int lastIndex = json.Length - 1;
        while (lastIndex >= 0 && json[lastIndex] == ' ')
        {
            lastIndex--;
        }

        // 제거된 문자열을 새로운 문자열에 할당
        string trimmedJsonStr = json.Substring(0, lastIndex + 1);

        //새롭게 변한 바이트 개수에 맞게 공백 추가
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
            // BinaryWriter를 사용하여 파일에 바이너리 데이터를 쓰기 위해 FileStream 열기
            using (FileStream fs = new FileStream(savedPath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // 바이트 배열을 파일에 씁니다.
                writer.Write(bytes);
            }

            Notification.notiList.Add("파일이 성공적으로 저장되었습니다.");
        }
        catch (Exception ex)
        {
            Notification.notiList.Add($"파일 저장 중 오류가 발생했습니다: {ex.Message}");
        }
    }
    int CalculatePadding(uint length, uint offset)
    {
        int paddingCount = (int)(4 - ((length + offset) % 4));
        if (paddingCount == 4) return 0;
        return paddingCount;
    }
}