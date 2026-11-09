using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
namespace AllSorted
{
 public interface ISortLevelRepository : IUtility
 {
  int Count {get;}
  SortBoardState Create(int number);
 }
 /// <summary>Loads only the five compiled, witnessed source stages; no raw archive or remote lookup at runtime.</summary>
 public sealed class SortExpressCampaign : ISortLevelRepository
 {
  public const string ResourceKey="Configs/Sorting/SortExpressFirstFive";
  [Serializable] private sealed class Data {public int version,sourceVersion;public SortItemDefinition[] items;public SortLevelDefinition[] levels;}
  private readonly string compiledJson;
  public SortExpressCampaign(){}
  public SortExpressCampaign(string compiledJson){if(compiledJson==null)throw new ArgumentNullException(nameof(compiledJson));this.compiledJson=compiledJson;}
  private Data data;private readonly Dictionary<int,SortItemDefinition> items=new Dictionary<int,SortItemDefinition>();
  public int Count {get{return 5;}}
  private void EnsureLoaded()
  {
   if(data!=null)return;
   string raw=compiledJson;
   if(raw==null){var text=Resources.Load<TextAsset>(ResourceKey);if(!text)throw new InvalidOperationException("Missing runtime stage data: "+ResourceKey);raw=text.text;}
   var parsed=JsonUtility.FromJson<Data>(raw);
   if(parsed==null || parsed.version!=1 || parsed.sourceVersion!=2165 || parsed.levels==null || parsed.levels.Length!=Count || parsed.items==null)throw new InvalidOperationException("Invalid source campaign schema.");
   var known=new HashSet<int>();var ids=new HashSet<string>();
   foreach(var item in parsed.items){Color color;
    if(item==null || item.kind<100 || !known.Add(item.kind) || string.IsNullOrWhiteSpace(item.iid) || !ids.Add(item.iid) || string.IsNullOrWhiteSpace(item.family) || item.shape<0 || item.shape>3 || item.colorHex==null || item.colorHex.Length!=6 || !ColorUtility.TryParseHtmlString("#"+item.colorHex,out color))throw new InvalidOperationException("Invalid source item catalog.");
   }
   for(int i=0;i<Count;i++){
    var level=parsed.levels[i];if(level==null || level.number!=i+1 || level.solution==null)throw new InvalidOperationException("Missing stage/witness "+(i+1));
    var board=new SortBoardState(level);
    foreach(var shelf in level.shelves)foreach(var row in shelf.rows)foreach(int kind in row.cells)if(kind>=0 && !known.Contains(kind))throw new InvalidOperationException("Stage references missing catalog kind: "+kind);
    foreach(var move in level.solution)if(!board.TryMove(move.a,move.b,move.c,move.d))throw new InvalidOperationException("Invalid stage witness "+level.number);
    if(board.Phase!=SortPhase.Won)throw new InvalidOperationException("Unsolved stage "+level.number);
   }
   foreach(var item in parsed.items)items.Add(item.kind,item);data=parsed;
  }
  public SortBoardState Create(int number)
  {
   if(number<1 || number>Count)throw new ArgumentOutOfRangeException(nameof(number));EnsureLoaded();return new SortBoardState(data.levels[number-1]);
  }
  public SortLevelDefinition GetDefinition(int number){if(number<1 || number>Count)throw new ArgumentOutOfRangeException(nameof(number));EnsureLoaded();return data.levels[number-1];}
  public SortItemDefinition GetItem(int kind){EnsureLoaded();SortItemDefinition item;return items.TryGetValue(kind,out item)?item:null;}
 }
}
