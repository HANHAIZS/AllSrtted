using System;
using System.Linq;
using NUnit.Framework;
namespace AllSorted.Tests
{
 public sealed class SortExpressCampaignTests
 {
  [Test] public void RuntimeCampaignRepositoryExists(){Assert.IsNotNull(typeof(SortBoardState).Assembly.GetType("AllSorted.SortExpressCampaign"));}
  [TestCase(1,3,9)][TestCase(2,9,54)][TestCase(3,10,84)][TestCase(4,9,51)][TestCase(5,13,81)]
  public void AllFiveSourceStagesSolveWithoutBoosters(int number,int shelves,int items)
  {
   var campaign=new SortExpressCampaign();var definition=campaign.GetDefinition(number);var board=campaign.Create(number);
   Assert.AreEqual(5,campaign.Count);Assert.AreEqual(shelves,board.ShelfCount);Assert.AreEqual(items,board.InitialCount);Assert.AreEqual(300,board.RemainingSeconds);Assert.AreEqual(number>=4,definition.compatibleGeneration);
   foreach(var move in definition.solution)Assert.IsTrue(board.TryMove(move.a,move.b,move.c,move.d),number+" witness move");
   Assert.AreEqual(SortPhase.Won,board.Phase);Assert.AreEqual(0,board.RemainingCount);Assert.AreEqual(items/3,board.Matches);
   board.Reset();Assert.AreEqual(items,board.RemainingCount);Assert.AreEqual(0,board.Matches);Assert.AreEqual(300,board.RemainingSeconds);
  }
  [Test] public void FirstLevelPreservesEmptySlotsUntilEntireFrontRowLeaves()
  {
   var board=new SortExpressCampaign().Create(1);var hidden=board.GetBehind(0,0);Assert.IsTrue(board.GetFront(0,1).IsEmpty);
   Assert.IsTrue(board.TryMove(0,0,0,1));Assert.IsTrue(board.GetFront(0,0).IsEmpty);Assert.AreEqual(hidden.InstanceId,board.GetBehind(0,0).InstanceId);
  }
  [Test] public void LockedShelvesRejectPickupAndDropThenUnlockAfterMatches()
  {
   var campaign=new SortExpressCampaign();var board=campaign.Create(4);Assert.AreEqual(3,board.LockRemaining(0));bool rejected=false;
   for(int c=0;c<3;c++)if(!board.GetFront(0,c).IsEmpty){Assert.IsFalse(board.TryMove(0,c,5,0));rejected=true;break;}Assert.IsTrue(rejected);
   foreach(var move in campaign.GetDefinition(4).solution){Assert.IsTrue(board.TryMove(move.a,move.b,move.c,move.d));Assert.AreEqual(Math.Max(0,3-board.Matches),board.LockRemaining(0));if(board.Matches>=3)break;}
   Assert.IsFalse(board.IsShelfLocked(0));
  }
  [Test] public void SourceItemsNeverCollideWithGoldenAndEntryBoostersKeepTheirEffects()
  {
   var campaign=new SortExpressCampaign();var definition=campaign.GetDefinition(3);foreach(var shelf in definition.shelves)foreach(var row in shelf.rows)foreach(int kind in row.cells)if(kind>=0){Assert.GreaterOrEqual(kind,100);Assert.IsNotNull(campaign.GetItem(kind));}
   var board=campaign.Create(2);Assert.IsTrue(board.TryUseBooster(SortBooster.Freeze,new Random(7)));board.Tick(10);Assert.AreEqual(300,board.RemainingSeconds);Assert.AreEqual(5,board.FreezeSeconds);
   board=campaign.Create(2);Assert.IsTrue(board.TryApplyEntryBoosters(3,new Random(7)));Assert.AreEqual(330,board.RemainingSeconds);Assert.LessOrEqual(board.RemainingCount,45);Assert.GreaterOrEqual(board.Matches,3);
  }
  [Test] public void InvalidSourceInputFailsWithoutInventingContent()
  {
   Assert.Throws<ArgumentException>(()=>new SortBoardState(new SortLevelDefinition{duration=300,shelves=new SortShelfDefinition[0]}));
   Assert.Throws<ArgumentOutOfRangeException>(()=>new SortExpressCampaign().Create(6));
  }
  [Serializable] private sealed class CatalogData {public int version,sourceVersion;public SortItemDefinition[] items;public SortLevelDefinition[] levels;}
  [TestCase("missing")][TestCase("family")][TestCase("shape")][TestCase("color")]
  public void MalformedItemCatalogIsRejectedBeforeStagePublication(string failure)
  {
   var data=UnityEngine.JsonUtility.FromJson<CatalogData>(UnityEngine.Resources.Load<UnityEngine.TextAsset>(SortExpressCampaign.ResourceKey).text);
   if(failure=="missing")data.items=data.items.Skip(1).ToArray();
   if(failure=="family")data.items[0].family="";
   if(failure=="shape")data.items[0].shape=9;
   if(failure=="color")data.items[0].colorHex="invalid";
   Assert.Throws<InvalidOperationException>(()=>new SortExpressCampaign(UnityEngine.JsonUtility.ToJson(data)).Create(1));
  }
  [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
  public void EveryStageAcceptsTheSelectedOpeningNineItemBooster(int number)
  {
   var board=new SortExpressCampaign().Create(number);Assert.IsTrue(board.TryApplyEntryBoosters(1,new Random(5)),"Entry booster must clear9 even when some stock starts locked");Assert.LessOrEqual(board.RemainingCount,board.InitialCount-9);
  }
  [Test] public void SourceFourWildcardUsesAllRemainingStockIncludingLockedShelves()
  {
   var board=new SortExpressCampaign().Create(4);var lockedIds=new System.Collections.Generic.HashSet<int>();
   for(int sh=0;sh<board.ShelfCount;sh++)if(board.IsShelfLocked(sh))for(int c=0;c<3;c++){lockedIds.Add(board.GetFront(sh,c).InstanceId);lockedIds.Add(board.GetBehind(sh,c).InstanceId);}
   Assert.IsTrue(board.CanUseBooster(SortBooster.Golden),"17 total triples must permit wildcard regardless of locks");Assert.IsTrue(board.TryUseBooster(SortBooster.Golden,new Random(17)));Assert.AreEqual(9,board.BoosterPieceIds.Count);Assert.IsTrue(board.BoosterPieceIds.Any(id=>lockedIds.Contains(id)));
  }
  [Test] public void SourceFourShuffleIncludesLockedPositions()
  {
   var board=new SortExpressCampaign().Create(4);var before=new System.Collections.Generic.List<int>();
   for(int sh=0;sh<board.ShelfCount;sh++)if(board.IsShelfLocked(sh))for(int c=0;c<3;c++)before.Add(board.GetFront(sh,c).InstanceId);
   Assert.IsTrue(board.TryUseBooster(SortBooster.Shuffle,new Random(17)));var after=new System.Collections.Generic.List<int>();
   for(int sh=0;sh<board.ShelfCount;sh++)if(board.IsShelfLocked(sh))for(int c=0;c<3;c++)after.Add(board.GetFront(sh,c).InstanceId);
   Assert.IsFalse(before.SequenceEqual(after),"Shuffle promises all remaining stock");Assert.AreEqual(51,board.RemainingCount+board.Matches*3);
  }
  [Test] public void LocksRejectOtherwiseValidPickupAndEmptyTargetDrops()
  {
   var level=new SortLevelDefinition{duration=300,shelves=new[]{new SortShelfDefinition{lockMatches=1,rows=new[]{new SortShelfRow{cells=new[]{100,-1,-1}}}},new SortShelfDefinition{rows=new[]{new SortShelfRow{cells=new[]{101,-1,-1}}}}}};
   var board=new SortBoardState(level);Assert.IsFalse(board.TryMove(0,0,1,1));Assert.IsFalse(board.TryMove(1,0,0,1));Assert.AreEqual(2,board.RemainingCount);
  }
 }
}
