using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    /// <summary>Authors only the new menu and its Sprite metadata; never rebuilds the gameplay scene.</summary>
    public static class HomeSceneBuilder
    {
        public const string ScenePath="Assets/Scenes/HomeScene.unity",ArtRoot="Assets/Sprites/UI/Home/";
        private static Font font;
        private static readonly Rect[] SettingRegions={new Rect(194,590,200,210),new Rect(438,590,200,210),new Rect(678,590,200,210)};
        private static readonly Rect[] SettingsStrips={new Rect(0,0,1088,590),new Rect(0,800,1088,1120),new Rect(0,590,194,210),new Rect(394,590,44,210),new Rect(638,590,40,210),new Rect(878,590,210,210)};

        [MenuItem("All Sorted/Build Home Screen")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring HomeScene.");
            var previous=SceneManager.GetActiveScene();
            if(previous.isDirty && previous.path!=ScenePath) throw new InvalidOperationException("The active scene has unsaved changes; preserve it before opening HomeScene.");
            ImportArt();font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/LilitaOne/LilitaOne-Regular.ttf");
            if(!font) font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Home Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,0,-20);camera.orthographic=true;camera.orthographicSize=6.4f;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.20f,.17f,.33f);camera.nearClipPlane=.1f;camera.farClipPlane=50;camera.allowHDR=false;
            var root=new GameObject("HomeUI",typeof(SortUiLayers));var layers=root.GetComponent<SortUiLayers>();layers.Hosts=new CanvasGroup[7];
            string[] names={"Pages","Panels","Popups","Guides","Effects","Toasts","Blockers"};
            for(int i=0;i<7;i++) {
                var host=new GameObject(names[i],typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));host.transform.SetParent(root.transform,false);
                var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=8;canvas.sortingOrder=i*100;
                var scaler=host.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=.5f;
                layers.Hosts[i]=host.GetComponent<CanvasGroup>();if(i>=3) {layers.Hosts[i].interactable=false;layers.Hosts[i].blocksRaycasts=false;}
            }
            var home=new GameObject("Home Navigation",typeof(SortHomeViewController)).GetComponent<SortHomeViewController>();home.HomeCamera=camera;home.Layers=layers;
            home.Pages=new CanvasGroup[3];home.PageArt=new RectTransform[3];home.TabButtons=new Button[3];home.TabIcons=new RectTransform[3];home.TabLabels=new Text[3];home.SettingButtons=new Button[3];home.OffMarks=new GameObject[3];
            for(int i=0;i<3;i++) {
                var page=Rect(new[]{"ShopPage","HomePage","SettingsPage"}[i],layers.Hosts[0].transform);Stretch(page);home.Pages[i]=page.gameObject.AddComponent<CanvasGroup>();
                home.Pages[i].alpha=i==1?1:0;home.Pages[i].interactable=home.Pages[i].blocksRaycasts=i==1;
                if(i==0) {var art=Rect("ShopBackground",page);Stretch(art);Picture(art,Sprite("ShopPage"),false);home.PageArt[i]=art;ShopHeaderArtwork.ApplyToPage(page);}
                else if(i==1) {var art=Rect("FactoryBackground",page);Layout(art,Vector2.zero,new Vector2(725,1280),new Vector2(.5f,.5f),new Vector2(.5f,.5f));Picture(art,Sprite("Background"),true);home.PageArt[i]=art;}
                else {
                    Picture(page,null,false).color=new Color(.22f,.16f,.48f);
                    var art=Rect("SettingsArtwork",page);Layout(art,Vector2.zero,new Vector2(725,1280),new Vector2(.5f,1),new Vector2(.5f,1));home.PageArt[i]=art;
                    for(int n=0;n<SettingsStrips.Length;n++) RegionImage("StaticRegion"+n,art,SettingsStrips[n],Region("SettingsPage","SettingsStatic"+n));
                    for(int n=0;n<3;n++) {
                        var hole=RegionImage("ButtonBackdrop"+n,art,SettingRegions[n],Region("SettingsPage","SettingsButton"+n));hole.GetComponent<Image>().color=Color.white;
                        var target=RegionImage("SettingButton"+n,art,SettingRegions[n],HomeSettingsButtonArtwork.ButtonSprite(n));
                        target.GetComponent<Image>().useSpriteMesh=false;
                        var button=Skin(target);home.SettingButtons[n]=button;
                        var off=Rect("DisabledMark",target);Layout(off,Vector2.zero,new Vector2(154,154),new Vector2(.5f,.5f),new Vector2(.5f,.5f));
                        var dim=Picture(off,AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),false);dim.color=new Color(0,0,0,.4f);
                        var slash=Rect("OffSlash",off);Layout(slash,Vector2.zero,new Vector2(112,13),new Vector2(.5f,.5f),new Vector2(.5f,.5f));slash.localRotation=Quaternion.Euler(0,0,-45);Picture(slash,null,false).color=new Color(1,.05f,.1f);
                        off.gameObject.SetActive(false);home.OffMarks[n]=off.gameObject;
                    }
                }
            }
            // Original settings pixels use an explicit 1088x1920 coordinate subtree scaled as one unit.
            var settingsRoot=home.PageArt[2];var pixels=Rect("SourcePixels",settingsRoot);Layout(pixels,Vector2.zero,new Vector2(1088,1920),new Vector2(0,1),new Vector2(0,1));
            var authoredChildren=new List<Transform>();foreach(Transform child in settingsRoot) if(child!=pixels) authoredChildren.Add(child);
            foreach(var child in authoredChildren) child.SetParent(pixels,false);
            // SourcePixels is fitted by its parent Rect; children retain source-pixel coordinates.
            pixels.localScale=Vector3.one*(725f/1088);pixels.gameObject.AddComponent<SortHomeArtworkScale>();

            var safeHome=Rect("SafeArea",home.Pages[1].transform);Stretch(safeHome);safeHome.gameObject.AddComponent<UiSafeAreaFitter>();
            var start=Rect("StartButton",safeHome);Layout(start,new Vector2(0,335),new Vector2(370,143),new Vector2(.5f,0),new Vector2(.5f,.5f));Picture(start,Sprite("StartBase"),true);home.StartButton=Skin(start);
            var label=Text("LevelLabel",start,"Level 1",44);Stretch((RectTransform)label.transform);var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,.33f,0,1);outline.effectDistance=new Vector2(2.5f,-2.5f);home.LevelLabel=label;

            var extension=Rect("BottomColorExtension",layers.Hosts[1].transform);Layout(extension,Vector2.zero,new Vector2(0,140),new Vector2(.5f,0),new Vector2(.5f,0));extension.anchorMin=new Vector2(0,0);extension.anchorMax=new Vector2(1,0);Picture(extension,Region("NavLeft","NavFlat"),false);
            var safeNav=Rect("SafeArea",layers.Hosts[1].transform);Stretch(safeNav);safeNav.gameObject.AddComponent<UiSafeAreaFitter>();
            var nav=Rect("Navigation",safeNav);Layout(nav,Vector2.zero,new Vector2(0,176),new Vector2(.5f,0),new Vector2(.5f,0));nav.anchorMin=new Vector2(0,0);nav.anchorMax=new Vector2(1,0);home.NavigationArea=nav;
            var flat=Rect("FlatBackground",nav);Layout(flat,Vector2.zero,new Vector2(0,140),new Vector2(.5f,0),new Vector2(.5f,0));flat.anchorMin=new Vector2(0,0);flat.anchorMax=new Vector2(1,0);Picture(flat,Region("NavLeft","NavFlat"),false);
            home.Dividers=new RectTransform[2];
            for(int i=0;i<2;i++) {var divider=Rect("SectionDivider"+i,nav);Layout(divider,new Vector2(i==0?-120:120,0),new Vector2(3.5f,140),new Vector2(.5f,0),new Vector2(.5f,0));Picture(divider,Region("NavLeft","NavDivider"),false);home.Dividers[i]=divider;}
            home.LeftRail=Rect("LeftFlatRail",nav);Layout(home.LeftRail,new Vector2(0,145),new Vector2(240,18),new Vector2(0,0),new Vector2(0,1));Picture(home.LeftRail,Region("RailLeft","RailFlat"),false);
            home.RightRail=Rect("RightFlatRail",nav);Layout(home.RightRail,new Vector2(0,145),new Vector2(240,18),new Vector2(1,0),new Vector2(1,1));Picture(home.RightRail,Region("RailLeft","RailFlat"),false);
            var plate=Rect("SelectedBacking",nav);Layout(plate,Vector2.zero,new Vector2(240,176),new Vector2(.5f,0),new Vector2(.5f,0));home.SelectionPlate=plate;
            JoinedCap(plate,Region("NavRight","NavInnerLeft"),Region("NavLeft","NavMiddle"),Region("NavLeft","NavInnerRight"),42,176);
            var gold=Rect("GoldCap",plate);Layout(gold,new Vector2(0,5),new Vector2(253,54),new Vector2(.5f,1),new Vector2(.5f,1));
            JoinedCap(gold,Region("RailRight","RailInnerLeft"),Region("RailLeft","RailMiddle"),Region("RailLeft","RailInnerRight"),54,54);
            string[] captions={"Shop","Home","Setting"},icons={"ShopIcon","HomeIcon","SettingsIcon"};
            for(int i=0;i<3;i++) {
                var tab=Rect("Tab_"+captions[i],nav);Layout(tab,new Vector2((i-1)*240,0),new Vector2(240,176),new Vector2(.5f,0),new Vector2(.5f,0));var hit=Picture(tab,null,false);hit.color=Color.clear;hit.raycastTarget=true;
                var button=tab.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;home.TabButtons[i]=button;
                var press=Rect("PressVisual",tab);Stretch(press);var pressImage=Picture(press,null,false);pressImage.color=Color.clear;
                // Feedback owns this separate visual parent; the stable tab hit area never changes size.
                var pressButton=press.gameObject.AddComponent<Button>();pressButton.targetGraphic=pressImage;pressButton.transition=Selectable.Transition.None;press.gameObject.AddComponent<SortButtonPressFeedback>();
                var relay=tab.gameObject.AddComponent<SortHomeTabPressRelay>();relay.Feedback=press.GetComponent<SortButtonPressFeedback>();
                var icon=Rect("Icon",press);Layout(icon,new Vector2(0,i==1?100:78),i==1?new Vector2(108,126):new Vector2(105,105),new Vector2(.5f,0),new Vector2(.5f,.5f));Picture(icon,Sprite(icons[i]),true);home.TabIcons[i]=icon;icon.localScale=Vector3.one*(i==1?1.2f:1);
                var caption=Text("Caption",press,captions[i],32);Layout((RectTransform)caption.transform,new Vector2(0,24),new Vector2(200,40),new Vector2(.5f,0),new Vector2(.5f,.5f));var shadow=caption.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.29f,.14f,.58f);shadow.effectDistance=new Vector2(0,-3);var stroke=caption.gameObject.AddComponent<Outline>();stroke.effectColor=new Color(.43f,.23f,.75f);stroke.effectDistance=new Vector2(1.5f,-1.5f);home.TabLabels[i]=caption;var color=caption.color;color.a=i==1?1:0;caption.color=color;
            }
            var message=Text("LoadError",layers.Hosts[5].transform,"",18);Layout((RectTransform)message.transform,new Vector2(0,-100),new Vector2(680,100),new Vector2(.5f,1),new Vector2(.5f,1));message.color=new Color(1,.9f,.4f);message.gameObject.SetActive(false);home.ErrorLabel=message;
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            HomeWalletArtwork.ApplyToHome(home);ShopCoinArtwork.ApplyToHome(home);ShopBundleArtwork.ApplyToHome(home);HomeSettingsButtonArtwork.ApplyDisabledShades(home);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();scenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
        }

        private static void ImportArt()
        {
            AssetDatabase.Refresh();foreach(string path in Directory.GetFiles(ArtRoot,"*.png")) {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=path.Contains("Icon")?1024:2048;importer.spriteImportMode=SpriteImportMode.Single;var regions=new List<SpriteMetaData>();string name=Path.GetFileNameWithoutExtension(path);
                if(name=="NavLeft") {regions.Add(Meta("NavCap",new Rect(0,0,285,209),209));regions.Add(Meta("NavFlat",new Rect(310,42,230,167),209));regions.Add(Meta("NavDivider",new Rect(557,42,7,167),209));regions.Add(Meta("NavMiddle",new Rect(50,0,185,209),209));regions.Add(Meta("NavInnerRight",new Rect(235,0,50,209),209));}
                if(name=="NavRight") regions.Add(Meta("NavInnerLeft",new Rect(555,0,50,209),209));
                if(name=="RailLeft") {regions.Add(Meta("RailCap",new Rect(0,0,295,63),63));regions.Add(Meta("RailFlat",new Rect(320,42,520,21),63));regions.Add(Meta("RailMiddle",new Rect(60,0,175,63),63));regions.Add(Meta("RailInnerRight",new Rect(235,0,65,63),63));}
                if(name=="RailRight") regions.Add(Meta("RailInnerLeft",new Rect(540,0,65,63),63));
                if(name=="SettingsPage") {for(int i=0;i<SettingsStrips.Length;i++) regions.Add(Meta("SettingsStatic"+i,SettingsStrips[i],1920));for(int i=0;i<3;i++) regions.Add(Meta("SettingsButton"+i,SettingRegions[i],1920));}
                if(name=="ShopIcon") regions.Add(Meta("ShopIcon",new Rect(63,54,1167,1124),1254));
                if(name=="HomeIcon") regions.Add(Meta("HomeIcon",new Rect(93,74,1069,1121),1254));
                if(regions.Count>0) {importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritesheet=regions.ToArray();AssignSliceIdentities(importer,path,regions);}importer.SaveAndReimport();
            }
        }
        /// <summary>Unity 2022's legacy sheet setter leaves new slices with duplicate GUIDs/zero file IDs.
        /// Author deterministic identities through the importer serialization API before scene references are saved.</summary>
        private static void AssignSliceIdentities(TextureImporter importer,string path,List<SpriteMetaData> regions)
        {
            var serialized=new SerializedObject(importer);var sprites=serialized.FindProperty("m_SpriteSheet.m_Sprites");
            var names=serialized.FindProperty("m_SpriteSheet.m_NameFileIdTable");names.arraySize=regions.Count;
            using(var md5=System.Security.Cryptography.MD5.Create()) for(int i=0;i<regions.Count;i++) {
                byte[] bytes=md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(path.Replace('\\','/')+":"+regions[i].name));
                string spriteGuid=BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();long fileId=BitConverter.ToInt64(bytes,0)&long.MaxValue;if(fileId==0) fileId=i+1;
                var entry=sprites.GetArrayElementAtIndex(i);entry.FindPropertyRelative("m_SpriteID").stringValue=spriteGuid;entry.FindPropertyRelative("m_InternalID").longValue=fileId;
                var pair=names.GetArrayElementAtIndex(i);pair.FindPropertyRelative("first").stringValue=regions[i].name;pair.FindPropertyRelative("second").longValue=fileId;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static SpriteMetaData Meta(string name,Rect top,float height) {return new SpriteMetaData{name=name,rect=new Rect(top.x,height-top.y-top.height,top.width,top.height),alignment=9,pivot=new Vector2(.5f,.5f)};}
        private static void JoinedCap(RectTransform parent,Sprite left,Sprite middle,Sprite right,float endWidth,float height)
        {
            var l=Rect("LeftJoin",parent);Layout(l,Vector2.zero,new Vector2(endWidth,height),new Vector2(0,0),new Vector2(0,0));Picture(l,left,false);
            var r=Rect("RightJoin",parent);Layout(r,Vector2.zero,new Vector2(endWidth,height),new Vector2(1,0),new Vector2(1,0));Picture(r,right,false);
            var m=Rect("Middle",parent);Stretch(m);m.offsetMin=new Vector2(endWidth,0);m.offsetMax=new Vector2(-endWidth,0);Picture(m,middle,false);
        }
        private static Sprite Sprite(string name) {var path=ArtRoot+name+".png";return AssetDatabase.LoadAssetAtPath<Sprite>(path)??AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();}
        private static Sprite Region(string file,string name) {return AssetDatabase.LoadAllAssetsAtPath(ArtRoot+file+".png").OfType<Sprite>().First(s=>s.name==name);}
        private static RectTransform RegionImage(string name,Transform parent,Rect region,Sprite sprite) {var r=Rect(name,parent);Layout(r,new Vector2(region.center.x,-region.center.y),region.size,new Vector2(0,1),new Vector2(.5f,.5f));Picture(r,sprite,false);return r;}
        private static RectTransform Rect(string name,Transform parent) {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        private static void Layout(RectTransform r,Vector2 pos,Vector2 size,Vector2 anchor,Vector2 pivot) {r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;}
        private static void Stretch(RectTransform r) {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static Image Picture(RectTransform r,Sprite sprite,bool aspect) {var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.color=Color.white;im.raycastTarget=false;im.preserveAspect=aspect;im.type=Image.Type.Simple;return im;}
        private static Button Skin(RectTransform r) {var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.targetGraphic.raycastTarget=true;button.transition=Selectable.Transition.ColorTint;var c=button.colors;c.highlightedColor=Color.white;c.pressedColor=new Color(.82f,.82f,.82f);button.colors=c;r.gameObject.AddComponent<SortButtonPressFeedback>();return button;}
        private static Text Text(string name,Transform parent,string value,int size) {var t=Rect(name,parent).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.fontStyle=FontStyle.Normal;t.text=value;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
    }
}
