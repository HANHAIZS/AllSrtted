using UnityEditor;
using UnityEngine;
namespace AllSorted.Editor
{
    /// <summary>Restores exact source normals carried by the offline FBX attribute channels.
    /// Material boundaries must not change the existing cabinet bevel shading.</summary>
    public sealed class CabinetNormalImporter : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject model)
        {
            if(!assetPath.StartsWith("Assets/Art/Models/Shelves/Connected/SM_ConnectedStage") || !assetPath.EndsWith(".fbx"))return;
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true)) {
                var mesh=filter.sharedMesh;var xy=mesh.uv2;var z=mesh.uv3;
                if(xy.Length!=mesh.vertexCount || z.Length!=mesh.vertexCount)return;
                var normals=new Vector3[mesh.vertexCount];
                for(int i=0;i<normals.Length;i++)normals[i]=new Vector3(xy[i].x,xy[i].y,z[i].x);
                mesh.normals=normals;
            }
        }
    }
}
