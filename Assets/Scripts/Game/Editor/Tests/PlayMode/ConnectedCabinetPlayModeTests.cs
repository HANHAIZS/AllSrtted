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
    public sealed class ConnectedCabinetPlayModeTests
    {
        private static readonly FieldInfo ArchitectureField = typeof(Architecture<AllSortedApp>)
            .GetField("mArchitecture", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly List<GameObject> fixtures = new List<GameObject>();
        private object oldArchitecture;
        private Action<AllSortedApp> oldPatch;

        // Independently derived from the original JSON's min/max midpoint and compact cell dimensions.
        // Stage 3 retains its asymmetric .63/3.13 horizontal stagger rather than snapping to a grid.
        private static readonly Vector2[][] Centers =
        {
            new[] { new Vector2(0, 1.6f), Vector2.zero, new Vector2(0, -1.6f) },
            new[]
            {
                new Vector2(0, 2), new Vector2(-3.55f, 1.2f), new Vector2(3.55f, 1.2f),
                new Vector2(0, .4f), new Vector2(-3.55f, -.4f), new Vector2(3.55f, -.4f),
                new Vector2(0, -1.2f), new Vector2(-3.55f, -2), new Vector2(3.55f, -2)
            },
            new[]
            {
                new Vector2(-2.8354633f, 3.2f), new Vector2(.7145367f, 3.2f),
                new Vector2(-.7145367f, 1.6f), new Vector2(2.8354633f, 1.6f),
                new Vector2(-2.8354633f, 0), new Vector2(.7145367f, 0),
                new Vector2(-.7145367f, -1.6f), new Vector2(2.8354633f, -1.6f),
                new Vector2(-2.8354633f, -3.2f), new Vector2(.7145367f, -3.2f)
            },
            new[]
            {
                new Vector2(-3.55f, 2), new Vector2(0, 1.6f), new Vector2(3.55f, 1.2f),
                new Vector2(-3.55f, .4f), Vector2.zero, new Vector2(3.55f, -.4f),
                new Vector2(-3.55f, -1.2f), new Vector2(0, -1.6f), new Vector2(3.55f, -2)
            },
            new[]
            {
                new Vector2(0, 3.2f), new Vector2(-3.55f, 2.4f), new Vector2(3.55f, 2.4f),
                new Vector2(0, 1.6f), new Vector2(-3.55f, .8f), new Vector2(3.55f, .8f),
                Vector2.zero, new Vector2(-3.55f, -.8f), new Vector2(3.55f, -.8f),
                new Vector2(0, -1.6f), new Vector2(-3.55f, -2.4f), new Vector2(3.55f, -2.4f),
                new Vector2(0, -3.2f)
            }
        };

        private static readonly Vector2[] LegacyStageTwoCenters =
        {
            new Vector2(0, 2.625f), new Vector2(-3.9125f, 1.575f), new Vector2(3.9125f, 1.575f),
            new Vector2(0, .525f), new Vector2(-3.9125f, -.525f), new Vector2(3.9125f, -.525f),
            new Vector2(0, -1.575f), new Vector2(-3.9125f, -2.625f), new Vector2(3.9125f, -2.625f)
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
            ArchitectureField.SetValue(null, oldArchitecture);
            AllSortedApp.OnRegisterPatch = oldPatch;
            yield break;
        }

        [UnityTest]
        public IEnumerator FirstFiveStagesDisplayOneSharedCabinetAtTheCompactPlayableAnchors()
        {
            yield return LoadGame();
            var view = Object.FindObjectOfType<SortGameViewController>();
            var model = AllSortedApp.Interface.GetModel<SortBoardModel>();
            var prefabs = RequireConnectedPrefabs(view);
            var approvedRotation=view.BoardCamera.transform.rotation;

            for (int number = 1; number <= 5; number++)
            {
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(number));
                yield return null;
                yield return null;
                Assert.AreEqual(number, model.LevelNumber.Value);
                Assert.IsTrue(view.BoardCamera.orthographic);Assert.AreEqual(60,view.BoardCamera.fieldOfView);
                Assert.Less(Quaternion.Angle(approvedRotation,view.BoardCamera.transform.rotation),.01f,"All five stages share the approved first-stage angle");
                var root = RequireShelfRoot(view, Centers[number - 1].Length, true);
                var art = AssertConnectedArtwork(root, prefabs[number - 1]);
                Assert.IsFalse(GameObject.Find("Primitive Shelves"));
                Assert.AreEqual(0, root.GetComponentsInChildren<Collider>(true).Length);
                for (int shelf = 0; shelf < model.Board.ShelfCount; shelf++)
                {
                    var row = root.Find("SourceShelf_" + shelf);
                    Assert.IsNotNull(row, "Each model shelf retains its own lock owner");
                    var center = Centers[number - 1][shelf];
                    AssertNear(new Vector3(center.x, center.y, 0), row.position,
                        "The lock owner must use the compact source mapping");
                    Assert.IsNotNull(row.Find("MatchLock"));
                    Assert.IsNull(row.Find("SourceShelfArtwork"), "No independent row frame may overlap the cabinet");
                    foreach (var name in new[] { "Back", "Base", "Top", "Left", "Right" })
                        Assert.IsNull(row.Find(name), "Connected cabinets must not retain primitive row frames");
                    for (int slot = 0; slot < 3; slot++)
                    {
                        var front = new Vector3(center.x + (slot - 1) * .94f, center.y, -.5f);
                        var frontAnchor = art.Find("Anchors/Shelf_" + shelf + "/Front" + slot);
                        var rearAnchor = art.Find("Anchors/Shelf_" + shelf + "/Rear" + slot);
                        Assert.IsNotNull(frontAnchor);
                        Assert.IsNotNull(rearAnchor);
                        AssertNear(front, view.StageSlotWorld(shelf, slot), "Playable slot " + number + "/" + shelf + "/" + slot);
                        AssertNear(front, frontAnchor.position, "Front cabinet anchor");
                        AssertNear(front + new Vector3(0, .34f, .85f), rearAnchor.position, "Rear preview anchor");
                    }
                }
                AssertCabinetBounds(art, Centers[number - 1]);
                AssertSamePieces(((SortExpressCampaign)model.Levels).Create(number), model.Board);
            }
        }

        [UnityTest]
        public IEnumerator VisiblePrimitiveGoodsMeetTheConnectedCabinetFloor()
        {
            yield return LoadGame();
            var view = Object.FindObjectOfType<SortGameViewController>();
            var model = AllSortedApp.Interface.GetModel<SortBoardModel>();
            RequireConnectedPrefabs(view);
            for (int number = 2; number <= 5; number++)
            {
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(number));
                yield return new WaitForSecondsRealtime(.6f);
                Assert.IsNotNull(RequireShelfRoot(view, Centers[number - 1].Length, true).Find("ConnectedArtwork"));
                int measured = 0;
                for (int shelf = 0; shelf < model.Board.ShelfCount; shelf++)
                for (int slot = 0; slot < 3; slot++)
                for (int layer = 0; layer < 2; layer++)
                {
                    if (model.Board.IsShelfLocked(shelf)) continue;
                    var piece = layer == 0 ? model.Board.GetFront(shelf, slot) : model.Board.GetBehind(shelf, slot);
                    if (piece.IsEmpty) continue;
                    var product = view.PieceRoot.Find("Piece_" + piece.InstanceId + "_Kind" + piece.Kind);
                    Assert.IsNotNull(product);
                    Assert.IsTrue(product.GetComponent<Renderer>().enabled, "This check measures visible primitive goods");
                    var mesh = product.GetComponent<MeshFilter>().sharedMesh;
                    float bottom = float.MaxValue;
                    foreach (var vertex in mesh.vertices) bottom = Mathf.Min(bottom, product.TransformPoint(vertex).y);
                    float expected = Centers[number - 1][shelf].y - .487f + (layer == 1 ? .34f : 0);
                    Assert.That(bottom, Is.EqualTo(expected).Within(.005f),
                        "Product surface meets cabinet support: " + number + "/" + shelf + "/" + slot + "/" + layer);
                    measured++;
                }
                Assert.Greater(measured, 0, "Each stage must check actual visible goods");
            }
        }

        [UnityTest]
        public IEnumerator LocksFollowRealMovesAndReloadDestroysOnlyOwnedCabinetInstances()
        {
            yield return LoadGame();
            var view = Object.FindObjectOfType<SortGameViewController>();
            var model = AllSortedApp.Interface.GetModel<SortBoardModel>();
            var campaign = (SortExpressCampaign)model.Levels;
            var prefabs = RequireConnectedPrefabs(view);
            int[][] thresholds =
            {
                new[] { 3, 2, 1, 2, 1, 0, 1, 0, 0 },
                new[] { 4, 2, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
            };
            int[] movesToFirstMatch = { 4, 3 };

            for (int number = 4; number <= 5; number++)
            {
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(number));
                yield return new WaitForSecondsRealtime(.3f);
                var root = RequireShelfRoot(view, thresholds[number - 4].Length, true);
                var art = AssertConnectedArtwork(root, prefabs[number - 1]);
                AssertLocks(root, model.Board, thresholds[number - 4], 0);
                var expectedBoard = campaign.Create(number);
                var solution = campaign.GetDefinition(number).solution;
                for (int index = 0; index < movesToFirstMatch[number - 4]; index++)
                {
                    var move = solution[index];
                    Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.a, move.b))),
                        "Pick stage " + number + " solution move " + index);
                    Assert.IsTrue(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.c, move.d))),
                        "Drop stage " + number + " solution move " + index);
                    Assert.IsTrue(expectedBoard.TryMove(move.a, move.b, move.c, move.d));
                    yield return new WaitForSecondsRealtime(.25f);
                    AssertSamePieces(expectedBoard, model.Board);
                }
                Assert.AreEqual(1, model.Board.Matches);
                AssertLocks(root, model.Board, thresholds[number - 4], 1);
                Assert.AreSame(art, AssertConnectedArtwork(root, prefabs[number - 1]),
                    "A match refresh must retain the cabinet instance while updating independent locks");
            }

            var previousRoot = RequireShelfRoot(view, 13, true);
            var previousArt = previousRoot.Find("ConnectedArtwork");
            foreach (int number in new[] { 4, 5, 4, 5 })
            {
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(number));
                yield return null;
                yield return null;
                Assert.IsTrue(previousRoot == null && previousArt == null, "Reentry destroys the previous owned cabinet hierarchy");
                var root = RequireShelfRoot(view, thresholds[number - 4].Length, true);
                AssertLocks(root, model.Board, thresholds[number - 4], 0);
                previousArt = AssertConnectedArtwork(root, prefabs[number - 1]);
                previousRoot = root;
            }
            Object.Destroy(view.gameObject);
            yield return null;
            yield return null;
            Assert.IsTrue(previousRoot == null && previousArt == null, "Owner destruction cleans its cabinet hierarchy");
            foreach (var prefab in prefabs)
            {
                AssertSharedGroupAlive(prefab.transform, "StaticBody");
                AssertSharedGroupAlive(prefab.transform, "BackPanel");
            }
        }

        [UnityTest]
        public IEnumerator MissingInactiveOrPartialCabinetsFallBackToModularRowsAndRecover()
        {
            yield return LoadGame();
            var view = Object.FindObjectOfType<SortGameViewController>();
            var model = AllSortedApp.Interface.GetModel<SortBoardModel>();
            var campaign = (SortExpressCampaign)model.Levels;
            var field = RequireConnectedField();
            var prefabs = RequireConnectedPrefabs(view);
            var modular = view.ModularShelfPrefab;
            Assert.IsNotNull(modular, "Connected art failures must recover the configured modular rows first");
            var modularBody = RequireMesh(modular.transform, "StaticBody");
            var modularBack = RequireMesh(modular.transform, "BackPanel");
            var inactive = CopyFixture(prefabs[1], "Inactive connected cabinet");
            inactive.SetActive(false);
            var missingBack = CopyFixture(prefabs[1], "Partial connected cabinet without back");
            Object.DestroyImmediate(missingBack.transform.Find("BackPanel").gameObject);
            var missingAnchor = CopyFixture(prefabs[1], "Partial connected cabinet without rear anchor");
            Object.DestroyImmediate(missingAnchor.transform.Find("Anchors/Shelf_0/Rear0").gameObject);
            var absentEntry = (GameObject[])prefabs.Clone();
            absentEntry[1] = null;
            var cases = new List<GameObject[]> { null, new GameObject[0], absentEntry };
            foreach (var invalid in new[] { inactive, missingBack, missingAnchor })
            {
                var candidate = (GameObject[])prefabs.Clone();
                candidate[1] = invalid;
                cases.Add(candidate);
            }

            try
            {
                foreach (var candidate in cases)
                {
                    field.SetValue(view, candidate);
                    AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(2));
                    yield return null;
                    yield return null;
                    var root = RequireShelfRoot(view, 9, false);
                    Assert.AreEqual(0, CountNamed(root, "ConnectedArtwork"), "Rejected cabinets must leave no orphan instance");
                    for (int shelf = 0; shelf < 9; shelf++)
                    {
                        var row = root.Find("SourceShelf_" + shelf);
                        Assert.IsNotNull(row);
                        var art = row.Find("SourceShelfArtwork");
                        Assert.IsNotNull(art, "Missing or invalid connected art falls back to visible modular rows");
                        Assert.AreEqual(2, art.GetComponentsInChildren<MeshRenderer>(true).Length);
                        Assert.AreSame(modularBody.sharedMesh, RequireMesh(art, "StaticBody").sharedMesh);
                        Assert.AreSame(modularBack.sharedMesh, RequireMesh(art, "BackPanel").sharedMesh);
                        Assert.AreEqual(0, row.GetComponentsInChildren<Collider>(true).Length);
                        for (int slot = 0; slot < 3; slot++)
                        {
                            var center = LegacyStageTwoCenters[shelf];
                            var expected = new Vector3(center.x + (slot - 1) * .94f, center.y, -.5f);
                            AssertNear(expected, view.StageSlotWorld(shelf, slot), "Fallback keeps legacy row mapping");
                            AssertNear(expected, art.Find("Anchors/Front" + slot).position, "Fallback modular anchor follows its slot");
                        }
                    }
                    AssertSamePieces(campaign.Create(2), model.Board);
                    var move = campaign.GetDefinition(2).solution[0];
                    Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.a, move.b))),
                        "Fallback remains playable");
                    Assert.IsTrue(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.c, move.d))));
                    yield return new WaitForSecondsRealtime(.25f);

                    field.SetValue(view, prefabs);
                    AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(2));
                    yield return null;
                    yield return null;
                    Assert.IsTrue(root == null, "Recovery removes the temporary modular row hierarchy");
                    AssertConnectedArtwork(RequireShelfRoot(view, 9, true), prefabs[1]);
                    AssertSamePieces(campaign.Create(2), model.Board);
                }
            }
            finally
            {
                field.SetValue(view, prefabs);
            }
            AssertSharedGroupAlive(modular.transform, "StaticBody");
            AssertSharedGroupAlive(modular.transform, "BackPanel");
            foreach (var prefab in prefabs)
            {
                AssertSharedGroupAlive(prefab.transform, "StaticBody");
                AssertSharedGroupAlive(prefab.transform, "BackPanel");
            }
        }

        [UnityTest]
        public IEnumerator ContactShadowsStayAtSupportAndCleanUpWithoutTouchingSharedAssets()
        {
            yield return LoadGame();
            var view=Object.FindObjectOfType<SortGameViewController>();
            var model=AllSortedApp.Interface.GetModel<SortBoardModel>();
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(2));
            yield return new WaitForSecondsRealtime(.6f);
            var root=view.transform.Find("Product Contact Shadows");Assert.IsNotNull(root);
            Assert.AreEqual(view.RenderedPieceCount,root.childCount);
            for(int shelf=0;shelf<model.Board.ShelfCount;shelf++)for(int slot=0;slot<3;slot++){
                var rear=model.Board.GetBehind(shelf,slot);if(rear.IsEmpty || model.Board.IsShelfLocked(shelf))continue;
                var rearShadow=root.Find("Contact_"+rear.InstanceId);Assert.IsNotNull(rearShadow);
                Assert.That(rearShadow.position.y,Is.EqualTo(Centers[1][shelf].y+SortShelfGeometry.SupportY+.004f).Within(.001f),"Rear preview shadows remain on the physical floor");
            }
            var mesh=root.GetChild(0).GetComponent<MeshFilter>().sharedMesh;
            var material=root.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
            foreach(Transform shadow in root) {
                Assert.AreSame(mesh,shadow.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreSame(material,shadow.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.AreEqual(0,shadow.GetComponentsInChildren<Collider>().Length);
            }
            var move=((SortExpressCampaign)model.Levels).GetDefinition(2).solution[0];
            var piece=model.Board.GetFront(move.a,move.b);
            var contact=root.Find("Contact_"+piece.InstanceId);Assert.IsNotNull(contact);
            var before=contact.position;
            Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.a,move.b))));
            typeof(SortGameViewController).GetMethod("MoveDrag",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{(Vector2)view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.a,move.b)+Vector3.up)});
            yield return null;Assert.Less(Vector3.Distance(before,contact.position),.001f,"Contact remains on the support, not the flying item");
            view.CancelDrag();yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.a,move.b))));
            Assert.IsTrue(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.c,move.d))));
            yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(view.RenderedPieceCount,root.childCount);
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(5));
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(view.RenderedPieceCount,root.childCount,"Restart/replacement leaves no stale contacts");
            Object.Destroy(view.gameObject);yield return null;yield return null;
            Assert.IsTrue(root==null);Assert.IsTrue(mesh && material,"Shared authored resources survive view destruction");
        }

        [UnityTest]
        public IEnumerator CabinetLocalAOChangesRecessPixelsAndRecordsRealRenderPasses()
        {
            yield return LoadGame();
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(3));yield return new WaitForSecondsRealtime(.6f);
            var view=Object.FindObjectOfType<SortGameViewController>();var art=view.transform.Find("Source Shelves/ConnectedArtwork");
            var mats=new List<Material>();foreach(var renderer in art.GetComponentsInChildren<Renderer>())foreach(var mat in renderer.sharedMaterials)if(!mats.Contains(mat))mats.Add(mat);
            var first=RenderCabinetPixels(view.BoardCamera);
            try {
                foreach(var mat in mats)mat.SetFloat("_AOStrength",0);
                var second=RenderCabinetPixels(view.BoardCamera);int different=0;
                for(int i=0;i<first.Length;i++)if(Mathf.Abs(first[i].r-second[i].r)+Mathf.Abs(first[i].g-second[i].g)+Mathf.Abs(first[i].b-second[i].b)>6)different++;
                Assert.Greater(different,100,"AO must visibly shade the actual recess pixels, not just exist on a material");
            } finally {foreach(var mat in mats)mat.SetFloat("_AOStrength",.65f);}
            var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility");
            var flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;var enable=type.GetMethod("SetEnabled",flags);
            try {
                enable.Invoke(null,new object[]{true,0});yield return null;yield return null;
                int count=(int)type.GetProperty("count",flags).GetValue(null);var lines=new List<string>();
                for(int i=0;i<count;i++)lines.Add((string)type.GetMethod("GetFrameEventInfoName",flags).Invoke(null,new object[]{i}));
                System.IO.Directory.CreateDirectory("TestResults");System.IO.File.WriteAllLines("TestResults/cabinet-frame-events.txt",lines);
                System.IO.File.WriteAllText("TestResults/cabinet-global-ao.txt","cells="+Shader.GetGlobalInt("_CabinetCellCount")+"; rects="+Shader.GetGlobalVectorArray("_CabinetCellRects").Length+"; frameEvents="+count);
            } finally {enable.Invoke(null,new object[]{false,0});}
        }
        [UnityTest]
        public IEnumerator UpperRecessShadowDarkensPixelsWithoutChangingThePalette()
        {
            yield return LoadGame();var view=Object.FindObjectOfType<SortGameViewController>();
            var back=view.transform.Find("Source Shelves/ConnectedArtwork/BackPanel").GetComponent<MeshRenderer>();
            var color=back.sharedMaterial.GetColor("_BaseColor");var block=new MaterialPropertyBlock();
            block.SetFloat("_TopShadowStrength",0);back.SetPropertyBlock(block);var clear=RenderCabinetPixels(view.BoardCamera);
            block.SetFloat("_TopShadowStrength",.38f);back.SetPropertyBlock(block);var shaded=RenderCabinetPixels(view.BoardCamera);
            int darker=0;for(int i=0;i<clear.Length;i++)if(clear[i].r-shaded[i].r+clear[i].g-shaded[i].g+clear[i].b-shaded[i].b>6)darker++;
            Assert.AreEqual(color,back.sharedMaterial.GetColor("_BaseColor"));
            Assert.Greater(darker,100,"The requested upper interior shadow must change visible wall pixels");
        }
        private static Color32[] RenderCabinetPixels(Camera camera)
        {
            var previous=camera.targetTexture;var active=RenderTexture.active;
            var target=RenderTexture.GetTemporary(360,640,24);var texture=new Texture2D(360,640,TextureFormat.RGBA32,false);
            try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,360,640),0,0);texture.Apply();return texture.GetPixels32();}
            finally {camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(texture);}
        }

        private static IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync("GameScene");
            yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }

        private static FieldInfo RequireConnectedField()
        {
            var field = typeof(SortGameViewController).GetField("ConnectedShelfPrefabs", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(field, "The scene controller needs five connected cabinet prefab bindings");
            Assert.AreEqual(typeof(GameObject[]), field.FieldType);
            return field;
        }

        private static GameObject[] RequireConnectedPrefabs(SortGameViewController view)
        {
            Assert.IsNotNull(view);
            var prefabs = RequireConnectedField().GetValue(view) as GameObject[];
            Assert.IsNotNull(prefabs, "GameScene must bind its five baked connected cabinets");
            Assert.AreEqual(5, prefabs.Length);
            foreach (var prefab in prefabs) Assert.IsNotNull(prefab, "Each compiled source stage needs its own connected cabinet");
            return prefabs;
        }

        private static Transform RequireShelfRoot(SortGameViewController view, int shelfCount, bool connected)
        {
            Assert.AreEqual(1, CountNamed(view.transform, "Source Shelves"), "Entry owns exactly one shelf hierarchy");
            var root = view.transform.Find("Source Shelves");
            Assert.IsNotNull(root);
            Assert.AreEqual(shelfCount + (connected ? 1 : 0), root.childCount,
                "One connected artwork plus independent lock roots, or one legacy artwork per row");
            return root;
        }

        private static Transform AssertConnectedArtwork(Transform root, GameObject prefab)
        {
            Assert.AreEqual(1, CountNamed(root, "ConnectedArtwork"));
            Assert.AreEqual(0, CountNamed(root, "SourceShelfArtwork"), "Connected stages must not instantiate independent shelf boxes");
            var art = root.Find("ConnectedArtwork");
            Assert.IsNotNull(art);
            Assert.AreEqual(2, art.GetComponentsInChildren<MeshRenderer>(true).Length,
                "The whole cabinet shares one static body and one back instead of per-row frames");
            Assert.AreEqual(0, art.GetComponentsInChildren<Collider>(true).Length);
            foreach (var name in new[] { "StaticBody", "BackPanel" })
            {
                var expected = RequireMesh(prefab.transform, name);
                var actual = RequireMesh(art, name);
                Assert.AreSame(expected.sharedMesh, actual.sharedMesh, "Instances reuse baked meshes");
                CollectionAssert.AreEqual(expected.GetComponent<MeshRenderer>().sharedMaterials,
                    actual.GetComponent<MeshRenderer>().sharedMaterials, "Instances reuse shared material assets");
            }
            return art;
        }

        private static MeshFilter RequireMesh(Transform root, string name)
        {
            var group = root.Find(name);
            Assert.IsNotNull(group, root.name + " needs " + name);
            var filter = group.GetComponent<MeshFilter>();
            var renderer = group.GetComponent<MeshRenderer>();
            Assert.IsNotNull(filter);
            Assert.IsTrue(filter.sharedMesh && filter.sharedMesh.vertexCount > 0);
            Assert.IsNotNull(renderer);
            Assert.IsTrue(renderer.enabled);
            Assert.IsNotNull(renderer.sharedMaterial);
            for (var current = group; current; current = current.parent)
                Assert.IsTrue(current.gameObject.activeSelf, name + " must have an active hierarchy");
            return filter;
        }

        private static void AssertCabinetBounds(Transform art, Vector2[] centers)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var center in centers)
            {
                minX = Mathf.Min(minX, center.x - 1.775f);
                maxX = Mathf.Max(maxX, center.x + 1.775f);
                minY = Mathf.Min(minY, center.y - .65f);
                maxY = Mathf.Max(maxY, center.y + .95f);
            }
            var renderers = art.GetComponentsInChildren<MeshRenderer>();
            var actual = renderers[0].bounds;
            foreach (var renderer in renderers) actual.Encapsulate(renderer.bounds);
            Assert.That(actual.min.x, Is.EqualTo(minX).Within(.025f), "Cabinet spans the compact cell left edge");
            Assert.That(actual.max.x, Is.EqualTo(maxX).Within(.025f), "Cabinet spans the compact cell right edge");
            Assert.That(actual.min.y, Is.EqualTo(minY).Within(.025f), "Cabinet spans the compact cell bottom edge");
            Assert.That(actual.max.y, Is.EqualTo(maxY).Within(.025f), "Cabinet spans the compact cell top edge");
        }

        private static void AssertSharedGroupAlive(Transform prefab, string name)
        {
            var filter = RequireMesh(prefab, name);
            Assert.IsTrue(filter.sharedMesh, "Disposing a cabinet instance must never destroy its shared mesh");
            Assert.IsTrue(filter.GetComponent<MeshRenderer>().sharedMaterial,
                "Disposing a cabinet instance must never destroy its shared material");
        }

        private static void AssertLocks(Transform root, SortBoardState board, int[] thresholds, int matches)
        {
            for (int shelf = 0; shelf < thresholds.Length; shelf++)
            {
                int expected = Math.Max(0, thresholds[shelf] - matches);
                Assert.AreEqual(expected, board.LockRemaining(shelf));
                var bar = root.Find("SourceShelf_" + shelf + "/MatchLock");
                Assert.IsNotNull(bar);
                Assert.AreEqual(expected > 0, bar.gameObject.activeSelf);
                Assert.AreEqual(expected.ToString(), bar.Find("Matches remaining").GetComponent<TextMesh>().text);
                if(expected>0)Assert.AreEqual("AllSorted/LockDoor",bar.GetComponent<MeshRenderer>().sharedMaterial.shader.name);
            }
        }

        private static void AssertSamePieces(SortBoardState expected, SortBoardState actual)
        {
            Assert.AreEqual(expected.ShelfCount, actual.ShelfCount);
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
