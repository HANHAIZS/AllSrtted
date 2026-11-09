using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    /// <summary>Imports original transparent art and authors only the existing pause prefab.</summary>
    public static class PausePopupArtwork
    {
        public const string SpriteRoot="Assets/Sprites/UI/PauseSettings/";
        public const string PrefabPath="Assets/Resources/Prefabs/UI/Popups/PausePopup.prefab";
        private static readonly string[] Names={"Background","Header","PausedText","SettingsRow","Quit","Restart","Close"};
        // Top-left pixel regions partition the original row without modifying its pixels.
        private static readonly Rect[] Buttons={new Rect(38,87,160,155),new Rect(229,87,156,155),new Rect(416,87,161,155)};
        private static readonly Rect[] Strips={new Rect(0,0,602,87),new Rect(0,242,602,20),new Rect(0,87,38,155),new Rect(198,87,31,155),new Rect(385,87,31,155),new Rect(577,87,25,155)};

        /// <summary>Uses source PNGs unchanged; each row region gets native Sprite metadata.</summary>
        public static void ImportSprites()
        {
            foreach(string name in Names) {
                string path=SpriteRoot+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;
                if(name=="SettingsRow") {
                    importer.spriteImportMode=SpriteImportMode.Multiple;var regions=new List<SpriteMetaData>();
                    for(int i=0;i<Strips.Length;i++) regions.Add(Meta("RowStrip"+i,Strips[i]));
                    for(int i=0;i<Buttons.Length;i++) regions.Add(Meta("SettingButton"+i,Buttons[i]));
                    importer.spritesheet=regions.ToArray();
                } else importer.spriteImportMode=SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
        }

        /// <summary>Converts top-left design pixels to the bottom-left Sprite importer convention.</summary>
        private static SpriteMetaData Meta(string name,Rect rect)
        {
            return new SpriteMetaData{name=name,rect=new Rect(rect.x,262-rect.y-rect.height,rect.width,rect.height),alignment=9,pivot=new Vector2(.5f,.5f)};
        }

        /// <summary>Preserves prefab identity, required roots and the existing close/restart action references.</summary>
        [MenuItem("All Sorted/Apply Pause Settings Artwork")]
        public static void ApplySavedPrefab()
        {
            ImportSprites();var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try {
                var popup=root.GetComponent<SortPopupView>();var content=(RectTransform)root.transform.Find("PopupContent");
                if(!popup || !content) throw new InvalidOperationException("Existing pause prefab structure is missing.");
                Layout(content,Vector2.zero,new Vector2(620,822),new Vector2(.5f,.5f));Image(content,Sprite("Background"),false);
                content.localScale=Vector3.one*.85f;
                popup.Title.gameObject.SetActive(false);popup.Body.gameObject.SetActive(false);popup.PrimaryCaption.gameObject.SetActive(false);popup.SecondaryCaption.gameObject.SetActive(false);
                var header=Rect("Header",content);Layout(header,new Vector2(0,-46),new Vector2(413,146),new Vector2(.5f,1));Image(header,Sprite("Header"),false);
                var label=Rect("PausedLabel",header);Layout(label,new Vector2(0,11),new Vector2(221,57.5f),new Vector2(.5f,.5f));Image(label,Sprite("PausedText"),false);
                var close=popup.Primary;Layout((RectTransform)close.transform,new Vector2(-40,-70),new Vector2(102,122),Vector2.one);Skin(close,Sprite("Close"));close.transform.SetAsLastSibling();
                var restart=popup.Secondary;Layout((RectTransform)restart.transform,new Vector2(0,-245),new Vector2(370,154),new Vector2(.5f,.5f));Skin(restart,Sprite("Restart"));restart.gameObject.SetActive(true);
                var quit=Rect("QuitButton",content);Layout(quit,new Vector2(0,-60),new Vector2(370,154),new Vector2(.5f,.5f));var quitButton=Button(quit);Skin(quitButton,Sprite("Quit"));
                var row=Rect("SettingsRow",content);Layout(row,new Vector2(0,-135),new Vector2(516,262*516f/602),new Vector2(.5f,1));
                row.pivot=new Vector2(.5f,1);
                var backdrop=row.GetComponent<UnityEngine.UI.Image>();if(!backdrop) backdrop=row.gameObject.AddComponent<UnityEngine.UI.Image>();backdrop.sprite=null;backdrop.color=Color.clear;backdrop.raycastTarget=false;
                var settings=root.GetComponent<SortPauseSettingsView>();if(!settings) settings=root.AddComponent<SortPauseSettingsView>();
                settings.QuitButton=quitButton;settings.ToggleButtons=new Button[3];settings.DisabledMarks=new GameObject[3];
                float factor=516f/602;
                for(int i=0;i<Strips.Length;i++) {var strip=Rect("Strip"+i,row);RegionLayout(strip,Strips[i],factor);Image(strip,RowSprite("RowStrip"+i),false);}
                for(int i=0;i<3;i++) {
                    var rect=Rect("SettingButton_"+(i+1),row);RegionLayout(rect,Buttons[i],factor);var button=Button(rect);Skin(button,RowSprite("SettingButton"+i));settings.ToggleButtons[i]=button;
                    var mark=Rect("DisabledMark",rect);Layout(mark,new Vector2(0,2),new Vector2(116,116),new Vector2(.5f,.5f));
                    var oldText=mark.GetComponent<Text>();if(oldText) UnityEngine.Object.DestroyImmediate(oldText);
                    var oldOutline=mark.GetComponent<Outline>();if(oldOutline) UnityEngine.Object.DestroyImmediate(oldOutline);
                    var dim=Image(mark,AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),false);dim.color=new Color(0,0,0,.40f);
                    var slash=Rect("OffSlash",mark);Layout(slash,Vector2.zero,new Vector2(82,9),new Vector2(.5f,.5f));slash.localRotation=Quaternion.Euler(0,0,-45);
                    var stroke=Image(slash,null,false);stroke.color=new Color(1,.05f,.1f,1);
                    mark.gameObject.SetActive(false);settings.DisabledMarks[i]=mark.gameObject;
                }
                close.transform.SetAsLastSibling();root.SetActive(false);EditorUtility.SetDirty(popup);EditorUtility.SetDirty(settings);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }

        /// <summary>Places a Sprite partition at the same source-relative position with a central press pivot.</summary>
        private static void RegionLayout(RectTransform rect,Rect region,float factor)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2((region.x+region.width/2)*factor,-(region.y+region.height/2)*factor);rect.sizeDelta=region.size*factor;
        }

        private static Sprite Sprite(string name) {return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot+name+".png");}
        private static Sprite RowSprite(string name)
        {
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(SpriteRoot+"SettingsRow.png")) if(asset is Sprite && asset.name==name) return (Sprite)asset;
            throw new InvalidOperationException("Missing settings row Sprite: "+name);
        }
        private static RectTransform Rect(string name,Transform parent)
        {
            var existing=parent.Find(name) as RectTransform;if(existing) return existing;
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
        }
        private static void Layout(RectTransform rect,Vector2 position,Vector2 size,Vector2 anchor)
        {
            rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;
        }
        private static Image Image(RectTransform rect,Sprite sprite,bool raycast)
        {
            var image=rect.GetComponent<Image>();if(!image) image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=true;image.color=Color.white;image.raycastTarget=raycast;return image;
        }
        private static Button Button(RectTransform rect)
        {
            var button=rect.GetComponent<Button>();if(!button) button=rect.gameObject.AddComponent<Button>();return button;
        }
        private static void Skin(Button button,Sprite sprite)
        {
            var image=Image((RectTransform)button.transform,sprite,true);button.targetGraphic=image;button.interactable=true;
            button.transition=Selectable.Transition.ColorTint;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.84f,.84f,.84f);button.colors=colors;
            if(!button.GetComponent<SortButtonPressFeedback>()) button.gameObject.AddComponent<SortButtonPressFeedback>();
        }
    }
}
