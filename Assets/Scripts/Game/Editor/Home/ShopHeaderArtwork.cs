using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AllSorted.Editor
{
    /// <summary>Authors only the decorative Shop header; existing page and navigation bindings stay intact.</summary>
    public static class ShopHeaderArtwork
    {
        public const string ArtRoot = "Assets/Sprites/UI/Home/ShopHeader/";
        // The full PNG includes transparent padding; these values fit its visible plaque to the reference.
        private const float TitleWidthFraction = .52575f;
        private const float TitleTopAnchor = 1.17410f;

        [MenuItem("All Sorted/Apply Shop Header")]
        public static void ApplySavedScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring the Shop header.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save unsaved scenes before applying the Shop header.");

            var scene = SceneManager.GetActiveScene();
            if (scene.path != HomeSceneBuilder.ScenePath)
                scene = EditorSceneManager.OpenScene(HomeSceneBuilder.ScenePath, OpenSceneMode.Single);
            var home = UnityEngine.Object.FindObjectOfType<SortHomeViewController>();
            if (!home || home.gameObject.scene != scene || home.Pages == null || home.Pages.Length < 1 || !home.Pages[0])
                throw new InvalidOperationException("HomeScene is missing its Shop page binding.");
            ApplyToPage(home.Pages[0].transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("Could not save the Shop header in HomeScene.");
        }

        public static void ApplyToPage(Transform page)
        {
            if (!page) throw new ArgumentNullException("page");
            // Validate and import both images before touching the scene hierarchy.
            var background = ImportSprite("Background");
            var title = ImportSprite("Title");
            var header = Child(page, "ShopHeader");
            FitWidth(header, 0, 1, 1, background.rect.width / background.rect.height);
            Decorate(header, background);
            var badge = Child(header, "Title");
            FitWidth(badge, .5f - TitleWidthFraction / 2, .5f + TitleWidthFraction / 2,
                TitleTopAnchor, title.rect.width / title.rect.height);
            Decorate(badge, title);
        }

        private static Sprite ImportSprite(string name)
        {
            string path = ArtRoot + name + ".png";
            if (!File.Exists(path)) throw new FileNotFoundException("Missing Shop header art", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing TextureImporter: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite) throw new InvalidOperationException("Shop header Sprite did not import: " + path);
            return sprite;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing && !(existing is RectTransform))
                throw new InvalidOperationException("Shop header object is not a RectTransform: " + name);
            if (existing) return (RectTransform)existing;
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void FitWidth(RectTransform rect, float left, float right, float top, float aspect)
        {
            rect.anchorMin = new Vector2(left, top);
            rect.anchorMax = new Vector2(right, top);
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            var fitter = rect.GetComponent<AspectRatioFitter>() ?? rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = aspect;
        }

        private static void Decorate(RectTransform rect, Sprite sprite)
        {
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false; // AspectRatioFitter owns the entire Rect, including alpha padding.
            image.color = Color.white;
            image.raycastTarget = false;
        }
    }
}
