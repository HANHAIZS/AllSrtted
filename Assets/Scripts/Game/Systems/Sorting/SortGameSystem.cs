using QFramework;
using UnityEngine;
namespace AllSorted
{
    public sealed class SortGameSystem : AbstractSystem
    {
        private SortBoardModel model;
        private ISortLevelRepository levels;
        private SortBoosterModel boosters;
        private SortWalletModel walletModel;
        private SortWalletSystem wallet;
        private int submittedLossAttempt;
        private float walletPollRemaining;
        private readonly System.Random random=new System.Random();

        /// <summary>Caches board and session allowance dependencies at architecture initialization.</summary>
        protected override void OnInit()
        {
            levels=this.GetUtility<ISortLevelRepository>();model=this.GetModel<SortBoardModel>();boosters=this.GetModel<SortBoosterModel>();
            walletModel=this.GetModel<SortWalletModel>();wallet=this.GetSystem<SortWalletSystem>();
        }

        /// <summary>Validates content before changing selection; zero restarts the current level.</summary>
        public void StartLevel(int levelNumber=0)
        {
            int selected=levelNumber==0?model.LevelNumber.Value:levelNumber;
            var board=levels.Create(selected);wallet.BeginAttempt();model.IsEntering=false;model.Board=board;model.LevelNumber.Value=selected;submittedLossAttempt=0;walletPollRemaining=0;Publish(true);
        }

        public bool StartEntryLevel(int level,int mask)
        {
            if(level<1 || level>levels.Count || mask<0 || mask>3 || !wallet.Refresh() || !walletModel.CanEnter)return false;
            var board=levels.Create(level);if(!board.TryApplyEntryBoosters(mask,random) || !wallet.SpendEntryCharges(mask))return false;
            wallet.BeginAttempt();model.IsEntering=false;model.Board=board;model.LevelNumber.Value=level;submittedLossAttempt=0;walletPollRemaining=0;Publish(true);return true;
        }

        /// <summary>Advances only a winning board, wrapping after the final explicit start-over action.</summary>
        public bool AdvanceLevel()
        {
            if(model.IsEntering || model.Board.Phase!=SortPhase.Won || !wallet.Refresh() || !walletModel.CanEnter) return false;
            StartLevel(model.LevelNumber.Value<levels.Count?model.LevelNumber.Value+1:1);return true;
        }

        /// <summary>Validates before settlement; one remaining energy may fund the current retry to zero.</summary>
        public bool RestartLevel(int token=0)
        {
            if(model.IsEntering)return false;
            if(token!=0 && token!=walletModel.AttemptId) return false;
            if(!wallet.Refresh() || !walletModel.CanEnter) return false;
            var next=levels.Create(model.LevelNumber.Value);
            if(model.Board.Phase!=SortPhase.Won && !wallet.EndAttempt(walletModel.AttemptId)) return false;
            wallet.BeginAttempt();model.Board=next;submittedLossAttempt=0;walletPollRemaining=0;Publish(true);return true;
        }

        /// <summary>Executes the tested board transaction and publishes only accepted moves.</summary>
        public bool Move(int a,int b,int c,int d)
        {
            if(model.IsEntering)return false;
            bool accepted=model.Board.TryMove(a,b,c,d);
            if(accepted) Publish(true);
            return accepted;
        }

        /// <summary>Uses the cached random source and publishes only accepted booster transactions.</summary>
        public bool UseBooster(SortBooster booster,bool animateCard=false)
        {
            if(model.IsEntering)return false;
            if(boosters.GetCharges(booster)<=0 || !model.Board.CanUseBooster(booster)) return false;
            if(!wallet.TrySpendBooster(booster))return false;
            bool accepted=animateCard && booster==SortBooster.Golden
                ? model.Board.BeginCardTransformation(random) : model.Board.TryUseBooster(booster,random);
            if(accepted) Publish(true);
            return accepted;
        }

        public void CompleteCardTransformation(SortBoardState board)
        {
            if(board!=null && ReferenceEquals(board,model.Board) && board.CompleteCardTransformation()) Publish(true);
        }

        /// <summary>Refills only an empty allowance during active play without touching the board.</summary>
        public bool AddBoosterCharge(SortBooster booster)
        {
            if(model.IsEntering || model.Board.Phase!=SortPhase.Playing || model.Board.RemainingSeconds<=0) return false;
            return wallet.AddBoosterCharge(booster);
        }

        /// <summary>Only a current paused attempt may buy three uses of a depleted booster.</summary>
        public bool BuyBooster(SortBooster booster,int attempt)
        {
            if(model.IsEntering || attempt<=0 || attempt!=walletModel.AttemptId || model.Board.Phase!=SortPhase.Paused ||
                model.Board.RemainingSeconds<=0 || boosters.GetCharges(booster)!=0)return false;
            return wallet.BuyBooster(booster);
        }

        /// <summary>Advances the domain clock through the command boundary.</summary>
        public void Tick(float seconds) {walletPollRemaining-=Mathf.Max(0,seconds);if(walletPollRemaining<=0){walletPollRemaining=1;wallet.Refresh();}if(!model.IsEntering)model.Board.Tick(seconds);Publish(false);}

        /// <summary>Applies pause without allowing terminal states to reopen.</summary>
        public void SetEntryPresentation(bool value){model.IsEntering=value;}
        public void Pause(bool value) { if(model.IsEntering)return;model.Board.SetPaused(value);Publish(false); }

        /// <summary>Copies authoritative state to observable presentation properties.</summary>
        private void Publish(bool boardChanged)
        {
            if(model.Board.Phase==SortPhase.Lost && submittedLossAttempt!=walletModel.AttemptId) {
                // The ledger retains a failed settlement; the one-second refresh retries it before entry.
                submittedLossAttempt=walletModel.AttemptId;wallet.EndAttempt(walletModel.AttemptId);
            }
            model.Seconds.Value=Mathf.CeilToInt(model.Board.RemainingSeconds);
            model.MatchCount.Value=model.Board.Matches;model.Phase.Value=model.Board.Phase;
            if(boardChanged) model.BoardRevision.Value++;
        }
    }
}
