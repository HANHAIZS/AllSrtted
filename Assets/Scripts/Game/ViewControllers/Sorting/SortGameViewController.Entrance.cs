using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.Rendering;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        private bool entranceActive;
        private SortLevelTransitionView entranceBridge;
        private bool sceneEntrance;
        private float localEntryStarted;
        private readonly Dictionary<Transform,Vector3> entryPieceScales=new Dictionary<Transform,Vector3>();
        private readonly List<Renderer> entryRenderers=new List<Renderer>();
        private readonly List<Material> entryOriginals=new List<Material>();
        private readonly Dictionary<Material,Material> entryFades=new Dictionary<Material,Material>();
        private readonly Dictionary<Material,Color> entryColors=new Dictionary<Material,Color>();
        private float entryPagesAlpha,entryPanelsAlpha;
        public bool IsEntering {get{return entranceActive;}}
        private void BeginSceneEntrance(SortLevelTransitionView bridge)
        {
            if(!bridge || entryStartFailed){if(bridge)bridge.Finish();return;}
            BeginMerchandiseEntrance(bridge);
        }
        private void BeginRestartEntrance(float closeSeconds){BeginMerchandiseEntrance(null,closeSeconds);}
        private void BeginMerchandiseEntrance(SortLevelTransitionView bridge,float delay=0)
        {
            entranceBridge=bridge;sceneEntrance=bridge!=null;localEntryStarted=Time.realtimeSinceStartup+delay;
            entranceActive=true;this.SendCommand(new SetSortEntryPresentationCommand(true));Layers.SetModal(true);
            foreach(var piece in pieces.Values)if(piece.Transform)entryPieceScales.Add(piece.Transform,piece.Scale);
            CacheEntryRenderers(PieceRoot);
            entryPagesAlpha=Layers.Hosts[0].alpha;entryPanelsAlpha=Layers.Hosts[1].alpha;
            displayedSeconds=model.Seconds.Value;TimerLabel.text=string.Format("{0:00}:{1:00}",displayedSeconds/60,displayedSeconds%60);
            if(bridge)bridge.MarkSceneReady();ApplyEntrance(0,0);
        }
        private void CacheEntryRenderers(Transform root)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
                var original=renderer.sharedMaterial;if(!original)continue;Material fade;
                if(!entryFades.TryGetValue(original,out fade)){
                    fade=new Material(original);fade.name=original.name+" EntryFade";
                    if(fade.HasProperty("_Surface"))fade.SetFloat("_Surface",1);
                    if(fade.HasProperty("_Blend"))fade.SetFloat("_Blend",0);
                    if(fade.HasProperty("_SrcBlend"))fade.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
                    if(fade.HasProperty("_DstBlend"))fade.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                    if(fade.HasProperty("_SrcBlendAlpha"))fade.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
                    if(fade.HasProperty("_DstBlendAlpha"))fade.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
                    if(fade.HasProperty("_ZWrite"))fade.SetFloat("_ZWrite",0);
                    fade.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");fade.DisableKeyword("_ALPHAPREMULTIPLY_ON");fade.DisableKeyword("_ALPHAMODULATE_ON");fade.SetOverrideTag("RenderType","Transparent");fade.renderQueue=3000;fade.SetShaderPassEnabled("ShadowCaster",false);
                    entryFades.Add(original,fade);var endpoint=original.HasProperty("_BaseColor")?original.GetColor("_BaseColor"):original.color;
                    if(!original.HasProperty("_Surface") || original.GetFloat("_Surface")<.5f)endpoint.a=1;
                    entryColors.Add(fade,endpoint);
                }
                entryRenderers.Add(renderer);entryOriginals.Add(original);renderer.sharedMaterial=fade;
            }
        }
        private void AdvanceSceneEntrance()
        {
            if((sceneEntrance && !entranceBridge) || !model.IsEntering || !object.ReferenceEquals(renderedBoard,model.Board)){FinishSceneEntrance();return;}
            float elapsed=sceneEntrance?entranceBridge.Elapsed:SortLevelTransitionTiming.HomeFadeSeconds+Time.realtimeSinceStartup-localEntryStarted;
            float readyAt=sceneEntrance?entranceBridge.ReadyAt:SortLevelTransitionTiming.HomeFadeSeconds;
            ApplyEntrance(SortLevelTransitionTiming.ArrivalProgress(elapsed,readyAt),SortLevelTransitionTiming.MerchandiseScale(elapsed,readyAt));
            if(SortLevelTransitionTiming.ArrivalFraction(elapsed,readyAt)>=1)FinishSceneEntrance();
        }
        private void ApplyEntrance(float progress,float scale)
        {
            SetContactEntrance(progress);
            foreach(var piece in entryPieceScales)if(piece.Key)piece.Key.localScale=piece.Value*scale;
            foreach(var pair in entryColors){var color=pair.Value;color.a*=progress;if(pair.Key.HasProperty("_BaseColor"))pair.Key.SetColor("_BaseColor",color);else pair.Key.color=color;}
            Layers.Hosts[0].alpha=entryPagesAlpha*progress;Layers.Hosts[1].alpha=entryPanelsAlpha*progress;
        }
        private void FinishSceneEntrance()
        {
            if(!entranceActive)return;entranceActive=false;SetContactEntrance(1);
            foreach(var piece in entryPieceScales)if(piece.Key)piece.Key.localScale=piece.Value;entryPieceScales.Clear();
            for(int i=0;i<entryRenderers.Count;i++)if(entryRenderers[i])entryRenderers[i].sharedMaterial=entryOriginals[i];
            foreach(var material in entryFades.Values)if(material)Destroy(material);
            entryRenderers.Clear();entryOriginals.Clear();entryFades.Clear();entryColors.Clear();
            if(Layers){Layers.Hosts[0].alpha=entryPagesAlpha;Layers.Hosts[1].alpha=entryPanelsAlpha;Layers.SetModal(false);}
            this.SendCommand(new SetSortEntryPresentationCommand(false));if(entranceBridge)entranceBridge.Finish();entranceBridge=null;
        }
        private void OnDisable(){FinishSceneEntrance();if(cardBoard!=null)EndCardEffect(false);}
    }
}
