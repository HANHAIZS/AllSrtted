using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace AllSorted.Editor
{
 public static class SourceLevel1ArtworkImport
 {
  private const string Output="/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/level1-art-v2/";
  [MenuItem("All Sorted/Artwork/Import Reworked Source Level 1")]
  public static void Import(){Apply();}
  public static string Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Editor must be idle for art authoring.");
   var setup=EditorSceneManager.GetSceneManagerSetup();
   for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save dirty scenes before art authoring.");
   Directory.CreateDirectory("Assets/Art/Materials/Sorting/SourceLevel1");Directory.CreateDirectory("Assets/Prefabs/Merchandise/SourceLevel1");AssetDatabase.Refresh();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var records=new List<object>();
   try{
    foreach(string key in new[]{"Apple","Hanoi","Sunflower","Shelf"}){
     string texturePath="Assets/Art/Textures/SourceLevel1/T_S1_"+key+"_Albedo.png";
     AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
     var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.alphaSource=TextureImporterAlphaSource.None;ti.maxTextureSize=1024;ti.mipmapEnabled=true;ti.SaveAndReimport();
     var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);var front=Material(key,texture,false);if(key!="Shelf")Material(key,texture,true);
     string path="Assets/Art/Models/SourceLevel1/SM_S1_"+key+".fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
     var mi=(ModelImporter)AssetImporter.GetAtPath(path);mi.importAnimation=false;mi.importCameras=false;mi.importLights=false;mi.addCollider=false;mi.globalScale=1;mi.isReadable=false;mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
     foreach(var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),m.name),front);mi.SaveAndReimport();
     var root=new GameObject("PF_S1_"+key);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
     var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);art.name="Artwork";
     if(key=="Shelf")art.transform.localRotation=Quaternion.Euler(0,180,0);
     var renderers=root.GetComponentsInChildren<Renderer>();foreach(var r in renderers)r.sharedMaterials=new[]{front};
     if(renderers.Length!=1 || root.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Invalid art renderer/collider: "+key);
     var bounds=renderers[0].bounds;var mesh=root.GetComponentInChildren<MeshFilter>().sharedMesh;long triangles=(long)mesh.GetIndexCount(0)/3;
     if(bounds.center.magnitude>.001f || triangles>(key=="Shelf"?2000:2500) || texture.width!=1024 || !mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))throw new Exception("Native asset validation: "+key);
     PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Merchandise/SourceLevel1/PF_S1_"+key+".prefab");
     records.Add(new{key=key,bounds=new[]{bounds.size.x,bounds.size.y,bounds.size.z},center=new[]{bounds.center.x,bounds.center.y,bounds.center.z},triangles=triangles,material=front.name,texture=texturePath,colliders=0});UnityEngine.Object.DestroyImmediate(root);
    }
    EditorSceneManager.CloseScene(scene,true);
    var gameScene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity",OpenSceneMode.Additive);
    var view=gameScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SortGameViewController>(true)).Single();
    var before=EditorJsonUtility.ToJson(view);File.WriteAllText(Output+"bindings-before.json",before);
    string[] keys={"Apple","Hanoi","Sunflower"};view.SourceLevel1Prefabs=new GameObject[3];view.SourceLevel1FrontMaterials=new Material[3];view.SourceLevel1RearMaterials=new Material[3];
    for(int i=0;i<3;i++){view.SourceLevel1Prefabs[i]=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Merchandise/SourceLevel1/PF_S1_"+keys[i]+".prefab");view.SourceLevel1FrontMaterials[i]=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Sorting/SourceLevel1/M_S1_"+keys[i]+".mat");view.SourceLevel1RearMaterials[i]=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Sorting/SourceLevel1/M_S1_"+keys[i]+"_Behind.mat");}
    view.SourceLevel1ShelfPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Merchandise/SourceLevel1/PF_S1_Shelf.prefab");
    File.WriteAllText(Output+"bindings-after.json",EditorJsonUtility.ToJson(view));
    EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(gameScene);EditorSceneManager.SaveScene(gameScene);AssetDatabase.SaveAssets();
    string result=Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented);File.WriteAllText(Output+"unity-import.json",result);return result;
   }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  }
  private static Material Material(string key,Texture texture,bool rear)
  {
   string path="Assets/Art/Materials/Sorting/SourceLevel1/M_S1_"+key+(rear?"_Behind":"")+".mat";
   var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",rear?new Color(.68f,.68f,.68f,1):Color.white);m.SetFloat("_Smoothness",key=="Shelf"?.36f:.55f);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;
  }
 }
}
