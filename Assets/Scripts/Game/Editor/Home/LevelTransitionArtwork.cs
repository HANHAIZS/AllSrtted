using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted.Editor
{
    public static class LevelTransitionArtwork
    {
        public const string Path="Assets/Resources/Prefabs/UI/Transitions/LevelTransition.prefab";
        public static void Apply()
        {
            string art="Assets/Sprites/UI/Transitions/Loading.png";AssetDatabase.ImportAsset(art,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(art);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
            var loading=AssetDatabase.LoadAssetAtPath<Sprite>(art);var background=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/LevelBackground.png");
            var root=new GameObject("LevelTransition",typeof(RectTransform),typeof(SortLevelTransitionView),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var view=root.GetComponent<SortLevelTransitionView>();
            try{
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=600;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=.5f;
                view.BackgroundPreview=Picture(root.transform,"LevelPreview",background);view.LoadingPage=Picture(root.transform,"LoadingPage",loading);
                var frame=Rect(root.transform,"HomeFrame");view.HomeFrame=frame.gameObject.AddComponent<RawImage>();view.HomeFrame.raycastTarget=false;
                var blocker=Rect(root.transform,"InputBlocker").gameObject.AddComponent<Image>();blocker.color=Color.clear;blocker.raycastTarget=true;
                root.SetActive(false);Directory.CreateDirectory("Assets/Resources/Prefabs/UI/Transitions");PrefabUtility.SaveAsPrefabAsset(root,Path);
            }finally{Object.DestroyImmediate(root);}
        }
        private static RectTransform Rect(Transform parent,string name){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        private static Image Picture(Transform parent,string name,Sprite sprite){var r=Rect(parent,name);var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;var fit=r.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=sprite.rect.width/sprite.rect.height;return image;}
    }
}
