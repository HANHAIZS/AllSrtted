using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AllSorted.Editor
{
    /// <summary>Imports offline welded cabinet artwork and binds only the five new direct references.</summary>
    public static class ConnectedCabinetArtwork
    {
        private const string Models = "Assets/Art/Models/Shelves/Connected/";
        private const string Prefabs = "Assets/Prefabs/Shelves/Connected/";
        private const string MaterialPath = "Assets/Art/Materials/Sorting/SourceLevel1/M_S1_Shelf.mat";
        private const string BackMaterialPath = "Assets/Art/Materials/Sorting/Connected/M_ConnectedCabinetBack.mat";
        private const string TexturePath = "Assets/Art/Textures/SourceLevel1/T_S1_Shelf_Albedo.png";
        private const string ConfigPath = "Assets/Resources/Configs/Sorting/SortExpressFirstFive.json";
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string Evidence = "/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/connected-cabinets-first-five/";
        private static readonly int[] Counts = { 3, 9, 10, 9, 13 };

        [MenuItem("All Sorted/Artwork/Bake Connected First Five Cabinets")]
        public static void BakeFromMenu() { Debug.Log(ApplySavedScene()); }

        public static string ApplySavedScene()
        {
            RequireIdleSavedEditor();
            var field = typeof(SortGameViewController).GetField("ConnectedShelfPrefabs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(GameObject[]))
                throw new InvalidOperationException("Compile SortGameViewController.ConnectedShelfPrefabs before importing cabinets.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material) throw new InvalidOperationException("Preserved S1 shelf material is missing.");
            var texture = material.GetTexture("_BaseMap");
            if (!texture || AssetDatabase.GetAssetPath(texture) != TexturePath || texture.width != 1024 || texture.height != 1024)
                throw new InvalidOperationException("Preserved 1024 S1 shelf atlas binding is invalid.");
            var preserved = new[] { ConfigPath, MaterialPath, TexturePath }.ToDictionary(p => p, Hash);
            var offline = JObject.Parse(File.ReadAllText(Evidence + "export-validation.json"));
            foreach (var entry in preserved)
            {
                var absolute = Path.GetFullPath(entry.Key);
                if ((string)offline["source_hashes"][absolute] != entry.Value)
                    throw new InvalidOperationException("Offline artwork source changed; regenerate before import: " + entry.Key);
            }
            var levels = (JArray)JObject.Parse(File.ReadAllText(ConfigPath))["levels"];
            if (levels.Count < 5) throw new InvalidOperationException("First-five source configuration is incomplete.");
            for (int n = 1; n <= 5; n++)
                foreach (var group in new[] { "Body", "Back" })
                    if (!File.Exists(ModelPath(n, group))) throw new FileNotFoundException("Missing connected cabinet FBX", ModelPath(n, group));

            var setup = EditorSceneManager.GetSceneManagerSetup();
            Scene scratch = default(Scene);
            GameObject root = null;
            var stages = new List<object>();
            var prefabs = new GameObject[5];
            try
            {
                EnsureFolder(Prefabs.TrimEnd('/'));
                EnsureFolder("Assets/Art/Materials/Sorting/Connected");
                var palette = CabinetVolumeArtwork.EnsurePalette();
                var backMaterial = palette[2];
                scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                for (int n = 1; n <= 5; n++)
                {
                    var cells = MappedCenters(levels[n - 1], n);
                    ValidateOfflineCenters(offline, n, cells);
                    var body = ImportMesh(n, "Body");
                    var back = ImportMesh(n, "Back");
                    var expected = ExpectedBounds(cells);
                    ValidateBounds(body, back, expected, n);
                    var bodyTopology = Topology(body);
                    var backTopology = Topology(back);
                    var name = "PF_ConnectedStage" + n;
                    root = new GameObject(name);
                    SceneManager.MoveGameObjectToScene(root, scratch);
                    AddMesh(root.transform, "StaticBody", body, palette[0], palette[1]);
                    AddMesh(root.transform, "BackPanel", back, backMaterial);
                    var anchors = Child(root.transform, "Anchors");
                    for (int shelf = 0; shelf < cells.Length; shelf++)
                    {
                        var room = Child(anchors, "Shelf_" + shelf);
                        room.localPosition = cells[shelf];
                        for (int slot = 0; slot < 3; slot++)
                        {
                            Child(room, "Front" + slot).localPosition = new Vector3((slot - 1) * .94f, 0f, -.5f);
                            Child(room, "Rear" + slot).localPosition = new Vector3((slot - 1) * .94f, .34f, .35f);
                        }
                    }
                    ValidateRoot(root, material, cells, expected);
                    var path = Prefabs + name + ".prefab";
                    prefabs[n - 1] = PrefabUtility.SaveAsPrefabAsset(root, path);
                    if (!prefabs[n - 1]) throw new InvalidOperationException("Native cabinet prefab save failed: " + path);
                    UnityEngine.Object.DestroyImmediate(root); root = null;
                    ValidateRoot(prefabs[n - 1], material, cells, expected);
                    stages.Add(new
                    {
                        stage = n, prefab = path, sourceShelfCount = cells.Length,
                        body = new { model = ModelPath(n, "Body"), mesh = body.name, vertices = body.vertexCount, triangles = TriangleCount(body), topology = bodyTopology },
                        back = new { model = ModelPath(n, "Back"), mesh = back.name, vertices = back.vertexCount, triangles = TriangleCount(back), topology = backTopology },
                        triangles = TriangleCount(body) + TriangleCount(back), rendererCount = 2, colliderCount = 0,
                        boundsMin = Values(expected.min), boundsMax = Values(expected.max),
                        materials = palette.Select(AssetDatabase.GetAssetPath).ToArray(), preservedLegacyTexture = TexturePath,
                        anchors = Enumerable.Range(0, cells.Length).Select(i => new
                        {
                            shelf = i, center = Values(cells[i]),
                            front = Enumerable.Range(0, 3).Select(slot => Values(cells[i] + new Vector3((slot - 1) * .94f, 0f, -.5f))).ToArray(),
                            rear = Enumerable.Range(0, 3).Select(slot => Values(cells[i] + new Vector3((slot - 1) * .94f, .34f, .35f))).ToArray()
                        }).ToArray(),
                        sharesImportedMeshes = true, identityRootAndGroups = true
                    });
                }
                EditorSceneManager.CloseScene(scratch, true); scratch = default(Scene);
                var scene = SceneManager.GetSceneByPath(ScenePath);
                if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                var view = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SortGameViewController>(true)).Single();
                var before = EditorJsonUtility.ToJson(view);
                var serialized = new SerializedObject(view);
                var property = serialized.FindProperty("ConnectedShelfPrefabs");
                if (property == null || !property.isArray)
                    throw new InvalidOperationException("ConnectedShelfPrefabs serialized array is missing.");
                property.arraySize = 5;
                for (int i = 0; i < 5; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var after = EditorJsonUtility.ToJson(view);
                if (!UnrelatedFieldsEqual(before, after))
                {
                    EditorJsonUtility.FromJsonOverwrite(before, view);
                    throw new InvalidOperationException("Unrelated view fields changed; original bindings restored before scene save.");
                }
                foreach (var entry in preserved)
                    if (Hash(entry.Key) != entry.Value) throw new InvalidOperationException("Protected source asset changed: " + entry.Key);
                EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("GameScene save failed.");
                for (int i = 0; i < 5; i++) AssetDatabase.SaveAssetIfDirty(prefabs[i]);
                var readback = new SerializedObject(view).FindProperty("ConnectedShelfPrefabs");
                if (readback.arraySize != 5 || Enumerable.Range(0, 5).Any(i => readback.GetArrayElementAtIndex(i).objectReferenceValue != prefabs[i]))
                    throw new InvalidOperationException("Connected direct reference readback failed.");
                var result = JsonConvert.SerializeObject(new
                {
                    stages, binding = "GameScene.SortGameViewController.ConnectedShelfPrefabs only",
                    unrelatedViewFieldsUnchanged = true, protectedSourceHashes = preserved,
                    materialCountPerRenderer = new[] { 2, 1 }, offlineTopology = Evidence + "export-validation.json"
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

        private static Mesh ImportMesh(int stage, string group)
        {
            var path = ModelPath(stage, group);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Not a cabinet model: " + path);
            importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
            importer.importBlendShapes = false; importer.addCollider = false; importer.globalScale = 1f;
            importer.bakeAxisConversion = true; importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!model) throw new InvalidOperationException("Cabinet model did not import: " + path);
            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length != 1 || !filters[0].sharedMesh || model.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Cabinet FBX requires one mesh and no colliders: " + path);
            var mesh = filters[0].sharedMesh;
            if (mesh.vertexCount == 0 || mesh.subMeshCount != (group == "Body" ? 2 : 1) || mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount)
                throw new InvalidOperationException("Cabinet geometry, normals or original atlas UVs missing: " + path);
            if (mesh.uv.Any(uv => uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1))
                throw new InvalidOperationException("Cabinet original atlas UVs out of bounds: " + path);
            return mesh;
        }

        private static Vector3[] MappedCenters(JToken level, int stage)
        {
            var shelves = (JArray)level["shelves"];
            if ((int)level["number"] != stage || shelves.Count != Counts[stage - 1])
                throw new InvalidOperationException("Unexpected stage source shelf count: " + stage);
            var source = shelves.Select(s => new Vector2((float)s["x"], (float)s["y"])).ToArray();
            var middle = new Vector2((source.Min(p => p.x) + source.Max(p => p.x)) * .5f, (source.Min(p => p.y) + source.Max(p => p.y)) * .5f);
            return source.Select(p => new Vector3((p.x - middle.x) * (3.55f / 3.13f), (p.y - middle.y) * (stage == 1 ? 1.6f / 1.5f : 1.6f), 0f)).ToArray();
        }

        private static void ValidateOfflineCenters(JObject offline, int stage, Vector3[] cells)
        {
            var positions = (JArray)offline["stages"][stage - 1]["mapped_centers"];
            if (positions.Count != cells.Length) throw new InvalidOperationException("Offline art shelf count is stale.");
            for (int i = 0; i < cells.Length; i++)
                if ((cells[i] - new Vector3((float)positions[i][0], (float)positions[i][1], 0f)).sqrMagnitude > .0000001f)
                    throw new InvalidOperationException("Offline cabinet cell map differs from the source configuration.");
        }

        private static Bounds ExpectedBounds(Vector3[] cells)
        {
            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(cells.Min(p => p.x) - 1.775f, cells.Min(p => p.y) - .65f, -.70f), new Vector3(cells.Max(p => p.x) + 1.775f, cells.Max(p => p.y) + .95f, 1.10f));
            return bounds;
        }

        private static void ValidateBounds(Mesh body, Mesh back, Bounds expected, int stage)
        {
            var combined = body.bounds; combined.Encapsulate(back.bounds);
            if ((combined.min - expected.min).sqrMagnitude > .00001f || (combined.max - expected.max).sqrMagnitude > .00001f)
                throw new InvalidOperationException("Cabinet FBX basis/scale differs from final Unity cell map, stage " + stage + ". Actual " + combined + "; expected " + expected);
            if (Mathf.Abs(back.bounds.min.z - .98f) > .0001f || Mathf.Abs(back.bounds.max.z - 1.1f) > .0001f)
                throw new InvalidOperationException("Cabinet back depth is reversed; regenerate the FBX basis.");
        }

        private static object Topology(Mesh mesh)
        {
            // FBX splits vertices at normals/UV seams: weld by position for the structural check.
            var unique = new Dictionary<string, int>();
            var source = mesh.vertices;
            var remap = new int[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var p = source[i];
                var key = Math.Round(p.x * 1000000).ToString(CultureInfo.InvariantCulture) + ":" + Math.Round(p.y * 1000000).ToString(CultureInfo.InvariantCulture) + ":" + Math.Round(p.z * 1000000).ToString(CultureInfo.InvariantCulture);
                int id;
                if (!unique.TryGetValue(key, out id)) { id = unique.Count; unique.Add(key, id); }
                remap[i] = id;
            }
            var parent = Enumerable.Range(0, unique.Count).ToArray();
            var edges = new Dictionary<long, int>();
            var indices = mesh.triangles;
            int degenerate = 0;
            for (int i = 0; i < indices.Length; i += 3)
            {
                int a = remap[indices[i]], b = remap[indices[i + 1]], c = remap[indices[i + 2]];
                if (a == b || b == c || a == c || Vector3.Cross(source[indices[i + 1]] - source[indices[i]], source[indices[i + 2]] - source[indices[i]]).sqrMagnitude < 1e-20f) degenerate++;
                Join(parent, a, b); Join(parent, b, c);
                AddEdge(edges, a, b); AddEdge(edges, b, c); AddEdge(edges, c, a);
            }
            int components = Enumerable.Range(0, parent.Length).Select(i => Find(parent, i)).Distinct().Count();
            int boundary = edges.Values.Count(n => n == 1), nonmanifold = edges.Values.Count(n => n != 2);
            if (components != 1 || boundary != 0 || nonmanifold != 0 || degenerate != 0)
                throw new InvalidOperationException("Imported cabinet is not one closed structural solid: " + mesh.name + " components=" + components + " boundary=" + boundary + " nonmanifold=" + nonmanifold + " degenerate=" + degenerate);
            return new { weldedVertices = unique.Count, connectedComponents = components, boundaryEdges = boundary, nonmanifoldEdges = nonmanifold, degenerateTriangles = degenerate };
        }

        private static int Find(int[] parent, int value) { while (parent[value] != value) { parent[value] = parent[parent[value]]; value = parent[value]; } return value; }
        private static void Join(int[] parent, int a, int b) { parent[Find(parent, a)] = Find(parent, b); }
        private static void AddEdge(Dictionary<long, int> edges, int a, int b)
        {
            long key = ((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
            int count; edges.TryGetValue(key, out count); edges[key] = count + 1;
        }

        private static void ValidateRoot(GameObject root, Material material, Vector3[] cells, Bounds expected)
        {
            Identity(root.transform);
            if (root.GetComponentsInChildren<Collider>(true).Length != 0 || root.GetComponentsInChildren<MeshRenderer>(true).Length != 2 || root.GetComponentsInChildren<MeshFilter>(true).Length != 2)
                throw new InvalidOperationException("Connected cabinet requires two shared drawable groups and zero colliders.");
            var actual = new Bounds(); bool first = true;
            foreach (var key in new[] { "StaticBody", "BackPanel" })
            {
                var node = root.transform.Find(key);
                if (!node || !node.gameObject.activeSelf) throw new InvalidOperationException("Missing connected cabinet group " + key);
                Identity(node);
                var filter = node.GetComponent<MeshFilter>(); var renderer = node.GetComponent<MeshRenderer>();
                var palette=CabinetVolumeArtwork.EnsurePalette();
                var expectedMaterials=key=="BackPanel"?new[]{palette[2]}:new[]{palette[0],palette[1]};
                if (!filter || !filter.sharedMesh || !renderer || !renderer.enabled || !renderer.sharedMaterials.SequenceEqual(expectedMaterials) || filter.sharedMesh.colors.Length != filter.sharedMesh.vertexCount)
                    throw new InvalidOperationException("Invalid connected cabinet drawable " + key);
                if (!AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith(Models, StringComparison.Ordinal))
                    throw new InvalidOperationException("Cabinet must share its imported FBX mesh.");
                if (first) { actual = filter.sharedMesh.bounds; first = false; } else actual.Encapsulate(filter.sharedMesh.bounds);
            }
            if ((actual.min - expected.min).sqrMagnitude > .00001f || (actual.max - expected.max).sqrMagnitude > .00001f)
                throw new InvalidOperationException("Saved cabinet bounds changed.");
            var anchors = root.transform.Find("Anchors");
            if (!anchors || anchors.childCount != cells.Length) throw new InvalidOperationException("Cabinet shelf anchor count is invalid.");
            Identity(anchors);
            for (int i = 0; i < cells.Length; i++)
            {
                var room = anchors.Find("Shelf_" + i);
                if (!room || room.childCount != 6 || (room.localPosition - cells[i]).sqrMagnitude > .0000001f || room.localScale != Vector3.one || room.localRotation != Quaternion.identity)
                    throw new InvalidOperationException("Cabinet shelf anchor is invalid: " + i);
                for (int slot = 0; slot < 3; slot++)
                {
                    var front = room.Find("Front" + slot); var rear = room.Find("Rear" + slot);
                    if (!front || !rear || (front.localPosition - new Vector3((slot - 1) * .94f, 0f, -.5f)).sqrMagnitude > .0000001f || (rear.localPosition - new Vector3((slot - 1) * .94f, .34f, .35f)).sqrMagnitude > .0000001f)
                        throw new InvalidOperationException("Cabinet front/rear anchor is invalid: " + i + "/" + slot);
                }
            }
        }

        private static bool UnrelatedFieldsEqual(string before, string after)
        {
            var a = JObject.Parse(before); var b = JObject.Parse(after);
            ((a["MonoBehaviour"] as JObject) ?? a).Remove("ConnectedShelfPrefabs");
            ((b["MonoBehaviour"] as JObject) ?? b).Remove("ConnectedShelfPrefabs");
            return JToken.DeepEquals(a, b);
        }

        private static void RequireIdleSavedEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Editor must be idle outside Play Mode after compilation/import.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save dirty scenes before cabinet authoring.");
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty) throw new InvalidOperationException("Save dirty Prefab Mode changes before cabinet authoring.");
        }

        private static void Identity(Transform transform)
        {
            if (transform.localPosition != Vector3.zero || transform.localRotation != Quaternion.identity || transform.localScale != Vector3.one)
                throw new InvalidOperationException("Cabinet root/groups require identity transforms: " + transform.name);
        }
        private static string ModelPath(int stage, string group) { return Models + "SM_ConnectedStage" + stage + "_" + group + ".fbx"; }
        private static Transform Child(Transform parent, string name) { var node = new GameObject(name).transform; node.SetParent(parent, false); return node; }
        private static void AddMesh(Transform parent, string name, Mesh mesh, params Material[] materials)
        {
            var node = Child(parent, name); node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            // Cabinet shadows remain disabled; vertex/local AO supplies stable volume.
            // Goods still cast onto the cream/amber receiver materials.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        private static long TriangleCount(Mesh mesh) { return mesh.triangles.Length / 3; }
        private static float[] Values(Vector3 vector) { return new[] { vector.x, vector.y, vector.z }; }
        private static string Hash(string path)
        {
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
