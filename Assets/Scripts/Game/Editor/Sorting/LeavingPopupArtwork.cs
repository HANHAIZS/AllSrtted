using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    public static class LeavingPopupArtwork
    {
        public const string Path="Assets/Resources/Prefabs/UI/Popups/LeavingPopup.prefab";
        [MenuItem("All Sorted/Apply Leaving Confirmation Artwork")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            Import("Heart",new Rect(97,163,1056,937),512);Import("LeaveButton",new Rect(0,0,2057,714),512);Import("Inset",new Rect(0,0,564,409),1024);
            var root=PrefabUtility.LoadPrefabContents(AssetDatabase.LoadAssetAtPath<GameObject>(Path)?Path:PausePopupArtwork.PrefabPath);
            try {
                root.name="LeavingPopup";var popup=root.GetComponent<SortPopupView>();popup.Id="LeavingPopup";
                var settings=root.GetComponent<SortPauseSettingsView>();if(settings)UnityEngine.Object.DestroyImmediate(settings);
                var content=(RectTransform)root.transform.Find("PopupContent");content.sizeDelta=new Vector2(620,822);content.localScale=Vector3.one*.85f;
                var header=Child(content,"Header");Place(header,new Vector2(0,-16),new Vector2(413,146),new Vector2(.5f,1));
                Remove(header,"PausedLabel");Remove(content,"SettingsRow");
                var oldPrimary=popup.Primary;var oldSecondary=popup.Secondary;
                var leave=Child(content,"QuitButton");Place(leave,new Vector2(0,-265),new Vector2(420,150),new Vector2(.5f,.5f));
                var image=leave.GetComponent<Image>();image.sprite=Sprite("LeaveButton");image.preserveAspect=true;image.raycastTarget=true;
                popup.Primary=leave.GetComponent<Button>();popup.Primary.targetGraphic=image;
                // The original close button is retained as confirmation cancellation.
                if(oldPrimary!=popup.Primary)popup.Secondary=oldPrimary;
                var close=(RectTransform)popup.Secondary.transform;Place(close,new Vector2(-40,-55),new Vector2(102,122),Vector2.one);
                var title=popup.Title;title.transform.SetParent(header,false);Place((RectTransform)title.transform,new Vector2(0,12),new Vector2(370,100),new Vector2(.5f,.5f));Style(title,"Leaving",64,new Color(.42f,.2f,.86f));
                var caption=popup.PrimaryCaption;caption.transform.SetParent(leave,false);Place((RectTransform)caption.transform,new Vector2(0,5),new Vector2(380,110),new Vector2(.5f,.5f));Style(caption,"Leave",60,new Color(.4f,.015f,0));
                popup.SecondaryCaption.transform.SetParent(close,false);popup.SecondaryCaption.gameObject.SetActive(false);
                if(oldSecondary && oldSecondary!=popup.Secondary && oldSecondary!=popup.Primary)UnityEngine.Object.DestroyImmediate(oldSecondary.gameObject);
                var body=popup.Body;body.transform.SetParent(content,false);Place((RectTransform)body.transform,new Vector2(0,-125),new Vector2(540,70),new Vector2(.5f,.5f));Style(body,"You will lose 1 life",34,new Color(.42f,.2f,.86f));
                var inset=Child(content,"Inset");Place(inset,new Vector2(0,-270),new Vector2(510,370),new Vector2(.5f,1));Picture(inset,Sprite("Inset"));
                var heart=Child(inset,"Heart");Place(heart,Vector2.zero,new Vector2(260,245),new Vector2(.5f,.5f));Picture(heart,Sprite("Heart"));
                var loss=Child(inset,"Loss");Place(loss,new Vector2(100,-90),new Vector2(110,80),new Vector2(.5f,.5f));var lossText=loss.GetComponent<Text>();if(!lossText)lossText=loss.gameObject.AddComponent<Text>();Style(lossText,"-1",64,new Color(.4f,.015f,0));
                close.SetAsLastSibling();root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,Path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void Import(string name,Rect top,int max)
        {
            string path="Assets/Sprites/UI/Leaving/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);
            i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Multiple;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.isReadable=false;i.maxTextureSize=max;i.textureCompression=TextureImporterCompression.Uncompressed;
            int height=name=="Heart"?1254:name=="LeaveButton"?765:409;
            i.spritesheet=new[]{new SpriteMetaData{name=name,rect=new Rect(top.x,height-top.y-top.height,top.width,top.height),alignment=9,pivot=new Vector2(.5f,.5f)}};i.SaveAndReimport();
        }
        private static Sprite Sprite(string name){foreach(var a in AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Leaving/"+name+".png"))if(a is Sprite)return (Sprite)a;throw new InvalidOperationException("Missing "+name);}
        private static void Picture(RectTransform rect,Sprite sprite){var image=rect.GetComponent<Image>();if(!image)image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=Color.white;image.preserveAspect=true;image.raycastTarget=false;}
        private static void Style(Text text,string value,int size,Color outlineColor)
        {
            text.gameObject.SetActive(true);text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Fredoka/Fredoka-Bold.ttf");text.fontStyle=FontStyle.Normal;text.fontSize=size;text.text=value;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.resizeTextForBestFit=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;
            var outline=text.GetComponent<Outline>();if(!outline)outline=text.gameObject.AddComponent<Outline>();outline.effectColor=outlineColor;outline.effectDistance=new Vector2(2,-2);
        }
        private static void Place(RectTransform r,Vector2 position,Vector2 size,Vector2 anchor){r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;}
        private static RectTransform Child(Transform p,string name){var c=p.Find(name);if(c)return (RectTransform)c;var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);return r;}
        private static void Remove(Transform p,string name){var c=p.Find(name);if(c)UnityEngine.Object.DestroyImmediate(c.gameObject);}
    }
}
