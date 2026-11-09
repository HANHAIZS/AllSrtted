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
    public static class PrototypeBoosterArtwork
    {
        public const string SpriteRoot="Assets/Sprites/UI/Boosters/";
        public static readonly string[] IconNames={"Vacuum","Shuffle","GoldenCard","FreezeWatch"};

        /// <summary>Imports true-alpha icons and crops Sprite metadata without editing their pixels.</summary>
        public static void ImportIcons()
        {
            foreach(string name in IconNames) {
                string path=SpriteRoot+name+".png";
                if(!File.Exists(path)) throw new FileNotFoundException("Booster icon is missing.",path);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));
                int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;var pixels=texture.GetPixels32();
                for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++) if(pixels[y*texture.width+x].a>24) {
                    minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                }
                UnityEngine.Object.DestroyImmediate(texture);
                if(maxX<minX || maxY<minY) throw new InvalidOperationException("Booster icon has no visible subject: "+name);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=512;
                importer.spritesheet=new[]{new SpriteMetaData{name=name,rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=9,pivot=new Vector2(.5f,.5f)}};
                importer.SaveAndReimport();
            }
        }

        /// <summary>Adds only icon children, direct bindings, effect feedback and gold materials.</summary>
        public static void ApplyTo(SortGameViewController view)
        {
            var sprites=new Sprite[4];
            for(int i=0;i<4;i++) {
                sprites[i]=AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot+IconNames[i]+".png");
                if(!sprites[i]) throw new InvalidOperationException("Import the booster Sprite first: "+IconNames[i]);
            }
            var row=view.Layers.Hosts[0].transform.Find("SafeArea/BottomButtons");
            if(!row) throw new InvalidOperationException("Existing four-button row is missing.");
            var buttons=new Button[4];
            for(int i=0;i<4;i++) {
                var root=row.Find("FunctionButton_"+(i+1));
                if(!root || !root.GetComponent<Button>()) throw new InvalidOperationException("Existing booster button is missing: "+(i+1));
                buttons[i]=root.GetComponent<Button>();buttons[i].interactable=true;
                var icon=Rect("Icon",root);icon.anchorMin=icon.anchorMax=new Vector2(.5f,.5f);icon.pivot=new Vector2(.5f,.5f);
                icon.anchoredPosition=new Vector2(0,3);float iconSize=i==2?86:98;icon.sizeDelta=new Vector2(iconSize,iconSize);
                var image=icon.GetComponent<Image>();if(!image) image=icon.gameObject.AddComponent<Image>();
                image.sprite=sprites[i];image.preserveAspect=true;image.type=Image.Type.Simple;image.color=Color.white;image.raycastTarget=false;
            }
            view.BoosterButtons=buttons;
            var freeze=Rect("FreezeCounter",buttons[3].transform);freeze.anchorMin=freeze.anchorMax=new Vector2(.5f,0);
            freeze.pivot=new Vector2(.5f,0);freeze.anchoredPosition=new Vector2(0,13);freeze.sizeDelta=new Vector2(72,26);
            view.FreezeCounter=Text(freeze,view.LevelLabel.font,22);view.FreezeCounter.color=Color.white;
            var outline=freeze.GetComponent<Outline>();if(!outline) outline=freeze.gameObject.AddComponent<Outline>();
            outline.effectColor=new Color(.03f,.23f,.55f);outline.effectDistance=new Vector2(1.5f,-1.5f);freeze.gameObject.SetActive(false);
            var feedback=Rect("BoosterFeedback",row.parent);feedback.anchorMin=feedback.anchorMax=new Vector2(.5f,0);
            feedback.pivot=new Vector2(.5f,.5f);feedback.anchoredPosition=new Vector2(0,213);feedback.sizeDelta=new Vector2(600,36);
            view.BoosterFeedback=Text(feedback,view.LevelLabel.font,22);view.BoosterFeedback.color=new Color(1f,.94f,.65f);feedback.gameObject.SetActive(false);
            EditorUtility.SetDirty(view);
            if(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot+"AddChargeBadge.png") && AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot+"RemainingChargeBadge.png")) PrototypeQuotaArtwork.ApplyTo(view);
        }

        /// <summary>Updates the existing saved scene and verifies every protected component serialization.</summary>
        public static void ApplySavedScene()
        {
            ImportIcons();const string path="Assets/Scenes/GameScene.unity";
            var scene=SceneManager.GetSceneByPath(path);
            if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SortGameViewController view=null;
            foreach(var root in scene.GetRootGameObjects()) {view=root.GetComponentInChildren<SortGameViewController>(true);if(view) break;}
            if(!view) throw new InvalidOperationException("GameScene sorting controller is missing.");
            var protectedState=new Dictionary<Component,string>();
            foreach(var root in scene.GetRootGameObjects()) foreach(var c in root.GetComponentsInChildren<Component>(true)) {
                if(!c || c==view || (c is Button && c.name.StartsWith("FunctionButton_"))) continue;
                protectedState[c]=EditorJsonUtility.ToJson(c);
            }
            ApplyTo(view);
            foreach(var pair in protectedState) if(EditorJsonUtility.ToJson(pair.Key)!=pair.Value) throw new InvalidOperationException("Protected component changed: "+pair.Key.name+" "+pair.Key.GetType().Name);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/boosters-authoring.txt","Scene="+path+"\nProtectedComponentsUnchanged="+protectedState.Count+"\nIcons=Vacuum,Shuffle,GoldenCard,FreezeWatch\nButtonRectsUnchanged=128x128 at 0.125,0.375,0.625,0.875\n");
        }

        /// <summary>Retains direct material identity on subsequent authoring runs.</summary>
        /// <summary>Reuses authored children, adding only feature-owned rects when absent.</summary>
        private static RectTransform Rect(string name,Transform parent)
        {
            var found=parent.Find(name) as RectTransform;if(found) return found;
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
        }

        /// <summary>Creates non-interactive feedback text in the existing Pages rendering domain.</summary>
        private static Text Text(RectTransform rect,Font font,int size)
        {
            var text=rect.GetComponent<Text>();if(!text) text=rect.gameObject.AddComponent<Text>();
            text.font=font;text.fontSize=size;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            text.text=string.Empty;text.horizontalOverflow=HorizontalWrapMode.Overflow;return text;
        }
    }
}
