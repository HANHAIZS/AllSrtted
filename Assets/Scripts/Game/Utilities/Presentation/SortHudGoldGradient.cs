using UnityEngine;
using UnityEngine.UI;
namespace AllSorted
{
    /// <summary>Stateless vertical color on live uGUI glyphs; no bitmap numbers or runtime allocations.</summary>
    public sealed class SortHudGoldGradient : BaseMeshEffect
    {
        public Color Top=new Color(1,.94f,.70f,1),Bottom=new Color(.96f,.69f,.23f,1);
        public override void ModifyMesh(VertexHelper vertices)
        {
            if(!IsActive() || vertices.currentVertCount==0)return;
            var vertex=new UIVertex();float min=float.MaxValue,max=float.MinValue;
            for(int i=0;i<vertices.currentVertCount;i++){vertices.PopulateUIVertex(ref vertex,i);min=Mathf.Min(min,vertex.position.y);max=Mathf.Max(max,vertex.position.y);}
            float height=Mathf.Max(.001f,max-min);
            for(int i=0;i<vertices.currentVertCount;i++){
                vertices.PopulateUIVertex(ref vertex,i);var color=Color.Lerp(Bottom,Top,(vertex.position.y-min)/height);color.a*=vertex.color.a/255f;vertex.color=color;vertices.SetUIVertex(vertex,i);
            }
        }
    }
}
