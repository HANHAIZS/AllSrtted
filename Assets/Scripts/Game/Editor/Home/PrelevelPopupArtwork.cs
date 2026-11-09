using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    public static class PrelevelPopupArtwork
    {
        public const string Path="Assets/Resources/Prefabs/UI/Popups/PrelevelPopup.prefab";
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var vacuum=Import("StrongVacuum",512);var time=Import("ExtraTime",512);var idle=ImportTrimmed("BluePlate",512);var check=ImportTrimmed("CheckBadge",256);
            var root=PrefabUtility.LoadPrefabContents(LeavingPopupArtwork.Path);
            try {
                root.name="PrelevelPopup";var popup=root.GetComponent<SortPopupView>();popup.Id="PrelevelPopup";
                var content=(RectTransform)root.transform.Find("PopupContent");var inset=content.Find("Inset");if(inset)UnityEngine.Object.DestroyImmediate(inset.gameObject);
                popup.Title.text="LEVEL 01";popup.Title.fontSize=62;
                popup.Body.text="Select boosters";popup.Body.fontSize=32;Place(popup.Body.rectTransform,new Vector2(0,130),new Vector2(520,75));
                popup.Primary.image.sprite=SpriteAt("Assets/Sprites/UI/BoosterPurchase/Button.png");popup.PrimaryCaption.text="PLAY";popup.PrimaryCaption.fontSize=64;
                var plate=SpriteAt("Assets/Sprites/UI/BottomFunctionButton.png");var plusSprite=SpriteAt("Assets/Sprites/UI/Home/Wallet/PlusBadge.png");var remainingSprite=SpriteAt("Assets/Sprites/UI/Boosters/RemainingChargeBadge.png");
                for(int i=0;i<2;i++){
                    var item=Child(content,"Item"+i);Place(item,new Vector2(i==0?-128:128,-45),new Vector2(210,210));var image=Picture(item,idle,true);var button=item.gameObject.AddComponent<Button>();button.targetGraphic=image;item.gameObject.AddComponent<SortButtonPressFeedback>();
                    var icon=Child(item,"Icon");Place(icon,new Vector2(0,3),new Vector2(165,165));Picture(icon,i==0?vacuum:time,false);
                    var plus=Child(item,"Plus");Place(plus,new Vector2(86,-86),new Vector2(64,64));var plusImage=Picture(plus,plusSprite,true);var add=plus.gameObject.AddComponent<Button>();add.targetGraphic=plusImage;var colors=add.colors;colors.disabledColor=Color.white;add.colors=colors;plus.gameObject.AddComponent<SortButtonPressFeedback>();
                    Label(item,"Name",popup.Body.font,i==0?"Strong Vacuum":"+30 Seconds",25,new Vector2(0,145),new Vector2(245,50));
                    var badge=Child(item,"RemainingBadge");Place(badge,new Vector2(86,-86),new Vector2(64,64));Picture(badge,remainingSprite,false);
                    Label(item,"Count",popup.Body.font,"3",32,new Vector2(86,-86),new Vector2(64,64));
                    var selected=Child(item,"Selected");Place(selected,Vector2.zero,new Vector2(210,210));Picture(selected,plate,false);selected.SetAsFirstSibling();selected.gameObject.SetActive(false);
                    var tick=Child(item,"CheckBadge");Place(tick,new Vector2(86,-86),new Vector2(64,64));Picture(tick,check,false);tick.gameObject.SetActive(false);
                }
                popup.Secondary.transform.SetAsLastSibling();root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static Sprite ImportTrimmed(string name,int max)
        {
            string path="Assets/Sprites/UI/Prelevel/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(System.IO.File.ReadAllBytes(path));int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;var pixels=texture.GetPixels32();
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>128){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
            UnityEngine.Object.DestroyImmediate(texture);if(maxX<0)throw new InvalidOperationException("Empty state artwork: "+name);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=max;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var slices=new List<SpriteMetaData>{new SpriteMetaData{name=name,rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=9,pivot=new Vector2(.5f,.5f)}};importer.spritesheet=slices.ToArray();typeof(HomeSceneBuilder).GetMethod("AssignSliceIdentities",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{importer,path,slices});importer.SaveAndReimport();return SpriteAt(path);
        }
        private static Sprite Import(string name,int max){string path="Assets/Sprites/UI/Prelevel/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.isReadable=false;i.maxTextureSize=max;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
        private static Sprite SpriteAt(string path){foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path))if(a is Sprite)return (Sprite)a;throw new InvalidOperationException("Missing Sprite "+path);}
        private static Image Picture(RectTransform rect,Sprite sprite,bool hit){var i=rect.gameObject.AddComponent<Image>();i.sprite=sprite;i.color=Color.white;i.preserveAspect=true;i.raycastTarget=hit;return i;}
        private static Text Label(Transform p,string n,Font f,string s,int size,Vector2 pos,Vector2 dim){var r=Child(p,n);Place(r,pos,dim);var t=r.gameObject.AddComponent<Text>();t.font=f;t.text=s;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;var o=r.gameObject.AddComponent<Outline>();o.effectColor=new Color(.4f,.15f,.75f);o.effectDistance=new Vector2(1.5f,-1.5f);return t;}
        private static RectTransform Child(Transform p,string n){var r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);return r;}
        private static void Place(RectTransform r,Vector2 p,Vector2 s){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=p;r.sizeDelta=s;}
    }
}
