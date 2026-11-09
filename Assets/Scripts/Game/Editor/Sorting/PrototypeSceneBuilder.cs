using System;
using System.IO;
using System.Collections.Generic;
using AllSorted;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AllSorted.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath="Assets/Scenes/GameScene.unity";
        private static Font font;
        private static Sprite rounded;

        /// <summary>Opens the saved prototype without rebuilding or replacing authored assets.</summary>
        [MenuItem("All Sorted/Open Single-Level Prototype")]
        public static void OpenForPlay() {EditorSceneManager.OpenScene(ScenePath);}

        /// <summary>Builds only this feature's declared assets through Unity's authoring APIs.</summary>
        [MenuItem("All Sorted/Build Single-Level Prototype")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Art/Materials/Sorting");
            Directory.CreateDirectory("Assets/Resources/Prefabs/UI/Popups");
            AssetDatabase.Refresh();
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if(!font) throw new InvalidOperationException("Unity built-in font is missing.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();
            camera.transform.position=new Vector3(0,0,-20);camera.orthographic=true;camera.orthographicSize=7.65f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=ColorHex("142E43");
            camera.nearClipPlane=.1f;camera.farClipPlane=50;camera.allowHDR=false;
            SpriteRenderer backdrop=null;
            var backgroundArt=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/LevelBackground.png");
            if(backgroundArt) {
                backdrop=new GameObject("Level Background",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                backdrop.sprite=backgroundArt;backdrop.sortingOrder=-1000;backdrop.transform.position=new Vector3(0,0,4);
                float backdropScale=Mathf.Max(camera.orthographicSize*2/backgroundArt.bounds.size.y,camera.orthographicSize*2*(720f/1280f)/backgroundArt.bounds.size.x);
                backdrop.transform.localScale=new Vector3(backdropScale,backdropScale,1);
            }
            var light=new GameObject("Key Light",typeof(Light)).GetComponent<Light>();
            light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(35,-30,0);
            light.shadows=LightShadows.Soft;RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.56f,.65f,.73f);RenderSettings.fog=false;
            var fill=new GameObject("Fill Light",typeof(Light)).GetComponent<Light>();
            fill.type=LightType.Directional;fill.intensity=.35f;fill.transform.rotation=Quaternion.Euler(-20,140,0);
            fill.shadows=LightShadows.None;

            var palette=new Material[4];var backs=new Material[4];
            string[] colors={"FF7669","64D7AD","F6C95C","6DAFF7"};
            string[] kinds={"CoralSphere","MintCube","GoldCapsule","BlueCylinder"};
            for(int i=0;i<4;i++) {
                palette[i]=MaterialAsset(kinds[i],ColorHex(colors[i]),.48f);
                backs[i]=MaterialAsset(kinds[i]+"Behind",ColorHex(colors[i])*.47f,.25f);
            }
            var rim=MaterialAsset("ShelfIvory",ColorHex("E9E7D8"),.22f);
            var inside=MaterialAsset("ShelfInterior",ColorHex("28556B"),.15f);
            var shadow=MaterialAsset("ShelfShadow",ColorHex("10283A"),.05f);
            var marker=MaterialAsset("EmptySlot",ColorHex("447386"),.1f);
            var shelves=new GameObject("Primitive Shelves").transform;
            for(int s=0;s<6;s++) {
                var root=new GameObject("Shelf_"+s).transform;root.SetParent(shelves);root.position=SortGameViewController.ShelfWorld(s);
                Box("SoftShadow",root,new Vector3(.07f,-.09f,1.35f),new Vector3(3.62f,1.88f,.20f),shadow);
                Box("IvoryRim",root,new Vector3(0,0,1.05f),new Vector3(3.55f,1.82f,.28f),rim);
                Box("Interior",root,new Vector3(0,.035f,.83f),new Vector3(3.36f,1.59f,.20f),inside);
                Box("ShelfLedge",root,new Vector3(0,-.80f,.10f),new Vector3(3.68f,.18f,.85f),rim);
                for(int c=0;c<3;c++) {
                    var dot=GameObject.CreatePrimitive(PrimitiveType.Cylinder);dot.name="EmptySlotMarker_"+c;
                    dot.transform.SetParent(root,false);dot.transform.localPosition=new Vector3((c-1)*.94f,-.29f,.53f);
                    dot.transform.localRotation=Quaternion.Euler(90,0,0);dot.transform.localScale=new Vector3(.24f,.012f,.24f);
                    dot.GetComponent<Renderer>().sharedMaterial=marker;UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());
                }
            }

            var uiRoot=new GameObject("PrototypeUI",typeof(SortUiLayers));var layers=uiRoot.GetComponent<SortUiLayers>();
            layers.Hosts=new CanvasGroup[7];string[] hostNames={"Pages","Panels","Popups","Guides","Effects","Toasts","Blockers"};
            for(int i=0;i<7;i++) {
                var host=new GameObject(hostNames[i],typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
                host.transform.SetParent(uiRoot.transform,false);var canvas=host.GetComponent<Canvas>();
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=8;canvas.sortingOrder=i*100;
                var scaler=host.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=.5f;
                layers.Hosts[i]=host.GetComponent<CanvasGroup>();
                if(i==3 || i==4 || i==5) {layers.Hosts[i].interactable=false;layers.Hosts[i].blocksRaycasts=false;}
            }
            var safe=Rect("SafeArea",layers.Hosts[0].transform);Stretch(safe);safe.gameObject.AddComponent<UiSafeAreaFitter>();
            TextAt("Brand",safe,"ALL SORTED!",42,FontStyle.Bold,new Vector2(36,-48),new Vector2(430,58),new Vector2(0,1),TextAnchor.MiddleLeft,ColorHex("F2F0E6"));
            TextAt("BrandCaption",safe,"THE SHAPE SORTING CLUB",13,FontStyle.Normal,new Vector2(39,-96),new Vector2(420,24),new Vector2(0,1),TextAnchor.MiddleLeft,ColorHex("92B4C5"));
            var level=Panel("LevelPill",safe,new Vector2(-36,-51),new Vector2(130,42),new Vector2(1,1),ColorHex("2B5368"));
            TextFill("Level",level,"LEVEL 01",17,FontStyle.Bold,ColorHex("E8EBDD"));
            TextAt("SectionCaption",safe,"ONE GOOD SORT.",15,FontStyle.Bold,new Vector2(38,-153),new Vector2(240,28),new Vector2(0,1),TextAnchor.MiddleLeft,ColorHex("BCD2D8"));
            var clock=Panel("ClockPill",safe,new Vector2(0,-167),new Vector2(174,68),new Vector2(.5f,1),ColorHex("244E62"));
            var timer=TextFill("Timer",clock,"03:00",36,FontStyle.Bold,ColorHex("F4D47C"));
            var pause=ButtonAt("Pause",safe,"II",new Vector2(-36,-167),new Vector2(64,64),new Vector2(1,1),ColorHex("F0EEDC"),ColorHex("1E4355"));
            var pauseArt=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/PauseGear.png");
            if(pauseArt) {
                var pauseImage=pause.GetComponent<Image>();pauseImage.sprite=pauseArt;pauseImage.color=Color.white;pauseImage.type=Image.Type.Simple;
                pause.GetComponentInChildren<Text>().text=string.Empty;
            }
            var count=TextAt("MatchCount",safe,"0 / 8 SETS",14,FontStyle.Bold,new Vector2(38,-210),new Vector2(400,28),new Vector2(0,1),TextAnchor.MiddleLeft,ColorHex("A9C6CF"));
            var track=Panel("ProgressTrack",safe,new Vector2(0,-247),new Vector2(644,8),new Vector2(.5f,1),ColorHex("2B5368"));
            var fillRect=Panel("ProgressFill",track,Vector2.zero,new Vector2(644,8),new Vector2(.5f,.5f),ColorHex("6DDDB0"));
            var progress=fillRect.GetComponent<Image>();progress.type=Image.Type.Filled;progress.fillMethod=Image.FillMethod.Horizontal;progress.fillAmount=0;
            var status=TextAt("Status",safe,"MAKE ROOM. MATCH THREE.",23,FontStyle.Bold,new Vector2(0,194),new Vector2(660,40),new Vector2(.5f,0),TextAnchor.MiddleCenter,ColorHex("F2F0E6"));
            TextAt("Instruction",safe,"Drag matching shapes into the same shelf.",17,FontStyle.Normal,new Vector2(0,150),new Vector2(660,40),new Vector2(.5f,0),TextAnchor.MiddleCenter,ColorHex("A9C6CF"));
            TextAt("Footer",safe,"A LITTLE ORDER FEELS GOOD.",12,FontStyle.Normal,new Vector2(0,65),new Vector2(660,28),new Vector2(.5f,0),TextAnchor.MiddleCenter,ColorHex("6D98AB"));
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));

            var controller=new GameObject("Sorting Game",typeof(SortGameViewController)).GetComponent<SortGameViewController>();
            controller.BoardCamera=camera;controller.Background=backdrop;controller.PieceRoot=new GameObject("Pieces").transform;
            controller.Palette=palette;controller.BackPalette=backs;controller.Layers=layers;
            controller.LevelLabel=level.GetComponentInChildren<Text>();controller.TimerLabel=timer;controller.CountLabel=count;controller.StatusLabel=status;
            controller.ProgressFill=progress;controller.PauseButton=pause;
            PrototypeHudLayout.Apply(controller);
            CreatePopup("PausePopup");CreatePopup("ResultPopup");
            if(System.IO.File.Exists(PausePopupArtwork.SpriteRoot+"Background.png")) PausePopupArtwork.ApplySavedPrefab();
            EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(ScenePath,true)};
            foreach(var old in EditorBuildSettings.scenes)
                if(old.path!=ScenePath) scenes.Add(new EditorBuildSettingsScene(old.path,false));
            EditorBuildSettings.scenes=scenes.ToArray();
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth=720;PlayerSettings.defaultScreenHeight=1280;
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("ALL_SORTED_SCENE_BUILT "+ScenePath);
        }

        /// <summary>Creates a shared original URP material, retaining asset identity on a rebuild.</summary>
        private static Material MaterialAsset(string name,Color color,float smoothness)
        {
            string path="Assets/Art/Materials/Sorting/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",smoothness);
            EditorUtility.SetDirty(material);return material;
        }

        /// <summary>Authors shelf geometry from Unity cubes and shared materials.</summary>
        private static void Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        /// <summary>Creates a UI rect under an explicit owner, without implicit scene singletons.</summary>
        private static RectTransform Rect(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
        }

        /// <summary>Stretches only the feature-owned rect to its semantic host.</summary>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
        }

        /// <summary>Applies authored anchors with offset coordinates measured from that anchor.</summary>
        private static void Layout(RectTransform rect,Vector2 position,Vector2 size,Vector2 anchor)
        {
            rect.anchorMin=anchor;rect.anchorMax=anchor;rect.pivot=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;
        }

        /// <summary>Creates a non-interactive decorative panel with native Unity UI artwork.</summary>
        private static RectTransform Panel(string name,Transform parent,Vector2 position,Vector2 size,Vector2 anchor,Color color)
        {
            var rect=Rect(name,parent);Layout(rect,position,size,anchor);var image=rect.gameObject.AddComponent<Image>();
            image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color;image.raycastTarget=false;return rect;
        }

        /// <summary>Creates text at an authored rect and makes decorative glyphs transparent to raycasts.</summary>
        private static Text TextAt(string name,Transform parent,string value,int size,FontStyle style,Vector2 pos,Vector2 rectSize,Vector2 anchor,TextAnchor align,Color color)
        {
            var rect=Rect(name,parent);Layout(rect,pos,rectSize,anchor);var text=rect.gameObject.AddComponent<Text>();
            text.font=font;text.text=value;text.fontSize=size;text.fontStyle=style;text.alignment=align;text.color=color;
            text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
        }

        /// <summary>Authors a centered label that follows its button or panel bounds.</summary>
        private static Text TextFill(string name,Transform parent,string value,int size,FontStyle style,Color color)
        {
            var text=TextAt(name,parent,value,size,style,Vector2.zero,Vector2.zero,new Vector2(.5f,.5f),TextAnchor.MiddleCenter,color);
            Stretch((RectTransform)text.transform);return text;
        }

        /// <summary>Creates a raycastable button; fitting, tints and caption are explicitly project-owned.</summary>
        private static Button ButtonAt(string name,Transform parent,string caption,Vector2 pos,Vector2 size,Vector2 anchor,Color background,Color foreground)
        {
            var rect=Panel(name,parent,pos,size,anchor,background);var image=rect.GetComponent<Image>();image.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            TextFill("Caption",rect,caption,21,FontStyle.Bold,foreground);return button;
        }

        /// <summary>Creates a convention-shaped inactive popup saved only at its declared lazy Resources key.</summary>
        private static void CreatePopup(string name)
        {
            var root=Rect(name,null);Stretch(root);var view=root.gameObject.AddComponent<SortPopupView>();view.Id=name;
            var dim=Rect("DimBackground",root);Stretch(dim);var image=dim.gameObject.AddComponent<Image>();
            var profile=AssetDatabase.LoadAssetAtPath<UiPrefabConventionProfile>("Assets/Settings/GameFoundation/AllSortedUiPrefabConvention.asset");
            if(!profile) throw new InvalidOperationException("Project UI convention profile is missing.");
            image.color=new Color(0,0,0,profile.DimBackgroundAlpha);image.raycastTarget=true;
            var content=Panel("PopupContent",root,Vector2.zero,new Vector2(580,430),new Vector2(.5f,.5f),ColorHex("F2F0E6"));
            view.Title=TextAt("Title",content,"TAKE A BREATHER",34,FontStyle.Bold,new Vector2(0,115),new Vector2(520,80),new Vector2(.5f,.5f),TextAnchor.MiddleCenter,ColorHex("173D50"));
            view.Body=TextAt("Body",content,"Your shapes will be right here.",18,FontStyle.Normal,new Vector2(0,38),new Vector2(490,60),new Vector2(.5f,.5f),TextAnchor.MiddleCenter,ColorHex("567582"));
            view.Primary=ButtonAt("PrimaryButton",content,"KEEP SORTING",new Vector2(0,-60),new Vector2(466,67),new Vector2(.5f,.5f),ColorHex("6DD7AC"),ColorHex("123C3C"));
            view.Secondary=ButtonAt("SecondaryButton",content,"START AGAIN",new Vector2(0,-143),new Vector2(466,56),new Vector2(.5f,.5f),ColorHex("DFE5DF"),ColorHex("355668"));
            view.PrimaryCaption=view.Primary.GetComponentInChildren<Text>();view.SecondaryCaption=view.Secondary.GetComponentInChildren<Text>();
            root.gameObject.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Resources/Prefabs/UI/Popups/"+name+".prefab");
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        /// <summary>Converts declared project palette constants to opaque colors.</summary>
        private static Color ColorHex(string value) { Color color;ColorUtility.TryParseHtmlString("#"+value,out color);return color; }
    }
}
