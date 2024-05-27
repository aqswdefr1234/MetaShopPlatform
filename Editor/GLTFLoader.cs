using UnityEngine;
using GLTFast;
using System.IO;

public class GLTFLoader : MonoBehaviour
{
    [SerializeField] private Transform gltfPrefab;
    Transform gltfGround;

    void Start()
    {
        gltfGround = LoadedPlace.gltfsPlace;
    }
    public void LoadLocal(string path)
    {
        Transform gltf = Instantiate(gltfPrefab, gltfGround);
        Import(path, gltf);
    }
    public void Import(string path, Transform target)
    {
        target.GetComponent<GltfAsset>().Url = path;
        string targetName = Path.GetFileNameWithoutExtension(path);
        string newName = SameNameChanger.Change_NumInParenthesis(gltfGround, target, targetName);
        target.name = newName;
    }
}
