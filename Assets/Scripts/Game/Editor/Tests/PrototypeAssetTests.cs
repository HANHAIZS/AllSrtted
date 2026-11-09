using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using AllSorted.Editor;
using GameFoundation.UI;
using GameFoundation.UI.Editor;
using UnityEngine;
namespace AllSorted.Tests
{
    public sealed class PrototypeAssetTests : WalletTestBase
    {
        /// <summary>Requires an actual authored scene, rather than runtime-only placeholder setup.</summary>
        [Test] public void PrototypeSceneIsAuthoredAndEnabled()
        {
            const string path="Assets/Scenes/GameScene.unity";
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
            Assert.IsTrue(System.Array.Exists(EditorBuildSettings.scenes,s=>s.enabled && s.path==path));
        }

        /// <summary>Checks actual path-loaded resources exist before exercising their lifetime.</summary>
        [Test] public void PauseAndResultPopupsHaveUniqueResourcesKeys()
        {
            Assert.IsNotNull(Resources.Load<GameObject>("Prefabs/UI/Popups/PausePopup"));
            Assert.IsNotNull(Resources.Load<GameObject>("Prefabs/UI/Popups/ResultPopup"));
        }

        /// <summary>Applies the installed Foundation validator to actual authored popup assets.</summary>
        [TestCase("PausePopup")][TestCase("ResultPopup")]
        public void PopupPrefabsPassFoundationConventions(string name)
        {
            var profile=AssetDatabase.LoadAssetAtPath<UiPrefabConventionProfile>("Assets/Settings/GameFoundation/AllSortedUiPrefabConvention.asset");
            var report=UiPrefabValidator.Validate("Assets/Resources/Prefabs/UI/Popups/"+name+".prefab",profile,UiPrefabKind.Popup);
            Assert.IsTrue(report.IsValid,report.ToMultilineString());
        }

        /// <summary>Reads saved Inspector bindings and the complete scene dependency closure.</summary>
        [Test] public void SavedSceneBindingsUseOnlyAuthoredProjectContent()
        {
            var previous=EditorSceneManager.GetSceneManagerSetup();
            try {
                EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
                var view=Object.FindObjectOfType<SortGameViewController>();Assert.IsNotNull(view);
                Assert.IsNotNull(view.BoardCamera);Assert.IsTrue(view.BoardCamera.orthographic);Assert.AreEqual(60,view.BoardCamera.fieldOfView);
                Assert.IsNotNull(view.PieceRoot);Assert.IsNotNull(view.TimerLabel);Assert.IsNotNull(view.CountLabel);
                Assert.IsNotNull(view.PauseButton);Assert.IsNotNull(view.ProgressFill);Assert.AreEqual(7,view.Layers.Hosts.Length);
                Assert.IsNotNull(view.LevelLabel);
                Assert.AreEqual(4,view.BoosterButtons.Length);Assert.IsNotNull(view.FreezeCounter);Assert.IsNotNull(view.BoosterFeedback);
                var card=Resources.Load<GameObject>("Prefabs/Merchandise/CardTransformation/CardItem");
                Assert.IsNotNull(card);
                var cardMaterials=card.transform.Find("Visual").GetComponent<MeshRenderer>().sharedMaterials;
                Assert.AreEqual("CardFrontMat",cardMaterials[4].name);
                Assert.AreEqual("CardBackMat",cardMaterials[5].name);
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Sorting/GoldBall.mat"));
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Sorting/GoldBallBack.mat"));
                Assert.AreEqual(4,view.BoosterBadges.Length);Assert.AreEqual(4,view.BoosterChargeLabels.Length);
                Assert.IsNotNull(view.AddChargeBadgeSprite);Assert.IsNotNull(view.RemainingChargeBadgeSprite);
                foreach(var sprite in new[]{view.AddChargeBadgeSprite,view.RemainingChargeBadgeSprite}) {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                    Assert.AreEqual(TextureImporterType.Sprite,importer.textureType);Assert.IsTrue(importer.alphaIsTransparency);
                    Assert.IsFalse(importer.mipmapEnabled);Assert.LessOrEqual(importer.maxTextureSize,512);
                }
                for(int i=0;i<4;i++) {
                    var badge=view.BoosterBadges[i];Assert.IsNotNull(badge);var rect=(RectTransform)badge.transform;
                    Assert.AreSame(view.BoosterButtons[i].transform,rect.parent);
                    Assert.AreEqual(new Vector2(1,0),rect.anchorMin);Assert.AreEqual(rect.anchorMin,rect.anchorMax);
                    Assert.AreEqual(new Vector2(1,0),rect.pivot);Assert.AreEqual(new Vector2(44,44),rect.sizeDelta);
                    Assert.AreEqual(new Vector2(4,-4),rect.anchoredPosition);Assert.IsTrue(badge.image.preserveAspect);
                    Assert.AreEqual(view.RemainingChargeBadgeSprite,badge.image.sprite);
                    Assert.IsNotNull(badge.GetComponent<SortButtonPressFeedback>());
                    Assert.IsNotNull(view.BoosterButtons[i].GetComponent<SortButtonPressFeedback>());
                    Assert.AreEqual("3",view.BoosterChargeLabels[i].text);Assert.IsFalse(view.BoosterChargeLabels[i].raycastTarget);
                }
                for(int i=0;i<7;i++) {
                    Assert.IsNotNull(view.Layers.Hosts[i]);
                    Assert.AreEqual(i*100,view.Layers.Hosts[i].GetComponent<Canvas>().sortingOrder);
                }
                for(int i=0;i<4;i++) {
                    StringAssert.StartsWith("Assets/Art/Materials/Sorting/",AssetDatabase.GetAssetPath(view.Palette[i]));
                    StringAssert.StartsWith("Assets/Art/Materials/Sorting/",AssetDatabase.GetAssetPath(view.BackPalette[i]));
                }
                var originalDependencies=new System.Collections.Generic.HashSet<string>();
                foreach(string key in new[]{"Apple","Hanoi","Sunflower","Pear","Shelf"}) {
                    originalDependencies.Add("Assets/Art/Models/Level1/SM_L1_"+key+"_A.fbx");
                    originalDependencies.Add("Assets/Art/Textures/Level1/T_L1_"+key+"_Albedo.png");
                    originalDependencies.Add("Assets/Prefabs/Merchandise/Level1/PF_L1_"+key+".prefab");
                }
                foreach(string name in new[]{"Frame","Snow","Pulse"})
                    originalDependencies.Add("Assets/Sprites/UI/WatchFreeze/"+name+".png");
                foreach(string key in new[]{"Apple","Hanoi","Sunflower","Shelf"}) {
                    originalDependencies.Add("Assets/Art/Models/SourceLevel1/SM_S1_"+key+".fbx");
                    originalDependencies.Add("Assets/Art/Textures/SourceLevel1/T_S1_"+key+"_Albedo.png");
                    originalDependencies.Add("Assets/Prefabs/Merchandise/SourceLevel1/PF_S1_"+key+".prefab");
                }
                originalDependencies.Add("Assets/Art/Textures/Sorting/LockDoor.png");
                originalDependencies.Add("Assets/Art/Models/Shelves/Connected/ContactShadowMesh.asset");
                originalDependencies.Add("Assets/Settings/CabinetVolumeProfile.asset");
                originalDependencies.Add("Assets/Prefabs/Shelves/PF_ModularShelf3.prefab");
                originalDependencies.Add("Assets/Art/Models/Shelves/SM_ModularShelf3_Static.asset");
                originalDependencies.Add("Assets/Art/Models/Shelves/SM_ModularShelf3_BackPanel.asset");
                for(int number=1;number<=5;number++) {
                    originalDependencies.Add("Assets/Prefabs/Shelves/Connected/PF_ConnectedStage"+number+".prefab");
                    foreach(string part in new[]{"Body","Back"})
                        originalDependencies.Add("Assets/Art/Models/Shelves/Connected/SM_ConnectedStage"+number+"_"+part+".fbx");
                }
                var audit=new System.Text.StringBuilder();
                foreach(string root in new[]{"Assets/Scenes/GameScene.unity","Assets/Resources/Prefabs/UI/Popups/PausePopup.prefab","Assets/Resources/Prefabs/UI/Popups/ResultPopup.prefab"}) {
                    audit.AppendLine(root);
                    foreach(string path in AssetDatabase.GetDependencies(root,true)) {
                        audit.AppendLine("  "+path);
                        if(!path.StartsWith("Assets/") || path==root) continue;
                        Assert.IsTrue(originalDependencies.Contains(path) || path=="Assets/Sprites/UI/HudFrames/Normal.png" || path=="Assets/Sprites/UI/HudFrames/Frozen.png" || path=="Assets/Sprites/UI/HudFrames/Level.png" || path=="Assets/Art/Fonts/Fredoka/Fredoka-Bold.ttf" || path.StartsWith("Assets/Scripts/Game/") || path.StartsWith("Assets/Art/Materials/Sorting/") || path.StartsWith("Assets/Art/Shaders/Sorting/") || path.StartsWith(PrototypeBoosterArtwork.SpriteRoot) || path.StartsWith("Assets/Sprites/UI/PauseSettings/") || path=="Assets/Sprites/UI/PauseGear.png" || path=="Assets/Sprites/Backgrounds/LevelBackground.png" || path==PrototypeHudLayout.BarPath || path==PrototypeHudLayout.ButtonPath,"Unexpected authored asset dependency: "+path);
                    }
                }
                System.IO.Directory.CreateDirectory("TestResults");
                System.IO.File.WriteAllText("TestResults/sorting-asset-dependencies.txt",audit.ToString());
            } finally {
                if(System.Array.Exists(previous,s=>s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
    }
}
