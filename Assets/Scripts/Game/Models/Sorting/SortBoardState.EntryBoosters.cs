using System;
using System.Collections.Generic;
namespace AllSorted
{
    public sealed partial class SortBoardState
    {
        /// <summary>Prepares selected starting effects on an unpublished, fresh level board.</summary>
        public bool TryApplyEntryBoosters(int mask,Random random)
        {
            if(mask<0 || mask>3 || Phase!=SortPhase.Playing || random==null)return false;
            var selected=new List<BoosterPosition>(9);
            if((mask&1)!=0){
                var groups=new Dictionary<int,List<BoosterPosition>>();
                foreach(var pos in CollectBoosterPositions(false,true)){int kind=ReadBoosterPosition(pos).Kind;List<BoosterPosition> group;if(!groups.TryGetValue(kind,out group)){group=new List<BoosterPosition>();groups.Add(kind,group);}group.Add(pos);}
                var eligible=new List<List<BoosterPosition>>();foreach(var group in groups.Values)if(group.Count>=3)eligible.Add(group);
                for(int n=0;n<3;n++){
                    if(eligible.Count==0)return false;int k=random.Next(eligible.Count);var group=eligible[k];
                    for(int i=0;i<3;i++){int pick=random.Next(group.Count);selected.Add(group[pick]);group.RemoveAt(pick);}if(group.Count<3)eligible.RemoveAt(k);
                }
            }
            if((mask&2)!=0)RemainingSeconds+=30;
            if(selected.Count>0){
                selected.Sort((a,b)=>a.Shelf!=b.Shelf?a.Shelf.CompareTo(b.Shelf):a.Slot!=b.Slot?a.Slot.CompareTo(b.Slot):b.Depth.CompareTo(a.Depth));
                boosterPieceIds.Clear();matchedShelves.Clear();foreach(var pos in selected){boosterPieceIds.Add(ReadBoosterPosition(pos).InstanceId);if(Definition==null)slots[pos.Shelf][pos.Slot].RemoveAt(pos.Depth);else slots[pos.Shelf][pos.Slot][pos.Depth]=default(SortPiece);}RemainingCount-=9;Matches+=3;ResolveMatches();if(RemainingCount==0)Phase=SortPhase.Won;
            }
            if(mask!=0)Revision++;return true;
        }
    }
}
