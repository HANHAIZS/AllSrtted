using QFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        public Button[] BoosterButtons;
        public Button[] BoosterBadges;
        public Text[] BoosterChargeLabels;
        public Sprite AddChargeBadgeSprite,RemainingChargeBadgeSprite;
        public Text FreezeCounter,BoosterFeedback;
        public Image FrozenTimerPanel;
        public SortWatchFreezeView WatchFreezeFx;
        private readonly UnityAction[] boosterActions=new UnityAction[4];
        private readonly Image[] boosterIcons=new Image[4];
        private readonly UnityAction[] badgeActions=new UnityAction[4];
        private readonly Image[] badgeImages=new Image[4];
        private readonly SortButtonPressFeedback[] pressFeedback=new SortButtonPressFeedback[8];
        private SortBoosterModel boosterStock;
        private int stockRevision=-1;
        private Color normalTimerColor;
        private int boosterRevision=-1,freezeDisplay=-1;
        private SortPhase boosterPhase=(SortPhase)(-1);
        private bool boosterGate;
        private float feedbackUntil;

        /// <summary>Requires explicit scene ownership for controls and gold materials.</summary>
        private bool ValidateBoosterBindings()
        {
            if(BoosterButtons==null || BoosterButtons.Length!=4 || BoosterBadges==null || BoosterBadges.Length!=4 || BoosterChargeLabels==null || BoosterChargeLabels.Length!=4 || !AddChargeBadgeSprite || !RemainingChargeBadgeSprite || !CardPrefab || !FreezeCounter || !BoosterFeedback) return false;
            for(int i=0;i<4;i++) {
                if(!BoosterButtons[i] || !BoosterBadges[i] || !BoosterChargeLabels[i] || !BoosterButtons[i].GetComponent<SortButtonPressFeedback>() || !BoosterBadges[i].GetComponent<SortButtonPressFeedback>()) return false;
                var icon=BoosterButtons[i].transform.Find("Icon");if(!icon || !icon.GetComponent<Image>()) return false;
            }
            return true;
        }

        /// <summary>Captures listeners once; runtime callbacks use cached project dependencies.</summary>
        private void BindBoosters()
        {
            normalTimerColor=TimerLabel.color;
            for(int i=0;i<4;i++) {
                int index=i;boosterActions[i]=()=>UseBooster((SortBooster)index);
                boosterIcons[i]=BoosterButtons[i].transform.Find("Icon").GetComponent<Image>();
                BoosterButtons[i].onClick.AddListener(boosterActions[i]);
                badgeImages[i]=BoosterBadges[i].GetComponent<Image>();
                badgeActions[i]=()=>TryOpenBoosterPurchase((SortBooster)index);
                BoosterBadges[i].onClick.AddListener(badgeActions[i]);
                pressFeedback[i]=BoosterButtons[i].GetComponent<SortButtonPressFeedback>();
                pressFeedback[i+4]=BoosterBadges[i].GetComponent<SortButtonPressFeedback>();
            }
        }

        /// <summary>Checks actual input eligibility before sending one synchronous booster command.</summary>
        private void UseBooster(SortBooster booster,bool atImpact=false)
        {
            if(!ready || entryStartFailed || !Layers || Layers.GameplayBlocked || model.Board.Phase!=SortPhase.Playing || boosterStock.GetCharges(booster)<=0 || !model.Board.CanUseBooster(booster)) return;
            if(booster==SortBooster.Freeze && WatchFreezeFx && !atImpact){if(WatchFreezeFx.IsLaunching)return;CancelDrag();WatchFreezeFx.Launch(()=>UseBooster(booster,true));return;}
            CancelDrag();
            bool animateCard=booster==SortBooster.Golden;
            var command=new UseSortBoosterCommand(booster,animateCard);this.SendCommand(command);
            if(!command.Accepted) return;
            if(animateCard){BeginCardEffect();return;}
            if(booster==SortBooster.Freeze && WatchFreezeFx)WatchFreezeFx.HitSkin();
            if(booster==SortBooster.Vacuum) BeginVacuumMerge(command.AffectedPieceIds);
            ReconcilePieces();RefreshBoosterUi(true);
            string text=booster==SortBooster.Vacuum?"VACUUM MATCH!":booster==SortBooster.Shuffle?"SHUFFLED!":booster==SortBooster.Golden?"9 CARDS!":"TIME FROZEN!";
            BoosterFeedback.text=text;BoosterFeedback.gameObject.SetActive(true);feedbackUntil=Time.unscaledTime+1.6f;
        }

        /// <summary>Merges the three selected front views before shrinking them away.</summary>
        private void BeginVacuumMerge(int[] ids)
        {
            Vector3 center=Vector3.zero;int count=0;
            for(int i=0;i<ids.Length;i++) {PieceVisual v;if(pieces.TryGetValue(ids[i],out v)) {center+=v.Transform.position;count++;}}
            if(count==0) return;center/=count;center.z=-2.2f;
            for(int i=0;i<ids.Length;i++) {
                PieceVisual v;if(!pieces.TryGetValue(ids[i],out v)) continue;
                v.Dying=true;v.Vacuum=true;v.Life=.42f;v.Target=center;
            }
        }

        /// <summary>Refreshes availability only when board, phase, gate or displayed freeze seconds change.</summary>
        private void RefreshBoosterUi(bool force=false)
        {
            if(WatchFreezeFx)WatchFreezeFx.Sync(model.Board.FreezeSeconds,model.Board.Phase==SortPhase.Playing,model.Board.Phase==SortPhase.Won || model.Board.Phase==SortPhase.Lost);
            int seconds=Mathf.CeilToInt(model.Board.FreezeSeconds);bool blocked=Layers.GameplayBlocked;
            if(force || stockRevision!=boosterStock.Revision.Value || boosterRevision!=model.BoardRevision.Value || boosterPhase!=model.Board.Phase || boosterGate!=blocked || freezeDisplay!=seconds) {
                boosterRevision=model.BoardRevision.Value;boosterPhase=model.Board.Phase;boosterGate=blocked;freezeDisplay=seconds;
                stockRevision=boosterStock.Revision.Value;
                for(int i=0;i<4;i++) {
                    int count=boosterStock.GetCharges((SortBooster)i);
                    BoosterButtons[i].interactable=!blocked && count>0 && model.Board.CanUseBooster((SortBooster)i);
                    BoosterBadges[i].interactable=!blocked && model.Board.Phase==SortPhase.Playing && model.Board.RemainingSeconds>0;
                    badgeImages[i].sprite=count>0?RemainingChargeBadgeSprite:AddChargeBadgeSprite;
                    BoosterChargeLabels[i].gameObject.SetActive(count>0);BoosterChargeLabels[i].text=count.ToString();
                }
                FreezeCounter.gameObject.SetActive(seconds>0);FreezeCounter.text=seconds>0?seconds+"s":string.Empty;
                if(FrozenTimerPanel && !WatchFreezeFx)FrozenTimerPanel.gameObject.SetActive(seconds>0);
                TimerLabel.color=seconds>0 && !FrozenTimerPanel?new Color(.4f,.92f,1f):normalTimerColor;
                for(int i=0;i<4;i++) {
                    boosterIcons[i].color=BoosterButtons[i].interactable || (i==3 && seconds>0)?Color.white:new Color(.68f,.68f,.68f,.8f);
                }
            }
            if(BoosterFeedback.gameObject.activeSelf && Time.unscaledTime>=feedbackUntil) BoosterFeedback.gameObject.SetActive(false);
        }

        /// <summary>Resets transient feedback and timer tint on restart, next level and Editor selection.</summary>
        private void ClearBoosterFeedback()
        {
            if(WatchFreezeFx)WatchFreezeFx.Clear();
            feedbackUntil=0;BoosterFeedback.gameObject.SetActive(false);FreezeCounter.gameObject.SetActive(false);
            if(FrozenTimerPanel)FrozenTimerPanel.gameObject.SetActive(false);
            TimerLabel.color=normalTimerColor;boosterRevision=-1;freezeDisplay=-1;
            for(int i=0;i<pressFeedback.Length;i++) if(pressFeedback[i]) pressFeedback[i].ResetFeedback();
        }

        /// <summary>Removes only listeners owned by this scene controller.</summary>
        private void UnbindBoosters()
        {
            for(int i=0;i<4;i++) {
                if(BoosterButtons[i] && boosterActions[i]!=null) BoosterButtons[i].onClick.RemoveListener(boosterActions[i]);
                if(BoosterBadges[i] && badgeActions[i]!=null) BoosterBadges[i].onClick.RemoveListener(badgeActions[i]);
            }
        }
    }
}
