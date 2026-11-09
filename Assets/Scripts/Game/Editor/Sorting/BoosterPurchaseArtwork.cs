using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    public static class BoosterPurchaseArtwork
    {
        public const string Path="Assets/Resources/Prefabs/UI/Popups/BoosterPurchasePopup.prefab";
        [MenuItem("All Sorted/Apply Booster Purchase Artwork")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            Import("Button",new Rect(0,0,2056,714),765,512);Import("Coin",new Rect(60,50,1141,1151),1254,256);
            var root=PrefabUtility.LoadPrefabContents(AssetDatabase.LoadAssetAtPath<GameObject>(Path)?Path:LeavingPopupArtwork.Path);
            try {
                root.name="BoosterPurchasePopup";var view=root.GetComponent<SortPopupView>();view.Id="BoosterPurchasePopup";
                var content=(RectTransform)root.transform.Find("PopupContent");
                view.Title.text="Vacuum";view.Title.resizeTextForBestFit=false;view.Title.resizeTextMinSize=32;view.Title.resizeTextMaxSize=64;Outline(view.Title,new Color(.4f,.015f,0));
                view.Body.text="Vacuums up a set of three identical items";view.Body.fontSize=32;view.Body.horizontalOverflow=HorizontalWrapMode.Wrap;((RectTransform)view.Body.transform).sizeDelta=new Vector2(530,130);Outline(view.Body,new Color(.4f,.015f,0));
                view.Primary.name="PurchaseButton";view.Primary.image.sprite=Sprite("Button");
                var caption=view.PrimaryCaption;caption.text="1600";caption.fontSize=64;Place((RectTransform)caption.transform,new Vector2(55,5),new Vector2(255,110));Outline(caption,new Color(.1f,.35f,0));
                var coin=Child(view.Primary.transform,"Coin");Place(coin,new Vector2(-115,5),new Vector2(100,100));Picture(coin,Sprite("Coin"));
                var inset=content.Find("Inset");var old=inset.Find("Heart");var icon=old?(RectTransform)old:Child(inset,"BoosterIcon");icon.name="BoosterIcon";Place(icon,new Vector2(-20,0),new Vector2(270,260));Picture(icon,AssetDatabase.LoadAssetAtPath<Sprite>(PrototypeBoosterArtwork.SpriteRoot+"Vacuum.png"));
                var loss=inset.Find("Loss");var quantity=loss?loss.GetComponent<Text>():inset.Find("Quantity").GetComponent<Text>();quantity.name="Quantity";quantity.text="×3";quantity.fontSize=64;Place((RectTransform)quantity.transform,new Vector2(140,-100),new Vector2(125,85));
                var notice=Child(content,"Notice");Place(notice,new Vector2(0,-365),new Vector2(520,48));var label=notice.GetComponent<Text>();if(!label)label=notice.gameObject.AddComponent<Text>();
                label.font=view.Body.font;label.fontSize=22;label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;Outline(label,new Color(.4f,.015f,0));notice.gameObject.SetActive(false);
                PurchasePopupWalletArtwork.ApplyTo(root);
                root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void Import(string name,Rect top,int height,int max)
        {
            string path="Assets/Sprites/UI/BoosterPurchase/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);
            i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Multiple;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.isReadable=false;i.maxTextureSize=max;i.textureCompression=TextureImporterCompression.Uncompressed;
            i.spritesheet=new[]{new SpriteMetaData{name=name,rect=new Rect(top.x,height-top.y-top.height,top.width,top.height),alignment=9,pivot=new Vector2(.5f,.5f)}};i.SaveAndReimport();
        }
        private static Sprite Sprite(string name){if(name=="Coin"){var replacement=CoinImageReplacementArtwork.TrySprite("Purchase");if(replacement)return replacement;}foreach(var a in AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/BoosterPurchase/"+name+".png"))if(a is Sprite)return (Sprite)a;throw new InvalidOperationException("Missing "+name);}
        private static void Picture(RectTransform r,Sprite sprite){var image=r.GetComponent<Image>();if(!image)image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=Color.white;image.preserveAspect=true;image.raycastTarget=false;}
        private static void Outline(Text t,Color color){var o=t.GetComponent<Outline>();if(!o)o=t.gameObject.AddComponent<Outline>();o.effectColor=color;o.effectDistance=new Vector2(2,-2);}
        private static void Place(RectTransform r,Vector2 position,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;}
        private static RectTransform Child(Transform p,string name){var c=p.Find(name);if(c)return (RectTransform)c;var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);return r;}
    }
}
