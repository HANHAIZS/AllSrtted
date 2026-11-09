using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    /// <summary>Replaces only the eight explicitly requested coin sprite references.</summary>
    public static class CoinImageReplacementArtwork
    {
        public const string Root="Assets/Sprites/UI/CoinReplacement/";
        public static Sprite TrySprite(string name)
        {
            string path=Root+name+".png";if(!File.Exists(path))return null;
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite)return (Sprite)asset;return null;
        }
        [MenuItem("All Sorted/Apply Replacement Coin Images")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Stop Play Mode and finish compilation first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Save unsaved scene changes first.");
            foreach(string name in new[]{"Pack1","Pack2","Pack3","Pack4","Pack5","Pack6","Hud","Purchase"})if(!File.Exists(Root+name+".png"))throw new FileNotFoundException("Missing replacement coin image",Root+name+".png");
            if(scene.path!=HomeSceneBuilder.ScenePath)scene=EditorSceneManager.OpenScene(HomeSceneBuilder.ScenePath);
            var home=UnityEngine.Object.FindObjectOfType<SortHomeViewController>();if(!home || !home.CoinBar || !home.CoinShopRoot)throw new InvalidOperationException("Home coin targets unavailable.");
            var images=new Image[7];for(int i=0;i<6;i++){var t=home.CoinShopRoot.Find("Pack"+i+"/Icon");if(!t || !(images[i]=t.GetComponent<Image>()))throw new InvalidOperationException("Missing coin pack target "+i);}images[6]=home.CoinBar.GetComponent<Image>();if(!images[6])throw new InvalidOperationException("Missing HUD Image.");
            var replacements=new Sprite[8];for(int i=0;i<6;i++)replacements[i]=Import("Pack"+(i+1),512);replacements[6]=Import("Hud",512);replacements[7]=Import("Purchase",256);
            var popup=PrefabUtility.LoadPrefabContents(BoosterPurchaseArtwork.Path);
            try {
                var view=popup.GetComponent<SortPopupView>();var target=view && view.Primary?view.Primary.transform.Find("Coin"):null;var coin=target?target.GetComponent<Image>():null;if(!coin)throw new InvalidOperationException("Purchase button LEFT coin target unavailable.");
                for(int i=0;i<images.Length;i++){Undo.RecordObject(images[i],"Replace supplied coin image");images[i].sprite=replacements[i];EditorUtility.SetDirty(images[i]);}
                coin.sprite=replacements[7];EditorUtility.SetDirty(coin);PrefabUtility.SaveAsPrefabAsset(popup,BoosterPurchaseArtwork.Path);
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save replacement coin images.");AssetDatabase.SaveAssets();
            }finally{PrefabUtility.UnloadPrefabContents(popup);}
        }
        private static Sprite Import(string name,int max)
        {
            string path=Root+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));int width=texture.width,height=texture.height,minX=width,minY=height,maxX=-1,maxY=-1;var pixels=texture.GetPixels32();
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(pixels[y*width+x].a>128){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            UnityEngine.Object.DestroyImmediate(texture);if(maxX<0)throw new InvalidOperationException("Empty replacement art: "+name);
            float w=maxX-minX+7,h=maxY-minY+7,cx=(minX+maxX+1)*.5f,cy=(minY+maxY+1)*.5f;
            if(name=="Hud"){const float ratio=1122f/429f;if(w/h>ratio)h=w/ratio;else w=h*ratio;}
            if(w>width || h>height)throw new InvalidOperationException("Insufficient transparent margin: "+name);
            var region=new Rect(Mathf.Clamp(cx-w*.5f,0,width-w),Mathf.Clamp(cy-h*.5f,0,height-h),w,h);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=max;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var slices=new List<SpriteMetaData>{new SpriteMetaData{name=name,rect=region,alignment=9,pivot=new Vector2(.5f,.5f)}};importer.spritesheet=slices.ToArray();typeof(HomeSceneBuilder).GetMethod("AssignSliceIdentities",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{importer,path,slices});importer.SaveAndReimport();var sprite=TrySprite(name);if(!sprite)throw new InvalidOperationException("Replacement Sprite unavailable: "+name);return sprite;
        }
    }
}
