using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using UnityEngine;
using System;

public class MeshMerger
{
    public void MergeMesh(Transform target)
    {
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        FillCollection.FillComponent<MeshFilter>(target, meshFilters);

        List<CombineInstance> combineInstances = new List<CombineInstance>();
        List<Material> materials = new List<Material>();
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter == null) continue;
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null) continue;

            Material[] meshMaterials = meshRenderer.materials;

            for (int subMeshIndex = 0; subMeshIndex < meshFilter.sharedMesh.subMeshCount; subMeshIndex++)
            {
                CombineInstance combineInstance = new CombineInstance();
                combineInstance.mesh = meshFilter.mesh;
                combineInstance.subMeshIndex = subMeshIndex;
                combineInstance.transform = meshFilter.transform.localToWorldMatrix;
                combineInstances.Add(combineInstance);

                materials.Add(meshMaterials[subMeshIndex]);
            }
            meshFilter.gameObject.SetActive(false);
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances.ToArray(), false, true); //세번째 매개변수를 true로 해야 localToWorldMatrix의 영향을 받는다.

        GameObject combinedObject = new GameObject("CombinedMesh");
        combinedObject.transform.SetParent(target.parent);

        MeshFilter combinedMeshFilter = combinedObject.AddComponent<MeshFilter>();
        combinedMeshFilter.mesh = combinedMesh;
        MergeMeshMatGroup(combinedObject.transform, materials.ToArray(), target.name);

        materials.Clear();
        MonoBehaviour.Destroy(target.gameObject);
    }
    void MergeMeshMatGroup(Transform target, Material[] materialArr, string name)
    {
        GameObject parentObject = new GameObject(name);
        parentObject.transform.SetParent(target.parent);

        Dictionary<int, List<int>> dict = CompareMaterials(materialArr);
        Mesh targetMesh = target.GetComponent<MeshFilter>().sharedMesh;
        foreach (KeyValuePair<int, List<int>> pair in dict)
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            foreach (int index in pair.Value)
            {
                CombineInstance combineInstance = new CombineInstance();
                combineInstance.mesh = targetMesh;
                combineInstance.subMeshIndex = index;
                combineInstance.transform = target.localToWorldMatrix;
                combineInstances.Add(combineInstance);
            }
            Mesh mergedMesh = new Mesh();
            mergedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

            GameObject combinedObject = new GameObject(pair.Key.ToString());
            combinedObject.AddComponent<MeshFilter>().mesh = mergedMesh;
            combinedObject.AddComponent<MeshRenderer>().material = materialArr[pair.Value[0]];
            combinedObject.transform.SetParent(parentObject.transform);
        }
        OptimizeTexture(parentObject.transform);
        MonoBehaviour.Destroy(target.gameObject);
    }
    public void OptimizeTexture(Transform target)
    {
        List<Material> matList = new List<Material>();
        List<MeshRenderer> list = new List<MeshRenderer>();
        FillCollection.FillComponent<MeshRenderer>(target, list);
        foreach (MeshRenderer render in list)
        {
            if (render == null) continue;
            matList.Add(render.material);
        }
        CompressTexture(matList.ToArray());
    }
    Dictionary<int, List<int>> CompareMaterials(Material[] materials)
    {
        List<Material> allList = new List<Material>();
        allList.AddRange(materials);

        Dictionary<int, List<int>> matGroup = new Dictionary<int, List<int>>();
        int groupNum = 0;
        while (true)
        {
            //같은 그룹 구하기: 앞부터 검사
            List<int> sameList = new List<int>();
            for (int i = 0; i < allList.Count; i++)
            {
                if (AreMaterialsEqual(allList[0], allList[i]))
                {
                    sameList.Add(i);//인덱스 낮은순에서 높은 순으로 작성됨
                }
            }

            //원본데이터에서 새로운 리스트 할당
            List<int> newList = new List<int>();
            foreach (int same in sameList)
            {
                int originalIndex = System.Array.IndexOf(materials, allList[same]);//같은 그룹의 원본데이터의 인덱스를 구함.
                newList.Add(originalIndex);
            }
            matGroup.Add(groupNum, newList);


            //제외 하기 : 앞에서 인덱스 낮은순에서 높은 순으로 작성되었으므로 뒤부터 검사(앞에서 부터 제거하면 인덱스가 한칸씩 앞으로 당겨져 오므로 인덱스 범위가 벗어 날수 있음.)
            for (int i = sameList.Count - 1; i >= 0; i--)
            {
                allList.RemoveAt(sameList[i]);
            }
            groupNum++;
            if (allList.Count == 0) break;
        }
        return matGroup;
    }

    bool AreMaterialsEqual(Material mat1, Material mat2)
    {
        if (mat1 == null || mat2 == null) return false;
        if (mat1.name != mat2.name) return false;
        if (mat1.shader != mat2.shader) return false;

        // 머티리얼의 주요 속성 비교
        if (mat1.GetColor("baseColorFactor") != mat2.GetColor("baseColorFactor")) return false;
        if (mat1.GetFloat("metallicFactor") != mat2.GetFloat("metallicFactor")) return false;
        //if (!CompareTexture(mat1, mat2)) return false;

        return true;
    }
    
    public void CompressTexture(Material[] matArr)
    {
        foreach (Material mat in matArr)
        {
            string[] names = mat.GetTexturePropertyNames();//reference
            foreach (string name in names)
            {
                try
                {
                    if (mat.GetTexture(name) == null) continue;
                    Texture2D compressTex = TextureCompress(mat.GetTexture(name));
                    compressTex.filterMode = FilterMode.Trilinear;
                    compressTex.wrapMode = TextureWrapMode.Repeat;
                    compressTex.Apply();
                    mat.SetTexture(name, compressTex);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Notification.notiList.Add(e.ToString());
                }
            }
        }
    }
    public Texture2D TextureCompress(Texture oriTex)
    {
        int originalWidth = oriTex.width;
        int originalHeight = oriTex.height;
        (int newWidth, int newHeight) = ReturnMultipleFour(originalWidth, originalHeight);

        Texture2D newTexture = DuplicateTexture(oriTex);
        if (newWidth + newHeight == originalWidth + originalHeight)
        {
            
            newTexture.Compress(true);
            newTexture.Apply();
            return newTexture;
        }

        return newTexture;
    }

    public Texture2D DuplicateTexture(Texture source)
    {
        RenderTexture renderTex = RenderTexture.GetTemporary(source.width,source.height);
        Graphics.Blit(source, renderTex);
        RenderTexture.active = renderTex;
        Texture2D readableTexture = new Texture2D(source.width, source.height);
        readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.ReleaseTemporary(renderTex);
        MonoBehaviour.Destroy(source);

        return readableTexture;
    }
    (int, int) ReturnMultipleFour(int width, int height)
    {
        int newWidth = width; int newHeight = height;
        while (newWidth % 4 != 0)
        {
            newWidth++;
        }
        while (newHeight % 4 != 0)
        {
            newHeight++;
        }
        return (newWidth, newHeight);
    }
    
}
//Texture2D(int width, int height, TextureFormat textureFormat, int mipCount = -1, bool linear = false)
//Destroying assets is not permitted to avoid data loss. 편집기에서 나타나는 에러

/*
 bool CompareTexture(Material mat1, Material mat2)
    {
        string[] names = mat1.GetTexturePropertyNames();
        foreach (string name in names)
        {
            Texture2D tex1 = (Texture2D)mat1.GetTexture(name);
            Texture2D tex2 = (Texture2D)mat2.GetTexture(name);
            if (tex1 == null && tex2 == null) continue;//둘 다 비었다면
            if (tex1 == null || tex2 == null) return false;//둘 다 비진 않았지만 하나가 비어있다면

            if (tex1.width != tex2.width || tex1.height != tex2.height) return false;
            if (tex1.format != tex2.format) return false;
        }
        return true;
    }
 */
