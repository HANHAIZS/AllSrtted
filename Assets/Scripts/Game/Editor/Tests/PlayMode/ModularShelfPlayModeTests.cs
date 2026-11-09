using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GameFoundation.Save;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AllSorted.SourcePlayTests
{
    public sealed class ModularShelfPlayModeTests
    {
        private static readonly FieldInfo ArchitectureField = typeof(Architecture<AllSortedApp>)
            .GetField("mArchitecture", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly List<GameObject> fixtures = new List<GameObject>();
        private readonly List<Mesh> fixtureMeshes = new List<Mesh>();
        private object oldArchitecture;
        private Action<AllSortedApp> oldPatch;

        // Hand-derived centers from the compiled source layout, before the shelf art migration.
        private static readonly Vector2[][] ShelfCenters =
        {
            new[] { new Vector2(0, 3.15f), Vector2.zero, new Vector2(0, -3.15f) },
            new[]
            {
                new Vector2(0, 2.625f), new Vector2(-3.9125f, 1.575f), new Vector2(3.9125f, 1.575f),
                new Vector2(0, .525f), new Vector2(-3.9125f, -.525f), new Vector2(3.9125f, -.525f),
                new Vector2(0, -1.575f), new Vector2(-3.9125f, -2.625f), new Vector2(3.9125f, -2.625f)
            },
            new[]
            {
                new Vector2(-3.125f, 4.2f), new Vector2(.7875f, 4.2f),
                new Vector2(-.7875f, 2.1f), new Vector2(3.125f, 2.1f),
                new Vector2(-3.125f, 0), new Vector2(.7875f, 0),
                new Vector2(-.7875f, -2.1f), new Vector2(3.125f, -2.1f),
                new Vector2(-3.125f, -4.2f), new Vector2(.7875f, -4.2f)
            },
            new[]
            {
                new Vector2(-3.9125f, 2.625f), new Vector2(0, 2.1f), new Vector2(3.9125f, 1.575f),
                new Vector2(-3.9125f, .525f), Vector2.zero, new Vector2(3.9125f, -.525f),
                new Vector2(-3.9125f, -1.575f), new Vector2(0, -2.1f), new Vector2(3.9125f, -2.625f)
            },
            new[]
            {
                new Vector2(0, 4.2f), new Vector2(-3.9125f, 3.15f), new Vector2(3.9125f, 3.15f),
                new Vector2(0, 2.1f), new Vector2(-3.9125f, 1.05f), new Vector2(3.9125f, 1.05f),
                Vector2.zero, new Vector2(-3.9125f, -1.05f), new Vector2(3.9125f, -1.05f),
                new Vector2(0, -2.1f), new Vector2(-3.9125f, -3.15f), new Vector2(3.9125f, -3.15f),
                new Vector2(0, -4.2f)
            }
        };

        [UnitySetUp]
        public IEnumerator Setup()
        {
            foreach (var owner in Object.FindObjectsOfType<AllSortedController>())
                Object.DestroyImmediate(owner);
            oldArchitecture = ArchitectureField.GetValue(null);
            oldPatch = AllSortedApp.OnRegisterPatch;
            AllSortedApp.OnRegisterPatch = app =>
            {
                if (oldPatch != null) oldPatch(app);
                app.RegisterUtility<ISortLevelRepository>(new SortExpressCampaign());
                app.RegisterUtility<ISaveStore>(new MemorySaveStore());
            };
            ArchitectureField.SetValue(null, null);
            Time.timeScale = 1;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var owner in Object.FindObjectsOfType<AllSortedController>())
                Object.DestroyImmediate(owner);
            foreach (var fixture in fixtures)
                if (fixture) Object.DestroyImmediate(fixture);
            fixtures.Clear();
            foreach (var mesh in fixtureMeshes) if (mesh) Object.DestroyImmediate(mesh);
            fixtureMeshes.Clear();
            ArchitectureField.SetValue(null, oldArchitecture);
            AllSortedApp.OnRegisterPatch = oldPatch;
            yield break;
        }

        [UnityTest]
        public IEnumerator FirstFiveSourceStagesShareModularArtworkWithoutMovingTheirSlots()
        {
            yield return LoadGame();
            var view = Object.FindObjectOfType<SortGameViewController>();
            var model = AllSortedApp.Interface.GetModel<SortBoardModel>();
            var prefab = RequireModularPrefab(view);
            var body = RequireMeshGroup(prefab.transform, "StaticBody");
            var back = RequireMeshGroup(prefab.transform, "BackPanel");

            for (int number = 1; number <= 5; number++)
            {
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(number));
                yield return null;
                yield return null;
                Assert.AreEqual(number, model.LevelNumber.Value);
                var root = RequireShelfRoot(view, ShelfCenters[number - 1].Length);
                Assert.IsFalse(GameObject.Find("Primitive Shelves"));
                for (int shelfIndex = 0; shelfIndex < root.childCount; shelfIndex++)
                {
                    var shelf = root.Find("SourceShelf_" + shelfIndex);
                    Assert.IsNotNull(shelf, "Stage " + number + " shelf " + shelfIndex);
                    var art = RequireModularArtwork(shelf, body, back);
                    Assert.AreEqual(0, shelf.GetComponentsInChildren<Collider>(true).Length);
                    for (int slot = 0; slot < 3; slot++)
                    {
                        var center = ShelfCenters[number - 1][shelfIndex];
                        var expected = new Vector3(center.x + (slot - 1) * .94f, center.y, -.5f);
                        AssertNear(expected, view.StageSlotWorld(shelfIndex, slot),
                            "Original source input position: " + number + "/" + shelfIndex + "/" + slot);
                        AssertNear(expected, RequireAnchor(art, "Front" + slot).position,
                            "Front anchor must follow the playable slot");
                        AssertNear(expected + new Vector3(0, .34f, .85f), RequireAnchor(art, "Rear" + slot).position,
                            "Rear anchor must follow the existing depth preview");
                    }
                }
                Assert.AreEqual(model.Board.InitialCount, model.Board.RemainingCount,
                    "Changing presentation must not resolve or discard pieces on entry");
                Assert.AreEqual(0, model.Board.Matches);
            }
        }

        [UnityTest]
        public IEnumerator PrimitiveProductsRestOnTheNewShelfSupports()
        {
            yield return LoadGame();
            var view = Object.FindObjectOfType<SortGameViewController>();
            var model = AllSortedApp.Interface.GetModel<SortBoardModel>();
            for (int number = 2; number <= 5; number++)
            {
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(number));
                yield return new WaitForSecondsRealtime(.6f);
                for (int shelf = 0; shelf < model.Board.ShelfCount; shelf++)
                for (int slot = 0; slot < 3; slot++)
                for (int layer = 0; layer < 2; layer++)
                {
                    if (model.Board.IsShelfLocked(shelf)) continue;
                    var piece = layer == 0 ? model.Board.GetFront(shelf, slot) : model.Board.GetBehind(shelf, slot);
                    if (piece.IsEmpty) continue;
                    var product = view.PieceRoot.Find("Piece_" + piece.InstanceId + "_Kind" + piece.Kind);
                    Assert.IsNotNull(product, "Visible product " + number + "/" + shelf + "/" + slot + "/" + layer);
                    var mesh = product.GetComponent<MeshFilter>().sharedMesh;
                    float bottom = float.MaxValue;
                    foreach (var vertex in mesh.vertices) bottom = Mathf.Min(bottom, product.TransformPoint(vertex).y);
                    float support = view.StageSlotWorld(shelf, slot).y - .487f + (layer == 1 ? .34f : 0);
                    Assert.That(bottom, Is.EqualTo(support).Within(.005f),
                        "Actual product surface must meet the shelf support: " + number + "/" + shelf + "/" + slot + "/" + layer);
                }
            }
        }

        [UnityTest]
        public IEnumerator MissingOrMalformedModularArtworkFallsBackVisiblyAndRecoversOnReload()
        {
            yield return LoadGame();
            var view = Object.FindObjectOfType<SortGameViewController>();
            var model = AllSortedApp.Interface.GetModel<SortBoardModel>();
            var field = RequireModularField();
            var prefab = RequireModularPrefab(view);
            var legacyPrefab = view.SourceLevel1ShelfPrefab;
            Assert.IsNotNull(legacyPrefab, "Source1 recovery art must remain bound");
            var legacyMesh = legacyPrefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            var body = RequireMeshGroup(prefab.transform, "StaticBody");
            var back = RequireMeshGroup(prefab.transform, "BackPanel");
            var inactive = CopyFixture(prefab, "Inactive modular fixture");
            inactive.SetActive(false);
            var meshless = CopyFixture(prefab, "Meshless modular fixture");
            RequireMeshGroup(meshless.transform, "StaticBody").sharedMesh = null;
            var missingBackRenderer = CopyFixture(prefab, "Missing back renderer fixture");
            Object.DestroyImmediate(RequireMeshGroup(missingBackRenderer.transform, "BackPanel").GetComponent<MeshRenderer>());
            var collapsed = CopyFixture(prefab, "Collapsed static body fixture");
            collapsed.transform.Find("StaticBody").localScale = Vector3.zero;
            var noSurface = CopyFixture(prefab, "Vertices without drawable surface fixture");
            var surfaceFilter = RequireMeshGroup(noSurface.transform, "StaticBody");
            var emptySurface = Object.Instantiate(surfaceFilter.sharedMesh);
            fixtureMeshes.Add(emptySurface);
            emptySurface.triangles = new int[0];
            surfaceFilter.sharedMesh = emptySurface;

            try
            {
                foreach (var invalid in new[] { null, inactive, meshless, missingBackRenderer, collapsed, noSurface })
                {
                    field.SetValue(view, invalid);
                    view.SourceLevel1ShelfPrefab = legacyPrefab;
                    AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));
                    yield return null;
                    yield return null;
                    var firstRoot = RequireShelfRoot(view, 3);
                    foreach (Transform shelf in firstRoot)
                    {
                        var art = shelf.Find("SourceShelfArtwork");
                        Assert.IsNotNull(art, "Invalid modular art must recover the valid source1 art");
                        Assert.AreSame(legacyMesh, RequireVisibleMesh(art).sharedMesh);
                        Assert.IsNull(shelf.Find("Base"), "Successful legacy recovery must not overlap a primitive frame");
                        Assert.AreEqual(1, CountNamed(shelf, "SourceShelfArtwork"), "Rejected art must not leave an orphan");
                    }
                    Assert.AreEqual(9, model.Board.RemainingCount);

                    AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(2));
                    yield return null;
                    yield return null;
                    foreach (Transform shelf in RequireShelfRoot(view, 9))
                        Assert.AreSame(legacyMesh, RequireVisibleMesh(shelf.Find("SourceShelfArtwork")).sharedMesh,
                            "All three-slot stages must recover the valid legacy shelf");
                    Assert.AreEqual(54, model.Board.RemainingCount);
                }

                field.SetValue(view, null);
                view.SourceLevel1ShelfPrefab = null;
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));
                yield return null;
                yield return null;
                AssertPrimitiveShelves(RequireShelfRoot(view, 3));
                Assert.AreEqual(9, model.Board.RemainingCount);

                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(2));
                yield return null;
                yield return null;
                AssertPrimitiveShelves(RequireShelfRoot(view, 9));
                Assert.AreEqual(54, model.Board.RemainingCount);

                field.SetValue(view, prefab);
                view.SourceLevel1ShelfPrefab = legacyPrefab;
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(2));
                yield return null;
                yield return null;
                foreach (Transform shelf in RequireShelfRoot(view, 9)) RequireModularArtwork(shelf, body, back);
                Assert.IsTrue(body.sharedMesh && back.sharedMesh, "Fallback and cleanup must retain shared asset meshes");
            }
            finally
            {
                field.SetValue(view, prefab);
                view.SourceLevel1ShelfPrefab = legacyPrefab;
            }
        }

        [UnityTest]
        public IEnumerator MatchLocksFollowRealMovesAndRepeatedStageEntryDisposesOwnedShelves()
        {
            yield return LoadGame();
            var view = Object.FindObjectOfType<SortGameViewController>();
            var model = AllSortedApp.Interface.GetModel<SortBoardModel>();
            var campaign = (SortExpressCampaign)model.Levels;
            var prefab = RequireModularPrefab(view);
            var body = RequireMeshGroup(prefab.transform, "StaticBody");
            var back = RequireMeshGroup(prefab.transform, "BackPanel");
            int[][] initialLocks = { new[] { 3, 2, 1, 2, 1, 0, 1, 0, 0 }, new[] { 4, 2, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } };
            int[] firstMatchMoveCounts = { 4, 3 };

            for (int number = 4; number <= 5; number++)
            {
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(number));
                yield return new WaitForSecondsRealtime(.3f);
                var root = RequireShelfRoot(view, initialLocks[number - 4].Length);
                AssertLocks(root, model.Board, initialLocks[number - 4], 0);
                var untouchedPresentationBoard = campaign.Create(number);
                var solution = campaign.GetDefinition(number).solution;
                for (int index = 0; index < firstMatchMoveCounts[number - 4]; index++)
                {
                    var move = solution[index];
                    Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.a, move.b))),
                        "Pick source stage " + number + " witness move " + index);
                    Assert.IsTrue(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.c, move.d))),
                        "Drop source stage " + number + " witness move " + index);
                    Assert.IsTrue(untouchedPresentationBoard.TryMove(move.a, move.b, move.c, move.d));
                    yield return new WaitForSecondsRealtime(.25f);
                    AssertSamePieces(untouchedPresentationBoard, model.Board);
                }
                Assert.AreEqual(1, model.Board.Matches);
                Assert.AreEqual(model.Board.InitialCount - 3, model.Board.RemainingCount);
                AssertLocks(root, model.Board, initialLocks[number - 4], 1);
                foreach (Transform shelf in root) RequireModularArtwork(shelf, body, back);
            }

            var previous = RequireShelfRoot(view, 13);
            foreach (int number in new[] { 4, 5, 4, 5 })
            {
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(number));
                yield return null;
                yield return null;
                Assert.IsTrue(previous == null, "The preceding owned shelf root must be destroyed after reentry");
                var root = RequireShelfRoot(view, initialLocks[number - 4].Length);
                AssertLocks(root, model.Board, initialLocks[number - 4], 0);
                foreach (Transform shelf in root) RequireModularArtwork(shelf, body, back);
                previous = root;
            }
            Assert.IsTrue(body.sharedMesh && back.sharedMesh, "Disposing instances must not destroy shared meshes");
            Assert.IsTrue(body.GetComponent<MeshRenderer>().sharedMaterial && back.GetComponent<MeshRenderer>().sharedMaterial,
                "Disposing instances must not destroy shared materials");
        }

        private static IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync("GameScene");
            yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            // This suite preserves the legacy modular-row layout and its fallback chain.
            var view = Object.FindObjectOfType<SortGameViewController>();
            var connected = typeof(SortGameViewController).GetField("ConnectedShelfPrefabs", BindingFlags.Instance | BindingFlags.Public);
            if (connected != null) connected.SetValue(view, null);
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));
            yield return null;
            yield return null;
        }

        private static FieldInfo RequireModularField()
        {
            var field = typeof(SortGameViewController).GetField("ModularShelfPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(field, "Source stages need the shared ModularShelfPrefab scene binding");
            Assert.AreEqual(typeof(GameObject), field.FieldType);
            return field;
        }

        private static GameObject RequireModularPrefab(SortGameViewController view)
        {
            Assert.IsNotNull(view);
            var prefab = RequireModularField().GetValue(view) as GameObject;
            Assert.IsNotNull(prefab, "GameScene must bind its baked shared modular shelf prefab");
            return prefab;
        }

        private static Transform RequireShelfRoot(SortGameViewController view, int expectedShelves)
        {
            Assert.AreEqual(1, CountNamed(view.transform, "Source Shelves"), "Repeated entry must own exactly one shelf root");
            var root = view.transform.Find("Source Shelves");
            Assert.IsNotNull(root);
            Assert.AreEqual(expectedShelves, root.childCount);
            return root;
        }

        private static Transform RequireModularArtwork(Transform shelf, MeshFilter body, MeshFilter back)
        {
            var art = shelf.Find("SourceShelfArtwork");
            Assert.IsNotNull(art, shelf.name + " must display the modular recessed frame");
            Assert.AreEqual(1, CountNamed(shelf, "SourceShelfArtwork"));
            Assert.AreEqual(2, art.GetComponentsInChildren<MeshRenderer>(true).Length,
                "Merged static frame and independent back need only two renderers");
            var actualBody = RequireMeshGroup(art, "StaticBody");
            var actualBack = RequireMeshGroup(art, "BackPanel");
            Assert.AreSame(body.sharedMesh, actualBody.sharedMesh, "Every stage must reuse the baked static mesh");
            Assert.AreSame(back.sharedMesh, actualBack.sharedMesh, "Every stage must reuse the independent back mesh");
            Assert.AreSame(body.GetComponent<MeshRenderer>().sharedMaterial, actualBody.GetComponent<MeshRenderer>().sharedMaterial,
                "Static frame materials must remain shared assets");
            Assert.AreSame(back.GetComponent<MeshRenderer>().sharedMaterial, actualBack.GetComponent<MeshRenderer>().sharedMaterial,
                "Back materials must remain shared assets");
            foreach (var name in new[] { "Back", "Base", "Top", "Left", "Right" }) Assert.IsNull(shelf.Find(name));
            return art;
        }

        private static MeshFilter RequireMeshGroup(Transform art, string name)
        {
            var group = art.Find(name);
            Assert.IsNotNull(group, art.name + " needs " + name);
            return RequireVisibleMesh(group);
        }

        private static MeshFilter RequireVisibleMesh(Transform root)
        {
            Assert.IsNotNull(root, "Expected visible shelf artwork");
            var filter = root.GetComponentInChildren<MeshFilter>(true);
            Assert.IsNotNull(filter, root.name + " needs a mesh filter");
            Assert.IsTrue(filter.sharedMesh && filter.sharedMesh.vertexCount > 0, root.name + " needs a nonempty shared mesh");
            var renderer = filter.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer, root.name + " needs its mesh renderer");
            Assert.IsTrue(renderer.enabled, root.name + " must have an enabled renderer");
            for (var current = renderer.transform; current; current = current.parent)
                Assert.IsTrue(current.gameObject.activeSelf, root.name + " must have an active hierarchy");
            Assert.IsNotNull(renderer.sharedMaterial, root.name + " needs its shared material");
            return filter;
        }

        private static Transform RequireAnchor(Transform art, string name)
        {
            var anchor = art.Find("Anchors/" + name);
            if (!anchor) anchor = art.Find(name);
            Assert.IsNotNull(anchor, art.name + " needs " + name);
            return anchor;
        }

        private static void AssertPrimitiveShelves(Transform root)
        {
            foreach (Transform shelf in root)
            {
                Assert.AreEqual(0, CountNamed(shelf, "SourceShelfArtwork"), "Rejected art must leave no orphan visual");
                foreach (var name in new[] { "Back", "Base", "Top", "Left", "Right" })
                {
                    var part = shelf.Find(name);
                    Assert.IsNotNull(part, "Fallback must retain visible " + name);
                    RequireVisibleMesh(part);
                }
                Assert.AreEqual(0, shelf.GetComponentsInChildren<Collider>(true).Length);
            }
        }

        private static void AssertLocks(Transform root, SortBoardState board, int[] thresholds, int matches)
        {
            for (int index = 0; index < thresholds.Length; index++)
            {
                int expected = Math.Max(0, thresholds[index] - matches);
                Assert.AreEqual(expected, board.LockRemaining(index));
                var bar = root.Find("SourceShelf_" + index + "/MatchLock");
                Assert.IsNotNull(bar);
                Assert.AreEqual(expected > 0, bar.gameObject.activeSelf, "Shelf lock must follow remaining match count");
                var label = bar.Find("Matches remaining").GetComponent<TextMesh>();
                Assert.AreEqual(expected.ToString(), label.text);
            }
        }

        private static void AssertSamePieces(SortBoardState expected, SortBoardState actual)
        {
            Assert.AreEqual(expected.Matches, actual.Matches);
            Assert.AreEqual(expected.RemainingCount, actual.RemainingCount);
            Assert.AreEqual(expected.Phase, actual.Phase);
            for (int shelf = 0; shelf < expected.ShelfCount; shelf++)
                for (int slot = 0; slot < 3; slot++)
                {
                    Assert.AreEqual(expected.GetFront(shelf, slot).InstanceId, actual.GetFront(shelf, slot).InstanceId);
                    Assert.AreEqual(expected.GetFront(shelf, slot).Kind, actual.GetFront(shelf, slot).Kind);
                    Assert.AreEqual(expected.GetBehind(shelf, slot).InstanceId, actual.GetBehind(shelf, slot).InstanceId);
                    Assert.AreEqual(expected.GetBehind(shelf, slot).Kind, actual.GetBehind(shelf, slot).Kind);
                }
        }

        private GameObject CopyFixture(GameObject prefab, string name)
        {
            var fixture = Object.Instantiate(prefab);
            fixture.name = name;
            fixture.transform.position = Vector3.one * 1000;
            fixtures.Add(fixture);
            return fixture;
        }

        private static int CountNamed(Transform root, string name)
        {
            int count = 0;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) count++;
            return count;
        }

        private static void AssertNear(Vector3 expected, Vector3 actual, string message)
        {
            Assert.Less(Vector3.Distance(expected, actual), .001f, message + ": expected " + expected + ", got " + actual);
        }
    }
}
