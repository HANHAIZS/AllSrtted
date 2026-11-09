using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted
{
    /// <summary>Soft purple glyph halo; cached vertex buffers rebuild only when the text mesh is dirty.</summary>
    public sealed class SortHudTextGlow : BaseMeshEffect
    {
        private readonly List<UIVertex> source=new List<UIVertex>(128);
        private readonly List<UIVertex> result=new List<UIVertex>(3200);
        private static readonly float[] Radii={4,2.5f,1.25f},Alphas={.055f,.10f,.20f};
        public override void ModifyMesh(VertexHelper mesh)
        {
            if(!IsActive() || mesh.currentVertCount==0)return;source.Clear();result.Clear();mesh.GetUIVertexStream(source);
            for(int ring=0;ring<3;ring++)for(int direction=0;direction<8;direction++){
                float angle=direction*Mathf.PI*.25f;var offset=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*Radii[ring];
                for(int i=0;i<source.Count;i++){var vertex=source[i];vertex.position+=offset;vertex.color=new Color(.48f,.18f,.94f,Alphas[ring]*vertex.color.a/255f);result.Add(vertex);}
            }
            result.AddRange(source);mesh.Clear();mesh.AddUIVertexTriangleStream(result);
        }
    }
}
