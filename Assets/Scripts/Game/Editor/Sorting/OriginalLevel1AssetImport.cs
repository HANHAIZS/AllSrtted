using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace AllSorted.Editor
{
 public static class OriginalLevel1AssetImport
 {
  public static string Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Editor must be idle.");
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save your scene first.");
   var old=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
   var records=new List<object>();
   try{
    string[] keys={"Apple","Hanoi","Sunflower","Pear","Shelf"};int index=0;
    foreach(string key in keys){
     string texPath="Assets/Art/Textures/Level1/T_L1_"+key+"_Albedo.png";AssetDatabase.ImportAsset(texPath,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(texPath);ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.alphaSource=TextureImporterAlphaSource.None;ti.maxTextureSize=1024;ti.mipmapEnabled=true;ti.SaveAndReimport();var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
     var mat=Mat(key,tex,false);Mat(key,tex,true);
     string modelPath="Assets/Art/Models/Level1/SM_L1_"+key+"_A.fbx";AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceSynchronousImport);var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.isReadable=false;importer.globalScale=1;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
     foreach(var sourceMat in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),sourceMat.name),mat);importer.SaveAndReimport();
     var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);var root=new GameObject("PF_L1_"+key);var art=(GameObject)PrefabUtility.InstantiatePrefab(source);art.transform.SetParent(root.transform,false);art.name="Artwork";
     foreach(var r in root.GetComponentsInChildren<Renderer>()){r.sharedMaterials=new[]{mat};r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
     var renderers=root.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);var meshes=root.GetComponentsInChildren<MeshFilter>();long triangles=meshes.Sum(m=>(long)m.sharedMesh.GetIndexCount(0)/3);
     Vector3 expected=key=="Shelf"?new Vector3(3.55f,1.82f,.435f):key=="Apple"?new Vector3(.675f,.7f,.675f):key=="Hanoi"?new Vector3(.64784f,.7f,.64416f):key=="Pear"?new Vector3(.48f,.7f,.48f):new Vector3(.43063f,.7f,.26425f);
     bool pass=(bounds.size-expected).magnitude<.001f && bounds.center.magnitude<.001f && tex.width==1024 && tex.height==1024 && root.GetComponentsInChildren<Collider>().Length==0 && renderers.All(r=>r.sharedMaterials.Length==1 && r.sharedMaterial.shader.name=="Universal Render Pipeline/Lit" && r.sharedMaterial.GetTexture("_BaseMap")!=null);
     if(!pass)throw new Exception(key+" native asset validation failed: "+bounds);
     PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Merchandise/Level1/PF_L1_"+key+".prefab");
     root.transform.position=key=="Shelf"?new Vector3(0,.92f,.2f):new Vector3((index++-1.5f)*.94f,.44f,-.25f);
     records.Add(new {key=key,bounds=new[]{bounds.size.x,bounds.size.y,bounds.size.z},pivot=new[]{bounds.center.x,bounds.center.y,bounds.center.z},triangles=triangles,texture=new[]{tex.width,tex.height},materials=1,shader=mat.shader.name,colliders=0,pass=pass});
    }
    var camera=new GameObject("ValidationCamera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.35f;camera.transform.position=new Vector3(0,1.7f,-6);camera.transform.LookAt(new Vector3(0,.9f,0));camera.backgroundColor=new Color(.09f,.13f,.19f);camera.clearFlags=CameraClearFlags.SolidColor;
    var light=new GameObject("ValidationKey",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(35,-25,0);RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
    string json=Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented);File.WriteAllText("/Users/zhangshuai/Documents/Sort-Rework/export/unity-validation.json",json);EditorSceneManager.SaveScene(scene,"Assets/Scenes/Validation/Level1ModelValidation.unity");AssetDatabase.SaveAssets();return json;
   }finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
  }
  private static Material Mat(string key,Texture texture,bool rear){string path="Assets/Art/Materials/Sorting/M_L1_"+key+(rear?"_Behind":"")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",rear?new Color(.68f,.68f,.68f,1):Color.white);m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.55f);m.SetFloat("_Surface",0);m.DisableKeyword("_ALPHATEST_ON");EditorUtility.SetDirty(m);return m;}
 }
}
