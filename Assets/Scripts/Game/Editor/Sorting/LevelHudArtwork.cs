using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    /// <summary>Authors only the existing level/timer widgets and one new frozen-frame reference.</summary>
    public static class LevelHudArtwork
    {
        public const string ArtRoot="Assets/Sprites/UI/HudFrames/";
        public static void ApplySavedScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Save unsaved scene changes first.");
            if(scene.path!="Assets/Scenes/GameScene.unity")scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
            var view=UnityEngine.Object.FindObjectOfType<SortGameViewController>();if(!view)throw new InvalidOperationException("Game controller missing.");
            ApplyTo(view);EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void ApplyTo(SortGameViewController view)
        {
            var normal=Import("Normal",512);var frozen=Import("Frozen",1024);var level=Import("Level",512);
            var clock=(RectTransform)view.TimerLabel.transform.parent;var levelRect=(RectTransform)view.LevelLabel.transform.parent;
            levelRect.anchoredPosition=new Vector2(36,-41);levelRect.sizeDelta=new Vector2(180,64);var image=levelRect.GetComponent<Image>();image.sprite=level;image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=false;image.raycastTarget=false;
            clock.sizeDelta=new Vector2(174,64);image=clock.GetComponent<Image>();image.sprite=normal;image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=false;image.raycastTarget=false;
            var overlay=clock.Find("FrozenFrame") as RectTransform;if(!overlay){overlay=new GameObject("FrozenFrame",typeof(RectTransform)).GetComponent<RectTransform>();overlay.SetParent(clock,false);}
            overlay.anchorMin=overlay.anchorMax=overlay.pivot=new Vector2(.5f,.5f);overlay.anchoredPosition=Vector2.zero;overlay.sizeDelta=new Vector2(220,94);overlay.SetAsFirstSibling();
            image=overlay.GetComponent<Image>();if(!image)image=overlay.gameObject.AddComponent<Image>();image.sprite=frozen;image.type=Image.Type.Simple;image.preserveAspect=false;image.color=Color.white;image.raycastTarget=false;overlay.gameObject.SetActive(false);view.FrozenTimerPanel=image;
            var timer=view.TimerLabel;ClearEffects(timer);Fill(timer.rectTransform,8);timer.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");timer.fontStyle=FontStyle.Bold;timer.fontSize=44;timer.color=Color.white;timer.alignment=TextAnchor.MiddleCenter;timer.horizontalOverflow=HorizontalWrapMode.Overflow;timer.verticalOverflow=VerticalWrapMode.Overflow;timer.resizeTextForBestFit=false;timer.raycastTarget=false;
            var edge=timer.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.03f,.13f,.16f,1);edge.effectDistance=new Vector2(.8f,-.8f);
            var shadow=timer.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,.08f,.1f,.8f);shadow.effectDistance=new Vector2(1.4f,-2);timer.transform.SetAsLastSibling();
            var title=view.LevelLabel;ClearEffects(title);Fill(title.rectTransform,6);title.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Fredoka/Fredoka-Bold.ttf");title.fontStyle=FontStyle.Normal;title.fontSize=34;title.color=Color.white;title.alignment=TextAnchor.MiddleCenter;title.horizontalOverflow=HorizontalWrapMode.Overflow;title.verticalOverflow=VerticalWrapMode.Overflow;title.resizeTextForBestFit=false;title.raycastTarget=false;
            title.gameObject.AddComponent<SortHudTextGlow>();
        }
        private static void Fill(RectTransform rect,float inset){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=new Vector2(inset,0);rect.offsetMax=new Vector2(-inset,0);rect.localScale=Vector3.one;}
        private static void ClearEffects(Text text){foreach(var effect in text.GetComponents<BaseMeshEffect>())UnityEngine.Object.DestroyImmediate(effect);}
        private static Sprite Import(string name,int max)
        {
            string path=ArtRoot+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new FileNotFoundException(path);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;var pixels=texture.GetPixels32();
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>128){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            if(maxX<0)throw new InvalidOperationException("Empty HUD frame: "+name);UnityEngine.Object.DestroyImmediate(texture);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=max;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var slices=new List<SpriteMetaData>{new SpriteMetaData{name=name,rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=9,pivot=new Vector2(.5f,.5f)}};importer.spritesheet=slices.ToArray();typeof(HomeSceneBuilder).GetMethod("AssignSliceIdentities",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{importer,path,slices});importer.SaveAndReimport();
            foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path))if(a is Sprite)return (Sprite)a;throw new InvalidOperationException("HUD frame missing: "+name);
        }
    }
}
