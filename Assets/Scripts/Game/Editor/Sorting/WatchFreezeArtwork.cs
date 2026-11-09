using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace AllSorted.Editor
{
 public static class WatchFreezeArtwork
 {
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Stop Play Mode and finish compilation.");
   var old=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(old.isDirty)throw new InvalidOperationException("Save scene changes first.");
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");var game=UnityEngine.Object.FindObjectOfType<SortGameViewController>();
   var frame=Import("Frame",2048);var snow=Import("Snow",256);var ring=MakeRing();
   var host=game.Layers.Hosts[4].transform;var r=Rect(host,"WatchFreeze");Stretch(r);var view=r.GetComponent<SortWatchFreezeView>();if(!view)view=r.gameObject.AddComponent<SortWatchFreezeView>();game.WatchFreezeFx=view;
   view.Surface=r;view.Origin=(RectTransform)game.BoosterButtons[3].transform;view.Clock=(RectTransform)game.TimerLabel.transform.parent;view.FrozenSkin=game.FrozenTimerPanel;
   view.Frame=Picture(r,"FrostFrame",frame,Vector2.zero,Vector2.zero);Stretch(view.Frame.rectTransform);view.Frame.preserveAspect=false;
   view.Snow=new Image[32];for(int i=0;i<32;i++)view.Snow[i]=Picture(r,"Snow"+i,snow,Vector2.zero,Vector2.one*(12+i%5*4));
   view.Glow=Picture(r,"DialGlow",ring,Vector2.zero,Vector2.one*150);view.Glow.color=new Color(.45f,.95f,1,0);
   view.Wave=Picture(r,"FrostWave",ring,Vector2.zero,new Vector2(210,120));view.Wave.color=new Color(.7f,.98f,1,0);
   view.Watch=Picture(r,"FlyingWatch",game.BoosterButtons[3].transform.Find("Icon").GetComponent<Image>().sprite,Vector2.zero,Vector2.one*90);
   view.Flash=Picture(r,"ClockFlash",null,Vector2.zero,new Vector2(174,64));view.Flash.rectTransform.position=view.Clock.position;
   view.Clear();EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(old.path!="Assets/Scenes/GameScene.unity" && !string.IsNullOrEmpty(old.path))EditorSceneManager.OpenScene(old.path);
  }
  private static Sprite MakeRing()
  {
   const string path="Assets/Sprites/UI/WatchFreeze/Pulse.png";var t=new Texture2D(128,128,TextureFormat.RGBA32,false);
   for(int y=0;y<128;y++)for(int x=0;x<128;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(64,64))/64;float a=Mathf.Exp(-Mathf.Pow((d-.7f)/.08f,2))*.9f+Mathf.Exp(-d*d*7)*.2f;t.SetPixel(x,y,new Color(1,1,1,a));}t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);return Import("Pulse",128);
  }
  private static Sprite Import(string name,int max){string path="Assets/Sprites/UI/WatchFreeze/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.maxTextureSize=max;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
  private static RectTransform Rect(Transform parent,string name){var r=parent.Find(name) as RectTransform;if(!r){r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);}return r;}
  private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
  private static Image Picture(Transform parent,string name,Sprite sprite,Vector2 pos,Vector2 size){var r=Rect(parent,name);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;var i=r.GetComponent<Image>();if(!i)i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.preserveAspect=true;i.color=Color.white;i.raycastTarget=false;return i;}
 }
}
