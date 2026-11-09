using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    /// <summary>Authors the bundle cards and scroll host without rebuilding existing coin cards.</summary>
    public static class ShopBundleArtwork
    {
        public const string ArtRoot="Assets/Sprites/UI/Home/Bundles/";
        [MenuItem("All Sorted/Apply Shop Bundles")]
        public static void ApplySavedScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Save unsaved changes first.");
            if(scene.path!=HomeSceneBuilder.ScenePath)scene=EditorSceneManager.OpenScene(HomeSceneBuilder.ScenePath);
            var home=UnityEngine.Object.FindObjectOfType<SortHomeViewController>();if(!home)throw new InvalidOperationException("Home controller missing.");
            ApplyToHome(home);EditorUtility.SetDirty(home);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void ApplyToHome(SortHomeViewController home)
        {
            var numberFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Fredoka/Fredoka-Bold.ttf");
            var wordFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/LilitaOne/LilitaOne-Regular.ttf");
            var card=Import("Card",1024);var heart=Import("Infinite",256);var icons=new[]{Import("Small",512),Import("Medium",512),Import("Big",512)};
            var boosters=new[]{Sprite("Assets/Sprites/UI/Boosters/Vacuum.png"),Sprite("Assets/Sprites/UI/Boosters/Shuffle.png"),Sprite("Assets/Sprites/UI/Boosters/GoldenCard.png"),Sprite("Assets/Sprites/UI/Boosters/FreezeWatch.png")};
            var box=Sprite("Assets/Sprites/UI/Prelevel/StrongVacuum.png");var alarm=Sprite("Assets/Sprites/UI/Prelevel/ExtraTime.png");var purchase=Sprite(ShopCoinArtwork.ArtRoot+"Button.png");
            var viewport=Child(home.Pages[0].transform,"ShopScroll");viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=new Vector2(20,210);viewport.offsetMax=new Vector2(-20,-260);
            var hit=viewport.GetComponent<Image>();if(!hit)hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            if(!viewport.GetComponent<RectMask2D>())viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.GetComponent<ScrollRect>();if(!scroll)scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.inertia=true;scroll.decelerationRate=.12f;scroll.scrollSensitivity=42;
            var content=Child(viewport,"Content");content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(666,1620);content.anchoredPosition=Vector2.zero;scroll.content=content;home.ShopScroll=scroll;
            var bundles=Child(content,"Bundles");Place(bundles,new Vector2(0,-450),new Vector2(666,900));bundles.anchorMin=bundles.anchorMax=new Vector2(.5f,1);home.BundleRoot=bundles;home.BundleButtons=new Button[3];
            for(int i=0;i<3;i++) {
                var row=Child(bundles,"Bundle"+i);Place(row,new Vector2(0,300-i*300),new Vector2(666,282));Picture(row,card,false,false);
                var icon=Child(row,"Icon");Place(icon,new Vector2(-195,35),new Vector2(236,180));Picture(icon,icons[i],true,false);
                Label(row,"Coins",numberFont,SortBundleCatalog.GetCoins(i).ToString(),48,new Vector2(-195,-38),new Vector2(265,62));
                for(int b=0;b<4;b++) {
                    var item=Child(row,"Booster"+b);Place(item,new Vector2(19+b%2*81,77-b/2*75),new Vector2(64,64));Picture(item,boosters[b],true,false);
                    Label(item,"Count",numberFont,"×"+SortBundleCatalog.GetCharges(i),24,new Vector2(20,-21),new Vector2(64,34));
                }
                var entryIcons=new[]{box,alarm};
                for(int b=0;b<2;b++) {
                    var item=Child(row,b==0?"VacuumBox":"TimeAlarm");Place(item,new Vector2(195+b*76,77),new Vector2(64,64));Picture(item,entryIcons[b],true,false);
                    Label(item,"Count",numberFont,"×"+SortBundleCatalog.GetCharges(i),24,new Vector2(20,-21),new Vector2(64,34));
                }
                var infinite=Child(row,"UnlimitedLives");Place(infinite,new Vector2(235,9),new Vector2(70,60));Picture(infinite,heart,true,false);
                Label(infinite,"Duration",numberFont,SortBundleCatalog.GetHours(i)+"h",25,new Vector2(0,-31),new Vector2(90,34));
                Label(row,"Name",wordFont,SortBundleCatalog.GetName(i),32,new Vector2(-166,-101),new Vector2(305,60));
                var buy=Child(row,"Purchase");Place(buy,new Vector2(202,-101),new Vector2(230,72));var image=Picture(buy,purchase,false,true);var button=buy.GetComponent<Button>();if(!button)button=buy.gameObject.AddComponent<Button>();button.targetGraphic=image;
                var colors=button.colors;colors.pressedColor=new Color(.84f,.84f,.84f);button.colors=colors;if(!buy.GetComponent<SortButtonPressFeedback>())buy.gameObject.AddComponent<SortButtonPressFeedback>();
                Label(buy,"Price",numberFont,SortBundleCatalog.GetPrice(i),34,Vector2.zero,new Vector2(214,64));home.BundleButtons[i]=button;
            }
            home.BundleNotice=Label(bundles,"Notice",wordFont,string.Empty,18,new Vector2(0,-445),new Vector2(666,34));home.BundleNotice.gameObject.SetActive(false);
            home.CoinShopRoot.SetParent(content,false);home.CoinShopRoot.anchorMin=home.CoinShopRoot.anchorMax=new Vector2(.5f,1);home.CoinShopRoot.localScale=Vector3.one;home.CoinShopRoot.anchoredPosition=new Vector2(0,-1275);
            if(File.Exists(ArtRoot+"NoAds.png"))ApplyNoAdsOffer(home);
            scroll.verticalNormalizedPosition=1;
        }
        /// <summary>Adds the display-only offer without rebuilding any existing product card.</summary>
        public static void ApplyNoAdsOffer(SortHomeViewController home)
        {
            if(!home.ShopScroll || !home.ShopScroll.content || !home.BundleRoot)throw new InvalidOperationException("Shop scroll is missing.");
            var offer=Child(home.ShopScroll.content,"NoAdsOffer");
            Place(offer,new Vector2(0,-117),new Vector2(666,234));offer.anchorMin=offer.anchorMax=new Vector2(.5f,1);
            Picture(offer,Import("NoAds",1024),false,false);offer.SetAsFirstSibling();home.NoAdsOffer=offer;
            var buy=Child(offer,"Purchase");Place(buy,new Vector2(211,-64),new Vector2(218,70));
            var image=Picture(buy,Sprite(ShopCoinArtwork.ArtRoot+"Button.png"),false,true);
            var button=buy.GetComponent<Button>();if(!button)button=buy.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.pressedColor=new Color(.84f,.84f,.84f);button.colors=colors;
            if(!buy.GetComponent<SortButtonPressFeedback>())buy.gameObject.AddComponent<SortButtonPressFeedback>();
            Label(buy,"Price",AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Fredoka/Fredoka-Bold.ttf"),"US$6.99",34,Vector2.zero,new Vector2(204,64));
            home.ShopScroll.content.sizeDelta=new Vector2(666,1880);
            home.BundleRoot.anchoredPosition=new Vector2(0,-710);
            home.CoinShopRoot.anchoredPosition=new Vector2(0,-1535);
            home.ShopScroll.verticalNormalizedPosition=1;
        }
        private static Sprite Sprite(string path){foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path))if(a is Sprite)return (Sprite)a;throw new InvalidOperationException("Sprite missing: "+path);}
        private static Sprite Import(string name,int max)
        {
            string path=ArtRoot+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new FileNotFoundException(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=max;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;var pixels=texture.GetPixels32();
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>128){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            if(maxX<0)throw new InvalidOperationException("Empty bundle art: "+path);
            var slices=new List<SpriteMetaData>{new SpriteMetaData{name=name,rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=9,pivot=new Vector2(.5f,.5f)}};UnityEngine.Object.DestroyImmediate(texture);
            importer.spritesheet=slices.ToArray();typeof(HomeSceneBuilder).GetMethod("AssignSliceIdentities",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{importer,path,slices});
            importer.SaveAndReimport();return Sprite(path);
        }
        private static Text Label(Transform p,string name,Font font,string value,int size,Vector2 position,Vector2 dimensions)
        {
            var rect=Child(p,name);Place(rect,position,dimensions);var t=rect.GetComponent<Text>();if(!t)t=rect.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;
            var stroke=t.GetComponent<Outline>();if(!stroke)stroke=t.gameObject.AddComponent<Outline>();stroke.effectColor=new Color(.07f,.04f,.30f);stroke.effectDistance=new Vector2(1.5f,-1.5f);return t;
        }
        private static Image Picture(RectTransform rect,Sprite sprite,bool aspect,bool hit){var image=rect.GetComponent<Image>();if(!image)image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=Color.white;image.preserveAspect=aspect;image.raycastTarget=hit;return image;}
        private static RectTransform Child(Transform p,string name){var found=p.Find(name);if(found)return (RectTransform)found;var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(p,false);return rect;}
        private static void Place(RectTransform rect,Vector2 position,Vector2 size){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;}
    }
}
