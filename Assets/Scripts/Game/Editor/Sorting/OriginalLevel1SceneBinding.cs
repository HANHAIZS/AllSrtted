using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace AllSorted.Editor
{
 public static class OriginalLevel1SceneBinding
 {
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Editor must be idle.");var old=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string oldPath=old.path;if(old.isDirty)throw new InvalidOperationException("Save scene changes first.");
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");var game=UnityEngine.Object.FindObjectOfType<SortGameViewController>();string[] keys={"Apple","Hanoi","Sunflower","Pear"};game.OriginalMerchandisePrefabs=new GameObject[keys.Length];game.OriginalFrontMaterials=new Material[keys.Length];game.OriginalRearMaterials=new Material[keys.Length];
   for(int i=0;i<keys.Length;i++){game.OriginalMerchandisePrefabs[i]=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Merchandise/Level1/PF_L1_"+keys[i]+".prefab");game.OriginalFrontMaterials[i]=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Sorting/M_L1_"+keys[i]+".mat");game.OriginalRearMaterials[i]=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Sorting/M_L1_"+keys[i]+"_Behind.mat");}
   var shelves=GameObject.Find("Primitive Shelves").transform;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Merchandise/Level1/PF_L1_Shelf.prefab");
   foreach(Transform shelf in shelves){foreach(Transform child in shelf)if(child.name!="OriginalShelf")child.gameObject.SetActive(false);var existing=shelf.Find("OriginalShelf");if(!existing){var art=(GameObject)PrefabUtility.InstantiatePrefab(prefab,shelf);art.name="OriginalShelf";art.transform.localPosition=new Vector3(0,0,.6f);}}
   EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(oldPath!="Assets/Scenes/GameScene.unity" && !string.IsNullOrEmpty(oldPath))EditorSceneManager.OpenScene(oldPath);
  }
 }
}
