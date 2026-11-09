using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace AllSorted.Editor
{
    public static class VictoryPopupArtwork
    {
        public const string Path="Assets/Resources/Prefabs/UI/Popups/VictoryPopup.prefab";
        private const string Art="Assets/Sprites/UI/Victory/";
        [MenuItem("All Sorted/Apply Victory Result Artwork")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            ImportArt();var root=PrefabUtility.LoadPrefabContents(AssetDatabase.LoadAssetAtPath<GameObject>(Path)?Path:"Assets/Resources/Prefabs/UI/Popups/ResultPopup.prefab");
            try{
                root.name="VictoryPopup";var view=root.GetComponent<SortPopupView>();view.Id="VictoryPopup";
                var content=(RectTransform)root.transform.Find("PopupContent");var oldFit=content.Find("VictoryLayout");if(oldFit){while(oldFit.childCount>0)oldFit.GetChild(0).SetParent(content,false);UnityEngine.Object.DestroyImmediate(oldFit.gameObject);}content.sizeDelta=new Vector2(660,1000);content.localScale=Vector3.one*.9f;
                var background=content.GetComponent<Image>();if(background)UnityEngine.Object.DestroyImmediate(background);
                var dim=root.transform.Find("DimBackground").GetComponent<Image>();dim.color=new Color(0,0,0,.72f);
                view.Title.text="ALL SORTED!";view.Title.gameObject.SetActive(false);
                Place((RectTransform)view.Body.transform,new Vector2(0,-330),new Vector2(620,70));view.Body.fontSize=25;view.Body.color=Color.white;view.Body.text=string.Empty;view.Body.raycastTarget=false;view.Body.gameObject.SetActive(false);
                var banner=Child(content,"SuccessBanner");Place(banner,new Vector2(0,450),new Vector2(650,190));Picture(banner,Sprite("Artwork","SuccessBanner"));
                var coins=Child(content,"VictoryCoins");Place(coins,new Vector2(0,25),new Vector2(400,430));Picture(coins,Sprite("Artwork","VictoryCoins"));
                var button=(RectTransform)view.Primary.transform;Place(button,new Vector2(0,-450),new Vector2(430,160));var image=button.GetComponent<Image>();image.sprite=Sprite("Continue","Continue");image.preserveAspect=true;image.color=Color.white;image.raycastTarget=true;view.Primary.targetGraphic=image;
                var colors=view.Primary.colors;colors.pressedColor=new Color(.84f,.84f,.84f);view.Primary.colors=colors;if(!button.GetComponent<SortButtonPressFeedback>())button.gameObject.AddComponent<SortButtonPressFeedback>();view.PrimaryCaption.text="Continue";view.PrimaryCaption.gameObject.SetActive(false);
                Place((RectTransform)view.Secondary.transform,new Vector2(0,-550),new Vector2(240,65));view.Secondary.gameObject.SetActive(false);
                ReplaceEffect(content,"Celebration","Assets/Prefabs/Vfx/EpicToon/Environment/Confetti/Directional/ConfettiDirectionalRainbow.prefab",new Vector2(0,175),new Vector2(650,700),120,true);
                ReplaceEffect(content,"CoinSparkle","Assets/Prefabs/Vfx/EpicToon/Interactive/Loot/ItemSparkle/ItemSparkleYellow.prefab",new Vector2(0,25),new Vector2(430,450),150,false);
                AddReward(root,content,coins,view);
                banner.SetAsLastSibling();view.Body.transform.SetAsLastSibling();button.SetAsLastSibling();view.Secondary.transform.SetAsLastSibling();
                foreach(var coin in root.GetComponent<SortVictoryRewardView>().FlyingCoins)coin.transform.SetAsLastSibling();
                var fit=Child(content,"VictoryLayout");Place(fit,Vector2.zero,new Vector2(660,1180));
                var children=new List<Transform>();for(int i=0;i<content.childCount;i++)if(content.GetChild(i)!=fit)children.Add(content.GetChild(i));foreach(var child in children)child.SetParent(fit,false);
                var layout=fit.GetComponent<SortVictoryLayout>();if(!layout)layout=fit.gameObject.AddComponent<SortVictoryLayout>();layout.BoundsRoot=(RectTransform)root.transform;layout.AuthoredExtent=new Vector2(660,1340);
                root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void AddReward(GameObject root,RectTransform content,RectTransform pile,SortPopupView popup)
        {
            var reward=root.GetComponent<SortVictoryRewardView>();if(!reward)reward=root.AddComponent<SortVictoryRewardView>();reward.Popup=popup;reward.CoinPile=pile;
            var bar=Child(content,"RewardWallet");Place(bar,new Vector2(-185,590),new Vector2(270,103));Picture(bar,CoinImageReplacementArtwork.TrySprite("Hud"));reward.WalletBar=bar;
            var target=Child(bar,"CoinTarget");Place(target,new Vector2(-84,0),new Vector2(90,90));reward.CoinTarget=target;
            var balance=Label(bar,"Balance",new Vector2(40,0),new Vector2(160,80),42,new Color(.53f,0,0),false);balance.text="1600";reward.Balance=balance;
            var count=Label(content,"RewardAmount",new Vector2(0,-240),new Vector2(250,90),72,Color.white,true);count.text="30";reward.Reward=count;
            reward.FlyingCoins=new Image[10];for(int i=0;i<10;i++){var r=Child(content,"FlyingCoin"+i);Place(r,Vector2.zero,new Vector2(58,58));Picture(r,CoinImageReplacementArtwork.TrySprite("Purchase"));reward.FlyingCoins[i]=r.GetComponent<Image>();r.gameObject.SetActive(false);}
        }
        private static Text Label(RectTransform parent,string name,Vector2 pos,Vector2 size,int fontSize,Color color,bool outline)
        {
            var r=Child(parent,name);Place(r,pos,size);var t=r.GetComponent<Text>();if(!t)t=r.gameObject.AddComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Fredoka/Fredoka-Bold.ttf");t.fontSize=fontSize;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=20;t.resizeTextMaxSize=fontSize;
            if(outline){var o=t.GetComponent<Outline>();if(!o)o=t.gameObject.AddComponent<Outline>();o.effectColor=new Color(.03f,.32f,0);o.effectDistance=new Vector2(3,-3);}return t;
        }
        private static void ReplaceEffect(RectTransform content,string name,string path,Vector2 position,Vector2 size,float units,bool confetti)
        {
            var old=content.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var host=Child(content,name);Place(host,position,size);
            var source=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),host);source.transform.localPosition=Vector3.zero;source.transform.localRotation=Quaternion.identity;source.transform.localScale=Vector3.one;
            foreach(var ps in source.GetComponentsInChildren<ParticleSystem>(true)){
                var main=ps.main;main.useUnscaledTime=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;main.maxParticles=confetti?100:32;
                var renderer=ps.GetComponent<ParticleSystemRenderer>();var texture=renderer.sharedMaterial.mainTexture;renderer.enabled=false;
                if(confetti && ps!=source.GetComponent<ParticleSystem>()){ps.gameObject.SetActive(false);continue;}
                var emission=ps.emission;
                if(confetti){main.loop=false;main.duration=.2f;main.startLifetime=new ParticleSystem.MinMaxCurve(2.3f,3.2f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.18f,.32f);main.startRotation=new ParticleSystem.MinMaxCurve(0,6.28f);main.gravityModifier=0;
                    emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,85)});var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(5.1f,.2f,.1f);shape.position=new Vector3(0,1.8f,0);
                    var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;velocity.x=new ParticleSystem.MinMaxCurve(-.5f,.5f);velocity.y=new ParticleSystem.MinMaxCurve(-1.4f,-.8f);velocity.z=new ParticleSystem.MinMaxCurve(0,0);
                }else{main.loop=true;main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.72f,.13f),new Color(1,.97f,.68f));main.startSize=new ParticleSystem.MinMaxCurve(.18f,.38f);main.startLifetime=new ParticleSystem.MinMaxCurve(.4f,.8f);main.startSpeed=.12f;emission.rateOverTime=ps==source.GetComponent<ParticleSystem>()?14:9;
                    var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(2.2f,2.4f,.1f);shape.position=Vector3.zero;}
                var rect=Child(host,ps.name+"Canvas");Place(rect,Vector2.zero,size);var graphic=rect.gameObject.AddComponent<SortVictoryParticleGraphic>();graphic.Source=ps;graphic.ParticleTexture=texture;graphic.PixelsPerUnit=units;graphic.raycastTarget=false;
            }
        }
        private static void ImportArt()
        {
            string path=Art+"Artwork.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);Setup(importer,1024);importer.spriteImportMode=SpriteImportMode.Multiple;
            var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(path));var pixels=tex.GetPixels32();var slices=new List<SpriteMetaData>();
            for(int part=0;part<2;part++){
                int minX=tex.width,minY=tex.height,maxX=-1,maxY=-1;
                for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(pixels[y*tex.width+x].a>128 && (part==0?y>tex.height*.68f:y<=tex.height*.68f)){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
                slices.Add(new SpriteMetaData{name=part==0?"SuccessBanner":"VictoryCoins",rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),pivot=new Vector2(.5f,.5f),alignment=9});
            }
            UnityEngine.Object.DestroyImmediate(tex);importer.spritesheet=slices.ToArray();typeof(HomeSceneBuilder).GetMethod("AssignSliceIdentities",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{importer,path,slices});importer.SaveAndReimport();
            path=Art+"Continue.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);importer=(TextureImporter)AssetImporter.GetAtPath(path);Setup(importer,512);importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();
        }
        private static void Setup(TextureImporter i,int max){i.textureType=TextureImporterType.Sprite;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.maxTextureSize=max;i.textureCompression=TextureImporterCompression.Uncompressed;}
        private static Sprite Sprite(string file,string name){foreach(var a in AssetDatabase.LoadAllAssetsAtPath(Art+file+".png"))if(a is Sprite && a.name==name)return (Sprite)a;throw new Exception("Missing victory sprite "+name);}
        private static RectTransform Child(Transform p,string name){var t=p.Find(name);if(t)return (RectTransform)t;var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);return r;}
        private static void Place(RectTransform r,Vector2 pos,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;}
        private static void Picture(RectTransform r,Sprite sprite){var i=r.GetComponent<Image>();if(!i)i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.color=Color.white;i.preserveAspect=true;i.raycastTarget=false;}
    }
}
