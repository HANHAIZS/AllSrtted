using System;
namespace AllSorted
{
 [Serializable] public sealed class SortLevelDefinition
 {
  public int number,seed;public float duration;public bool compatibleGeneration;public string sourceSha256;
  public SortShelfDefinition[] shelves;public SortSolutionMove[] solution;
 }
 [Serializable] public sealed class SortShelfDefinition
 {
  public string id;public float x,y;public int lockMatches;public SortShelfRow[] rows;
 }
 [Serializable] public sealed class SortShelfRow {public int[] cells;}
 [Serializable] public sealed class SortSolutionMove {public int a,b,c,d;}
 [Serializable] public sealed class SortItemDefinition {public int kind,shape,variant;public string iid,family,colorHex;}
}
