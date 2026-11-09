using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    /// <summary>Replaces only the three Home setting sprites with the supplied button design.</summary>
    public static class HomeSettingsButtonArtwork
    {
        private const string Folder="Assets/Sprites/UI/Home/SettingsButtons/";
        private static readonly string[] Names={"Music","Sound","Haptic"};
        private static readonly Rect[] Regions={new Rect(114,129,1024,994),new Rect(165,86,1015,992),new Rect(78,71,1130,1092)};
        private static readonly int[] Heights={1254,1167,1223};
        public static Sprite ButtonSprite(int index)
        {
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Folder+Names[index]+".png"))if(asset is Sprite)return (Sprite)asset;
            throw new InvalidOperationException("Import Home settings button sprites first.");
        }
        public static void ApplyDisabledShades(SortHomeViewController home)
        {
            for(int i=0;i<3;i++) {
                var source=home.SettingButtons[i].image;var shade=home.OffMarks[i].GetComponent<Image>();var rect=shade.rectTransform;
                Undo.RecordObjects(new UnityEngine.Object[]{rect,shade},"Fit disabled setting shade");
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                shade.sprite=source.sprite;shade.type=source.type;shade.preserveAspect=source.preserveAspect;shade.useSpriteMesh=source.useSpriteMesh;shade.color=new Color(0,0,0,.4f);shade.raycastTarget=false;
                EditorUtility.SetDirty(rect);EditorUtility.SetDirty(shade);
            }
        }
        [MenuItem("All Sorted/Apply Home Settings Buttons")]
        public static void ApplySavedScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Save unsaved changes first.");
            if(scene.path!=HomeSceneBuilder.ScenePath)scene=EditorSceneManager.OpenScene(HomeSceneBuilder.ScenePath);
            for(int i=0;i<3;i++) {
                string path=Folder+Names[i]+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;
                var r=Regions[i];var slices=new List<SpriteMetaData>{new SpriteMetaData{name=Names[i],rect=new Rect(r.x,Heights[i]-r.y-r.height,r.width,r.height),alignment=9,pivot=new Vector2(.5f,.5f)}};importer.spritesheet=slices.ToArray();
                typeof(HomeSceneBuilder).GetMethod("AssignSliceIdentities",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{importer,path,slices});importer.SaveAndReimport();
            }
            var home=UnityEngine.Object.FindObjectOfType<SortHomeViewController>();
            for(int i=0;i<3;i++){
                var image=home.SettingButtons[i].image;
                var backdrop=home.SettingButtons[i].transform.parent.Find("ButtonBackdrop"+i).GetComponent<UnityEngine.UI.Image>();
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Home/SettingsPage.png"))if(asset is Sprite && asset.name=="SettingsButton"+i)backdrop.sprite=(Sprite)asset;
                backdrop.color=Color.white;EditorUtility.SetDirty(backdrop);
                Undo.RecordObject(image,"Replace Home setting button");image.sprite=ButtonSprite(i);image.useSpriteMesh=false;EditorUtility.SetDirty(image);
            }
            ApplyDisabledShades(home);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
