using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using QFramework;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        private bool dragging;
        private int draggedId,sourceShelf,sourceSlot,pointerId;
        private readonly List<RaycastResult> uiHits=new List<RaycastResult>(12);
        private PointerEventData pointerData;
        private bool lifecycleAppPaused,lifecycleFocusLost;
        private readonly Plane dragPlane=new Plane(Vector3.back,new Vector3(0,0,-.5f));

        /// <summary>Routes desktop or single-owner mobile pointer input through the same drag path.</summary>
        private void HandleInput()
        {
            if(Application.isMobilePlatform) {
                if(!dragging) {
                    for(int i=0;i<Input.touchCount;i++) {
                        var began=Input.GetTouch(i);
                        if(began.phase==TouchPhase.Began && TryBeginDrag(began.position,began.fingerId)) break;
                    }
                    return;
                }
                bool found=false;
                for(int i=0;i<Input.touchCount;i++) {
                    var touch=Input.GetTouch(i);
                    if(touch.fingerId!=pointerId) continue;
                    found=true;
                    if(touch.phase==TouchPhase.Canceled) CancelDrag();
                    else if(touch.phase==TouchPhase.Ended) ReleaseDrag(touch.position);
                    else MoveDrag(touch.position);
                    break;
                }
                if(dragging && !found) CancelDrag();
            } else {
                if(Input.GetMouseButtonDown(0)) TryBeginDrag(Input.mousePosition,-1);
                if(dragging && Input.GetMouseButton(0)) MoveDrag(Input.mousePosition);
                if(dragging && Input.GetMouseButtonUp(0)) ReleaseDrag(Input.mousePosition);
            }
        }

        /// <summary>Checks actual uGUI raycasts before selecting only a currently visible front piece.</summary>
        public bool TryBeginDrag(Vector2 screen,int id=-1)
        {
            if(!ready || dragging || Layers.GameplayBlocked || model.Board.Phase!=SortPhase.Playing) return false;
            if(EventSystem.current) {
                if(pointerData==null) pointerData=new PointerEventData(EventSystem.current);
                pointerData.position=screen;uiHits.Clear();EventSystem.current.RaycastAll(pointerData,uiHits);
                if(uiHits.Count>0) return false;
            }
            Vector3 point=PointerWorld(screen);float best=.55f*.55f;int foundShelf=-1,foundSlot=-1,foundId=0;
            for(int s=0;s<model.Board.ShelfCount;s++) for(int c=0;c<3;c++) {
                var p=model.Board.GetFront(s,c);PieceVisual v;
                if(model.Board.IsShelfLocked(s) || p.IsEmpty || !pieces.TryGetValue(p.InstanceId,out v) || v.Dying) continue;
                Vector3 delta=v.Transform.position-point;delta.z=0;float distance=delta.sqrMagnitude;
                if(distance<best) {best=distance;foundShelf=s;foundSlot=c;foundId=p.InstanceId;}
            }
            if(foundShelf<0) return false;
            dragging=true;sourceShelf=foundShelf;sourceSlot=foundSlot;draggedId=foundId;pointerId=id;
            MoveDrag(screen);return true;
        }

        /// <summary>Moves only the presentation; the model source stack remains unchanged until release.</summary>
        private void MoveDrag(Vector2 screen)
        {
            PieceVisual v;if(!dragging || !pieces.TryGetValue(draggedId,out v)) return;
            v.Transform.position=PointerWorld(screen)+BoardCamera.transform.up*.12f-BoardCamera.transform.forward*1.4f;v.Transform.localScale=v.Scale*1.12f;
        }

        /// <summary>Submits a command only for a nearby slot, then snaps accepted or cancelled visuals.</summary>
        public bool ReleaseDrag(Vector2 screen)
        {
            if(!dragging) return false;
            Vector3 point=PointerWorld(screen);float best=.58f*.58f;int shelf=-1,slot=-1;
            for(int s=0;s<model.Board.ShelfCount;s++) for(int c=0;c<3;c++) {
                if(model.Board.IsShelfLocked(s))continue;
                Vector3 d=StageSlotWorld(s,c)-point;d.z=0;
                if(d.sqrMagnitude<best) {best=d.sqrMagnitude;shelf=s;slot=c;}
            }
            bool accepted=false;
            if(shelf>=0 && !Layers.GameplayBlocked && model.Board.Phase==SortPhase.Playing) {
                var command=new MoveSortPieceCommand(sourceShelf,sourceSlot,shelf,slot);this.SendCommand(command);accepted=command.Accepted;
            }
            CancelDrag();if(accepted) ReconcilePieces();return accepted;
        }

        /// <summary>Cancels an uncommitted drag without touching model slots or clock.</summary>
        public void CancelDrag() { dragging=false;draggedId=0; }

        /// <summary>Projects pointer positions into the shared front-item plane.</summary>
        private Vector3 PointerWorld(Vector2 screen)
        {
            var ray=BoardCamera.ScreenPointToRay(screen);float distance;
            return dragPlane.Raycast(ray,out distance)?ray.GetPoint(distance):Vector3.zero;
        }

        /// <summary>Freezes device gameplay and cancels uncommitted input on a real app suspension.</summary>
        private void OnApplicationPause(bool paused)
        {
            if(!ready)return;
            lifecycleAppPaused=paused;
            if(paused){SuspendLevel();this.SendCommand(flushResources);}
            else ResumeSuspendedLevel();
        }

        /// <summary>Applies desktop focus policy; headless test processes have no meaningful window focus.</summary>
        private void OnApplicationFocus(bool focused)
        {
            if(!ready)return;
            if(!focused && !Application.isBatchMode){lifecycleFocusLost=true;SuspendLevel();this.SendCommand(flushResources);}
            else if(focused){lifecycleFocusLost=false;ResumeSuspendedLevel();}
        }
    }
}
