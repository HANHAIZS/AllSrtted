using System;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AllSorted
{
    /// <summary>Temporary scene-boundary blocker, owns only capture/loading presentation and load lifetime.</summary>
    public sealed class SortLevelTransitionView : AllSortedController
    {
        public RawImage HomeFrame;
        public Image BackgroundPreview,LoadingPage;
        private RenderTexture capture;
        private AsyncOperation operation;
        private float started,readyAt=-1,loadFinishedAt=-1;
        private bool running;
        public static SortLevelTransitionView Active {get;private set;}
        public float Elapsed {get{return Mathf.Max(0,Time.realtimeSinceStartup-started);}}
        public bool SceneReady {get{return readyAt>=0;}}
        public float ReadyAt {get{return readyAt;}}
        public bool IsLoadingPageVisible {get{return LoadingPage && LoadingPage.gameObject.activeSelf;}}
        public static SortLevelTransitionView Prepare(string key,Camera camera)
        {
            if(Active || !camera)return null;
            var prefab=string.IsNullOrEmpty(key)?null:Resources.Load<GameObject>(key);
            if(!prefab){Debug.LogError("Level transition resource missing: "+key);return null;}
            var go=Instantiate(prefab);go.SetActive(false);var view=go.GetComponent<SortLevelTransitionView>();
            if(!view || !view.HomeFrame || !view.BackgroundPreview || !view.BackgroundPreview.sprite || !view.LoadingPage || !view.LoadingPage.sprite){Destroy(go);Debug.LogError("Invalid level transition resource: "+key);return null;}
            try{view.Capture(camera);}catch(Exception error){Destroy(go);Debug.LogError("Unable to capture Home: "+error.Message);return null;}
            Active=view;DontDestroyOnLoad(go);return view;
        }
        private void Capture(Camera camera)
        {
            int width=Mathf.Max(1,Screen.width),height=Mathf.Max(1,Screen.height);float scale=Mathf.Min(1,1280f/Mathf.Max(width,height));
            capture=new RenderTexture(Mathf.Max(1,Mathf.RoundToInt(width*scale)),Mathf.Max(1,Mathf.RoundToInt(height*scale)),24);capture.name="HomeEntryCapture";
            var old=camera.targetTexture;
            try{camera.targetTexture=capture;camera.Render();HomeFrame.texture=capture;}finally{camera.targetTexture=old;}
        }
        public bool Begin(string scene)
        {
            started=Time.realtimeSinceStartup;running=true;LoadingPage.gameObject.SetActive(false);HomeFrame.color=Color.white;gameObject.SetActive(true);
            try{operation=SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);if(operation==null)throw new InvalidOperationException("Scene load returned no operation.");return true;}
            catch(Exception error){Debug.LogError("Unable to open game scene: "+error.Message);Finish();return false;}
        }
        public void MarkSceneReady(){if(readyAt<0)readyAt=Elapsed;RefreshPresentation();}
        private void Update()
        {
            if(!running)return;RefreshPresentation();
            if(operation!=null && operation.isDone && !SceneReady){if(loadFinishedAt<0)loadFinishedAt=Elapsed;else if(Elapsed-loadFinishedAt>1){
                this.SendCommand(new ClearSortHomeEntryCommand());this.SendCommand(new ReportSortEntryErrorCommand("Level scene could not initialize. Please retry."));
                Finish();if(Application.CanStreamedLevelBeLoaded("HomeScene"))SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);
            }}
        }
        private void RefreshPresentation()
        {
            float elapsed=Elapsed;var color=HomeFrame.color;color.a=SortLevelTransitionTiming.HomeAlpha(elapsed);HomeFrame.color=color;
            BackgroundPreview.gameObject.SetActive(!SceneReady || elapsed<SortLevelTransitionTiming.HomeFadeSeconds);
            bool loading=SortLevelTransitionTiming.ShowLoading(elapsed,SceneReady);if(LoadingPage.gameObject.activeSelf!=loading)LoadingPage.gameObject.SetActive(loading);
        }
        public void Finish(){if(Active==this)Active=null;running=false;gameObject.SetActive(false);Destroy(gameObject);}
        private void OnDestroy(){if(Active==this)Active=null;if(capture){HomeFrame.texture=null;capture.Release();Destroy(capture);capture=null;}}
    }
}
