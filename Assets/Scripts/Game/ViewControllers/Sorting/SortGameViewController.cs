using System.Collections.Generic;
using GameFoundation.UI;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted
{
    public sealed partial class SortGameViewController : AllSortedController
    {
        public Camera BoardCamera;
        public SpriteRenderer Background;
        public Transform PieceRoot;
        public Material[] Palette,BackPalette;
        public SortUiLayers Layers;
        public Text LevelLabel,TimerLabel,CountLabel,StatusLabel;
        public Image ProgressFill;
        public Button PauseButton;
        public string PausePopupKey="Prefabs/UI/Popups/PausePopup";
        public string ResultPopupKey="Prefabs/UI/Popups/ResultPopup";
        public string VictoryPopupKey="Prefabs/UI/Popups/VictoryPopup";
        private SortBoardModel model;
        private UiPopupRouter router;
        private SortWalletModel wallet;
        private readonly RefreshSortWalletCommand flushResources=new RefreshSortWalletCommand{Flush=true};
        private readonly TickSortCommand tick=new TickSortCommand();
        private readonly Dictionary<int,PieceVisual> pieces=new Dictionary<int,PieceVisual>(32);
        private readonly List<int> removeIds=new List<int>(24);
        private readonly Dictionary<string,SortPopupView> popups=new Dictionary<string,SortPopupView>(2);
        private int seenRevision=-1,seenMatches,lastWidth,lastHeight,displayedSeconds=-1;
        private SortPhase seenPhase=(SortPhase)(-1);
        private bool ready;
        private SortBoardState renderedBoard;
        private Text pauseCaption;
        private string defaultPauseCaption;
        public int RenderedPieceCount { get { return pieces.Count; } }
        public int CachedPopupCount { get { return popups.Count; } }
        public SortPopupView ActivePopup { get { return router==null?null:router.ActiveView as SortPopupView; } }

        private sealed class PieceVisual
        {
            public Transform Transform;public Renderer Renderer;public TextMesh MatchId;public Vector3 Target,Scale;
            public Transform Contact;public MeshRenderer ContactRenderer;public MaterialPropertyBlock ContactBlock;public Vector3 ContactSize;
            public bool Seen,Dying,Vacuum;public float Life,FrontSupport,RearSupport;public int Kind;
        }

        /// <summary>Caches project dependencies and validates explicitly authored scene bindings.</summary>
        private void Awake()
        {
            wallet=this.GetModel<SortWalletModel>();model=this.GetModel<SortBoardModel>();boosterStock=this.GetModel<SortBoosterModel>();router=this.GetUtility<UiPopupRouter>();
            if(!BoardCamera || !PieceRoot || !Layers || !LevelLabel || Palette.Length!=4 || BackPalette.Length!=4 || !ValidateBoosterBindings()) {
                Debug.LogError("Sorting scene bindings are incomplete.",this);enabled=false;return;
            }
            Layers.Initialize();PauseButton.onClick.AddListener(RequestPause);
            pauseCaption=PauseButton.GetComponentInChildren<Text>();
            defaultPauseCaption=pauseCaption?pauseCaption.text:string.Empty;ready=true;BindBoosters();
        }

        /// <summary>Starts through a Command after scene and EventSystem initialization.</summary>
        private void Start()
        {
            if(!ready) return;
            ReloadLevel(false,ConsumeStartLevel());
            if(SortLevelTransitionView.Active)BeginSceneEntrance(SortLevelTransitionView.Active);
        }

        /// <summary>Ticks before accepting input, then updates only changed board/UI state.</summary>
        private void Update()
        {
            if(!ready || !Layers || entryStartFailed) {CancelDrag();return;}
            if(entranceActive){AdvanceSceneEntrance();return;}
            if(cardBoard!=null){AdvanceCardEffect();return;}
            tick.DeltaTime=Time.unscaledDeltaTime;this.SendCommand(tick);
            if(Screen.width!=lastWidth || Screen.height!=lastHeight) FitCamera();
            if(model.BoardRevision.Value!=seenRevision) ReconcilePieces();
            if(model.Phase.Value!=seenPhase) RefreshPhase();
            if(ActivePopup && model.Board.Phase==SortPhase.Paused && ActivePopup.GetComponent<SortPauseSettingsView>())ActivePopup.Secondary.interactable=wallet.CanEnter;
            if(displayedSeconds!=model.Seconds.Value) {
                displayedSeconds=model.Seconds.Value;
                TimerLabel.text=string.Format("{0:00}:{1:00}",displayedSeconds/60,displayedSeconds%60);
            }
            RefreshPurchaseOffer();RefreshBoosterUi();HandleInput();AnimatePieces();
        }

        /// <summary>Places the authored two-column shelf grid in a single camera domain.</summary>
        public static Vector3 ShelfWorld(int shelf) { return new Vector3(shelf%2==0?-1.96f:1.96f,2.4f-(shelf/2)*2.4f,0); }

        /// <summary>Shares exact slot geometry between input, rendering and the Editor builder.</summary>
        public static Vector3 SlotWorld(int shelf,int slot) { return ShelfWorld(shelf)+new Vector3((slot-1)*.94f,0,-.5f); }

        /// <summary>Fits board width while reserving vertical room for the portrait phone and iPad HUD.</summary>
        private void FitCamera()
        {
            lastWidth=Screen.width;lastHeight=Screen.height;
            float aspect=lastHeight>0?(float)lastWidth/lastHeight:9f/16f;
            BoardCamera.orthographicSize=SourceCameraSize(aspect);
            if(!BoardCamera.orthographic)BoardCamera.transform.position=-BoardCamera.transform.forward*(BoardCamera.orthographicSize/Mathf.Tan(BoardCamera.fieldOfView*.5f*Mathf.Deg2Rad));
            FitBackground(aspect);
        }

        /// <summary>Covers the viewport with the selected background while preserving its aspect ratio.</summary>
        private void FitBackground(float aspect)
        {
            if(!Background || !Background.sprite) return;
            var size=Background.sprite.bounds.size;float height=BoardCamera.orthographic?BoardCamera.orthographicSize*2:48*Mathf.Tan(BoardCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            if(!BoardCamera.orthographic)Background.transform.SetPositionAndRotation(BoardCamera.transform.position+BoardCamera.transform.forward*24,BoardCamera.transform.rotation);
            float scale=Mathf.Max(height/size.y,height*aspect/size.x);
            Background.transform.localScale=new Vector3(scale,scale,1);
        }

        /// <summary>Reuses unique piece instances; hidden previews never own gameplay input.</summary>
        private void ReconcilePieces()
        {
            if(!object.ReferenceEquals(renderedBoard,model.Board)) {
                foreach(var pair in pieces) DestroyPieceVisual(pair.Value);
                pieces.Clear();renderedBoard=model.Board;ConfigureSourceStage();FitCamera();
            }
            seenRevision=model.BoardRevision.Value;
            foreach(var pair in pieces) pair.Value.Seen=false;
            for(int s=0;s<model.Board.ShelfCount;s++) for(int c=0;c<3;c++) {
                if(model.Board.IsShelfLocked(s))continue;
                ShowPiece(model.Board.GetBehind(s,c),s,c,true);
                ShowPiece(model.Board.GetFront(s,c),s,c,false);
            }
            foreach(var pair in pieces) if(!pair.Value.Seen && !pair.Value.Dying) {pair.Value.Dying=true;pair.Value.Life=.22f;}
            RefreshSourceLocks();
            if(model.MatchCount.Value>seenMatches) StatusLabel.text="NICE MATCH!";
            seenMatches=model.MatchCount.Value;
            LevelLabel.text="LEVEL "+model.LevelNumber.Value.ToString("00");
            CountLabel.text=seenMatches+" / "+model.TotalMatches+" SETS";ProgressFill.fillAmount=(float)seenMatches/model.TotalMatches;
        }

        /// <summary>Creates only native primitives with shared project-authored palette materials.</summary>
        private void ShowPiece(SortPiece piece,int shelf,int slot,bool behind)
        {
            if(piece.IsEmpty) return;
            PieceVisual view;
            Vector3 target=StageSlotWorld(shelf,slot)+(behind?new Vector3(0,.34f,.85f):Vector3.zero);
            Vector3 scale=StagePieceScale(piece.Kind)*(behind?.72f:1f);
            if(pieces.TryGetValue(piece.InstanceId,out view) && view.Kind!=piece.Kind) {
                var oldPosition=view.Transform.position;DestroyPieceVisual(view);pieces.Remove(piece.InstanceId);
                target=oldPosition;
            }
            if(!pieces.TryGetValue(piece.InstanceId,out view)) {
                if(piece.Kind==SortBoardState.CardKind)
                {
                    var card=Instantiate(CardPrefab,PieceRoot);
                    card.name="Piece_"+piece.InstanceId+"_Kind"+piece.Kind;
                    card.transform.SetPositionAndRotation(target,BoardCamera.transform.rotation);
                    card.transform.localScale=scale*.8f;
                    view=new PieceVisual{Transform=card.transform,Renderer=card.GetComponentInChildren<Renderer>(),Kind=piece.Kind};
                    view.FrontSupport=-.14f;view.RearSupport=-.10f;
                    pieces.Add(piece.InstanceId,view);
                }
                else {
                var kind=piece.Kind==0?PrimitiveType.Sphere:piece.Kind==1?PrimitiveType.Cube:piece.Kind==2?PrimitiveType.Capsule:PrimitiveType.Cylinder;
                if(IsSourceStage)kind=SourcePrimitive(piece.Kind);
                var go=GameObject.CreatePrimitive(kind);go.name="Piece_"+piece.InstanceId+"_Kind"+piece.Kind;
                go.transform.SetParent(PieceRoot,false);go.transform.position=target;go.transform.localScale=scale*.8f;
                go.transform.rotation=Quaternion.Euler(piece.Kind==1?-12:-10,piece.Kind==1?22:0,piece.Kind==2?-12:0);
                Destroy(go.GetComponent<Collider>());if(IsSourceStage && !CanUseSourceArtwork(piece.Kind))AddSourceIdentifier(go,piece.Kind);
                view=new PieceVisual{Transform=go.transform,Renderer=DecorateMerchandise(go,piece.Kind),Kind=piece.Kind,MatchId=go.GetComponentInChildren<TextMesh>()};
                ConfigureSourceSupport(view);go.transform.position+=Vector3.up*(behind?view.RearSupport:view.FrontSupport);pieces.Add(piece.InstanceId,view);
                }
            }
            if(view.MatchId)view.MatchId.color=behind?new Color(.7f,.7f,.7f,1):Color.white;
            view.Seen=true;view.Dying=false;view.Vacuum=false;view.Life=0;
            view.Target=StageSlotWorld(shelf,slot)+(behind?SortShelfGeometry.RearOffset:Vector3.zero)+Vector3.up*(behind?view.RearSupport:view.FrontSupport);view.Scale=scale;
            ConfigureContact(view,piece.InstanceId,shelf,slot,behind);
            if(piece.Kind==SortBoardState.CardKind) return;
            var originalMaterial=IsSourceStage?SourceMaterial(piece.Kind,behind):OriginalMerchandiseMaterial(piece.Kind,behind);view.Renderer.sharedMaterial=originalMaterial?originalMaterial:(behind?BackPalette[piece.Kind]:Palette[piece.Kind]);
        }

        /// <summary>Defines primitive silhouette sizes without reading any original asset metadata.</summary>
        private static Vector3 PieceScale(int kind)
        {
            if(kind==SortBoardState.CardKind) return Vector3.one;
            if(kind==0) return Vector3.one*.70f;
            if(kind==1) return Vector3.one*.64f;
            if(kind==2) return new Vector3(.53f,.43f,.53f);
            return new Vector3(.65f,.36f,.65f);
        }

        /// <summary>Animates cached transforms without lookups, LINQ or per-frame collections.</summary>
        private void AnimatePieces()
        {
            removeIds.Clear();float blend=1-Mathf.Exp(-20*Time.unscaledDeltaTime);
            foreach(var pair in pieces) {
                var v=pair.Value;
                if(v.Dying) {
                    v.Life-=Time.unscaledDeltaTime;UpdateContact(v,false);
                    if(v.Vacuum) v.Transform.position=Vector3.Lerp(v.Transform.position,v.Target,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
                    v.Transform.localScale=v.Scale*Mathf.Clamp01(v.Life/(v.Vacuum?.28f:.22f));
                    if(v.Life<=0) {DestroyPieceVisual(v);removeIds.Add(pair.Key);}continue;
                }
                UpdateContact(v,dragging && pair.Key==draggedId);
                if(dragging && pair.Key==draggedId) continue;
                v.Transform.position=Vector3.Lerp(v.Transform.position,v.Target,blend);
                v.Transform.localScale=Vector3.Lerp(v.Transform.localScale,v.Scale,blend);
            }
            for(int i=0;i<removeIds.Count;i++) pieces.Remove(removeIds[i]);
        }

        /// <summary>Closes only this controller's owned views and releases every owned scene instance.</summary>
        private void OnDestroy()
        {
            FinishSceneEntrance();if(!ready) return;CancelDrag();UnbindBoosters();if(PauseButton) PauseButton.onClick.RemoveListener(RequestPause);
            foreach(var p in popups.Values) {
                if(object.ReferenceEquals(p,null)) continue;
                if(p && object.ReferenceEquals(router.ActiveView,p)) router.Clear(true);
                // Foundation stores interface references; destroyed Unity wrappers still need unregistering.
                router.Unregister(p);if(p) Destroy(p.gameObject);
            }
            popups.Clear();if(Layers) Layers.SetModal(false);
            foreach(var p in pieces.Values) DestroyPieceVisual(p);
            pieces.Clear();DisposeContacts();DisposeSourcePresentation();
        }
    }
}
