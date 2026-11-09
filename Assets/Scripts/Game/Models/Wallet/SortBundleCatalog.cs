namespace AllSorted
{
    /// <summary>Simulated Shop products; one purchase adds all six booster allowances.</summary>
    public static class SortBundleCatalog
    {
        public const int Count=3;
        public static int GetCoins(int index){return index==0?5000:index==1?10000:index==2?25000:0;}
        public static int GetCharges(int index){return index==0?1:index==1?3:index==2?6:0;}
        public static int GetHours(int index){return index==0?1:index==1?3:index==2?12:0;}
        public static string GetName(int index){return index==0?"Small Bundle":index==1?"Medium Bundle":index==2?"Big Bundle":string.Empty;}
        public static string GetPrice(int index){return index==0?"US$9.99":index==1?"US$18.99":index==2?"US$36.99":string.Empty;}
    }
}
