using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    /// <summary>Authors only the four booster badges, feedback components and their direct references.</summary>
    public static class PrototypeQuotaArtwork
    {
        private static readonly string[] Names={"AddChargeBadge","RemainingChargeBadge"};

        /// <summary>Uses metadata bounds to exclude transparent margins without changing source pixels.</summary>
        private static void ImportBadges()
        {
            foreach(string name in Names) {
                string path=PrototypeBoosterArtwork.SpriteRoot+name+".png";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));
                int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;var pixels=texture.GetPixels32();
                for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++) if(pixels[y*texture.width+x].a>24) {
                    minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                }
                UnityEngine.Object.DestroyImmediate(texture);
                if(maxX<minX || maxY<minY) throw new InvalidOperationException("Empty quota badge: "+name);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=512;
                importer.spritesheet=new[]{new SpriteMetaData{name=name,rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=9,pivot=new Vector2(.5f,.5f)}};
                importer.SaveAndReimport();
            }
        }

        /// <summary>Adds one independent badge hit target per booster without changing any base-button rect.</summary>
        public static void ApplyTo(SortGameViewController view)
        {
            view.AddChargeBadgeSprite=AssetDatabase.LoadAssetAtPath<Sprite>(PrototypeBoosterArtwork.SpriteRoot+Names[0]+".png");
            view.RemainingChargeBadgeSprite=AssetDatabase.LoadAssetAtPath<Sprite>(PrototypeBoosterArtwork.SpriteRoot+Names[1]+".png");
            if(!view.AddChargeBadgeSprite || !view.RemainingChargeBadgeSprite) throw new InvalidOperationException("Import quota badge Sprites first.");
            view.BoosterBadges=new Button[4];view.BoosterChargeLabels=new Text[4];
            for(int i=0;i<4;i++) {
                var parent=view.BoosterButtons[i];
                if(!parent.GetComponent<SortButtonPressFeedback>()) parent.gameObject.AddComponent<SortButtonPressFeedback>();
                var rect=Rect("QuotaBadge",parent.transform);rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(1,0);
                rect.anchoredPosition=new Vector2(4,-4);rect.sizeDelta=new Vector2(44,44);rect.localScale=Vector3.one;rect.SetAsLastSibling();
                var image=rect.GetComponent<Image>();if(!image) image=rect.gameObject.AddComponent<Image>();
                image.sprite=view.RemainingChargeBadgeSprite;image.preserveAspect=true;image.type=Image.Type.Simple;image.color=Color.white;image.raycastTarget=true;
                var button=rect.GetComponent<Button>();if(!button) button=rect.gameObject.AddComponent<Button>();
                button.targetGraphic=image;button.interactable=true;button.transition=Selectable.Transition.ColorTint;
                var colors=button.colors;colors.pressedColor=new Color(.88f,.88f,.88f);button.colors=colors;
                if(!rect.GetComponent<SortButtonPressFeedback>()) rect.gameObject.AddComponent<SortButtonPressFeedback>();
                var count=Rect("Count",rect);count.anchorMin=Vector2.zero;count.anchorMax=Vector2.one;count.offsetMin=count.offsetMax=Vector2.zero;
                var text=count.GetComponent<Text>();if(!text) text=count.gameObject.AddComponent<Text>();
                text.font=view.LevelLabel.font;text.fontStyle=FontStyle.Bold;text.fontSize=25;text.alignment=TextAnchor.MiddleCenter;
                text.color=Color.white;text.text="3";text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;
                var outline=count.GetComponent<Outline>();if(!outline) outline=count.gameObject.AddComponent<Outline>();
                outline.effectColor=new Color(.4f,0,0,.85f);outline.effectDistance=new Vector2(1,-1);
                view.BoosterBadges[i]=button;view.BoosterChargeLabels[i]=text;
            }
            var freeze=(RectTransform)view.FreezeCounter.transform;freeze.anchoredPosition=new Vector2(-12,13);freeze.sizeDelta=new Vector2(60,26);
            EditorUtility.SetDirty(view);
        }

        /// <summary>Saves through Editor APIs and verifies every existing component outside the requested boundary.</summary>
        [MenuItem("All Sorted/Apply Quota Badges")]
        public static void ApplySavedScene()
        {
            ImportBadges();const string path="Assets/Scenes/GameScene.unity";
            var scene=SceneManager.GetSceneByPath(path);
            if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SortGameViewController view=null;
            foreach(var root in scene.GetRootGameObjects()) {view=root.GetComponentInChildren<SortGameViewController>(true);if(view) break;}
            if(!view) throw new InvalidOperationException("GameScene controller missing.");
            var protectedState=new Dictionary<Component,string>();
            foreach(var root in scene.GetRootGameObjects()) foreach(var c in root.GetComponentsInChildren<Component>(true)) {
                if(!c || c==view || (c is RectTransform && c==view.FreezeCounter.transform) || IsBadge(c.transform)) continue;
                protectedState[c]=EditorJsonUtility.ToJson(c);
            }
            ApplyTo(view);
            foreach(var pair in protectedState) if(EditorJsonUtility.ToJson(pair.Key)!=pair.Value) throw new InvalidOperationException("Protected component changed: "+pair.Key.name+" "+pair.Key.GetType().Name);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/quota-authoring.txt","Scene="+path+"\nProtectedComponentsUnchanged="+protectedState.Count+"\nBadgeSize=44x44\nDefaultQuota=3 each\n");
        }

        /// <summary>Identifies only the feature-owned badge subtree for repeatable authoring.</summary>
        private static bool IsBadge(Transform value)
        {
            for(var node=value;node;node=node.parent) if(node.name=="QuotaBadge") return true;return false;
        }

        /// <summary>Reuses a feature-owned child or creates it under the existing UI parent.</summary>
        private static RectTransform Rect(string name,Transform parent)
        {
            var value=parent.Find(name) as RectTransform;if(value) return value;
            value=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();value.SetParent(parent,false);return value;
        }
    }
}
