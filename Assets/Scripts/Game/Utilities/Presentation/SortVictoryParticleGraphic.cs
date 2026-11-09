using UnityEngine;
using UnityEngine.UI;
namespace AllSorted
{
    /// <summary>Draws a cached native particle preset inside the modal Canvas, including its fade and input boundaries.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SortVictoryParticleGraphic : MaskableGraphic
    {
        public ParticleSystem Source;
        public Texture ParticleTexture;
        public float PixelsPerUnit=120;
        private ParticleSystem.Particle[] particles;
        private bool hadParticles;
        public override Texture mainTexture {get{return ParticleTexture?ParticleTexture:Texture2D.whiteTexture;}}
        protected override void Awake(){base.Awake();raycastTarget=false;particles=new ParticleSystem.Particle[256];}
        protected override void OnEnable()
        {
            base.OnEnable();raycastTarget=false;if(particles==null)particles=new ParticleSystem.Particle[256];
            if(Source){Source.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);Source.Simulate(0,false,true,true);Source.Play(false);}
        }
        protected override void OnDisable()
        {
            if(Source)Source.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            hadParticles=false;base.OnDisable();
        }
        private void LateUpdate()
        {
            if(!Source)return;bool alive=Source.IsAlive(false);
            if(alive || hadParticles)SetVerticesDirty();hadParticles=alive;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!Source || particles==null)return;
            int count=Source.GetParticles(particles);var rect=rectTransform.rect;
            for(int i=0;i<count;i++){
                var p=particles[i];Vector3 center=p.position*PixelsPerUnit;
                if(Source.main.simulationSpace==ParticleSystemSimulationSpace.World)center=Source.transform.InverseTransformPoint(p.position)*PixelsPerUnit;
                if(!rect.Contains(center))continue;
                float size=p.GetCurrentSize(Source)*PixelsPerUnit,half=size*.5f,angle=p.rotation*Mathf.Deg2Rad;
                Vector2 right=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*half,up=new Vector2(-Mathf.Sin(angle),Mathf.Cos(angle))*half;
                var sheet=Source.textureSheetAnimation;int tilesX=sheet.enabled?Mathf.Max(1,sheet.numTilesX):1,tilesY=sheet.enabled?Mathf.Max(1,sheet.numTilesY):1;
                int frame=(int)(p.randomSeed%(uint)(tilesX*tilesY));float u=(frame%tilesX)/(float)tilesX,v=(frame/tilesX)/(float)tilesY,du=1f/tilesX,dv=1f/tilesY;
                Color tint=p.GetCurrentColor(Source);tint*=color;int start=vh.currentVertCount;
                vh.AddVert(center-(Vector3)right-(Vector3)up,tint,new Vector2(u,v));vh.AddVert(center-(Vector3)right+(Vector3)up,tint,new Vector2(u,v+dv));
                vh.AddVert(center+(Vector3)right+(Vector3)up,tint,new Vector2(u+du,v+dv));vh.AddVert(center+(Vector3)right-(Vector3)up,tint,new Vector2(u+du,v));
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start+2,start+3,start);
            }
        }
    }
}
