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
    /// <summary>Authors only the new six-pack Shop subtree and its three controller bindings.</summary>
    public static class ShopCoinArtwork
    {
        public const string ArtRoot="Assets/Sprites/UI/Home/CoinShop/";
        [MenuItem("All Sorted/Apply Shop Coin Packs")]
        public static void ApplySavedScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Save unsaved scene changes first.");
            if(scene.path!=HomeSceneBuilder.ScenePath)scene=EditorSceneManager.OpenScene(HomeSceneBuilder.ScenePath);
            var home=UnityEngine.Object.FindObjectOfType<SortHomeViewController>();if(!home)throw new InvalidOperationException("Home controller unavailable.");
            ApplyToHome(home);EditorUtility.SetDirty(home);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void ApplyToHome(SortHomeViewController home)
        {
            var card=Import("Card",1024);var purchase=Import("Button",512);var icons=new Sprite[6];for(int i=0;i<6;i++)icons[i]=CoinImageReplacementArtwork.TrySprite("Pack"+(i+1))??Import("Coins"+(i+1),512);
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Fredoka/Fredoka-Bold.ttf");if(!font)throw new InvalidOperationException("Shop font unavailable.");
            var root=Child(home.Pages[0].transform,"CoinPacks");Place(root,Vector2.zero,new Vector2(666,670));home.CoinShopRoot=root;home.CoinPackButtons=new Button[6];
            for(int i=0;i<6;i++) {
                var pack=Child(root,"Pack"+i);Place(pack,new Vector2((i%3-1)*228,189-(i/3)*318),new Vector2(210,292));Picture(pack,card,false,false);
                var icon=Child(pack,"Icon");Place(icon,new Vector2(0,45),new Vector2(176,166));Picture(icon,icons[i],true,false);
                var amount=Label(pack,"Amount",font,SortCoinPackCatalog.GetDisplayAmount(i),36,new Vector2(0,-37),new Vector2(210,64));amount.color=new Color(1,.81f,.32f);
                var outlines=amount.GetComponents<Outline>();for(int j=1;j<outlines.Length;j++)UnityEngine.Object.DestroyImmediate(outlines[j]);
                var amountStroke=amount.GetComponent<Outline>();if(!amountStroke)amountStroke=amount.gameObject.AddComponent<Outline>();amountStroke.effectColor=new Color(.55f,.34f,.12f);amountStroke.effectDistance=new Vector2(1,-1);
                var buy=Child(pack,"Purchase");Place(buy,new Vector2(0,-102),new Vector2(176,64));var image=Picture(buy,purchase,false,true);var button=buy.GetComponent<Button>();if(!button)button=buy.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.ColorTint;var colors=button.colors;colors.pressedColor=new Color(.84f,.84f,.84f);button.colors=colors;
                if(!buy.GetComponent<SortButtonPressFeedback>())buy.gameObject.AddComponent<SortButtonPressFeedback>();home.CoinPackButtons[i]=button;
                var price=Label(buy,"Price",font,SortCoinPackCatalog.GetPrice(i),28,new Vector2(19,0),new Vector2(124,62));var stroke=price.GetComponent<Outline>();if(!stroke)stroke=price.gameObject.AddComponent<Outline>();stroke.effectColor=new Color(.015f,.10f,.32f);stroke.effectDistance=new Vector2(1,-1);
                var prefix=Label(buy,"US",font,"US",28,Vector2.zero,new Vector2(48,62));var prefixStroke=prefix.GetComponent<Outline>();if(!prefixStroke)prefixStroke=prefix.gameObject.AddComponent<Outline>();prefixStroke.effectColor=stroke.effectColor;prefixStroke.effectDistance=stroke.effectDistance;
                float prefixWidth=prefix.preferredWidth,priceWidth=price.preferredWidth,gap=4,totalWidth=prefixWidth+gap+priceWidth;
                Place(prefix.rectTransform,new Vector2((prefixWidth-totalWidth)*.5f,0),new Vector2(prefixWidth,62));Place(price.rectTransform,new Vector2((totalWidth-priceWidth)*.5f,0),new Vector2(priceWidth,62));
            }
            home.CoinShopNotice=Label(root,"Notice",font,"Simulated purchases · No charge",18,new Vector2(0,-311),new Vector2(666,48));home.CoinShopNotice.color=new Color(1,.94f,.73f);home.CoinShopNotice.horizontalOverflow=HorizontalWrapMode.Wrap;
        }
        private static Sprite Import(string name,int max)
        {
            string path=ArtRoot+name+".png";if(!File.Exists(path))throw new FileNotFoundException("Shop art missing",path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;var pixels=texture.GetPixels32();
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>128){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            int width=texture.width,height=texture.height;UnityEngine.Object.DestroyImmediate(texture);if(maxX<0)throw new InvalidOperationException("Empty Shop art: "+name);
            minX=Mathf.Max(0,minX-3);minY=Mathf.Max(0,minY-3);maxX=Mathf.Min(width-1,maxX+3);maxY=Mathf.Min(height-1,maxY+3);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=max;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var slices=new List<SpriteMetaData>{new SpriteMetaData{name=name,rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=9,pivot=new Vector2(.5f,.5f)}};importer.spritesheet=slices.ToArray();typeof(HomeSceneBuilder).GetMethod("AssignSliceIdentities",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{importer,path,slices});importer.SaveAndReimport();
            foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path))if(a is Sprite)return (Sprite)a;throw new InvalidOperationException("Shop sprite missing: "+name);
        }
        private static Text Label(Transform parent,string name,Font font,string value,int size,Vector2 position,Vector2 dimensions){var r=Child(parent,name);Place(r,position,dimensions);var t=r.GetComponent<Text>();if(!t)t=r.gameObject.AddComponent<Text>();t.font=font;t.fontStyle=FontStyle.Normal;t.text=value;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;}
        private static Image Picture(RectTransform r,Sprite sprite,bool aspect,bool hit){var image=r.GetComponent<Image>();if(!image)image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=aspect;image.raycastTarget=hit;return image;}
        private static void Place(RectTransform r,Vector2 position,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;}
        private static RectTransform Child(Transform p,string name){var c=p.Find(name);if(c)return (RectTransform)c;var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);return r;}
    }
}
