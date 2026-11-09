using System;
using System.Collections.Generic;
using System.Reflection;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    /// <summary>Appends only the Home/Shop resource HUD and its new bindings.</summary>
    public static class HomeWalletArtwork
    {
        private const string Root="Assets/Sprites/UI/Home/Wallet/";
        [MenuItem("All Sorted/Apply Home Wallet HUD")]
        public static void ApplySavedScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring the wallet HUD.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save unsaved scenes before authoring the wallet HUD.");
            var scene=SceneManager.GetActiveScene();if(scene.path!=HomeSceneBuilder.ScenePath)scene=EditorSceneManager.OpenScene(HomeSceneBuilder.ScenePath,OpenSceneMode.Single);
            var home=UnityEngine.Object.FindObjectOfType<SortHomeViewController>();
            if(!home || home.gameObject.scene!=scene)throw new InvalidOperationException("HomeScene controller is unavailable.");
            ApplyToHome(home);EditorUtility.SetDirty(home);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the Home wallet HUD.");
        }
        public static void ApplyToHome(SortHomeViewController home)
        {
            var coin=CoinImageReplacementArtwork.TrySprite("Hud")??Import("Coins",new Rect(73,386,1122,429));var energy=Import("Energy",new Rect(62,376,1150,398));
            var badge=Import("PlusBadge",new Rect(230,214,789,792));
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/LilitaOne/LilitaOne-Regular.ttf");
            var safe=Child(home.Layers.Hosts[1].transform,"ResourceSafeArea");Stretch(safe);
            if(!safe.GetComponent<UiSafeAreaFitter>())safe.gameObject.AddComponent<UiSafeAreaFitter>();home.ResourceSafeArea=safe;
            var hud=Child(safe,"ResourceHud");Stretch(hud);home.ResourceHud=Get<CanvasGroup>(hud);
            home.EnergyBar=Bar(hud,"Energy",energy,false);home.CoinBar=Bar(hud,"Coins",coin,true);
            home.EnergyBar.sizeDelta=home.CoinBar.sizeDelta;
            home.EnergyBar.GetComponent<Image>().preserveAspect=false;
            var red=new Color(.53f,0,0,1);
            home.CoinValue=Label(home.CoinBar,"Value",font,"1600",red,24,.37f,.95f);
            home.CoinValue.resizeTextForBestFit=true;home.CoinValue.resizeTextMinSize=12;home.CoinValue.resizeTextMaxSize=24;
            home.EnergyValue=Label(home.EnergyBar,"Value",font,"5",Color.white,30,.025f,.37f);
            var outline=Get<Outline>((RectTransform)home.EnergyValue.transform);outline.effectColor=new Color(.40f,.015f,0,1);outline.effectDistance=new Vector2(1.5f,-1.5f);
            home.EnergyRecoveryLabel=Label(home.EnergyBar,"Recovery",font,"Full",red,23,.43f,.95f);
            home.EnergyRecoveryLabel.resizeTextForBestFit=true;home.EnergyRecoveryLabel.resizeTextMinSize=13;home.EnergyRecoveryLabel.resizeTextMaxSize=23;
            home.EnergyPlus=Plus(home.EnergyBar,badge,.35f);home.CoinPlus=Plus(home.CoinBar,badge,.31f);
        }
        private static Sprite Import(string name,Rect top)
        {
            string path=Root+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)throw new InvalidOperationException("Missing resource art: "+path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.isReadable=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=name=="PlusBadge"?256:512;
            var regions=new List<SpriteMetaData>{new SpriteMetaData{name=name+"Background",rect=new Rect(top.x,1254-top.y-top.height,top.width,top.height),alignment=9,pivot=new Vector2(.5f,.5f)}};
            importer.spritesheet=regions.ToArray();
            typeof(HomeSceneBuilder).GetMethod("AssignSliceIdentities",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{importer,path,regions});importer.SaveAndReimport();
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite)return (Sprite)asset;
            throw new InvalidOperationException("Resource Sprite missing after import: "+path);
        }
        private static RectTransform Bar(Transform parent,string name,Sprite sprite,bool right)
        {
            var rect=Child(parent,name);rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=new Vector2(right?111.5f:-111.5f,-18);rect.localScale=Vector3.one*1.25f;rect.sizeDelta=new Vector2(156,156*sprite.rect.height/sprite.rect.width);
            var image=Get<Image>(rect);image.sprite=sprite;image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;return rect;
        }
        private static Text Label(Transform parent,string name,Font font,string text,Color color,int size,float left,float right)
        {
            var rect=Child(parent,name);rect.anchorMin=new Vector2(left,.1f);rect.anchorMax=new Vector2(right,.9f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            var label=Get<Text>(rect);label.font=font;label.text=text;label.fontSize=size;label.color=color;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;return label;
        }
        private static Button Plus(Transform parent,Sprite badge,float iconCorner)
        {
            var rect=Child(parent,"Plus");rect.anchorMin=rect.anchorMax=new Vector2(iconCorner,0);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,6);rect.sizeDelta=new Vector2(44,44);
            var hit=Get<Image>(rect);hit.color=Color.clear;hit.raycastTarget=true;
            var button=Get<Button>(rect);button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            if(!rect.GetComponent<SortButtonPressFeedback>())rect.gameObject.AddComponent<SortButtonPressFeedback>();
            foreach(string obsolete in new[]{"Gold","Caption"}){var child=rect.Find(obsolete);if(child)Undo.DestroyObjectImmediate(child.gameObject);}
            var icon=Child(rect,"Icon");icon.anchorMin=icon.anchorMax=icon.pivot=new Vector2(.5f,.5f);icon.anchoredPosition=Vector2.zero;icon.sizeDelta=new Vector2(32,32);
            var image=Get<Image>(icon);image.sprite=badge;image.color=Color.white;image.preserveAspect=true;image.raycastTarget=false;
            return button;
        }
        private static T Get<T>(RectTransform rect) where T:Component{var current=rect.GetComponent<T>();return current?current:rect.gameObject.AddComponent<T>();}
        private static RectTransform Child(Transform parent,string name){var found=parent.Find(name);if(found)return (RectTransform)found;var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;}
        private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
