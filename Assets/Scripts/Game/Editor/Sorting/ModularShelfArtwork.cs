using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AllSorted.Editor
{
    /// <summary>Offline assembly of real UV-preserving S1 shelf modules. Never runs in Play Mode.</summary>
    public static class ModularShelfArtwork
    {
        private const string Models = "Assets/Art/Models/Shelves/";
        private const string BodyPath = Models + "SM_ModularShelf3_Static.asset";
        private const string BackPath = Models + "SM_ModularShelf3_BackPanel.asset";
        private const string PrefabPath = "Assets/Prefabs/Shelves/PF_ModularShelf3.prefab";
        private const string MaterialPath = "Assets/Art/Materials/Sorting/SourceLevel1/M_S1_Shelf.mat";
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string Evidence = "/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/modular-shelves-first-five/";
        private static readonly string[] BodyModules =
        {
            "LeftEnd", "MiddleSection0", "MiddleSection1", "MiddleSection2", "RightEnd", "FrontLip"
        };
        private static readonly Vector3 ArtOffset = new Vector3(0f, .12f, .05f);
        private static readonly Matrix4x4 ArtTransform = Matrix4x4.TRS(ArtOffset, Quaternion.Euler(0f, 180f, 0f), Vector3.one);

        [MenuItem("All Sorted/Artwork/Bake Modular Three Slot Shelf")]
        public static void BakeFromMenu() { Debug.Log(ApplySavedScene()); }

        public static string ApplySavedScene()
        {
            RequireIdleSavedEditor();
            // Check the new field before creating assets, and never touch legacy art references.
            var field = typeof(SortGameViewController).GetField("ModularShelfPrefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(GameObject))
                throw new InvalidOperationException("SortGameViewController.ModularShelfPrefab must be compiled before baking.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material) throw new InvalidOperationException("Existing S1 shelf material is missing.");
            var texture = material.GetTexture("_BaseMap");
            if (!texture || AssetDatabase.GetAssetPath(texture) != "Assets/Art/Textures/SourceLevel1/T_S1_Shelf_Albedo.png")
                throw new InvalidOperationException("Existing S1 shelf baked texture binding is invalid.");
            foreach (var module in BodyModules.Concat(new[] { "BackPanel" }))
                if (!File.Exists(ModulePath(module))) throw new FileNotFoundException("Missing real shelf module", ModulePath(module));

            var setup = EditorSceneManager.GetSceneManagerSetup();
            GameObject root = null;
            Scene scratch = default(Scene);
            try
            {
                EnsureFolder("Assets/Prefabs/Shelves");
                var moduleEvidence = new List<object>();
                foreach (var module in BodyModules.Concat(new[] { "BackPanel" }))
                    ConfigureNewModule(module, moduleEvidence);
                var body = PersistMesh(BodyPath, Combine(BodyModules, "SM_ModularShelf3_Static", true));
                var back = PersistMesh(BackPath, Combine(new[] { "BackPanel" }, "SM_ModularShelf3_BackPanel", false));

                scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                root = new GameObject("PF_ModularShelf3");
                SceneManager.MoveGameObjectToScene(root, scratch);
                AddMesh(root.transform, "StaticBody", body, material);
                AddMesh(root.transform, "BackPanel", back, material);
                var anchors = new GameObject("Anchors").transform;
                anchors.SetParent(root.transform, false);
                for (int i = 0; i < 3; i++)
                {
                    AddAnchor(anchors, "Front" + i, new Vector3((i - 1) * .94f, 0f, -.5f));
                    AddAnchor(anchors, "Rear" + i, new Vector3((i - 1) * .94f, .34f, .35f));
                }
                ValidateRoot(root, material);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (!prefab) throw new InvalidOperationException("Native modular shelf prefab save failed.");
                UnityEngine.Object.DestroyImmediate(root); root = null;
                EditorSceneManager.CloseScene(scratch, true); scratch = default(Scene);
                ValidateRoot(prefab, material);

                var scene = SceneManager.GetSceneByPath(ScenePath);
                if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                var view = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SortGameViewController>(true)).Single();
                var before = EditorJsonUtility.ToJson(view);
                var serialized = new SerializedObject(view);
                var property = serialized.FindProperty("ModularShelfPrefab");
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                    throw new InvalidOperationException("New ModularShelfPrefab serialized binding is missing.");
                property.objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var after = EditorJsonUtility.ToJson(view);
                var beforeFields = JObject.Parse(before); var afterFields = JObject.Parse(after);
                // EditorJsonUtility wraps scene components under their native type.
                var beforeComponent = (beforeFields["MonoBehaviour"] as JObject) ?? beforeFields;
                var afterComponent = (afterFields["MonoBehaviour"] as JObject) ?? afterFields;
                beforeComponent.Remove("ModularShelfPrefab"); afterComponent.Remove("ModularShelfPrefab");
                if (!JToken.DeepEquals(beforeFields, afterFields))
                    throw new InvalidOperationException("Unrelated scene-view property changed during modular shelf binding.");
                EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("GameScene save failed.");
                AssetDatabase.SaveAssetIfDirty(body);
                AssetDatabase.SaveAssetIfDirty(back);
                AssetDatabase.SaveAssetIfDirty(prefab);
                var bounds = CombinedBounds(prefab);
                var result = JsonConvert.SerializeObject(new
                {
                    prefab = PrefabPath,
                    bodyMesh = BodyPath,
                    backMesh = BackPath,
                    material = MaterialPath,
                    texture = AssetDatabase.GetAssetPath(texture),
                    rendererCount = 2,
                    colliderCount = 0,
                    bodyTriangles = TriangleCount(body),
                    backTriangles = TriangleCount(back),
                    totalTriangles = TriangleCount(body) + TriangleCount(back),
                    boundsSize = Values(bounds.size),
                    boundsCenter = Values(bounds.center),
                    rootPosition = Values(prefab.transform.localPosition),
                    rootScale = Values(prefab.transform.localScale),
                    rootRotation = Values(prefab.transform.localEulerAngles),
                    innerSillTopY = -.505f,
                    slotInsetTopY = -.487f,
                    anchors = Enumerable.Range(0, 3).SelectMany(i => new[] { "Front" + i, "Rear" + i }).Select(n => new { name = n, localPosition = Values(prefab.transform.Find("Anchors/" + n).localPosition) }).ToArray(),
                    modules = moduleEvidence,
                    binding = "GameScene.SortGameViewController.ModularShelfPrefab only",
                    unrelatedViewFieldsUnchanged = true
                }, Formatting.Indented);
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "unity-import.json", result);
                File.WriteAllText(Evidence + "bindings-before.json", before);
                File.WriteAllText(Evidence + "bindings-after.json", after);
                return result;
            }
            finally
            {
                if (root) UnityEngine.Object.DestroyImmediate(root);
                if (scratch.IsValid() && scratch.isLoaded) EditorSceneManager.CloseScene(scratch, true);
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static void RequireIdleSavedEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Editor must be idle, outside Play Mode, with compilation/import finished.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save dirty scenes before modular shelf authoring.");
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty) throw new InvalidOperationException("Save dirty Prefab Mode edits before modular shelf authoring.");
        }

        private static string ModulePath(string key) { return Models + "SM_ModularShelf_" + key + ".fbx"; }

        private static void ConfigureNewModule(string key, List<object> evidence)
        {
            var path = ModulePath(key);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Not a model module: " + path);
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.globalScale = 1f;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length != 1 || !filters[0].sharedMesh || model.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Invalid real shelf module: " + key);
            var mesh = filters[0].sharedMesh;
            if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))
                throw new InvalidOperationException("Baked UVs missing from " + key);
            evidence.Add(new { key, path, vertices = mesh.vertexCount, triangles = TriangleCount(mesh), uvCount = mesh.uv.Length, subMeshes = mesh.subMeshCount });
        }

        private static Mesh Combine(string[] modules, string name, bool stripInternalCaps)
        {
            var sources = new List<CombineInstance>();
            foreach (var key in modules)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath(key));
                foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh;
                    var relative = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                    // FBX root transform is also part of the original S1 imported artwork instance.
                    var full = ArtTransform * model.transform.localToWorldMatrix * relative;
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                        sources.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = full });
                }
            }
            var combined = new Mesh { name = name };
            combined.CombineMeshes(sources.ToArray(), true, true, false);
            if (stripInternalCaps)
            {
                var points = combined.vertices;
                var indices = combined.triangles;
                var kept = new List<int>(indices.Length);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var a = points[indices[i]]; var b = points[indices[i + 1]]; var c = points[indices[i + 2]];
                    bool cut = new[] { -1.41f, -.47f, .47f, 1.41f }.Any(x => Mathf.Abs(a.x - x) < .00001f && Mathf.Abs(b.x - x) < .00001f && Mathf.Abs(c.x - x) < .00001f);
                    if (!cut) { kept.Add(indices[i]); kept.Add(indices[i + 1]); kept.Add(indices[i + 2]); }
                }
                combined.SetTriangles(kept, 0);
            }
            combined.RecalculateBounds();
            if (combined.vertexCount == 0 || combined.subMeshCount != 1 || combined.uv.Length != combined.vertexCount)
                throw new InvalidOperationException("Invalid combined mesh " + name);
            return combined;
        }

        private static Mesh PersistMesh(string path, Mesh mesh)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(mesh, existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void AddMesh(Transform root, string name, Mesh mesh, Material material)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void AddAnchor(Transform parent, string name, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
        }

        private static Bounds CombinedBounds(GameObject root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var bounds = new Bounds(); bool first = true;
            foreach (var f in filters)
            {
                var relative = root.transform.worldToLocalMatrix * f.transform.localToWorldMatrix;
                var b = f.sharedMesh.bounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    var point = relative.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3(x, y, z)));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }

        private static void ValidateRoot(GameObject root, Material material)
        {
            if (root.transform.localPosition != Vector3.zero || root.transform.localRotation != Quaternion.identity || root.transform.localScale != Vector3.one)
                throw new InvalidOperationException("Modular shelf root must have identity local transform.");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0 || root.GetComponentsInChildren<MeshRenderer>(true).Length != 2)
                throw new InvalidOperationException("Modular shelf needs two renderers and no colliders.");
            foreach (var key in new[] { "StaticBody", "BackPanel" })
            {
                var node = root.transform.Find(key);
                if (!node || !node.gameObject.activeSelf || !node.GetComponent<MeshFilter>() || !node.GetComponent<MeshFilter>().sharedMesh || !node.GetComponent<MeshRenderer>() || !node.GetComponent<MeshRenderer>().enabled || node.GetComponent<MeshRenderer>().sharedMaterial != material)
                    throw new InvalidOperationException("Invalid modular shelf group " + key);
            }
            for (int i = 0; i < 3; i++)
            {
                var front = root.transform.Find("Anchors/Front" + i); var rear = root.transform.Find("Anchors/Rear" + i);
                if (!front || !rear || (front.localPosition - new Vector3((i - 1) * .94f, 0f, -.5f)).sqrMagnitude > .000001f || (rear.localPosition - new Vector3((i - 1) * .94f, .34f, .35f)).sqrMagnitude > .000001f)
                    throw new InvalidOperationException("Invalid modular shelf anchors " + i);
            }
            var bounds = CombinedBounds(root);
            if ((bounds.size - new Vector3(3.55f, 1.74f, 1.54f)).sqrMagnitude > .00001f || (bounds.center - ArtOffset).sqrMagnitude > .00001f)
                throw new InvalidOperationException("Modular shelf no longer matches original S1 bounds: " + bounds);
        }

        private static long TriangleCount(Mesh mesh)
        {
            long count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++) count += (long)mesh.GetIndexCount(i) / 3;
            return count;
        }

        private static float[] Values(Vector3 value) { return new[] { value.x, value.y, value.z }; }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
