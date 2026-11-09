using System;
namespace AllSorted
{
    public static class PrototypeLevel
    {
        public const int Count=3;

        /// <summary>Keeps the original entry point starting at the unchanged first level.</summary>
        public static SortBoardState Create() {return Create(1);}

        /// <summary>Creates one independently authored fixed layout, never loading reference-game data.</summary>
        public static SortBoardState Create(int levelNumber)
        {
            switch(levelNumber) {
                case 1: return new SortBoardState(new[] {
                    new[] {new[]{0,3},new[]{0,2},new int[0]},
                    new[] {new[]{1,0},new[]{1,3},new int[0]},
                    new[] {new[]{2,1},new[]{2,0},new int[0]},
                    new[] {new[]{3,2},new[]{3,1},new int[0]},
                    new[] {new[]{0,1},new[]{1,3},new int[0]},
                    new[] {new[]{2,0},new[]{3,2},new int[0]},
                },180f);
                case 2: return new SortBoardState(new[] {
                    new[] {new[]{1,1},new[]{2,0},new int[0]},
                    new[] {new[]{2,0},new[]{1,3},new int[0]},
                    new[] {new[]{2,0},new[]{1,3},new int[0]},
                    new[] {new[]{3,3},new[]{2,0},new int[0]},
                    new[] {new[]{0,1},new[]{1,2},new int[0]},
                    new[] {new[]{2,0},new[]{3,3},new int[0]},
                },180f);
                case 3: return new SortBoardState(new[] {
                    new[] {new[]{0,0},new[]{3,3},new[]{0,2}},
                    new[] {new[]{3,1,0},new[]{0,0},new int[0]},
                    new[] {new[]{3,1},new[]{2,1},new int[0]},
                    new[] {new[]{0,1},new[]{2,3},new[]{2,2}},
                    new[] {new[]{2,1},new[]{0,1,1},new int[0]},
                    new[] {new[]{3,0},new[]{1,1},new int[0]},
                },180f);
                default: throw new ArgumentOutOfRangeException(nameof(levelNumber),"Only levels 1 through 3 are available.");
            }
        }
    }
}
