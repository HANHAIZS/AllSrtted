using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AllSorted.Editor
{
    /// <summary>Idempotent native authoring for presentation only; no UI or board configuration edits.</summary>
    public static class CabinetVolumeArtwork
    {
        public const string Materials="Assets/Art/Materials/Sorting/Connected/";
        public static Material[] EnsurePalette()
        {
            return new[]{Make("CabinetFrameMaterial","#E8E0CF",.40f,0,.20f,true),Make("CabinetShelfMaterial","#C98B4B",.35f,1,0,true),Make("CabinetBackMaterial","#2A5A63",.15f,2,0,false)};
        }
        static Material Make(string name,string hex,float smooth,int role,float rim,bool receive)
        {
            var shader=Shader.Find("AllSorted/CabinetLit");if(!shader || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("CabinetLit must compile before authoring.");
            var path=Materials+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
            Color c;ColorUtility.TryParseHtmlString(hex,out c);m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",smooth);
            m.SetFloat("_Role",role);m.SetFloat("_AOStrength",.65f);m.SetColor("_AOColor",new Color(.25f,.32f,.34f));m.SetColor("_RimColor",new Color(.80f,.86f,.94f));m.SetFloat("_RimStrength",rim);
            m.SetFloat("_ReceiveShadows",receive?1:0);if(receive)m.DisableKeyword("_RECEIVE_SHADOWS_OFF");else m.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);return m;
        }
        [MenuItem("All Sorted/Artwork/Apply Cabinet Volume Lighting")]
        public static void ApplyScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before scene authoring.");
            EnsurePalette();
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/GameScene.unity");
            if(!scene.IsValid() || !scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity",OpenSceneMode.Additive);
            var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SortGameViewController>(true)).Single();
            var shader=Shader.Find("AllSorted/ContactShadow");if(!shader || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("ContactShadow must compile.");
            string path=Materials+"ContactShadowMaterial.mat";var shadow=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!shadow){shadow=new Material(shader);AssetDatabase.CreateAsset(shadow,path);}shadow.SetColor("_BaseColor",new Color(.06f,.09f,.11f,.25f));EditorUtility.SetDirty(shadow);
            const string meshPath="Assets/Art/Models/Shelves/Connected/ContactShadowMesh.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(!mesh){mesh=new Mesh{name="ContactShadowMesh"};mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.triangles=new[]{0,2,1,2,3,1};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);}
            view.ContactShadowMesh=mesh;view.ContactShadowMaterial=shadow;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Hex("#53677D");RenderSettings.ambientEquatorColor=Hex("#3A4247");RenderSettings.ambientGroundColor=Hex("#625746");RenderSettings.ambientIntensity=1;
            var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
            var key=lights.FirstOrDefault(l=>l.name.IndexOf("Key",StringComparison.OrdinalIgnoreCase)>=0);
            var fill=lights.FirstOrDefault(l=>l.name.IndexOf("Fill",StringComparison.OrdinalIgnoreCase)>=0);
            if(!key || !fill)throw new InvalidOperationException("Expected authored Key and Fill lights.");
            key.transform.eulerAngles=new Vector3(35,-30,0);key.intensity=1.2f;key.shadows=LightShadows.Soft;key.shadowStrength=.5f;key.shadowBias=.04f;key.shadowNormalBias=.4f;
            fill.transform.eulerAngles=new Vector3(20,35,0);fill.intensity=.25f;fill.shadows=LightShadows.None;
            // A private layer prevents Home/Shop/Settings cameras consuming this profile.
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");int layer=LayerMask.NameToLayer("CabinetEffects");
            if(layer<0){for(int i=31;i>=8;i--)if(string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)){layer=i;layers.GetArrayElementAtIndex(i).stringValue="CabinetEffects";break;}if(layer<0)throw new InvalidOperationException("No free layer for CabinetEffects");tags.ApplyModifiedPropertiesWithoutUndo();}
            const string profilePath="Assets/Settings/CabinetVolumeProfile.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
            Bloom bloom;if(!profile.TryGet(out bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.threshold.Override(1);bloom.intensity.Override(.07f);bloom.scatter.Override(.5f);
            ColorAdjustments color;if(!profile.TryGet(out color)){color=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,profile);}color.contrast.Override(5);color.saturation.Override(5);
            Vignette vignette;if(!profile.TryGet(out vignette)){vignette=profile.Add<Vignette>(true);AssetDatabase.AddObjectToAsset(vignette,profile);}vignette.intensity.Override(.06f);
            var node=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Cabinet Effects");if(!node){node=new GameObject("Cabinet Effects");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(node,scene);}node.layer=layer;
            var volume=node.GetComponent<Volume>() ?? node.AddComponent<Volume>();volume.isGlobal=true;volume.priority=5;volume.sharedProfile=profile;
            var camera=view.BoardCamera.GetUniversalAdditionalCameraData();camera.renderPostProcessing=true;camera.volumeLayerMask=1<<layer;
            // Do not change camera transform, projection, UI, layout or authored goods.
            Quality("Balanced",2,false);Quality("HighFidelity",4,true);
            foreach(var c in profile.components)EditorUtility.SetDirty(c);EditorUtility.SetDirty(profile);EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        [MenuItem("All Sorted/Artwork/Apply Elevated Cabinet View")]
        public static void ApplyElevatedView()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before authoring.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/GameScene.unity");
            if(!scene.IsValid() || !scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity",OpenSceneMode.Additive);
            var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SortGameViewController>(true)).Single();
            var rotation=view.BoardCamera.transform.rotation;
            view.BoardCamera.orthographic=true;view.BoardCamera.orthographicSize=4.8f;view.BoardCamera.fieldOfView=60;view.BoardCamera.nearClipPlane=.1f;view.BoardCamera.farClipPlane=50;view.BoardCamera.usePhysicalProperties=false;
            view.BoardCamera.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Base;
            var cameraSettings=new SerializedObject(view.BoardCamera);var axis=cameraSettings.FindProperty("m_FOVAxisMode");if(axis!=null){axis.intValue=0;cameraSettings.ApplyModifiedPropertiesWithoutUndo();}
            float distance=20;
            view.BoardCamera.transform.SetPositionAndRotation(rotation*new Vector3(0,0,-distance),rotation);
            if(view.Background){view.Background.transform.SetPositionAndRotation(view.BoardCamera.transform.position+view.BoardCamera.transform.forward*24,rotation);}
            var shader=Shader.Find("AllSorted/CabinetBackdropShadow");if(!shader || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Backdrop shadow shader must compile.");
            var path=Materials+"CabinetBackdropShadowMaterial.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",new Color(.015f,.025f,.065f,.23f));material.SetVector("_ShadowOffset",new Vector4(.22f,-.33f,0,0));material.SetFloat("_Softness",.09f);
            view.CabinetBackdropShadowMaterial=material;
            EditorUtility.SetDirty(material);EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static string ApplyApprovedCameraAndLock()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit playback before saving the approved scene.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/GameScene.unity");
            if(!scene.IsValid() || !scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity",OpenSceneMode.Additive);
            var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SortGameViewController>(true)).Single();
            var c=view.BoardCamera;var approved=c.transform.rotation;
            c.orthographic=true;c.orthographicSize=4.8f;c.fieldOfView=60;c.nearClipPlane=.1f;c.farClipPlane=50;c.usePhysicalProperties=false;
            c.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Base;
            var settings=new SerializedObject(c);var axis=settings.FindProperty("m_FOVAxisMode");if(axis!=null){axis.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();}
            c.transform.rotation=approved;
            const string texturePath="Assets/Art/Textures/Sorting/LockDoor.png";
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var shader=Shader.Find("AllSorted/LockDoor");if(!shader || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Lock door shader must compile.");
            var path=Materials+"LockDoorMaterial.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetFloat("_DoorAspect",3.28f/1.327f);
            view.SourceLockDoorMaterial=material;view.SourceLockCountFont=view.LevelLabel.font;
            EditorUtility.SetDirty(material);EditorUtility.SetDirty(c);EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return "Approved camera retained: "+approved.eulerAngles+"; Orthographic Size4.8 near0.1 far50; lock image bound";
        }
        static Color Hex(string hex){Color c;ColorUtility.TryParseHtmlString(hex,out c);return c;}
        static void Quality(string name,int samples,bool ssao)
        {
            var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP-"+name+".asset");asset.msaaSampleCount=samples;EditorUtility.SetDirty(asset);
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/URP-"+name+"-Renderer.asset");
            foreach(var feature in renderer.rendererFeatures.Where(f=>f && f.GetType().Name=="ScreenSpaceAmbientOcclusion")) {
                feature.SetActive(ssao);var so=new SerializedObject(feature);var settings=so.FindProperty("m_Settings");
                settings.FindPropertyRelative("Radius").floatValue=.65f;settings.FindPropertyRelative("Intensity").floatValue=.60f;settings.FindPropertyRelative("DirectLightingStrength").floatValue=.20f;
                settings.FindPropertyRelative("Source").enumValueIndex=1;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(feature);
            }
            EditorUtility.SetDirty(renderer);
        }
    }
}
