using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace AllSorted.Tests
{
    public sealed class CabinetVolumeAssetTests
    {
        const string Materials="Assets/Art/Materials/Sorting/Connected/";
        [TestCase("CabinetFrameMaterial", "#E8E0CF", .40f)]
        [TestCase("CabinetShelfMaterial", "#C98B4B", .35f)]
        [TestCase("CabinetBackMaterial", "#2A5A63", .15f)]
        public void CabinetPaletteIsIndependentAndLit(string name,string hex,float smoothness)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Materials+name+".mat");
            Assert.IsNotNull(material);Assert.AreEqual("AllSorted/CabinetLit",material.shader.name);
            Color expected;ColorUtility.TryParseHtmlString(hex,out expected);
            Assert.Less(Vector4.Distance(expected,material.GetColor("_BaseColor")),.005f);
            Assert.AreEqual(0,material.GetFloat("_Metallic"));Assert.That(material.GetFloat("_Smoothness"),Is.EqualTo(smoothness).Within(.001f));
            Assert.IsFalse(ShaderUtil.ShaderHasError(material.shader));
        }
        [Test] public void SavedCameraRevealsShelfTopsWithoutYawOrRoll()
        {
            var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
            try {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
                var camera=Object.FindObjectOfType<SortGameViewController>().BoardCamera;
                Assert.IsTrue(camera.orthographic);Assert.AreEqual(60,camera.fieldOfView);Assert.AreEqual(.1f,camera.nearClipPlane);Assert.AreEqual(50,camera.farClipPlane);
                Assert.Less(Quaternion.Angle(Quaternion.Euler(8,0,0),camera.transform.rotation),.01f);
                Assert.Greater(Vector3.Dot(-camera.transform.position.normalized,camera.transform.forward),.99999f,"Camera orbits board origin");
            } finally {UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        [Test] public void LockDoorUsesTheProvidedArtworkWithoutChangingThresholds()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Sorting/Connected/LockDoorMaterial.mat");
            Assert.IsNotNull(material);Assert.AreEqual("AllSorted/LockDoor",material.shader.name);
            Assert.AreEqual("Assets/Art/Textures/Sorting/LockDoor.png",AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")));
            Assert.IsFalse(ShaderUtil.ShaderHasError(material.shader));
        }
        [TestCase(1,2232)][TestCase(2,7808)][TestCase(3,8382)][TestCase(4,9488)][TestCase(5,10720)]
        public void CabinetsKeepGeometryAndThreeMaterialRoles(int stage,int triangles)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Shelves/Connected/PF_ConnectedStage"+stage+".prefab");
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);Assert.AreEqual(2,renderers.Length);
            Assert.AreEqual(0,root.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(2,root.transform.Find("StaticBody").GetComponent<MeshFilter>().sharedMesh.subMeshCount);
            Assert.AreEqual(3,renderers.Sum(r=>r.sharedMaterials.Length));
            Assert.AreEqual(triangles,root.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3));
            foreach(var renderer in renderers) {
                Assert.AreEqual(ShadowCastingMode.Off,renderer.shadowCastingMode);
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                Assert.AreEqual(mesh.vertexCount,mesh.colors.Length,"Offline vertex AO must survive FBX import");
                StringAssert.EndsWith(".fbx",AssetDatabase.GetAssetPath(mesh));
                foreach(var material in renderer.sharedMaterials)Assert.AreEqual("AllSorted/CabinetLit",material.shader.name);
            }
        }
    }
}
