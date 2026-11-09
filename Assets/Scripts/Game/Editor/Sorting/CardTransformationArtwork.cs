using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AllSorted
{
    public static class CardTransformationArtwork
    {
        private const string Folder = "Assets/Resources/Prefabs/Merchandise/CardTransformation";

        [MenuItem("All Sorted/Artwork/Build Card Transformation")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var face = Shader.Find("AllSorted/CardFace");
            var light = Shader.Find("AllSorted/CardLight");
            if (!face || !light) throw new System.InvalidOperationException("Card shaders failed to import.");

            var edge = Material("CardEdgeMat", face, null);
            edge.color = new Color(.85f, .51f, .14f);
            edge.SetFloat("_Face", 0);
            var front = Material("CardFrontMat", face, Texture("Assets/Art/Textures/CardItem/Front.png", 512));
            front.mainTextureScale = new Vector2(.779f, .898f);
            front.mainTextureOffset = new Vector2(.111f, .053f);
            var back = Material("CardBackMat", face, Texture("Assets/Art/Textures/CardItem/Back.png", 512));
            back.mainTextureScale = new Vector2(.843f, .902f);
            back.mainTextureOffset = new Vector2(.078f, .043f);
            back.SetFloat("_Radius", .065f);
            string[] effects = {"Beam","Flash","Glow","Sparkle"};
            for (int i = 0; i < effects.Length; i++)
            {
                var material = Material(effects[i], light, Texture($"Assets/Art/Vfx/CardTransformation/{effects[i]}.png", 512));
                material.SetFloat("_Shape", i);
            }

            var root = new GameObject("CardItem", typeof(BoxCollider));
            try
            {
                var size = new Vector3(.35f, .50f, .03f);
                root.GetComponent<BoxCollider>().size = size;
                var visual = new GameObject("Visual", typeof(MeshFilter), typeof(MeshRenderer));
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = size;
                visual.GetComponent<MeshFilter>().sharedMesh = CreateCardMesh();
                var renderer = visual.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { edge, edge, edge, edge, front, back };
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/CardItem.prefab");
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Texture2D Texture(string path, int size)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize = size;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Material(string name, Shader shader, Texture2D texture)
        {
            string path = $"{Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh CreateCardMesh()
        {
            string path = Folder + "/CardCube.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh) return mesh;
            mesh = new Mesh {name = "CardCube"};
            Vector3[] normals = {Vector3.left,Vector3.right,Vector3.up,Vector3.down,Vector3.back,Vector3.forward};
            Vector3[] horizontal = {Vector3.back,Vector3.forward,Vector3.right,Vector3.right,Vector3.right,Vector3.left};
            var vertices = new Vector3[24];
            var uv = new Vector2[24];
            for (int face = 0; face < 6; face++)
            {
                Vector3 right = horizontal[face], up = Vector3.Cross(right, normals[face]);
                for (int corner = 0; corner < 4; corner++)
                {
                    int i = face*4+corner;
                    float x = corner%2, y = corner/2;
                    uv[i] = new Vector2(x,y);
                    vertices[i] = normals[face]*.5f + right*(x-.5f) + up*(y-.5f);
                }
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.subMeshCount = 6;
            for (int face = 0; face < 6; face++)
            {
                int i=face*4;
                mesh.SetTriangles(new[] {i,i+2,i+1,i+1,i+2,i+3},face);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
    }
}
