using System;
using System.Collections.Generic;

namespace AllSorted
{
    public enum SortBooster { Vacuum, Shuffle, Golden, Freeze }

    public sealed partial class SortBoardState
    {
        public const int CardKind=4;
        public const int GoldenKind=CardKind; // Existing layouts and callers use the legacy numeric kind.
        public const string CardMatchKey="card";
        public bool CardTransformationPending { get; private set; }
        private readonly List<(int Id,int Shelf,int Slot,int Depth)> cardTargets =
            new List<(int,int,int,int)>(9);
        public IReadOnlyList<(int Id,int Shelf,int Slot,int Depth)> CardTargets => cardTargets;

        public bool BeginCardTransformation(Random random)
        {
            if(!CanUseBooster(SortBooster.Golden)) return false;
            if(random==null) throw new ArgumentNullException(nameof(random));
            boosterPieceIds.Clear();
            cardTargets.Clear();
            matchedShelves.Clear();
            CardTransformationPending=true;
            ApplyGolden(random);
            Revision++;
            return true;
        }

        public bool CompleteCardTransformation()
        {
            if(!CardTransformationPending) return false;
            CardTransformationPending=false;
            cardTargets.Clear();
            ResolveMatches();
            if(RemainingCount==0) Phase=SortPhase.Won;
            Revision++;
            return true;
        }
        private readonly List<int> boosterPieceIds=new List<int>(9);
        public float FreezeSeconds { get; private set; }
        public IReadOnlyList<int> BoosterPieceIds { get { return boosterPieceIds; } }

        /// <summary>Checks availability without consuming randomness or changing board state.</summary>
        public bool CanUseBooster(SortBooster booster)
        {
            if(CardTransformationPending || Phase!=SortPhase.Playing || RemainingSeconds<=0) return false;
            switch(booster) {
                case SortBooster.Vacuum:
                    for(int shelf=0;shelf<slots.Length;shelf++) for(int slot=0;slot<3;slot++) {
                        var piece=GetFront(shelf,slot);if(IsShelfLocked(shelf) || piece.IsEmpty) continue;
                        int count=0;
                        for(int other=0;other<slots.Length;other++) for(int column=0;column<3;column++) {
                            var front=GetFront(other,column);
                            if(!IsShelfLocked(other) && !front.IsEmpty && front.Kind==piece.Kind && ++count>=3) return true;
                        }
                    }
                    return false;
                case SortBooster.Shuffle:
                    bool found=false;int firstKind=0;
                    foreach(var position in CollectBoosterPositions(false,true)) {
                        var piece=ReadBoosterPosition(position);
                        if(found && piece.Kind!=firstKind)return true;firstKind=piece.Kind;found=true;
                    }
                    return false;
                case SortBooster.Golden:
                    var counts=new Dictionary<int,int>();int triples=0;
                    for(int shelf=0;shelf<slots.Length;shelf++) for(int slot=0;slot<3;slot++)
                        foreach(var piece in slots[shelf][slot]) {
                            if(piece.IsEmpty || piece.Kind==GoldenKind) continue;
                            int count;counts.TryGetValue(piece.Kind,out count);counts[piece.Kind]=++count;
                            if(count%3==0 && ++triples>=3) return true;
                        }
                    return false;
                case SortBooster.Freeze: return FreezeSeconds<=0;
                default: return false;
            }
        }

        /// <summary>Commits one booster plus settling as a single revision, leaving rejected actions untouched.</summary>
        public bool TryUseBooster(SortBooster booster,Random random)
        {
            if(!CanUseBooster(booster)) return false;
            if(booster!=SortBooster.Freeze && random==null) throw new ArgumentNullException(nameof(random));
            boosterPieceIds.Clear();matchedShelves.Clear();
            switch(booster) {
                case SortBooster.Vacuum: ApplyVacuum(random);break;
                case SortBooster.Shuffle: ApplyShuffle(random);break;
                case SortBooster.Golden: ApplyGolden(random);break;
                case SortBooster.Freeze: FreezeSeconds=15f;break;
            }
            if(booster!=SortBooster.Freeze) ResolveMatches();
            if(RemainingCount==0) Phase=SortPhase.Won;
            Revision++;return true;
        }

        /// <summary>Draws one eligible kind uniformly, then three distinct front pieces of that kind.</summary>
        private void ApplyVacuum(Random random)
        {
            var groups=new Dictionary<int,List<BoosterPosition>>();var eligibleKinds=new List<int>();
            for(int shelf=0;shelf<slots.Length;shelf++) for(int slot=0;slot<3;slot++) {
                var piece=GetFront(shelf,slot);if(IsShelfLocked(shelf) || piece.IsEmpty) continue;
                List<BoosterPosition> group;
                if(!groups.TryGetValue(piece.Kind,out group)) {group=new List<BoosterPosition>();groups.Add(piece.Kind,group);}
                group.Add(new BoosterPosition(shelf,slot,0));
                if(group.Count==3) eligibleKinds.Add(piece.Kind);
            }
            var candidates=groups[eligibleKinds[random.Next(eligibleKinds.Count)]];
            for(int index=0;index<3;index++) {
                int selected=index+random.Next(candidates.Count-index);
                var position=candidates[selected];candidates[selected]=candidates[index];candidates[index]=position;
                boosterPieceIds.Add(slots[position.Shelf][position.Slot][0].InstanceId);
                slots[position.Shelf][position.Slot][0]=default(SortPiece);
            }
            RemainingCount-=3;Matches++;
        }

        /// <summary>Permutes occupied positions and guarantees a changed front kind before normal settling.</summary>
        private void ApplyShuffle(Random random)
        {
            var positions=CollectBoosterPositions(false,true);var pieces=new SortPiece[positions.Count];
            for(int index=0;index<positions.Count;index++) pieces[index]=ReadBoosterPosition(positions[index]);
            for(int index=pieces.Length-1;index>0;index--) {
                int selected=random.Next(index+1);var piece=pieces[index];pieces[index]=pieces[selected];pieces[selected]=piece;
            }
            bool changed=false;
            for(int index=0;index<positions.Count;index++)
                if(positions[index].Depth==0 && pieces[index].Kind!=ReadBoosterPosition(positions[index]).Kind) {changed=true;break;}
            if(!changed) {
                int front=0;while(positions[front].Depth!=0) front++;
                int other=0;while(pieces[other].Kind==pieces[front].Kind) other++;
                var piece=pieces[front];pieces[front]=pieces[other];pieces[other]=piece;
            }
            for(int index=0;index<positions.Count;index++) WriteBoosterPosition(positions[index],pieces[index]);
        }

        /// <summary>Draws three matching triples across all layers, keeping each original kind's remainder.</summary>
        private void ApplyGolden(Random random)
        {
            var groups=new Dictionary<int,List<BoosterPosition>>();var eligibleKinds=new List<int>();
            foreach(var position in CollectBoosterPositions(true,true)) {
                int kind=ReadBoosterPosition(position).Kind;List<BoosterPosition> group;
                if(!groups.TryGetValue(kind,out group)) {group=new List<BoosterPosition>();groups.Add(kind,group);}
                group.Add(position);if(group.Count==3) eligibleKinds.Add(kind);
            }
            for(int triple=0;triple<3;triple++) {
                int kindIndex=random.Next(eligibleKinds.Count);var candidates=groups[eligibleKinds[kindIndex]];
                for(int index=0;index<3;index++) {
                    int selected=random.Next(candidates.Count);var position=candidates[selected];
                    candidates[selected]=candidates[candidates.Count-1];candidates.RemoveAt(candidates.Count-1);
                    var piece=ReadBoosterPosition(position);boosterPieceIds.Add(piece.InstanceId);
                    if(CardTransformationPending) cardTargets.Add((piece.InstanceId,position.Shelf,position.Slot,position.Depth));
                    WriteBoosterPosition(position,new SortPiece(piece.InstanceId,CardKind));
                }
                if(candidates.Count<3) eligibleKinds.RemoveAt(kindIndex);
            }
        }

        /// <summary>Records real front and hidden positions while preserving all existing vacancy entries.</summary>
        private List<BoosterPosition> CollectBoosterPositions(bool nonGoldOnly,bool includeLocked=false)
        {
            var positions=new List<BoosterPosition>(RemainingCount);
            for(int shelf=0;shelf<slots.Length;shelf++) for(int slot=0;slot<3;slot++) {
                var stack=slots[shelf][slot];
                for(int depth=0;depth<stack.Count;depth++)
                    if((includeLocked || !IsShelfLocked(shelf)) && !stack[depth].IsEmpty && (!nonGoldOnly || stack[depth].Kind!=GoldenKind)) positions.Add(new BoosterPosition(shelf,slot,depth));
            }
            return positions;
        }

        private SortPiece ReadBoosterPosition(BoosterPosition position) {return slots[position.Shelf][position.Slot][position.Depth];}
        private void WriteBoosterPosition(BoosterPosition position,SortPiece piece) {slots[position.Shelf][position.Slot][position.Depth]=piece;}

        private readonly struct BoosterPosition
        {
            public readonly int Shelf,Slot,Depth;
            public BoosterPosition(int shelf,int slot,int depth) {Shelf=shelf;Slot=slot;Depth=depth;}
        }
    }
}
