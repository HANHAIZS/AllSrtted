using System;
using System.Collections.Generic;

namespace AllSorted
{
    public enum SortPhase { Playing, Paused, Won, Lost }

    public readonly struct SortPiece
    {
        public readonly int InstanceId;
        public readonly int Kind;
        public bool IsEmpty { get { return InstanceId==0; } }

        /// <summary>Identifies one physical piece independently of its matching kind.</summary>
        public SortPiece(int instanceId,int kind) { InstanceId=instanceId;Kind=kind; }
    }

    /// <summary>Model-owned shelf layers; an empty front is retained until its whole row clears.</summary>
    public sealed partial class SortBoardState
    {
        public SortLevelDefinition Definition {get;private set;}
        private int[] lockThresholds;
        public int LockRemaining(int shelf){return shelf>=0 && shelf<ShelfCount && lockThresholds!=null?Math.Max(0,lockThresholds[shelf]-Matches):0;}
        public bool IsShelfLocked(int shelf){return LockRemaining(shelf)>0;}
        public SortBoardState(SortLevelDefinition level):this(ToStacks(level),level.duration){Definition=level;lockThresholds=new int[ShelfCount];for(int s=0;s<ShelfCount;s++)lockThresholds[s]=level.shelves[s].lockMatches;}
        private static int[][][] ToStacks(SortLevelDefinition level)
        {
            if(level==null || level.shelves==null || level.shelves.Length==0)throw new ArgumentException("Source stage needs shelves.");
            var stacks=new int[level.shelves.Length][][];
            for(int s=0;s<stacks.Length;s++){
                var shelf=level.shelves[s];if(shelf==null || shelf.rows==null || shelf.rows.Length==0 || shelf.lockMatches<0)throw new ArgumentException("Invalid source shelf.");
                stacks[s]=new int[3][];for(int c=0;c<3;c++)stacks[s][c]=new int[shelf.rows.Length];
                for(int d=0;d<shelf.rows.Length;d++){var row=shelf.rows[d];if(row==null || row.cells==null || row.cells.Length!=3)throw new ArgumentException("Source shelf width must be three.");for(int c=0;c<3;c++)stacks[s][c][d]=row.cells[c];}
            }return stacks;
        }
        private readonly int[][][] initial;
        private readonly float duration;
        private readonly List<SortPiece>[][] slots;
        private readonly List<int> matchedShelves=new List<int>(8);
        public int ShelfCount { get { return slots.Length; } }
        public int RemainingCount { get; private set; }
        public int InitialCount { get; private set; }
        public int Matches { get; private set; }
        public int Revision { get; private set; }
        public float RemainingSeconds { get; private set; }
        public SortPhase Phase { get; private set; }
        public IReadOnlyList<int> MatchedShelves { get { return matchedShelves; } }

        /// <summary>Copies project-authored content so reset cannot inherit caller mutations.</summary>
        public SortBoardState(int[][][] stacks,float seconds)
        {
            if(stacks==null || stacks.Length==0) throw new ArgumentException("A level needs shelves.");
            if(seconds<=0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            duration=seconds;initial=new int[stacks.Length][][];slots=new List<SortPiece>[stacks.Length][];
            for(int s=0;s<stacks.Length;s++) {
                if(stacks[s]==null || stacks[s].Length!=3) throw new ArgumentException("Three slots per shelf are required.");
                initial[s]=new int[3][];slots[s]=new List<SortPiece>[3];
                for(int c=0;c<3;c++) {
                    if(stacks[s][c]==null) throw new ArgumentException("A slot stack cannot be null.");
                    initial[s][c]=(int[])stacks[s][c].Clone();slots[s][c]=new List<SortPiece>(stacks[s][c].Length+1);
                    foreach(int kind in initial[s][c]) if(kind< -1) throw new ArgumentException("Kinds must be nonnegative.");
                }
            }
            Reset();
        }

        /// <summary>Returns the front item, treating invalid presentation queries as empty.</summary>
        public SortPiece GetFront(int shelf,int slot) { return GetAtDepth(shelf,slot,0); }

        /// <summary>Returns the first hidden item for a non-interactive depth preview.</summary>
        public SortPiece GetBehind(int shelf,int slot) { return GetAtDepth(shelf,slot,1); }

        /// <summary>Reads a stack position without exposing the mutable collection.</summary>
        private SortPiece GetAtDepth(int shelf,int slot,int depth)
        {
            return ValidSlot(shelf,slot) && slots[shelf][slot].Count>depth ? slots[shelf][slot][depth] : default(SortPiece);
        }

        /// <summary>Commits a legal move and all matches as one deterministic state transition.</summary>
        public bool TryMove(int fromShelf,int fromSlot,int toShelf,int toSlot)
        {
            if(CardTransformationPending || Phase!=SortPhase.Playing || RemainingSeconds<=0 || !ValidSlot(fromShelf,fromSlot) ||
                !ValidSlot(toShelf,toSlot) || IsShelfLocked(fromShelf) || IsShelfLocked(toShelf) || (fromShelf==toShelf && fromSlot==toSlot)) return false;
            var source=slots[fromShelf][fromSlot];var target=slots[toShelf][toSlot];
            if(GetFront(fromShelf,fromSlot).IsEmpty || !GetFront(toShelf,toSlot).IsEmpty) return false;
            var piece=source[0];source[0]=default(SortPiece);
            if(target.Count==0) target.Add(piece);else target[0]=piece;
            matchedShelves.Clear();
            ResolveMatches();if(RemainingCount==0) Phase=SortPhase.Won;
            Revision++;return true;
        }

        /// <summary>Refills empty shelf rows together, then removes matching triples until stable.</summary>
        private void ResolveMatches()
        {
            bool changed;
            do {
                changed=false;
                for(int s=0;s<slots.Length;s++) {
                    if(IsShelfLocked(s))continue;
                    if(RefillEmptyShelf(s)) changed=true;
                    var a=GetFront(s,0);var b=GetFront(s,1);var c=GetFront(s,2);
                    if(a.IsEmpty || b.IsEmpty || c.IsEmpty || a.Kind!=b.Kind || a.Kind!=c.Kind) continue;
                    for(int i=0;i<3;i++) slots[s][i][0]=default(SortPiece);
                    RemainingCount-=3;Matches++;matchedShelves.Add(s);changed=true;
                }
            } while(changed);
        }

        /// <summary>Advances exactly one shared layer only when all three fronts are vacant.</summary>
        private bool RefillEmptyShelf(int shelf)
        {
            for(int c=0;c<3;c++) if(!GetFront(shelf,c).IsEmpty) return false;
            bool revealed=false;
            for(int c=0;c<3;c++) {
                var stack=slots[shelf][c];
                if(stack.Count>0) stack.RemoveAt(0);
                if(stack.Count>0) revealed=true;
            }
            return revealed;
        }

        /// <summary>Advances only active gameplay time and keeps terminal outcomes final.</summary>
        public void Tick(float seconds)
        {
            if(seconds<0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            if(CardTransformationPending || Phase!=SortPhase.Playing) return;
            float frozen=Math.Min(FreezeSeconds,seconds);
            FreezeSeconds-=frozen;seconds-=frozen;
            RemainingSeconds=Math.Max(0,RemainingSeconds-seconds);
            if(RemainingSeconds<=0) Phase=SortPhase.Lost;
        }

        /// <summary>Changes pause only for an ongoing game, without reopening terminal states.</summary>
        public void SetPaused(bool paused)
        {
            if(CardTransformationPending || Phase==SortPhase.Won || Phase==SortPhase.Lost) return;
            Phase=paused?SortPhase.Paused:SortPhase.Playing;
        }

        /// <summary>Recreates deterministic stacks and identities from the independent layout.</summary>
        public void Reset()
        {
            int id=1;RemainingCount=0;Matches=0;RemainingSeconds=duration;Phase=SortPhase.Playing;matchedShelves.Clear();
            FreezeSeconds=0;boosterPieceIds.Clear();CardTransformationPending=false;cardTargets.Clear();
            for(int s=0;s<slots.Length;s++) for(int c=0;c<3;c++) {
                slots[s][c].Clear();
                foreach(int kind in initial[s][c]) {if(kind<0)slots[s][c].Add(default(SortPiece));else{slots[s][c].Add(new SortPiece(id++,kind));RemainingCount++;}}
            }
            InitialCount=RemainingCount;Revision++;
        }

        /// <summary>Checks external move coordinates before indexing the model.</summary>
        private bool ValidSlot(int shelf,int slot) { return shelf>=0 && shelf<slots.Length && slot>=0 && slot<3; }
    }
}
